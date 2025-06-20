// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Data;
using Microsoft.Data.SqlClient;
using Polly;
using Polly.CircuitBreaker;

namespace MCP_Azsql.Resilience;

/// <summary>
/// Resilience patterns for database operations
/// </summary>
public class DatabaseResilienceService
{
    private readonly ILogger<DatabaseResilienceService> _logger;
    private readonly ResiliencePipeline _pipeline;

    public DatabaseResilienceService(ILogger<DatabaseResilienceService> logger)
    {
        _logger = logger;
        
        // Create resilience pipeline with retry and circuit breaker
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<SqlException>(IsTransientError).Handle<TimeoutException>(),
                MaxRetryAttempts = 3,
                DelayGenerator = static args => ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber))),
                OnRetry = args =>
                {
                    logger.LogWarning("Retry attempt {AttemptNumber} after {Duration}ms. Error: {Error}",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<SqlException>(ex => !IsTransientError(ex)),
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromMinutes(1),
                OnOpened = args =>
                {
                    logger.LogError("Circuit breaker opened due to: {Error}", args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("Circuit breaker closed");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
    {
        _logger.LogDebug("Executing operation: {OperationName}", operationName);
        return await _pipeline.ExecuteAsync(async (cancellationToken) =>
        {
            return await operation();
        });
    }

    public async Task ExecuteAsync(Func<Task> operation, string operationName)
    {
        _logger.LogDebug("Executing operation: {OperationName}", operationName);
        await _pipeline.ExecuteAsync(async (cancellationToken) =>
        {
            await operation();
        });
    }

    private static bool IsTransientError(SqlException sqlException)
    {
        // Common transient error numbers for SQL Server
        var transientErrors = new[]
        {
            -2,    // Timeout
            2,     // Timeout
            53,    // Network path not found
            121,   // The semaphore timeout period has expired
            232,   // The pipe is being closed
            10053, // A connection was aborted by the software in your host machine
            10054, // An existing connection was forcibly closed by the remote host
            10060, // A connection attempt failed
            10061, // No connection could be made because the target machine actively refused it
            18456, // Login failed (temporary)
            40197, // The server encountered an error processing the request
            40501, // The database is currently unavailable
            40613, // Database is not currently available
        };

        return transientErrors.Contains(sqlException.Number);
    }
}

/// <summary>
/// Enhanced exception handling for MCP operations
/// </summary>
public static class ExceptionHandler
{
    public static DbOperationResult HandleException(Exception ex, ILogger logger, string operation)
    {
        logger.LogError(ex, "Error in operation: {Operation}", operation);

        return ex switch
        {
            SqlException sqlEx => HandleSqlException(sqlEx, logger),
            TimeoutException timeoutEx => new DbOperationResult(false, "Operation timed out. Please try again."),
            ArgumentException argEx => new DbOperationResult(false, $"Invalid argument: {argEx.Message}"),
            InvalidOperationException invOpEx => new DbOperationResult(false, $"Invalid operation: {invOpEx.Message}"),
            UnauthorizedAccessException => new DbOperationResult(false, "Access denied. Check your permissions."),
            _ => new DbOperationResult(false, "An unexpected error occurred. Please try again.")
        };
    }

    private static DbOperationResult HandleSqlException(SqlException sqlEx, ILogger logger)
    {
        return sqlEx.Number switch
        {
            2 or -2 => new DbOperationResult(false, "Database operation timed out. Please try again."),
            18456 => new DbOperationResult(false, "Authentication failed. Check your credentials."),
            208 => new DbOperationResult(false, "Invalid object name. Check your table/column names."),
            102 => new DbOperationResult(false, "Incorrect syntax. Please check your SQL statement."),
            515 => new DbOperationResult(false, "Cannot insert NULL into a required column."),
            547 => new DbOperationResult(false, "Foreign key constraint violation."),
            2627 => new DbOperationResult(false, "Unique constraint violation."),
            _ when sqlEx.Number >= 50000 => new DbOperationResult(false, $"Custom error: {sqlEx.Message}"),
            _ => new DbOperationResult(false, $"Database error: {sqlEx.Message}")
        };
    }
}
