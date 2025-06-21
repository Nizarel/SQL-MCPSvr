using LLMModelFactory;

namespace LLMModelFactory.Demo;

/// <summary>
/// Simple demonstration application to test the configuration.
/// This shows how to validate configuration and use the factories.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Azure OpenAI LLM Model Factory Demo ===\n");

        try
        {
            // Test configuration
            await TestConfiguration();
            
            // Test factories
            await TestFactories();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }

        Console.WriteLine("\n✅ Demo completed successfully!");
    }

    private static async Task TestConfiguration()
    {
        Console.WriteLine("1. Testing Configuration...");
        
        var config = new Config();
        
        // Display configuration summary
        Console.WriteLine(config.GetConfigurationSummary());
        
        // Validate configuration
        try
        {
            config.ValidateConfiguration();
            Console.WriteLine("✅ Configuration validation passed!\n");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ Configuration validation failed:\n{ex.Message}\n");
            throw;
        }
    }

    private static async Task TestFactories()
    {
        Console.WriteLine("2. Testing Factory Creation...");

        try
        {
            // Test ExtensionsClientFactory
            Console.WriteLine("   Creating Azure OpenAI clients...");
            var chatClient = ExtensionsClientFactory.CreateChatClient();
            var embeddingClient = ExtensionsClientFactory.CreateEmbeddingClient();
            var azureClient = ExtensionsClientFactory.CreateAzureOpenAIClient();
            Console.WriteLine("   ✅ ExtensionsClientFactory - All clients created successfully");

            // Test KernelFactory
            Console.WriteLine("   Creating Semantic Kernel instances...");
            var chatKernel = KernelFactory.CreateChatKernel();
            var fullKernel = KernelFactory.CreateKernelWithEmbeddings();
            Console.WriteLine("   ✅ KernelFactory - All kernels created successfully");

            Console.WriteLine("✅ All factories working correctly!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Factory creation failed: {ex.Message}");
            throw;
        }
    }
}
