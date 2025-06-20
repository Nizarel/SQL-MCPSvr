// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MCP_Azsql.Caching;

namespace MCP_Azsql.Tools;

public partial class Tools
{
    private const string ListTablesQuery = @"SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME";    [McpServerTool(
        Title = "List Tables",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists all tables in the SQL Database.")]
    public async Task<DbOperationResult> ListTables()
    {
        using var activity = _telemetry.StartToolActivity("ListTables");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Check cache first
            var cachedTables = await _cacheService.GetAsync<List<string>>(CacheKeys.TableList);
            if (cachedTables != null)
            {
                _logger.LogDebug("Returning cached table list with {Count} tables", cachedTables.Count);
                activity?.SetTag("cache.hit", true);
                stopwatch.Stop();
                _telemetry.RecordToolExecution("ListTables", stopwatch.Elapsed.TotalSeconds, true);
                return new DbOperationResult(success: true, data: cachedTables);
            }

            activity?.SetTag("cache.hit", false);
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            
            using (conn)
            {
                using var cmd = new SqlCommand(ListTablesQuery, conn);
                var tables = new List<string>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
                }                // Cache the result with 35-minute TTL
                await _cacheService.SetTableListAsync(CacheKeys.TableList, tables);
                _logger.LogDebug("Cached table list with {Count} tables for 35 minutes", tables.Count);

                activity?.SetTag("tables.count", tables.Count);
                stopwatch.Stop();
                _telemetry.RecordToolExecution("ListTables", stopwatch.Elapsed.TotalSeconds, true);
                
                return new DbOperationResult(success: true, data: tables);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _telemetry.RecordToolExecution("ListTables", stopwatch.Elapsed.TotalSeconds, false);
            _telemetry.RecordError("database_error", "ListTables");
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            
            _logger.LogError(ex, "ListTables failed: {Message}", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
    }
}
