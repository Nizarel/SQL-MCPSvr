// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace MCP_Azsql;

/// <summary>
/// Application-wide constants
/// </summary>
public static class ApplicationConstants
{
    public const string Name = "MCP-MSSQL Azure Container App Server";
    public const string Version = "1.0.0";
    public const string Description = "Model Context Protocol server for SQL Server database operations";
    
    // Protocol information
    public const string McpProtocolVersion = "2024-11-05";
    
    // Network configuration
    public const int DefaultPort = 8080;
    public const int MaxRequestBodySizeMB = 1;
    public const int MaxRequestHeaders = 100;
    public const int MaxRequestHeadersSizeKB = 32;
    
    // Features
    public static readonly string[] SupportedTransports = ["stdio", "http"];
    public const string AuthenticationMethod = "Azure AD with Managed Identity";
    public const string DatabaseType = "Microsoft SQL Server";
    public const string Integration = "ModelContextProtocol.AspNetCore";
}
