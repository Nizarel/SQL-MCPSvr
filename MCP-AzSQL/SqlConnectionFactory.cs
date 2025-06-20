using Microsoft.Data.SqlClient;

namespace MCP_Azsql;

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlConnectionFactory> _logger;

    public SqlConnectionFactory(IConfiguration configuration, ILogger<SqlConnectionFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }    public async Task<SqlConnection> GetOpenConnectionAsync()
    {
        var connectionString = GetConnectionString();

        // Create connection - using SQL authentication from connection string
        var conn = new SqlConnection(connectionString);
        
        await conn.OpenAsync();
        _logger.LogDebug("Successfully opened SQL connection");
        return conn;
    }private string GetConnectionString()
    {
        // For Container Apps, environment variables are the most reliable way to get configuration
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        
        _logger.LogInformation("Reading CONNECTION_STRING environment variable...");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Fallback to configuration system
            connectionString = _configuration.GetConnectionString("DefaultConnection") ?? 
                              _configuration["CONNECTION_STRING"];
            
            _logger.LogInformation("Environment variable was empty, tried configuration fallback: {HasValue}", 
                !string.IsNullOrEmpty(connectionString));
        }
        else
        {
            _logger.LogInformation("CONNECTION_STRING environment variable found with length: {Length}", 
                connectionString.Length);
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Connection string not found in any source");
            throw new InvalidOperationException(
                "Connection string is not configured. Please set the CONNECTION_STRING environment variable.");
        }

        // Optimize connection string for performance and reliability
        connectionString = OptimizeConnectionString(connectionString);
        
        _logger.LogInformation("Connection string retrieved and optimized successfully");
        return connectionString;
    }

    private static string OptimizeConnectionString(string originalConnectionString)
    {
        var builder = new SqlConnectionStringBuilder(originalConnectionString);
        
        // Connection pooling optimizations
        builder.Pooling = true;
        builder.MinPoolSize = 1;        // Minimum connections in pool
        builder.MaxPoolSize = 100;      // Maximum connections in pool
        builder.ConnectTimeout = 30;    // Connection timeout in seconds
        builder.CommandTimeout = 60;    // Command timeout in seconds
        
        // Performance optimizations
        builder.Encrypt = true;         // Always encrypt in production
        builder.TrustServerCertificate = false; // Validate certificates
        builder.ApplicationName = "MCP-MSSQL-Server"; // For monitoring
        
        // Reliability settings
        builder.ConnectRetryCount = 3;  // Retry connection failures
        builder.ConnectRetryInterval = 10; // Wait between retries
        
        return builder.ConnectionString;
    }
}
