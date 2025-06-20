using System.Text.Json.Serialization;

namespace WebAPI_MCP_AzSQL.Models;

/// <summary>
/// Represents a standardized MCP query request
/// </summary>
public class McpQueryRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }

    [JsonPropertyName("maxRows")]
    public int? MaxRows { get; set; }

    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    [JsonPropertyName("includeSchema")]
    public bool IncludeSchema { get; set; } = false;
}

/// <summary>
/// Represents a standardized MCP query response
/// </summary>
public class McpQueryResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<Dictionary<string, object>>? Data { get; set; }

    [JsonPropertyName("schema")]
    public List<McpColumnSchema>? Schema { get; set; }

    [JsonPropertyName("rowCount")]
    public int RowCount { get; set; }

    [JsonPropertyName("executionTimeMs")]
    public long ExecutionTimeMs { get; set; }

    [JsonPropertyName("error")]
    public McpError? Error { get; set; }

    [JsonPropertyName("metadata")]
    public McpQueryMetadata? Metadata { get; set; }
}

/// <summary>
/// Represents column schema information
/// </summary>
public class McpColumnSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("nullable")]
    public bool Nullable { get; set; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    [JsonPropertyName("precision")]
    public int? Precision { get; set; }

    [JsonPropertyName("scale")]
    public int? Scale { get; set; }
}

/// <summary>
/// Represents error information in MCP responses
/// </summary>
public class McpError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "Error";
}

/// <summary>
/// Represents query execution metadata
/// </summary>
public class McpQueryMetadata
{
    [JsonPropertyName("affectedRows")]
    public int? AffectedRows { get; set; }

    [JsonPropertyName("lastInsertId")]
    public object? LastInsertId { get; set; }

    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }

    [JsonPropertyName("queryPlan")]
    public string? QueryPlan { get; set; }
}

/// <summary>
/// Represents a database schema request
/// </summary>
public class McpSchemaRequest
{
    [JsonPropertyName("schemaName")]
    public string? SchemaName { get; set; }

    [JsonPropertyName("tableName")]
    public string? TableName { get; set; }

    [JsonPropertyName("includeIndexes")]
    public bool IncludeIndexes { get; set; } = false;

    [JsonPropertyName("includeConstraints")]
    public bool IncludeConstraints { get; set; } = false;

    [JsonPropertyName("includePermissions")]
    public bool IncludePermissions { get; set; } = false;
}

/// <summary>
/// Represents database schema information
/// </summary>
public class McpSchemaResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("schemas")]
    public List<McpDatabaseSchema>? Schemas { get; set; }

    [JsonPropertyName("error")]
    public McpError? Error { get; set; }
}

/// <summary>
/// Represents a database schema
/// </summary>
public class McpDatabaseSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tables")]
    public List<McpTableSchema>? Tables { get; set; }

    [JsonPropertyName("views")]
    public List<McpViewSchema>? Views { get; set; }

    [JsonPropertyName("procedures")]
    public List<McpProcedureSchema>? Procedures { get; set; }
}

/// <summary>
/// Represents table schema information
/// </summary>
public class McpTableSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("columns")]
    public List<McpColumnSchema> Columns { get; set; } = new();

    [JsonPropertyName("primaryKey")]
    public List<string>? PrimaryKey { get; set; }

    [JsonPropertyName("indexes")]
    public List<McpIndexSchema>? Indexes { get; set; }

    [JsonPropertyName("foreignKeys")]
    public List<McpForeignKeySchema>? ForeignKeys { get; set; }
}

/// <summary>
/// Represents view schema information
/// </summary>
public class McpViewSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("columns")]
    public List<McpColumnSchema> Columns { get; set; } = new();

    [JsonPropertyName("definition")]
    public string? Definition { get; set; }
}

/// <summary>
/// Represents stored procedure schema information
/// </summary>
public class McpProcedureSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public List<McpParameterSchema>? Parameters { get; set; }

    [JsonPropertyName("definition")]
    public string? Definition { get; set; }
}

/// <summary>
/// Represents parameter schema information
/// </summary>
public class McpParameterSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty; // IN, OUT, INOUT

    [JsonPropertyName("defaultValue")]
    public object? DefaultValue { get; set; }
}

/// <summary>
/// Represents index schema information
/// </summary>
public class McpIndexSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("columns")]
    public List<string> Columns { get; set; } = new();

    [JsonPropertyName("isUnique")]
    public bool IsUnique { get; set; }

    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("isClustered")]
    public bool IsClustered { get; set; }
}

/// <summary>
/// Represents foreign key schema information
/// </summary>
public class McpForeignKeySchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("columns")]
    public List<string> Columns { get; set; } = new();

    [JsonPropertyName("referencedTable")]
    public string ReferencedTable { get; set; } = string.Empty;

    [JsonPropertyName("referencedColumns")]
    public List<string> ReferencedColumns { get; set; } = new();

    [JsonPropertyName("onDelete")]
    public string? OnDelete { get; set; }

    [JsonPropertyName("onUpdate")]
    public string? OnUpdate { get; set; }
}
