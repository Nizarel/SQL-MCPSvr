// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace MCP_Azsql.Tools;

public partial class Tools
{    // TEMPORARILY DISABLED - Drop table operations are currently disabled
    /*
    [McpServerTool(
        Title = "Drop Table",
        ReadOnly = false,
        Destructive = true),
        Description("Drops a table in the SQL Database. Expects a valid DROP TABLE SQL statement as input.")]
    public async Task<DbOperationResult> DropTable(
        [Description("DROP TABLE SQL statement")] string sql)
    {
        using var activity = _telemetry.StartToolActivity(nameof(DropTable));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            activity?.SetTag("sql.operation", "DROP_TABLE");
            activity?.SetTag("sql.statement_length", sql.Length);
            
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            using (conn)
            {
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                _ = await cmd.ExecuteNonQueryAsync();
                
                _telemetry.RecordToolExecution(nameof(DropTable), stopwatch.Elapsed.TotalSeconds, true);
                return new DbOperationResult(success: true);
            }
        }
        catch (Exception ex)
        {
            _telemetry.RecordError("drop_table_error", nameof(DropTable));
            _telemetry.RecordToolExecution(nameof(DropTable), stopwatch.Elapsed.TotalSeconds, false);
            _logger.LogError(ex, "DropTable failed: {Message}", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
        finally
        {
            stopwatch.Stop();
        }
    }
    */
}
