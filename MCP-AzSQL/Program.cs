// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json;
using MCP_Azsql;
using MCP_Azsql.Caching;
using MCP_Azsql.Observability;
using MCP_Azsql.Resilience;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// ENVIRONMENT DETECTION
// ===================================================================
var environmentInfo = DetectEnvironment(builder);

// ===================================================================
// LOGGING CONFIGURATION
// ===================================================================
ConfigureLogging(builder);

// ===================================================================
// AZURE SERVICES CONFIGURATION
// ===================================================================
ConfigureAzureServices(builder, environmentInfo);

// ===================================================================
// APPLICATION SERVICES
// ===================================================================
ConfigureApplicationServices(builder);

// ===================================================================
// MCP PROTOCOL CONFIGURATION  
// ===================================================================
ConfigureMcpProtocol(builder, environmentInfo);

// ===================================================================
// WEB API CONFIGURATION
// ===================================================================
ConfigureWebApi(builder);

// ===================================================================
// NETWORKING CONFIGURATION
// ===================================================================
ConfigureNetworking(builder);

var app = builder.Build();

// ===================================================================
// HTTP PIPELINE CONFIGURATION
// ===================================================================
ConfigureHttpPipeline(app, environmentInfo);

// ===================================================================
// APPLICATION STARTUP
// ===================================================================
await StartApplicationAsync(app);

// ===================================================================
// HELPER METHODS
// ===================================================================

static EnvironmentInfo DetectEnvironment(WebApplicationBuilder builder)
{
    var isAzureEnvironment = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null || 
                            Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") != null ||
                            !string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);

    // Enable container mode for HTTP MCP testing
    var isContainerEnvironment = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null || 
                               Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") != null ||
                               builder.Configuration.GetValue<bool>("UseContainerMode", true); // Default to true for HTTP MCP

    return new EnvironmentInfo(isAzureEnvironment, isContainerEnvironment, builder.Environment.IsDevelopment());
}

static void ConfigureLogging(WebApplicationBuilder builder)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
}

static void ConfigureAzureServices(WebApplicationBuilder builder, EnvironmentInfo environment)
{
    if (!environment.IsAzure) return;

    var connectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    if (!string.IsNullOrEmpty(connectionString))
    {
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = connectionString;
            options.EnableAdaptiveSampling = true;
            options.EnableQuickPulseMetricStream = true;
        });
    }
}

static void ConfigureApplicationServices(WebApplicationBuilder builder)
{
    // Core services
    builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
    builder.Services.AddScoped<SqlHealthCheck>();

    // Enhanced services
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
    builder.Services.AddSingleton<DatabaseResilienceService>();
    builder.Services.AddSingleton<McpTelemetry>();

    // Tools
    builder.Services.AddSingleton<MCP_Azsql.Tools.Tools>();

    // Configuration options
    builder.Services.Configure<CacheOptions>(
        builder.Configuration.GetSection("CacheOptions"));

    // Health checks
    builder.Services.AddHealthChecks()
        .AddCheck<SqlHealthCheck>("sql-server");
}

static void ConfigureMcpProtocol(WebApplicationBuilder builder, EnvironmentInfo environment)
{
    var mcpBuilder = builder.Services.AddMcpServer();

    if (environment.IsContainer)
    {
        // Production/Container: Use HTTP transport for web integration
        mcpBuilder.WithHttpTransport();
    }
    else
    {
        // Local development: Use stdio transport for CLI integration
        mcpBuilder.WithStdioServerTransport();
    }

    mcpBuilder.WithToolsFromAssembly();
}

static void ConfigureWebApi(WebApplicationBuilder builder)
{
    builder.Services.AddControllers(options =>
    {
        options.SuppressAsyncSuffixInActionNames = false;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddOpenApi();
    }

    // CORS configuration
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                                   ?? ["https://localhost"];
                
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
        });
    });
}

static void ConfigureNetworking(WebApplicationBuilder builder)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        var port = builder.Configuration.GetValue<int>("PORT", ApplicationConstants.DefaultPort);
        options.ListenAnyIP(port);
        
        // Security headers and limits
        options.AddServerHeader = false;
        options.Limits.MaxRequestBodySize = ApplicationConstants.MaxRequestBodySizeMB * 1_048_576; // Convert MB to bytes
        options.Limits.MaxRequestHeaderCount = ApplicationConstants.MaxRequestHeaders;
        options.Limits.MaxRequestHeadersTotalSize = ApplicationConstants.MaxRequestHeadersSizeKB * 1_024; // Convert KB to bytes
    });
}

static void ConfigureHttpPipeline(WebApplication app, EnvironmentInfo environment)
{
    // Development tools
    if (environment.IsDevelopment)
    {
        app.MapOpenApi();
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/error");
    }

    // Security headers (production)
    if (!environment.IsDevelopment)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            await next();
        });
    }

    // CORS
    app.UseCors();

    // Request logging (development only)
    if (environment.IsDevelopment)
    {
        app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            using var scope = logger.BeginScope("Request {RequestId}", context.TraceIdentifier);
            
            logger.LogDebug("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
            
            await next();
            
            logger.LogDebug("Response: {StatusCode}", context.Response.StatusCode);
        });
    }

    // Routing
    app.MapControllers();    // MCP protocol endpoint
    if (environment.IsContainer)
    {
        app.MapMcp("/mcp");
    }
    
    // Health checks
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) => await WriteHealthCheckResponse(context, report)
    });

    // Graceful shutdown
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopping.Register(() =>
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Application is shutting down gracefully...");
    });
}

static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    
    var response = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(x => new
        {
            name = x.Key,
            status = x.Value.Status.ToString(),
            description = x.Value.Description,
            duration = x.Value.Duration.TotalMilliseconds
        }),
        timestamp = DateTime.UtcNow
    };

    await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    }));
}

static async Task StartApplicationAsync(WebApplication app)
{
    try
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting MCP-MSSQL Azure Container App Server...");
        logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
        
        await app.RunAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILogger<Program>>();
        logger?.LogCritical(ex, "Application terminated unexpectedly");
        throw;
    }
}

// ===================================================================
// HELPER TYPES
// ===================================================================

internal record EnvironmentInfo(bool IsAzure, bool IsContainer, bool IsDevelopment);
