// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace MCP_Azsql.Controllers;

[ApiController]
[Route("")]
public class HomeController : ControllerBase
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _environment;

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Root endpoint with API information
    /// </summary>
    [HttpGet("/")]
    public IActionResult Get()
    {
        var isContainerEnvironment = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null || 
                                   Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") != null;        return Ok(new 
        { 
            name = ApplicationConstants.Name,
            version = ApplicationConstants.Version,
            description = ApplicationConstants.Description,            features = new
            {
                mcpProtocol = ApplicationConstants.McpProtocolVersion,
                transports = ApplicationConstants.SupportedTransports,
                authentication = ApplicationConstants.AuthenticationMethod,
                database = ApplicationConstants.DatabaseType,
                integration = ApplicationConstants.Integration
            },
            endpoints = new
            {
                health = "/health",
                liveness = "/health/live", 
                readiness = "/health/ready",
                mcpTools = "/api/mcp/tools",
                mcpProtocol = "/mcp"
            },
            protocols = new
            {
                mcp = new
                {
                    endpoint = "/mcp",
                    description = "Model Context Protocol (JSON-RPC) for AI agents",
                    format = "Server-Sent Events",
                    useCases = new[] { "AI agents", "LLM integrations", "Tool discovery" }
                },
                rest = new
                {
                    endpoint = "/api/mcp/tools",
                    description = "RESTful HTTP API for direct tool access",
                    format = "JSON",
                    useCases = new[] { "Web applications", "Direct testing", "Traditional integrations" }
                }
            },            mcpTools = new
            {
                listAvailable = "/api/mcp/tools",
                listTables = "/api/mcp/tools/list-tables",
                describeTable = "/api/mcp/tools/describe-table/{tableName}",
                readData = "/api/mcp/tools/read-data"
                // TEMPORARILY DISABLED: createTable, insertData, updateData, dropTable endpoints
                // createTable = "/api/mcp/tools/create-table",
                // insertData = "/api/mcp/tools/insert-data",
                // updateData = "/api/mcp/tools/update-data",
                // dropTable = "/api/mcp/tools/drop-table"
            },
            environment = _environment.EnvironmentName,
            timestamp = DateTime.UtcNow
        });
    }
}
