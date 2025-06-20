// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace MCP_Azsql.Tools;

public partial class Tools
{    // TEMPORARILY DISABLED - Insert operations are currently disabled
    /*
    [McpServerTool(
        Title = "Insert Data",
        ReadOnly = false,
        Destructive = false),
        Description("Updates data in a table in the SQL Database. Expects a valid INSERT SQL statement as input. ")]
    public async Task<DbOperationResult> InsertData(
        [Description("INSERT SQL statement")] string sql)
    {
        using var activity = _telemetry.StartToolActivity(nameof(InsertData));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            activity?.SetTag("sql.operation", "INSERT");
            activity?.SetTag("sql.statement_length", sql.Length);
            
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            using (conn)
            {
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                var rows = await cmd.ExecuteNonQueryAsync();
                
                activity?.SetTag("rows.affected", rows);
                _telemetry.RecordToolExecution(nameof(InsertData), stopwatch.Elapsed.TotalSeconds, true);
                return new DbOperationResult(success: true, rowsAffected: rows);
            }
        }
        catch (Exception ex)
        {
            _telemetry.RecordError("insert_data_error", nameof(InsertData));
            _telemetry.RecordToolExecution(nameof(InsertData), stopwatch.Elapsed.TotalSeconds, false);
            _logger.LogError(ex, "InsertData failed: {Message}", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
        finally
        {
            stopwatch.Stop();        }
    }
    */
}
