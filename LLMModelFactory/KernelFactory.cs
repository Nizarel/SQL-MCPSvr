using Microsoft.SemanticKernel;

namespace LLMModelFactory
{
    public static class KernelFactory
    {
        private static readonly Config Config = new();
        
        /// <summary>
        /// Creates a Semantic Kernel instance configured for Azure OpenAI GPT-4o.
        /// </summary>
        public static Kernel CreateChatKernel()
        {
            return CreateAzureGpt4oKernel();
        }

        /// <summary>
        /// Creates a Semantic Kernel instance configured for Azure OpenAI with both chat and embedding services.
        /// </summary>
        public static Kernel CreateKernelWithEmbeddings()
        {
            ValidateAzureConfig();

            var builder = Kernel.CreateBuilder();
            
            // Add chat completion service
            builder.AddAzureOpenAIChatCompletion(
                Config.Gpt4oDeploymentId, 
                Config.Endpoint, 
                Config.ApiKey);
            
            // Add text embedding service
#pragma warning disable SKEXP0010
            builder.AddAzureOpenAIEmbeddingGenerator(
                Config.EmbeddingDeploymentId, 
                Config.Endpoint, 
                Config.ApiKey);
#pragma warning restore SKEXP0010
                
            return builder.Build();
        }

        /// <summary>
        /// Creates a kernel configured for Azure OpenAI GPT-4o.
        /// </summary>
        private static Kernel CreateAzureGpt4oKernel()
        {
            ValidateAzureConfig();

            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                Config.Gpt4oDeploymentId, 
                Config.Endpoint, 
                Config.ApiKey);
                
            return builder.Build();
        }

        /// <summary>
        /// Validates that all required Azure OpenAI configuration values are present.
        /// </summary>
        private static void ValidateAzureConfig()
        {
            Config.ValidateConfiguration();
        }
    }
}
