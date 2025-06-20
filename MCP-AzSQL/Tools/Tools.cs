using System.Data;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MCP_Azsql.Caching;
using MCP_Azsql.Observability;
using System.Security.Cryptography;
using System.Text;

namespace MCP_Azsql.Tools;

// Register this class as a tool container
[McpServerToolType]
public partial class Tools(
    ISqlConnectionFactory connectionFactory, 
    ILogger<Tools> logger,
    ICacheService cacheService,
    McpTelemetry telemetry)
{
    private readonly ISqlConnectionFactory _connectionFactory = connectionFactory;
    private readonly ILogger<Tools> _logger = logger;
    private readonly ICacheService _cacheService = cacheService;
    private readonly McpTelemetry _telemetry = telemetry;

    // Helper to convert DataTable to a serializable list
    private static List<Dictionary<string, object>> DataTableToList(DataTable table)
    {
        var result = new List<Dictionary<string, object>>();
        foreach (DataRow row in table.Rows)
        {
            var dict = new Dictionary<string, object>();
            foreach (DataColumn col in table.Columns)
            {
                dict[col.ColumnName] = row[col];
            }
            result.Add(dict);
        }
        return result;
    }

    // Helper to generate cache key for SQL queries
    private static string GenerateQueryCacheKey(string sql)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sql.ToLowerInvariant().Trim()));
        return Convert.ToHexString(hashBytes)[..16]; // Use first 16 characters
    }
}