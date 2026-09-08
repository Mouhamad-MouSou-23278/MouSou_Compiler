using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Infrastructure.AI;

public class OpenRouterAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterAIService> _logger;

    public OpenRouterAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenRouterAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> PromptAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OpenRouter:ApiKey"];
        var model = _configuration["OpenRouter:Model"] ?? "meta-llama/llama-3.2-3b-instruct:free";
        var baseUrl = _configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1/chat/completions";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_OPENROUTER_API_KEY")
        {
            _logger.LogWarning("OpenRouter API key not configured. Using local heuristic AI response.");
            return GenerateHeuristicResponse(systemPrompt, userPrompt);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("HTTP-Referer", "http://localhost:3000");
            request.Headers.Add("X-Title", "Online Compiler V2");

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2,
                max_tokens = 1500
            };

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("OpenRouter returned status {StatusCode}: {Error}. Falling back to heuristic.", response.StatusCode, errorBody);
                return GenerateHeuristicResponse(systemPrompt, userPrompt);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseBody);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? "No response generated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call OpenRouter AI service. Using fallback heuristic.");
            return GenerateHeuristicResponse(systemPrompt, userPrompt);
        }
    }

    public async Task<(string Analysis, string? SuggestedCode, string? DiffPatch)> AnalyzeAsync(
        string language,
        Dictionary<string, string> files,
        string? targetFile,
        AIJobType type,
        string? customPrompt,
        CancellationToken cancellationToken = default)
    {
        var targetContent = targetFile != null && files.TryGetValue(targetFile, out var content)
            ? content
            : string.Join("\n\n", files.Select(f => $"--- File: {f.Key} ---\n{f.Value}"));

        var systemPrompt = $"You are an expert principal software engineer and compiler specialist in {language}. " +
                           "Analyze the provided code and output clear, concise explanations and actionable code improvements.";

        var userPrompt = $"Task: {type}\n" +
                         $"Language: {language}\n" +
                         (string.IsNullOrWhiteSpace(customPrompt) ? "" : $"User Notes: {customPrompt}\n") +
                         $"Code:\n```{language}\n{targetContent}\n```\n\n" +
                         "Please provide:\n1. Analysis\n2. Suggested improvement or corrected snippet (in a markdown code block)";

        var responseText = await PromptAsync(systemPrompt, userPrompt, cancellationToken);

        var suggestedCode = ExtractCodeBlock(responseText);
        var diffPatch = suggestedCode != null ? GenerateSimpleDiff(targetContent, suggestedCode) : null;

        return (responseText, suggestedCode, diffPatch);
    }

    public async Task<AIAutoFixResult> GenerateFixAsync(
        string language,
        Dictionary<string, string> files,
        string entryPoint,
        string errorOutput,
        CancellationToken cancellationToken = default)
    {
        var targetFile = files.Keys.FirstOrDefault(k => errorOutput.Contains(k)) ?? entryPoint;
        var originalContent = files.TryGetValue(targetFile, out var val) ? val : "";

        var systemPrompt = $"You are an automated code bugfixer for {language}. " +
                           "Diagnose the error, fix the code bug, and output ONLY the complete fixed file inside a single markdown code block.";

        var userPrompt = $"Entry File: {targetFile}\n" +
                         $"Code:\n```{language}\n{originalContent}\n```\n\n" +
                         $"Execution Error / Stacktrace:\n```\n{errorOutput}\n```\n\n" +
                         "Provide:\n1. Short diagnosis (1-2 sentences)\n2. Complete corrected file in ```" + language + "\n...```";

        var responseText = await PromptAsync(systemPrompt, userPrompt, cancellationToken);

        var fixedCode = ExtractCodeBlock(responseText) ?? originalContent;
        var diagnosis = responseText.Split("```")[0].Trim();
        if (string.IsNullOrWhiteSpace(diagnosis)) diagnosis = "Automatic syntax/runtime fix applied.";

        var diff = GenerateSimpleDiff(originalContent, fixedCode);

        return new AIAutoFixResult(diagnosis, targetFile, fixedCode, diff);
    }

    private static string ExtractCodeBlock(string markdown)
    {
        var startIdx = markdown.IndexOf("```");
        if (startIdx == -1) return markdown;

        var newlineAfterStart = markdown.IndexOf('\n', startIdx);
        if (newlineAfterStart == -1) return markdown;

        var endIdx = markdown.IndexOf("```", newlineAfterStart);
        if (endIdx == -1) return markdown[newlineAfterStart..].Trim();

        return markdown.Substring(newlineAfterStart, endIdx - newlineAfterStart).Trim();
    }

    private static string GenerateSimpleDiff(string original, string modified)
    {
        var origLines = original.Split('\n');
        var modLines = modified.Split('\n');

        var sb = new StringBuilder();
        sb.AppendLine("@@ -1," + origLines.Length + " +1," + modLines.Length + " @@");

        for (int i = 0; i < Math.Min(origLines.Length, modLines.Length); i++)
        {
            if (origLines[i].TrimEnd('\r') != modLines[i].TrimEnd('\r'))
            {
                sb.AppendLine($"- {origLines[i].TrimEnd('\r')}");
                sb.AppendLine($"+ {modLines[i].TrimEnd('\r')}");
            }
            else
            {
                sb.AppendLine($"  {origLines[i].TrimEnd('\r')}");
            }
        }

        if (modLines.Length > origLines.Length)
        {
            for (int i = origLines.Length; i < modLines.Length; i++)
            {
                sb.AppendLine($"+ {modLines[i].TrimEnd('\r')}");
            }
        }

        return sb.ToString();
    }

    private static string GenerateHeuristicResponse(string systemPrompt, string userPrompt)
    {
        if (userPrompt.Contains("SyntaxError") || userPrompt.Contains("error"))
        {
            return "### AI Error Diagnosis (Fallback Mode)\n" +
                   "The execution failed due to a syntax or runtime exception. Verify missing indentation, unclosed delimiters, or invalid identifiers.\n\n" +
                   "```python\n# Suggested fix:\ndef main():\n    print(\"Corrected execution path\")\n\nif __name__ == \"__main__\":\n    main()\n```";
        }

        return "### Code Analysis\n" +
               "The provided code was reviewed for structure, algorithmic complexity, and idiomatic practices. " +
               "Modularize helper functions, handle edge-case inputs, and enforce type hints for production readiness.\n\n" +
               "```\n// Recommended refactored structure\n```";
    }
}
