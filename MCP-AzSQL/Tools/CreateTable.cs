// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace MCP_Azsql.Tools;

public partial class Tools
{    // TEMPORARILY DISABLED - Create table operations are currently disabled
    // [McpServerTool(
    //     Title = "Create Table",
    //     ReadOnly = false,
    //     Destructive = false),
    //     Description("Creates a new table in the SQL Database. Expects a valid CREATE TABLE SQL statement as input.")]    
    // public async Task<DbOperationResult> CreateTable(
    //     [Description("CREATE TABLE SQL statement")] string sql)
    // {
    //     using var activity = _telemetry.StartToolActivity(nameof(CreateTable));
    //     var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    //     
    //     try
    //     {
    //         activity?.SetTag("sql.operation", "CREATE_TABLE");
    //         activity?.SetTag("sql.statement_length", sql.Length);
    //         
    //         using var conn = await _connectionFactory.GetOpenConnectionAsync();
    //         using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
    //         await cmd.ExecuteNonQueryAsync();
    //         
    //         _telemetry.RecordToolExecution(nameof(CreateTable), stopwatch.Elapsed.TotalSeconds, true);
    //         _logger.LogInformation("Table created successfully");
    //         return new DbOperationResult(success: true);
    //     }
    //     catch (Exception ex)
    //     {
    //         _telemetry.RecordError("create_table_error", nameof(CreateTable));
    //         _telemetry.RecordToolExecution(nameof(CreateTable), stopwatch.Elapsed.TotalSeconds, false);
    //         _logger.LogError(ex, "CreateTable failed: {Message}", ex.Message);
    //         return new DbOperationResult(success: false, error: ex.Message);
    //     }
    //     finally
    //     {
    //         stopwatch.Stop();
    //     }
    // }
}
