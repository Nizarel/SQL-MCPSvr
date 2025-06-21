# Azure OpenAI LLM Model Factory

This library provides factory classes for creating Azure OpenAI clients using both **Semantic Kernel** and **Azure OpenAI SDK** approaches.

## Features

- ✅ **Azure OpenAI GPT-4o** support for chat completions
- ✅ **Azure OpenAI Text Embedding** support for vector generation
- ✅ **Semantic Kernel** integration for advanced AI orchestration
- ✅ **Azure OpenAI SDK** direct access for maximum control
- ✅ **Environment-based configuration** for secure credential management

## Configuration

The library supports multiple configuration sources with the following priority:

1. **JSON Configuration Files** (Recommended for local development)
2. **Environment Variables** (Recommended for production/deployment)
3. **Default Values** (Fallback)

### Option 1: JSON Configuration (Recommended for Local Development)

Create `appsettings.json` in your project root:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentIds": {
      "Gpt4o": "gpt-4o",
      "Embedding": "text-embedding-3-large"
    }
  }
}
```

For development-specific settings, create `appsettings.Development.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-dev-resource.openai.azure.com/",
    "ApiKey": "your-dev-api-key",
    "DeploymentIds": {
      "Gpt4o": "gpt-4o",
      "Embedding": "text-embedding-3-large"
    }
  }
}
```

**⚠️ Security Note**: Add `appsettings.Development.json` to your `.gitignore` to prevent committing sensitive credentials.

### Option 2: Environment Variables (Recommended for Production)

Set the following environment variables (Machine-level):

```bash
# Required for Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_APIKEY=your-api-key-here

# Model deployment names
GPT4O_DEPLOYMENT_ID=gpt-4o
EMBEDDING_DEPLOYMENT_ID=text-embedding-3-large
```

### Configuration Validation

The library includes built-in configuration validation:

```csharp
var config = new Config();

// Validate all required settings
config.ValidateConfiguration();

// Get configuration summary (without sensitive data)
Console.WriteLine(config.GetConfigurationSummary());
```

## Usage

### Using Semantic Kernel (Recommended for AI Orchestration)

```csharp
using LLMModelFactory;

// Create a kernel with chat capabilities
var chatKernel = KernelFactory.CreateChatKernel();

// Create a kernel with both chat and embedding capabilities
var fullKernel = KernelFactory.CreateKernelWithEmbeddings();

// Use the kernel for AI operations
var result = await chatKernel.InvokePromptAsync("Hello, how are you?");
```

### Using Azure OpenAI SDK Directly (Recommended for Direct Control)

```csharp
using LLMModelFactory;
using OpenAI.Chat;

// Create chat client
var chatClient = ExtensionsClientFactory.CreateChatClient();

// Create embedding client
var embeddingClient = ExtensionsClientFactory.CreateEmbeddingClient();

// Use the chat client
var messages = new List<ChatMessage>
{
    new UserChatMessage("What is the capital of France?")
};

var completion = await chatClient.CompleteChatAsync(messages);
Console.WriteLine(completion.Value.Content[0].Text);

// Use the embedding client
var embeddings = await embeddingClient.GenerateEmbeddingsAsync(["Hello world"]);
var vector = embeddings.Value[0].Vector;
```

## Models Supported

| Model Type | Enum Value | Description |
|------------|------------|-------------|
| Chat | `LLMModel.AzureGpt4o` | GPT-4o model for chat completions |
| Embedding | `LLMModel.AzureEmbedding` | Text embedding model for vector generation |

## Architecture

The factory provides two main approaches:

1. **KernelFactory**: Creates Semantic Kernel instances for advanced AI workflows, function calling, and chaining operations.

2. **ExtensionsClientFactory**: Creates direct Azure OpenAI clients for maximum control and performance.

Both factories share the same configuration and provide access to Azure OpenAI's GPT-4o and embedding models.

## Error Handling

All factory methods include comprehensive validation:
- Missing endpoint configuration
- Missing API key
- Missing deployment IDs
- Invalid Azure OpenAI responses

Configure your environment variables properly to avoid runtime exceptions.

## Security Best Practices

✅ **Use environment variables** for credentials (never hardcode)  
✅ **Use Machine-level environment variables** for server deployments  
✅ **Rotate API keys regularly** following Azure security guidelines  
✅ **Use Azure Key Vault** in production environments  
✅ **Enable Azure OpenAI logging and monitoring**  

## Dependencies

- Microsoft.SemanticKernel (1.57.0)
- Azure.AI.OpenAI (2.2.0-beta.4) 
- Microsoft.Extensions.AI (9.6.0)
- Microsoft.Extensions.DependencyInjection (9.0.6)
