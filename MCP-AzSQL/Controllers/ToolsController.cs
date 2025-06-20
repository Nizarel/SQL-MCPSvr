// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;
using MCP_Azsql.Tools;
using System.ComponentModel.DataAnnotations;

namespace MCP_Azsql.Controllers;

[ApiController]
[Route("api/mcp/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly Tools.Tools _tools;
    private readonly ILogger<ToolsController> _logger;

    public ToolsController(Tools.Tools tools, ILogger<ToolsController> logger)
    {
        _tools = tools;
        _logger = logger;
    }    /// <summary>
    /// Lists all available MCP tools
    /// </summary>
    [HttpGet]
    public IActionResult GetAvailableTools()
    {        var tools = new[]
        {
            new { name = "ListTables", description = "Lists all tables in the SQL Database", readOnly = true },
            new { name = "DescribeTable", description = "Returns table schema", readOnly = true },
            new { name = "ReadData", description = "Executes SQL queries against SQL Database to read data", readOnly = true }
            // TEMPORARILY DISABLED: CreateTable, InsertData, UpdateData, DropTable operations
            // new { name = "CreateTable", description = "Creates a new table in the SQL Database", readOnly = false },
            // new { name = "InsertData", description = "Inserts data into a table in the SQL Database", readOnly = false },
            // new { name = "UpdateData", description = "Updates data in a table in the SQL Database", readOnly = false },
            // new { name = "DropTable", description = "Drops a table in the SQL Database", readOnly = false }
        };

        return Ok(new { tools, timestamp = DateTime.UtcNow });
    }    /// <summary>
    /// Execute ListTables tool
    /// </summary>
    [HttpGet("list-tables")]
    public async Task<IActionResult> ListTables()
    {
        return await ControllerHelpers.ExecuteToolAsync(
            () => _tools.ListTables(),
            _logger,
            "ListTables");
    }    /// <summary>
    /// Execute DescribeTable tool
    /// </summary>
    [HttpGet("describe-table/{tableName}")]
    public async Task<IActionResult> DescribeTable(string tableName)
    {
        var validationResult = ControllerHelpers.ValidateTableName(tableName);
        if (validationResult != null) return validationResult;

        return await ControllerHelpers.ExecuteToolAsync(
            () => _tools.DescribeTable(tableName),
            _logger,
            "DescribeTable",
            $"table {tableName}");
    }    /// <summary>
    /// Execute ReadData tool
    /// </summary>
    [HttpPost("read-data")]
    public async Task<IActionResult> ReadData([FromBody] SqlToolRequest request)
    {
        var validationResult = ControllerHelpers.ValidateSqlRequest(request, "");
        if (validationResult != null) return validationResult;

        var readOnlyValidation = ControllerHelpers.ValidateReadOnlySql(request!.Sql);
        if (readOnlyValidation != null) return readOnlyValidation;

        return await ControllerHelpers.ExecuteToolAsync(
            () => _tools.ReadData(request.Sql),
            _logger,
            "ReadData");
    }

    // TEMPORARILY DISABLED - Create table operations are currently disabled
    /*
    /// <summary>
    /// Execute CreateTable tool
    /// </summary>
    [HttpPost("create-table")]
    public async Task<IActionResult> CreateTable([FromBody] SqlToolRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Sql))
        {
            return BadRequest(new DbOperationResult(success: false, error: "CREATE TABLE SQL statement is required"));
        }

        // Validate that it's actually a CREATE TABLE statement
        var upperSql = request.Sql.Trim().ToUpperInvariant();
        if (!upperSql.StartsWith("CREATE TABLE"))
        {
            return BadRequest(new DbOperationResult(success: false, 
                error: "Only CREATE TABLE statements are allowed"));
        }

        try
        {
            var result = await _tools.CreateTable(request.Sql);
            _logger.LogInformation("CreateTable tool executed successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing CreateTable tool");
            return BadRequest(new DbOperationResult(success: false, error: ex.Message));
        }
    }
    */

    // TEMPORARILY DISABLED - Insert operations are currently disabled
    /*
    /// <summary>
    /// Execute InsertData tool
    /// </summary>
    [HttpPost("insert-data")]
    public async Task<IActionResult> InsertData([FromBody] SqlToolRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Sql))
        {
            return BadRequest(new DbOperationResult(success: false, error: "INSERT SQL statement is required"));
        }

        // Validate that it's actually an INSERT statement
        var upperSql = request.Sql.Trim().ToUpperInvariant();
        if (!upperSql.StartsWith("INSERT "))
        {
            return BadRequest(new DbOperationResult(success: false, 
                error: "Only INSERT statements are allowed"));
        }

        try
        {
            var result = await _tools.InsertData(request.Sql);
            _logger.LogInformation("InsertData tool executed successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing InsertData tool");
            return BadRequest(new DbOperationResult(success: false, error: ex.Message));
        }    }
    */

    // TEMPORARILY DISABLED - Update operations are currently disabled
    /*
    /// <summary>
    /// Execute UpdateData tool
    /// </summary>
    [HttpPost("update-data")]
    public async Task<IActionResult> UpdateData([FromBody] SqlToolRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Sql))
        {
            return BadRequest(new DbOperationResult(success: false, error: "UPDATE SQL statement is required"));
        }

        // Validate that it's actually an UPDATE statement
        var upperSql = request.Sql.Trim().ToUpperInvariant();
        if (!upperSql.StartsWith("UPDATE "))
        {
            return BadRequest(new DbOperationResult(success: false, 
                error: "Only UPDATE statements are allowed"));
        }

        try
        {
            var result = await _tools.UpdateData(request.Sql);
            _logger.LogInformation("UpdateData tool executed successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing UpdateData tool");            return BadRequest(new DbOperationResult(success: false, error: ex.Message));
        }
    }
    */

    // TEMPORARILY DISABLED - Drop table operations are currently disabled
    /*
    /// <summary>
    /// Execute DropTable tool
    /// </summary>
    [HttpPost("drop-table")]
    public async Task<IActionResult> DropTable([FromBody] SqlToolRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Sql))
        {
            return BadRequest(new DbOperationResult(success: false, error: "DROP TABLE SQL statement is required"));
        }

        // Validate that it's actually a DROP TABLE statement
        var upperSql = request.Sql.Trim().ToUpperInvariant();
        if (!upperSql.StartsWith("DROP TABLE"))
        {
            return BadRequest(new DbOperationResult(success: false, 
                error: "Only DROP TABLE statements are allowed"));
        }

        try
        {
            var result = await _tools.DropTable(request.Sql);
            _logger.LogWarning("DropTable tool executed successfully - Table: {Sql}", request.Sql);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing DropTable tool");
            return BadRequest(new DbOperationResult(success: false, error: ex.Message));
        }
    }
    */
}

public class SqlToolRequest
{
    [Required(ErrorMessage = "SQL statement is required")]
    [StringLength(8000, ErrorMessage = "SQL statement cannot exceed 8000 characters")]
    public string Sql { get; set; } = string.Empty;
}
