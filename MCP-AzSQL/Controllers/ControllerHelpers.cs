// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;
using MCP_Azsql.Tools;

namespace MCP_Azsql.Controllers;

/// <summary>
/// Shared helper methods for controllers to reduce code duplication
/// </summary>
public static class ControllerHelpers
{
    /// <summary>
    /// Executes a tool operation with consistent error handling and logging
    /// </summary>
    public static async Task<IActionResult> ExecuteToolAsync<T>(
        Func<Task<T>> operation,
        ILogger logger,
        string toolName,
        string? context = null) where T : class
    {
        try
        {
            var result = await operation();
            var contextInfo = context != null ? $" for {context}" : "";
            logger.LogInformation("{ToolName} tool executed successfully{Context}", toolName, contextInfo);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            var contextInfo = context != null ? $" for {context}" : "";
            logger.LogError(ex, "Error executing {ToolName} tool{Context}", toolName, contextInfo);
            return new BadRequestObjectResult(new DbOperationResult(success: false, error: ex.Message));
        }
    }

    /// <summary>
    /// Validates SQL tool request parameters
    /// </summary>
    public static IActionResult? ValidateSqlRequest(SqlToolRequest? request, string requiredStatementType)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Sql))
        {
            return new BadRequestObjectResult(new DbOperationResult(
                success: false, 
                error: $"{requiredStatementType} SQL statement is required"));
        }

        // Additional validation for specific statement types
        var upperSql = request.Sql.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(requiredStatementType) && !upperSql.StartsWith(requiredStatementType.ToUpperInvariant()))
        {
            return new BadRequestObjectResult(new DbOperationResult(
                success: false, 
                error: $"Only {requiredStatementType} statements are allowed"));
        }

        return null; // No validation errors
    }

    /// <summary>
    /// Validates table name parameter
    /// </summary>
    public static IActionResult? ValidateTableName(string? tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return new BadRequestObjectResult(new DbOperationResult(
                success: false, 
                error: "Table name is required"));
        }

        return null; // No validation errors
    }

    /// <summary>
    /// Validates SQL for ReadData to ensure only SELECT statements
    /// </summary>
    public static IActionResult? ValidateReadOnlySql(string sql)
    {
        var upperSql = sql.Trim().ToUpperInvariant();
        
        // Basic SQL injection protection - reject potentially dangerous statements
        if (upperSql.Contains("DROP ") || upperSql.Contains("DELETE ") || 
            upperSql.Contains("UPDATE ") || upperSql.Contains("INSERT ") ||
            upperSql.Contains("ALTER ") || upperSql.Contains("CREATE "))
        {
            return new BadRequestObjectResult(new DbOperationResult(
                success: false, 
                error: "ReadData only supports SELECT statements for security"));
        }

        return null; // No validation errors
    }
}
