using Azure.AI.OpenAI;
using Azure;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace LLMModelFactory;

/// <summary>
/// Factory for creating Azure OpenAI clients.
/// This factory provides access to Azure OpenAI chat and embedding clients.
/// </summary>
public static class ExtensionsClientFactory
{
    private static readonly Config Config = new();
    
    /// <summary>
    /// Creates a ChatClient for Azure OpenAI GPT-4o.
    /// </summary>
    public static ChatClient CreateChatClient()
    {
        return CreateAzureGpt4oClient();
    }

    /// <summary>
    /// Creates an EmbeddingClient for Azure OpenAI embedding model.
    /// </summary>
    public static EmbeddingClient CreateEmbeddingClient()
    {
        return CreateAzureEmbeddingClient();
    }

    /// <summary>
    /// Creates the complete Azure OpenAI client for full access to all services.
    /// </summary>
    public static AzureOpenAIClient CreateAzureOpenAIClient()
    {
        ValidateAzureConfig();

        return new AzureOpenAIClient(
            new Uri(Config.Endpoint), 
            new AzureKeyCredential(Config.ApiKey));
    }

    /// <summary>
    /// Creates a chat client configured for Azure OpenAI GPT-4o.
    /// </summary>
    private static ChatClient CreateAzureGpt4oClient()
    {
        ValidateAzureConfig();

        var azureClient = new AzureOpenAIClient(
            new Uri(Config.Endpoint), 
            new AzureKeyCredential(Config.ApiKey));

        return azureClient.GetChatClient(Config.Gpt4oDeploymentId);
    }

    /// <summary>
    /// Creates an embedding client configured for Azure OpenAI.
    /// </summary>
    private static EmbeddingClient CreateAzureEmbeddingClient()
    {
        ValidateAzureConfig();

        var azureClient = new AzureOpenAIClient(
            new Uri(Config.Endpoint), 
            new AzureKeyCredential(Config.ApiKey));

        return azureClient.GetEmbeddingClient(Config.EmbeddingDeploymentId);
    }

    /// <summary>
    /// Validates that all required Azure OpenAI configuration values are present.
    /// </summary>
    private static void ValidateAzureConfig()
    {
        Config.ValidateConfiguration();
    }
}