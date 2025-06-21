using Microsoft.Extensions.Configuration;

namespace LLMModelFactory;

#pragma warning disable
/// <summary>
/// Configuration class for Azure OpenAI services.
/// Supports multiple configuration sources with fallback hierarchy:
/// 1. appsettings.json / appsettings.Development.json (local development)
/// 2. Environment variables (deployment/production)
/// 3. Default fallback values
/// </summary>
public class Config 
{
    private static readonly Lazy<IConfiguration> _configuration = new(() => BuildConfiguration());
    
    // GPT-4o Chat Model Configuration
    public string Gpt4oDeploymentId { get; } = GetConfigValue("AzureOpenAI:DeploymentIds:Gpt4o", "GPT4O_DEPLOYMENT_ID", "gpt-4o");
    
    // Embedding Model Configuration  
    public string EmbeddingDeploymentId { get; } = GetConfigValue("AzureOpenAI:DeploymentIds:Embedding", "EMBEDDING_DEPLOYMENT_ID", "text-embedding-3-large");
    
    // Shared Azure OpenAI Configuration
    public string Endpoint { get; } = GetConfigValue("AzureOpenAI:Endpoint", "AZURE_OPENAI_ENDPOINT", string.Empty);
    public string ApiKey { get; } = GetConfigValue("AzureOpenAI:ApiKey", "AZURE_OPENAI_APIKEY", string.Empty);

    /// <summary>
    /// Builds the configuration using multiple sources with proper precedence.
    /// Priority: JSON files -> Environment variables -> Default values
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    /// <summary>
    /// Gets configuration value with fallback hierarchy:
    /// 1. JSON configuration (appsettings.json)
    /// 2. Environment variable (for backwards compatibility)
    /// 3. Default value
    /// </summary>
    private static string GetConfigValue(string jsonPath, string envVarName, string defaultValue)
    {
        var config = _configuration.Value;
        
        // Try JSON configuration first
        var jsonValue = config[jsonPath];
        if (!string.IsNullOrWhiteSpace(jsonValue))
        {
            return jsonValue;
        }
        
        // Fallback to environment variable (backwards compatibility)
        var envValue = Environment.GetEnvironmentVariable(envVarName, EnvironmentVariableTarget.Machine);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }
        
        // Return default value
        return defaultValue;
    }

    /// <summary>
    /// Validates that all required configuration values are present.
    /// Throws detailed exceptions indicating which values are missing.
    /// </summary>
    public void ValidateConfiguration()
    {
        var missingConfigs = new List<string>();

        if (string.IsNullOrWhiteSpace(Endpoint))
            missingConfigs.Add("Azure OpenAI Endpoint (AzureOpenAI:Endpoint or AZURE_OPENAI_ENDPOINT)");
        
        if (string.IsNullOrWhiteSpace(ApiKey))
            missingConfigs.Add("Azure OpenAI API Key (AzureOpenAI:ApiKey or AZURE_OPENAI_APIKEY)");
        
        if (string.IsNullOrWhiteSpace(Gpt4oDeploymentId))
            missingConfigs.Add("GPT-4o Deployment ID (AzureOpenAI:DeploymentIds:Gpt4o or GPT4O_DEPLOYMENT_ID)");
        
        if (string.IsNullOrWhiteSpace(EmbeddingDeploymentId))
            missingConfigs.Add("Embedding Deployment ID (AzureOpenAI:DeploymentIds:Embedding or EMBEDDING_DEPLOYMENT_ID)");

        if (missingConfigs.Any())
        {
            throw new InvalidOperationException(
                $"Missing required Azure OpenAI configuration values:\n" +
                $"- {string.Join("\n- ", missingConfigs)}\n\n" +
                $"Please configure these values in appsettings.json or as environment variables.");
        }
    }

    /// <summary>
    /// Returns a summary of current configuration values (without sensitive data).
    /// Useful for debugging configuration issues.
    /// </summary>
    public string GetConfigurationSummary()
    {
        return $"""
            Azure OpenAI Configuration Summary:
            - Endpoint: {(string.IsNullOrWhiteSpace(Endpoint) ? "❌ Not Set" : "✅ Configured")}
            - API Key: {(string.IsNullOrWhiteSpace(ApiKey) ? "❌ Not Set" : "✅ Configured (hidden)")}
            - GPT-4o Deployment: {Gpt4oDeploymentId}
            - Embedding Deployment: {EmbeddingDeploymentId}
            """;
    }
}