using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Application.DTOs.Executions;
using StackExchange.Redis;

namespace OnlineCompiler.Infrastructure.Queue;

public class RedisExecutionQueue : IExecutionQueue
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RedisExecutionQueue> _logger;
    private IConnectionMultiplexer? _redis;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly Channel<ExecutionJobPayload> _fallbackChannel;
    private readonly ConcurrentDictionary<Guid, Channel<string>> _stdinChannels = new();
    private const string QueueKey = "online_compiler:executions_queue";

    public RedisExecutionQueue(IConfiguration configuration, ILogger<RedisExecutionQueue> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _fallbackChannel = Channel.CreateUnbounded<ExecutionJobPayload>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    private async Task<IDatabase?> GetDatabaseAsync()
    {
        if (_redis != null && _redis.IsConnected)
        {
            return _redis.GetDatabase();
        }

        await _connectionLock.WaitAsync();
        try
        {
            if (_redis != null && _redis.IsConnected)
            {
                return _redis.GetDatabase();
            }

            var connectionString = _configuration.GetConnectionString("Redis") ?? "localhost:6379";
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 3000;
            options.AsyncTimeout = 3000;

            _redis = await ConnectionMultiplexer.ConnectAsync(options);
            if (_redis.IsConnected)
            {
                _logger.LogInformation("Connected to Redis at {Endpoint}", connectionString);
                return _redis.GetDatabase();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not connect to Redis ({Message}). Using resilient in-memory channel.", ex.Message);
        }
        finally
        {
            _connectionLock.Release();
        }

        return null;
    }

    public async Task EnqueueAsync(ExecutionJobPayload payload, CancellationToken cancellationToken = default)
    {
        var db = await GetDatabaseAsync();
        var json = JsonSerializer.Serialize(payload);

        if (db != null)
        {
            try
            {
                await db.ListRightPushAsync(QueueKey, json);
                _logger.LogInformation("Job {ExecutionId} enqueued to Redis list {QueueKey}", payload.ExecutionId, QueueKey);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to push to Redis queue: {Message}. Enqueuing to in-memory fallback.", ex.Message);
            }
        }

        // Fallback channel
        await _fallbackChannel.Writer.WriteAsync(payload, cancellationToken);
        _logger.LogInformation("Job {ExecutionId} written to in-memory queue fallback", payload.ExecutionId);
    }

    public async Task<ExecutionJobPayload?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var db = await GetDatabaseAsync();
        if (db != null)
        {
            try
            {
                // Pop with short timeout
                var value = await db.ListLeftPopAsync(QueueKey);
                if (value.HasValue)
                {
                    return JsonSerializer.Deserialize<ExecutionJobPayload>(value.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis dequeue failed: {Message}", ex.Message);
            }
        }

        // Try reading from in-memory fallback
        if (_fallbackChannel.Reader.TryRead(out var fallbackItem))
        {
            return fallbackItem;
        }

        return null;
    }

    public async Task PublishInputAsync(Guid executionId, string input, CancellationToken cancellationToken = default)
    {
        var db = await GetDatabaseAsync();
        if (db != null)
        {
            try
            {
                var channelName = $"online_compiler:stdin:{executionId}";
                await db.PublishAsync(RedisChannel.Literal(channelName), input);
                _logger.LogInformation("Published stdin for {ExecutionId} via Redis pub/sub", executionId);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to publish stdin via Redis: {Message}", ex.Message);
            }
        }

        if (_stdinChannels.TryGetValue(executionId, out var channel))
        {
            await channel.Writer.WriteAsync(input, cancellationToken);
        }
    }
}
