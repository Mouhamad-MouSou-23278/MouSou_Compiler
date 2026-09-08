using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Application.DTOs.Executions;

namespace OnlineCompiler.Infrastructure.Docker;

public class DockerSandboxService : IDockerSandboxService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DockerSandboxService> _logger;
    private readonly DockerClient _dockerClient;
    private readonly bool _dockerAvailable;

    public DockerSandboxService(IConfiguration configuration, ILogger<DockerSandboxService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var dockerUri = GetDockerUri();
        _dockerClient = new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();

        try
        {
            _dockerClient.System.PingAsync().GetAwaiter().GetResult();
            _dockerAvailable = true;
            _logger.LogInformation("Docker daemon connected successfully at {Uri}", dockerUri);
        }
        catch (Exception ex)
        {
            _dockerAvailable = false;
            _logger.LogWarning("Docker daemon not reachable ({Message}). Running in local process fallback mode.", ex.Message);
        }
    }

    private static string GetDockerUri()
    {
        if (OperatingSystem.IsWindows())
        {
            return "npipe://./pipe/docker_engine";
        }
        return "unix:///var/run/docker.sock";
    }

    public async Task<SandboxResult> ExecuteAsync(
        ExecutionJobPayload job,
        Func<string, string, Task> onLogReceived,
        CancellationToken cancellationToken = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "compiler_sandbox", job.ExecutionId.ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // 1. Write all source files into the sandbox temp directory
            foreach (var (relPath, content) in job.Files)
            {
                var fullFilePath = Path.Combine(tempDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                var parent = Path.GetDirectoryName(fullFilePath);
                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                await File.WriteAllTextAsync(fullFilePath, content, Encoding.UTF8, cancellationToken);
            }

            var timeoutSeconds = int.TryParse(_configuration["Execution:TimeoutSeconds"], out var ts) ? ts : 15;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var stopwatch = Stopwatch.StartNew();

            if (_dockerAvailable)
            {
                return await ExecuteInDockerAsync(job, tempDir, onLogReceived, stopwatch, cts.Token);
            }
            else
            {
                return await ExecuteInLocalProcessAsync(job, tempDir, onLogReceived, stopwatch, cts.Token);
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not clean up temp directory {TempDir}: {Message}", tempDir, ex.Message);
            }
        }
    }

    private async Task<SandboxResult> ExecuteInDockerAsync(
        ExecutionJobPayload job,
        string hostWorkspaceDir,
        Func<string, string, Task> onLogReceived,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var (imageName, command) = GetLanguageImageAndCommand(job.Language, job.EntryPoint);
        await EnsureImagePulledAsync(imageName, cancellationToken);

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var containerParams = new CreateContainerParameters
        {
            Image = imageName,
            Cmd = command,
            WorkingDir = "/workspace",
            NetworkDisabled = true,
            HostConfig = new HostConfig
            {
                NetworkMode = "none",
                Memory = 256 * 1024 * 1024, // 256 MB
                MemorySwap = 256 * 1024 * 1024, // Disallow extra swap
                NanoCPUs = 1_000_000_000, // 1 CPU
                PidsLimit = 64, // Limit fork bombs
                Binds = new List<string>
                {
                    $"{hostWorkspaceDir}:/workspace:rw"
                },
                Tmpfs = new Dictionary<string, string>
                {
                    { "/tmp", "rw,noexec,nosuid,size=64m" }
                }
            }
        };

        CreateContainerResponse container;
        try
        {
            container = await _dockerClient.Containers.CreateContainerAsync(containerParams, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Docker sandbox container");
            return new SandboxResult(1, "", $"Container creation failed: {ex.Message}", stopwatch.Elapsed, false, false);
        }

        try
        {
            await _dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), cancellationToken);
            await onLogReceived("system", $"Container started [ID: {container.ID[..12]} - Image: {imageName}]\n");

            var attachParams = new ContainerAttachParameters
            {
                Stream = true,
                Stdout = true,
                Stderr = true,
                Stdin = !string.IsNullOrEmpty(job.StdinInput)
            };

            using var stream = await _dockerClient.Containers.AttachContainerAsync(container.ID, false, attachParams, cancellationToken);

            // Stream logs
            var logTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (result.EOF) break;

                    var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (result.Target == MultiplexedStream.TargetStream.StandardError)
                    {
                        errorBuilder.Append(text);
                        await onLogReceived("stderr", text);
                    }
                    else
                    {
                        outputBuilder.Append(text);
                        await onLogReceived("stdout", text);
                    }
                }
            }, cancellationToken);

            // Wait for exit
            var waitTask = _dockerClient.Containers.WaitContainerAsync(container.ID, cancellationToken);
            var completedTask = await Task.WhenAny(waitTask, Task.Delay(Timeout.Infinite, cancellationToken));

            stopwatch.Stop();

            if (completedTask == waitTask)
            {
                var waitResponse = await waitTask;
                await logTask;
                return new SandboxResult(
                    (int)waitResponse.StatusCode,
                    outputBuilder.ToString(),
                    errorBuilder.ToString(),
                    stopwatch.Elapsed,
                    false,
                    waitResponse.StatusCode == 137 // OOM killed
                );
            }

            return new SandboxResult(124, outputBuilder.ToString(), "Execution timed out.", stopwatch.Elapsed, true, false);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            await onLogReceived("system", "\n[Execution timeout exceeded - Sandbox terminated]\n");
            return new SandboxResult(124, outputBuilder.ToString(), "Execution timed out.", stopwatch.Elapsed, true, false);
        }
        finally
        {
            try
            {
                await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters { WaitBeforeKillSeconds = 1 }, CancellationToken.None);
                await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to remove container {Id}: {Message}", container.ID, ex.Message);
            }
        }
    }

    private async Task<SandboxResult> ExecuteInLocalProcessAsync(
        ExecutionJobPayload job,
        string hostWorkspaceDir,
        Func<string, string, Task> onLogReceived,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var (fileName, args) = GetProcessExecutableAndArgs(job.Language, job.EntryPoint);

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = hostWorkspaceDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        process.OutputDataReceived += async (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                await onLogReceived("stdout", e.Data + "\n");
            }
        };

        process.ErrorDataReceived += async (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                await onLogReceived("stderr", e.Data + "\n");
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!string.IsNullOrEmpty(job.StdinInput))
            {
                await process.StandardInput.WriteLineAsync(job.StdinInput);
                process.StandardInput.Close();
            }

            await process.WaitForExitAsync(cancellationToken);
            stopwatch.Stop();

            return new SandboxResult(
                process.ExitCode,
                outputBuilder.ToString(),
                errorBuilder.ToString(),
                stopwatch.Elapsed,
                false,
                false
            );
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); } catch { /* ignore */ }
            stopwatch.Stop();
            await onLogReceived("system", "\n[Process timed out and was killed]\n");
            return new SandboxResult(124, outputBuilder.ToString(), "Process execution timed out.", stopwatch.Elapsed, true, false);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new SandboxResult(1, "", $"Process execution failed: {ex.Message}", stopwatch.Elapsed, false, false);
        }
    }

    private async Task EnsureImagePulledAsync(string image, CancellationToken cancellationToken)
    {
        try
        {
            var images = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), cancellationToken);
            foreach (var img in images)
            {
                if (img.RepoTags != null && img.RepoTags.Contains(image))
                    return;
            }

            _logger.LogInformation("Pulling Docker image {Image}...", image);
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = image },
                null,
                new Progress<JSONMessage>(m =>
                {
                    if (!string.IsNullOrEmpty(m.Status))
                        _logger.LogDebug("Pulling {Image}: {Status}", image, m.Status);
                }),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to pull image {Image}: {Message}", image, ex.Message);
        }
    }

    private static (string Image, IList<string> Command) GetLanguageImageAndCommand(string language, string entryPoint) => language.ToLowerInvariant() switch
    {
        "javascript" or "node" => ("node:20-alpine", new List<string> { "node", entryPoint }),
        "csharp" or "c#" => ("mcr.microsoft.com/dotnet/sdk:8.0-alpine", new List<string> { "sh", "-c", $"dotnet run --project ." }),
        "go" or "golang" => ("golang:1.22-alpine", new List<string> { "go", "run", entryPoint }),
        "cpp" or "c++" => ("gcc:alpine", new List<string> { "sh", "-c", $"g++ -O2 -o main {entryPoint} && ./main" }),
        "rust" => ("rust:alpine", new List<string> { "sh", "-c", $"rustc -O -o main {entryPoint} && ./main" }),
        _ => ("python:3.11-alpine", new List<string> { "python3", "-u", entryPoint })
    };

    private static (string Executable, string Arguments) GetProcessExecutableAndArgs(string language, string entryPoint) => language.ToLowerInvariant() switch
    {
        "javascript" or "node" => ("node", entryPoint),
        "csharp" or "c#" => ("dotnet", "run"),
        "go" or "golang" => ("go", $"run {entryPoint}"),
        _ => ("python", entryPoint)
    };
}
