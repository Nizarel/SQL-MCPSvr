// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MCP_Azsql.Caching;

namespace MCP_Azsql.Tools;

public partial class Tools
{    [McpServerTool(
        Title = "Read Data",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Executes SQL queries against SQL Database to read data")]
    public async Task<DbOperationResult> ReadData(
        [Description("SQL query to execute")] string sql)
    {
        using var activity = _telemetry.StartToolActivity("ReadData");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            activity?.SetTag("sql.query", sql);
            
            // Generate cache key for the query
            var cacheKey = CacheKeys.QueryResult(GenerateQueryCacheKey(sql));
            
            // Check cache first for SELECT queries
            if (sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                var cachedResult = await _cacheService.GetAsync<List<Dictionary<string, object?>>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Returning cached query result for key: {CacheKey}", cacheKey);
                    activity?.SetTag("cache.hit", true);
                    activity?.SetTag("rows.count", cachedResult.Count);
                    stopwatch.Stop();
                    _telemetry.RecordToolExecution("ReadData", stopwatch.Elapsed.TotalSeconds, true);
                    return new DbOperationResult(success: true, data: cachedResult);
                }
            }

            activity?.SetTag("cache.hit", false);
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            
            using (conn)
            {
                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                var results = new List<Dictionary<string, object?>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                }                // Cache SELECT query results for 20 minutes
                if (sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    await _cacheService.SetQueryResultAsync(cacheKey, results);
                    _logger.LogDebug("Cached query result for key: {CacheKey} with {Count} rows for 20 minutes", cacheKey, results.Count);
                }

                activity?.SetTag("rows.count", results.Count);
                stopwatch.Stop();
                _telemetry.RecordToolExecution("ReadData", stopwatch.Elapsed.TotalSeconds, true);
                _telemetry.RecordQuery(stopwatch.Elapsed.TotalSeconds, "SELECT");

                return new DbOperationResult(success: true, data: results);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _telemetry.RecordToolExecution("ReadData", stopwatch.Elapsed.TotalSeconds, false);
            _telemetry.RecordError("database_error", "ReadData");
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            
            _logger.LogError(ex, "ReadData failed: {Message}", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
    }
}
