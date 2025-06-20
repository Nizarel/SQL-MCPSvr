// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MCP_Azsql.Tools;

namespace MCP_Azsql.Controllers;

[ApiController]
[Route("mcp")]
public class McpController : ControllerBase
{
    private readonly Tools.Tools _tools;
    private readonly ILogger<McpController> _logger;

    public McpController(Tools.Tools tools, ILogger<McpController> logger)
    {
        _tools = tools;
        _logger = logger;
    }    /// <summary>
    /// MCP Protocol JSON-RPC endpoint
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleMcpRequest([FromBody] JsonElement request)
    {
        try
        {            // Parse JSON-RPC request
            if (!request.TryGetProperty("jsonrpc", out var jsonrpc) || jsonrpc.GetString() != "2.0")
            {
                return BadRequest(CreateErrorResponse(null, -32600, "Invalid Request", "Invalid JSON-RPC version"));
            }

            if (!request.TryGetProperty("method", out var methodElement))
            {
                return BadRequest(CreateErrorResponse(null, -32600, "Invalid Request", "Missing method"));
            }

            var method = methodElement.GetString();
            var id = request.TryGetProperty("id", out var idElement) ? idElement : default;

            _logger.LogInformation("Processing MCP request: {Method}", method);

            // Handle different MCP methods
            return method switch
            {
                "initialize" => await HandleInitialize(id, request),
                "tools/list" => await HandleToolsList(id),
                "tools/call" => await HandleToolsCall(id, request),
                _ => BadRequest(CreateErrorResponse(id, -32601, "Method not found", $"Method '{method}' not supported"))
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in MCP request");
            return BadRequest(CreateErrorResponse(null, -32700, "Parse error", "Invalid JSON"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            return StatusCode(500, CreateErrorResponse(null, -32603, "Internal error", "An unexpected error occurred"));
        }
    }    private Task<IActionResult> HandleInitialize(JsonElement id, JsonElement request)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id = GetIdValue(id),            result = new
            {
                protocolVersion = ApplicationConstants.McpProtocolVersion,
                capabilities = new
                {
                    tools = new { }
                },serverInfo = new
                {
                    name = ApplicationConstants.Name,
                    version = ApplicationConstants.Version
                }
            }
        };

        return Task.FromResult<IActionResult>(Ok(response));
    }    private Task<IActionResult> HandleToolsList(JsonElement id)
    {        var tools = new object[]
        {
            new { name = "ListTables", description = "Lists all tables in the SQL Database", inputSchema = new { type = "object", properties = new { } } },
            new { name = "DescribeTable", description = "Returns table schema", inputSchema = new { type = "object", properties = new { name = new { type = "string", description = "Table name" } }, required = new[] { "name" } } },
            new { name = "ReadData", description = "Executes SQL queries to read data", inputSchema = new { type = "object", properties = new { sql = new { type = "string", description = "SQL query" } }, required = new[] { "sql" } } }
            // TEMPORARILY DISABLED: CreateTable, InsertData, UpdateData, DropTable tools
            // new { name = "CreateTable", description = "Creates a new table", inputSchema = new { type = "object", properties = new { sql = new { type = "string", description = "CREATE TABLE SQL" } }, required = new[] { "sql" } } },
            // new { name = "InsertData", description = "Inserts data into a table", inputSchema = new { type = "object", properties = new { sql = new { type = "string", description = "INSERT SQL" } }, required = new[] { "sql" } } },
            // new { name = "UpdateData", description = "Updates data in a table", inputSchema = new { type = "object", properties = new { sql = new { type = "string", description = "UPDATE SQL" } }, required = new[] { "sql" } } },
            // new { name = "DropTable", description = "Drops a table", inputSchema = new { type = "object", properties = new { sql = new { type = "string", description = "DROP TABLE SQL" } }, required = new[] { "sql" } } }
        };

        var response = new
        {
            jsonrpc = "2.0",
            id = GetIdValue(id),
            result = new { tools }
        };

        return Task.FromResult<IActionResult>(Ok(response));
    }

    private async Task<IActionResult> HandleToolsCall(JsonElement id, JsonElement request)
    {
        if (!request.TryGetProperty("params", out var paramsElement))
        {
            return BadRequest(CreateErrorResponse(id, -32602, "Invalid params", "Missing params"));
        }

        if (!paramsElement.TryGetProperty("name", out var nameElement))
        {
            return BadRequest(CreateErrorResponse(id, -32602, "Invalid params", "Missing tool name"));
        }        var toolName = nameElement.GetString();
        if (string.IsNullOrEmpty(toolName))
        {
            return BadRequest(CreateErrorResponse(id, -32602, "Invalid params", "Tool name cannot be null or empty"));
        }
        
        var arguments = paramsElement.TryGetProperty("arguments", out var argsElement) ? argsElement : new JsonElement();

        try
        {
            var result = await ExecuteTool(toolName, arguments);
            
            var response = new
            {
                jsonrpc = "2.0",
                id = GetIdValue(id),
                result = new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = JsonSerializer.Serialize(result)
                        }
                    }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return BadRequest(CreateErrorResponse(id, -32603, "Tool execution error", ex.Message));
        }    }    private async Task<object> ExecuteTool(string toolName, JsonElement arguments)
    {        return toolName switch
        {
            "ListTables" => await _tools.ListTables(),
            "DescribeTable" => await _tools.DescribeTable(
                arguments.GetProperty("name").GetString() ?? throw new ArgumentException("Table name is required")),
            "ReadData" => await ExecuteReadDataWithValidation(arguments),
            // TEMPORARILY DISABLED: CreateTable, InsertData, UpdateData, DropTable operations
            "CreateTable" => throw new ArgumentException("CreateTable tool is temporarily disabled"),
            "InsertData" => throw new ArgumentException("InsertData tool is temporarily disabled"),
            "UpdateData" => throw new ArgumentException("UpdateData tool is temporarily disabled"),
            "DropTable" => throw new ArgumentException("DropTable tool is temporarily disabled"),
            _ => throw new ArgumentException($"Unknown tool: {toolName}")
        };
    }

    private async Task<object> ExecuteReadDataWithValidation(JsonElement arguments)
    {
        var sql = arguments.GetProperty("sql").GetString() ?? throw new ArgumentException("SQL query is required");
        
        // Apply the same security validation as the REST API
        var upperSql = sql.Trim().ToUpperInvariant();
        if (upperSql.Contains("DROP ") || upperSql.Contains("DELETE ") || 
            upperSql.Contains("UPDATE ") || upperSql.Contains("INSERT ") ||
            upperSql.Contains("ALTER ") || upperSql.Contains("CREATE "))
        {
            return new DbOperationResult(success: false, error: "ReadData only supports SELECT statements for security");
        }

        return await _tools.ReadData(sql);
    }private object CreateErrorResponse(JsonElement? id, int code, string message, string data)
    {
        return new
        {
            jsonrpc = "2.0",
            id = GetIdValue(id),
            error = new
            {
                code,
                message,
                data
            }
        };
    }

    private object? GetIdValue(JsonElement? id)
    {
        if (!id.HasValue)
            return null;
            
        return id.Value.ValueKind switch
        {
            JsonValueKind.String => id.Value.GetString(),
            JsonValueKind.Number => id.Value.GetInt32(),
            JsonValueKind.Null => null,
            _ => null
        };
    }
}
