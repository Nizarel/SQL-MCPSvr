// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace MCP_Azsql.Tools;

public partial class Tools
{    // TEMPORARILY DISABLED - Update operations are currently disabled
    /*
    [McpServerTool(
        Title = "Update Data",
        ReadOnly = false,
        Destructive = true),
        Description("Updates data in a table in the SQL Database. Expects a valid UPDATE SQL statement as input.")]
    public async Task<DbOperationResult> UpdateData(
        [Description("UPDATE SQL statement")] string sql)
    {
        using var activity = _telemetry.StartToolActivity(nameof(UpdateData));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            activity?.SetTag("sql.operation", "UPDATE");
            activity?.SetTag("sql.statement_length", sql.Length);
            
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            using (conn)
            {
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                var rows = await cmd.ExecuteNonQueryAsync();
                
                activity?.SetTag("rows.affected", rows);
                _telemetry.RecordToolExecution(nameof(UpdateData), stopwatch.Elapsed.TotalSeconds, true);
                return new DbOperationResult(true, null, rows);
            }
        }
        catch (Exception ex)
        {
            _telemetry.RecordError("update_data_error", nameof(UpdateData));
            _telemetry.RecordToolExecution(nameof(UpdateData), stopwatch.Elapsed.TotalSeconds, false);
            _logger.LogError(ex, "UpdateData failed: {Message}", ex.Message);
            return new DbOperationResult(false, ex.Message);
        }
        finally
        {
            stopwatch.Stop();
        }
    }
    */
}
