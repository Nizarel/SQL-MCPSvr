using LLMModelFactory;
using OpenAI.Chat;
using Microsoft.SemanticKernel;

namespace LLMModelFactory.Examples;

/// <summary>
/// Example usage of the Azure OpenAI LLM Model Factory.
/// Demonstrates both Semantic Kernel and direct Azure OpenAI SDK usage.
/// </summary>
public static class UsageExamples
{
    /// <summary>
    /// Example using Semantic Kernel for chat completion.
    /// Best for AI orchestration scenarios with function calling and complex workflows.
    /// </summary>
    public static async Task SemanticKernelChatExample()
    {
        try
        {
            // Create a chat kernel
            var kernel = KernelFactory.CreateChatKernel();
            
            // Simple prompt execution using Semantic Kernel
            var function = kernel.CreateFunctionFromPrompt("What are the benefits of renewable energy?");
            var result = await kernel.InvokeAsync(function);
            Console.WriteLine($"Semantic Kernel Response: {result}");
            
            // More complex kernel with embeddings
            var fullKernel = KernelFactory.CreateKernelWithEmbeddings();
            var embeddingFunction = fullKernel.CreateFunctionFromPrompt("Generate insights about sustainable technology");
            var embeddingResult = await fullKernel.InvokeAsync(embeddingFunction);
            Console.WriteLine($"Full Kernel Response: {embeddingResult}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Semantic Kernel Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Example using Azure OpenAI SDK directly for chat completion.
    /// Best for direct control and performance-critical scenarios.
    /// </summary>
    public static async Task DirectAzureOpenAIChatExample()
    {
        try
        {
            // Create chat client
            var chatClient = ExtensionsClientFactory.CreateChatClient();
            
            // Prepare messages
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful assistant that provides concise, accurate information."),
                new UserChatMessage("Explain the concept of machine learning in simple terms.")
            };

            // Configure options
            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 500,
                Temperature = 0.7f
            };

            // Get completion
            var completion = await chatClient.CompleteChatAsync(messages, options);
            Console.WriteLine($"Azure OpenAI Response: {completion.Value.Content[0].Text}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Azure OpenAI Chat Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Example using Azure OpenAI SDK for generating embeddings.
    /// Best for vector search, similarity matching, and semantic analysis.
    /// </summary>
    public static async Task DirectAzureOpenAIEmbeddingExample()
    {
        try
        {
            // Create embedding client
            var embeddingClient = ExtensionsClientFactory.CreateEmbeddingClient();
            
            // Text to embed
            var textsToEmbed = new[]
            {
                "Artificial intelligence is transforming industries.",
                "Machine learning algorithms can identify patterns in data.",
                "Natural language processing enables computers to understand human language."
            };

            // Generate embeddings
            var embeddings = await embeddingClient.GenerateEmbeddingsAsync(textsToEmbed);
              Console.WriteLine($"Generated {embeddings.Value.Count} embeddings:");
            for (int i = 0; i < embeddings.Value.Count; i++)
            {
                var embedding = embeddings.Value[i];
                var vectorData = embedding.ToFloats().ToArray(); // Convert to array for easier manipulation
                Console.WriteLine($"Text {i + 1}: {textsToEmbed[i]}");
                Console.WriteLine($"Embedding dimensions: {vectorData.Length}");
                Console.WriteLine($"First 5 values: [{string.Join(", ", vectorData.Take(5).Select(v => v.ToString("F4")))}]");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Azure OpenAI Embedding Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Example demonstrating configuration validation and error handling.
    /// Shows what happens when environment variables are not properly configured.
    /// </summary>
    public static void ConfigurationValidationExample()
    {
        try
        {
            // This will throw exceptions if environment variables are not set
            var chatClient = ExtensionsClientFactory.CreateChatClient();
            var embeddingClient = ExtensionsClientFactory.CreateEmbeddingClient();
            var kernel = KernelFactory.CreateChatKernel();
            
            Console.WriteLine("✅ All configurations are valid!");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ Configuration Error: {ex.Message}");
            Console.WriteLine("\nPlease ensure the following environment variables are set:");
            Console.WriteLine("- AZURE_OPENAI_ENDPOINT");
            Console.WriteLine("- AZURE_OPENAI_APIKEY");
            Console.WriteLine("- GPT4O_DEPLOYMENT_ID");
            Console.WriteLine("- EMBEDDING_DEPLOYMENT_ID");
        }
    }

    /// <summary>
    /// Comprehensive example showing different approaches for the same task.
    /// Compares Semantic Kernel vs direct Azure OpenAI SDK usage.
    /// </summary>
    public static async Task ComprehensiveComparisonExample()
    {
        const string userQuestion = "What are three key advantages of cloud computing?";
        
        Console.WriteLine($"Question: {userQuestion}\n");
        
        // Approach 1: Semantic Kernel
        Console.WriteLine("=== Semantic Kernel Approach ===");
        try
        {
            var kernel = KernelFactory.CreateChatKernel();
            var function = kernel.CreateFunctionFromPrompt(userQuestion);
            var skResult = await kernel.InvokeAsync(function);
            Console.WriteLine($"Result: {skResult}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine("\n" + new string('=', 50) + "\n");
        
        // Approach 2: Direct Azure OpenAI
        Console.WriteLine("=== Direct Azure OpenAI Approach ===");
        try
        {
            var chatClient = ExtensionsClientFactory.CreateChatClient();
            var messages = new List<ChatMessage>
            {
                new UserChatMessage(userQuestion)
            };
            
            var completion = await chatClient.CompleteChatAsync(messages);
            Console.WriteLine($"Result: {completion.Value.Content[0].Text}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
