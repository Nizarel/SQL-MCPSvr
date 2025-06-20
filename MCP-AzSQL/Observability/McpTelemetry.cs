// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MCP_Azsql.Observability;

/// <summary>
/// Telemetry and metrics collection for MCP operations - Active only in Azure environments
/// </summary>
public class McpTelemetry
{
    private static readonly ActivitySource? ActivitySource;
    private static readonly Meter? Meter;
    private static readonly bool IsAzureEnvironment;
    
    // Counters
    private readonly Counter<long>? _toolExecutionCounter;
    private readonly Counter<long>? _connectionCounter;
    private readonly Counter<long>? _errorCounter;
    
    // Histograms for timing
    private readonly Histogram<double>? _toolExecutionDuration;
    private readonly Histogram<double>? _connectionDuration;
    private readonly Histogram<double>? _queryDuration;

    static McpTelemetry()
    {
        // Only enable telemetry in Azure environments
        IsAzureEnvironment = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null || 
                           Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") != null ||
                           Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") != null;

        if (IsAzureEnvironment)
        {
            ActivitySource = new ActivitySource("MCP.MSSQL.Server");
            Meter = new Meter("MCP.MSSQL.Server");
        }
    }

    public McpTelemetry()
    {
        if (IsAzureEnvironment && Meter != null)
        {
            _toolExecutionCounter = Meter.CreateCounter<long>("mcp_tool_executions_total", 
                description: "Total number of MCP tool executions");
                
            _connectionCounter = Meter.CreateCounter<long>("mcp_db_connections_total", 
                description: "Total number of database connections");
                
            _errorCounter = Meter.CreateCounter<long>("mcp_errors_total", 
                description: "Total number of errors");

            _toolExecutionDuration = Meter.CreateHistogram<double>("mcp_tool_duration_seconds", 
                description: "Duration of MCP tool executions");
                
            _connectionDuration = Meter.CreateHistogram<double>("mcp_connection_duration_seconds", 
                description: "Duration of database connections");
                
            _queryDuration = Meter.CreateHistogram<double>("mcp_query_duration_seconds", 
                description: "Duration of SQL query executions");
        }
    }    public Activity? StartToolActivity(string toolName)
    {
        if (!IsAzureEnvironment || ActivitySource == null) return null;
        
        var activity = ActivitySource.StartActivity($"MCP.Tool.{toolName}");
        activity?.SetTag("tool.name", toolName);
        activity?.SetTag("component", "mcp-tool");
        return activity;
    }

    public Activity? StartConnectionActivity()
    {
        if (!IsAzureEnvironment || ActivitySource == null) return null;
        
        var activity = ActivitySource.StartActivity("MCP.Database.Connection");
        activity?.SetTag("component", "database");
        return activity;
    }

    public void RecordToolExecution(string toolName, double durationSeconds, bool success)
    {
        if (!IsAzureEnvironment) return;
        
        _toolExecutionCounter?.Add(1, new KeyValuePair<string, object?>("tool", toolName), 
                                      new KeyValuePair<string, object?>("success", success));
        _toolExecutionDuration?.Record(durationSeconds, new KeyValuePair<string, object?>("tool", toolName));
    }

    public void RecordConnection(double durationSeconds, bool success)
    {
        if (!IsAzureEnvironment) return;
        
        _connectionCounter?.Add(1, new KeyValuePair<string, object?>("success", success));
        _connectionDuration?.Record(durationSeconds);
    }

    public void RecordQuery(double durationSeconds, string operation)
    {
        if (!IsAzureEnvironment) return;
        
        _queryDuration?.Record(durationSeconds, new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordError(string errorType, string? toolName = null)
    {
        if (!IsAzureEnvironment) return;
        
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("error_type", errorType)
        };
        
        if (toolName != null)
        {
            tags.Add(new("tool", toolName));
        }
        
        _errorCounter?.Add(1, tags.ToArray());
    }

    public void RecordCacheHit(string operation, string cacheKey)
    {
        if (!IsAzureEnvironment) return;
        
        _toolExecutionCounter?.Add(1, 
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("cache", "hit"));
    }

    public void RecordCacheMiss(string operation, string cacheKey)
    {
        if (!IsAzureEnvironment) return;
        
        _toolExecutionCounter?.Add(1, 
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("cache", "miss"));
    }
}
