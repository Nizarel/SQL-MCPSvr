# Semantic Kernel Implementation Guide for Cement Business Agents

## Project Structure

```
CementBusinessAgents/
├── src/
│   ├── Agents/
│   │   ├── PricingAgent/
│   │   ├── CustomerServiceAgent/
│   │   ├── InventoryAgent/
│   │   ├── SalesAnalyticsAgent/
│   │   └── OrchestratorAgent/
│   ├── Plugins/
│   │   ├── McpServerPlugin/
│   │   ├── PricingPlugin/
│   │   └── BusinessLogicPlugin/
│   ├── Models/
│   ├── Services/
│   └── Configuration/
├── tests/
└── docs/
```

## Core Dependencies

```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.14.1" />
<PackageReference Include="Microsoft.SemanticKernel.Plugins.Core" Version="1.14.1-alpha" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.17" />
```

## 1. MCP Server Plugin Implementation

### McpServerPlugin.cs
```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

public class McpServerPlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _mcpServerUrl;

    public McpServerPlugin(HttpClient httpClient, string mcpServerUrl)
    {
        _httpClient = httpClient;
        _mcpServerUrl = mcpServerUrl;
    }

    [KernelFunction]
    [Description("Lists all tables available in the cement business database")]
    public async Task<string> ListTables()
    {
        var request = new
        {
            method = "mcp_ciment-mcp-se_ListTables",
            parameters = new { }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_mcpServerUrl}/call", request);
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }

    [KernelFunction]
    [Description("Describes the schema of a specific table")]
    public async Task<string> DescribeTable([Description("Name of the table")] string tableName)
    {
        var request = new
        {
            method = "mcp_ciment-mcp-se_DescribeTable",
            parameters = new { name = tableName }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_mcpServerUrl}/call", request);
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }

    [KernelFunction]
    [Description("Executes SQL queries to read data from the cement database")]
    public async Task<string> ReadData([Description("SQL query to execute")] string sql)
    {
        var request = new
        {
            method = "mcp_ciment-mcp-se_ReadData",
            parameters = new { sql = sql }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_mcpServerUrl}/call", request);
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }
}
```

## 2. Pricing Intelligence Agent

### PricingAgent.cs
```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;

public class PricingAgent
{
    private readonly Kernel _kernel;
    private readonly McpServerPlugin _mcpPlugin;

    public PricingAgent(Kernel kernel, McpServerPlugin mcpPlugin)
    {
        _kernel = kernel;
        _mcpPlugin = mcpPlugin;
    }

    [KernelFunction]
    [Description("Generate a detailed price quote for concrete products")]
    public async Task<PriceQuote> GenerateQuote(
        [Description("Type of concrete product (e.g., B20, B25, B30)")] string productType,
        [Description("Region name (e.g., GRAND CASABLANCA, RABAT)")] string region,
        [Description("Volume in cubic meters")] decimal volume,
        [Description("Include pumping service")] bool includePumping = false)
    {
        // SQL query to get product price
        var sql = $@"
            SELECT r.Region_Libelle, a.Designation, a.Tarif 
            FROM Region r 
            INNER JOIN Article a ON r.Region_Id = a.RegionId 
            WHERE r.Region_Libelle LIKE '%{region}%' 
            AND a.Designation LIKE '%{productType}%'
            ORDER BY a.Tarif";

        var productData = await _mcpPlugin.ReadData(sql);

        // Get pumping price if needed
        string pumpingData = null;
        if (includePumping)
        {
            var pumpingSql = $@"
                SELECT r.Region_Libelle, a.Designation, a.Tarif 
                FROM Region r 
                INNER JOIN Article a ON r.Region_Id = a.RegionId 
                WHERE r.Region_Libelle LIKE '%{region}%' 
                AND a.Designation = 'Pompage'";
            
            pumpingData = await _mcpPlugin.ReadData(pumpingSql);
        }

        // Use AI to process and format the quote
        var prompt = $@"
            Based on the following product data: {productData}
            {(pumpingData != null ? $"And pumping data: {pumpingData}" : "")}
            
            Generate a detailed price quote for:
            - Product: {productType}
            - Region: {region}
            - Volume: {volume} m³
            - Include pumping: {includePumping}
            
            Calculate:
            1. Unit price per m³
            2. Subtotal for concrete
            3. Pumping cost (if applicable)
            4. Total HT (excluding tax)
            5. TVA (20%)
            6. Total TTC (including tax)
            
            Format as a professional quote with clear breakdown.";

        var result = await _kernel.InvokePromptAsync(prompt);
        
        return new PriceQuote
        {
            ProductType = productType,
            Region = region,
            Volume = volume,
            QuoteText = result.ToString(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    [KernelFunction]
    [Description("Compare prices across different regions for a specific product")]
    public async Task<List<RegionalPrice>> CompareRegionalPrices(
        [Description("Product designation to compare")] string productDesignation)
    {
        var sql = $@"
            SELECT r.Region_Libelle, a.Designation, a.Tarif 
            FROM Region r 
            INNER JOIN Article a ON r.Region_Id = a.RegionId 
            WHERE a.Designation LIKE '%{productDesignation}%'
            ORDER BY a.Tarif ASC";

        var data = await _mcpPlugin.ReadData(sql);

        var prompt = $@"
            Based on this pricing data: {data}
            
            Create a list of regional prices for {productDesignation}.
            Show the price in each region, identify the cheapest and most expensive regions,
            and calculate the price variance across regions.
            
            Format as a clear comparison table.";

        var result = await _kernel.InvokePromptAsync(prompt);

        // Parse and return structured data
        return ParseRegionalPrices(result.ToString());
    }

    [KernelFunction]
    [Description("Find the most cost-effective region for a large order")]
    public async Task<OptimizedOrder> OptimizePricing(
        [Description("List of products and quantities needed")] List<OrderItem> items,
        [Description("Preferred region (optional)")] string preferredRegion = null)
    {
        var optimizationResults = new List<string>();

        foreach (var item in items)
        {
            var sql = $@"
                SELECT r.Region_Libelle, a.Designation, a.Tarif 
                FROM Region r 
                INNER JOIN Article a ON r.Region_Id = a.RegionId 
                WHERE a.Designation LIKE '%{item.ProductType}%'
                ORDER BY a.Tarif ASC";

            var data = await _mcpPlugin.ReadData(sql);
            optimizationResults.Add($"Product: {item.ProductType}, Quantity: {item.Quantity}, Data: {data}");
        }

        var prompt = $@"
            Optimize this order for best pricing:
            {string.Join("\n", optimizationResults)}
            
            Preferred region: {preferredRegion ?? "No preference"}
            
            Analyze:
            1. Best region for each product
            2. Overall best region considering all products
            3. Potential savings by splitting order across regions
            4. Logistics considerations
            5. Final recommendation
            
            Provide a clear optimization strategy.";

        var result = await _kernel.InvokePromptAsync(prompt);

        return new OptimizedOrder
        {
            Items = items,
            OptimizationStrategy = result.ToString(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private List<RegionalPrice> ParseRegionalPrices(string aiResponse)
    {
        // Implementation to parse AI response into structured data
        // This would use regex or JSON parsing depending on response format
        return new List<RegionalPrice>();
    }
}
```

## 3. Customer Service Agent

### CustomerServiceAgent.cs
```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;

public class CustomerServiceAgent
{
    private readonly Kernel _kernel;
    private readonly McpServerPlugin _mcpPlugin;
    private readonly PricingAgent _pricingAgent;

    public CustomerServiceAgent(Kernel kernel, McpServerPlugin mcpPlugin, PricingAgent pricingAgent)
    {
        _kernel = kernel;
        _mcpPlugin = mcpPlugin;
        _pricingAgent = pricingAgent;
    }

    [KernelFunction]
    [Description("Recommend the best concrete product for a specific application")]
    public async Task<ProductRecommendation> RecommendProduct(
        [Description("Construction application (e.g., foundation, slab, column)")] string application,
        [Description("Specific requirements (e.g., waterproof, high strength)")] string requirements,
        [Description("Project location")] string location)
    {
        // Get all available products in the region
        var sql = $@"
            SELECT r.Region_Libelle, a.Designation, a.Tarif, ca.Libelle as Category
            FROM Region r 
            INNER JOIN Article a ON r.Region_Id = a.RegionId 
            LEFT JOIN CategorieArticles ca ON a.CategorieId = ca.CategorieId
            WHERE r.Region_Libelle LIKE '%{location}%'
            ORDER BY a.Designation";

        var products = await _mcpPlugin.ReadData(sql);

        var prompt = $@"
            You are a concrete specialist helping customers choose the right product.
            
            Application: {application}
            Requirements: {requirements}
            Location: {location}
            
            Available products in region: {products}
            
            Based on the application and requirements, recommend:
            1. Primary product recommendation with technical justification
            2. Alternative options if primary is unavailable
            3. Technical specifications explanation
            4. Application guidelines
            5. Price estimate
            
            Consider factors like:
            - Compressive strength requirements
            - Environmental exposure (hydrofuge needs)
            - Workability requirements
            - Special applications (vertical, dallage, etc.)
            
            Provide practical, expert advice.";

        var result = await _kernel.InvokePromptAsync(prompt);

        return new ProductRecommendation
        {
            Application = application,
            Requirements = requirements,
            Location = location,
            Recommendation = result.ToString(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    [KernelFunction]
    [Description("Explain technical specifications of concrete products")]
    public async Task<TechnicalSpecs> ExplainSpecifications(
        [Description("Product code or designation")] string productCode)
    {
        var sql = $@"
            SELECT a.Designation, a.Tarif, ca.Libelle as Category, r.Region_Libelle
            FROM Article a 
            LEFT JOIN CategorieArticles ca ON a.CategorieId = ca.CategorieId
            LEFT JOIN Region r ON a.RegionId = r.Region_Id
            WHERE a.Designation LIKE '%{productCode}%'";

        var productInfo = await _mcpPlugin.ReadData(sql);

        var prompt = $@"
            Explain the technical specifications for this concrete product:
            Product: {productCode}
            Database info: {productInfo}
            
            Provide detailed explanation of:
            1. Compressive strength (B10, B15, B20, B25, B30, B35 meaning)
            2. Exposure class (X0, XCA1, XCA2, XM1 meaning)
            3. Consistency class (S1, S2, S3, S4 meaning)
            4. Special features (Hydrofuge, PLUS, PT, etc.)
            5. Typical applications
            6. Performance characteristics
            7. Installation guidelines
            
            Use technical accuracy but explain in understandable terms for construction professionals.";

        var result = await _kernel.InvokePromptAsync(prompt);

        return new TechnicalSpecs
        {
            ProductCode = productCode,
            Explanation = result.ToString(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    [KernelFunction]
    [Description("Check product availability in specific region and timeframe")]
    public async Task<AvailabilityStatus> CheckAvailability(
        [Description("Product name or code")] string product,
        [Description("Region name")] string region,
        [Description("Required delivery date")] DateTime requiredDate)
    {
        var sql = $@"
            SELECT r.Region_Libelle, a.Designation, a.Tarif
            FROM Region r 
            INNER JOIN Article a ON r.Region_Id = a.RegionId 
            WHERE r.Region_Libelle LIKE '%{region}%' 
            AND a.Designation LIKE '%{product}%'";

        var availability = await _mcpPlugin.ReadData(sql);

        var prompt = $@"
            Check availability for:
            Product: {product}
            Region: {region}
            Required date: {requiredDate:yyyy-MM-dd}
            
            Database results: {availability}
            
            Analyze:
            1. Is the product available in the region?
            2. Standard delivery timeframes
            3. Any potential constraints
            4. Alternative products if not available
            5. Recommendations for delivery planning
            
            Provide clear availability status and next steps.";

        var result = await _kernel.InvokePromptAsync(prompt);

        return new AvailabilityStatus
        {
            Product = product,
            Region = region,
            RequiredDate = requiredDate,
            Status = result.ToString(),
            CheckedAt = DateTime.UtcNow
        };
    }
}
```

## 4. Orchestrator Agent Setup

### OrchestratorAgent.cs
```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using System.ComponentModel;

public class OrchestratorAgent
{
    private readonly Kernel _kernel;
    private readonly PricingAgent _pricingAgent;
    private readonly CustomerServiceAgent _customerServiceAgent;
    private readonly InventoryAgent _inventoryAgent;
    private readonly SalesAnalyticsAgent _salesAgent;

    public OrchestratorAgent(
        Kernel kernel,
        PricingAgent pricingAgent,
        CustomerServiceAgent customerServiceAgent,
        InventoryAgent inventoryAgent,
        SalesAnalyticsAgent salesAgent)
    {
        _kernel = kernel;
        _pricingAgent = pricingAgent;
        _customerServiceAgent = customerServiceAgent;
        _inventoryAgent = inventoryAgent;
        _salesAgent = salesAgent;
    }

    [KernelFunction]
    [Description("Route customer queries to the appropriate specialist agent")]
    public async Task<AgentResponse> RouteToSpecialistAgent(
        [Description("Customer query or request")] string query,
        [Description("Additional context about the customer or situation")] string context = "")
    {
        var routingPrompt = $@"
            Analyze this customer query and determine which specialist agent should handle it:
            
            Query: {query}
            Context: {context}
            
            Available agents:
            1. PricingAgent - handles pricing, quotes, cost calculations
            2. CustomerServiceAgent - handles product recommendations, technical questions
            3. InventoryAgent - handles availability, delivery, supply chain
            4. SalesAnalyticsAgent - handles business analytics, trends, reports
            
            Respond with:
            1. Primary agent to handle the query
            2. Secondary agents that might need to be consulted
            3. Key parameters to extract from the query
            4. Suggested approach for handling the request
            
            Format as JSON with clear agent routing instructions.";

        var routing = await _kernel.InvokePromptAsync(routingPrompt);

        // Based on routing decision, call appropriate agent(s)
        // This is a simplified version - in practice, you'd parse the JSON response
        if (query.ToLower().Contains("price") || query.ToLower().Contains("quote") || query.ToLower().Contains("cost"))
        {
            // Route to pricing agent
            return new AgentResponse
            {
                HandlingAgent = "PricingAgent",
                Response = "Routing to pricing specialist...",
                Timestamp = DateTime.UtcNow
            };
        }
        else if (query.ToLower().Contains("recommend") || query.ToLower().Contains("what type") || query.ToLower().Contains("specification"))
        {
            // Route to customer service agent
            return new AgentResponse
            {
                HandlingAgent = "CustomerServiceAgent",
                Response = "Routing to product specialist...",
                Timestamp = DateTime.UtcNow
            };
        }
        
        return new AgentResponse
        {
            HandlingAgent = "General",
            Response = routing.ToString(),
            Timestamp = DateTime.UtcNow
        };
    }

    [KernelFunction]
    [Description("Execute complex business workflows involving multiple agents")]
    public async Task<WorkflowResult> ExecuteBusinessWorkflow(
        [Description("Type of workflow (quote_generation, product_analysis, etc.)")] string workflowType,
        [Description("Parameters for the workflow")] Dictionary<string, object> parameters)
    {
        switch (workflowType.ToLower())
        {
            case "comprehensive_quote":
                return await ExecuteComprehensiveQuoteWorkflow(parameters);
            case "product_analysis":
                return await ExecuteProductAnalysisWorkflow(parameters);
            case "regional_comparison":
                return await ExecuteRegionalComparisonWorkflow(parameters);
            default:
                throw new ArgumentException($"Unknown workflow type: {workflowType}");
        }
    }

    private async Task<WorkflowResult> ExecuteComprehensiveQuoteWorkflow(Dictionary<string, object> parameters)
    {
        var results = new List<string>();
        
        // Step 1: Get product recommendations
        if (parameters.ContainsKey("application"))
        {
            var recommendation = await _customerServiceAgent.RecommendProduct(
                parameters["application"].ToString(),
                parameters.GetValueOrDefault("requirements", "").ToString(),
                parameters["location"].ToString()
            );
            results.Add($"Product Recommendation: {recommendation.Recommendation}");
        }

        // Step 2: Check availability
        var availability = await _customerServiceAgent.CheckAvailability(
            parameters["product"].ToString(),
            parameters["region"].ToString(),
            DateTime.Parse(parameters.GetValueOrDefault("requiredDate", DateTime.Now.AddDays(7)).ToString())
        );
        results.Add($"Availability: {availability.Status}");

        // Step 3: Generate pricing
        var quote = await _pricingAgent.GenerateQuote(
            parameters["product"].ToString(),
            parameters["region"].ToString(),
            Convert.ToDecimal(parameters["volume"]),
            Convert.ToBoolean(parameters.GetValueOrDefault("pumping", false))
        );
        results.Add($"Quote: {quote.QuoteText}");

        return new WorkflowResult
        {
            WorkflowType = "comprehensive_quote",
            Results = results,
            Success = true,
            ExecutedAt = DateTime.UtcNow
        };
    }

    // Additional workflow methods...
}
```

## 5. Dependency Injection Setup

### Program.cs
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

var builder = Host.CreateApplicationBuilder(args);

// Configure Semantic Kernel
builder.Services.AddKernel();

// Add HttpClient for MCP server communication
builder.Services.AddHttpClient<McpServerPlugin>(client =>
{
    client.BaseAddress = new Uri("https://mcp-azsql-cmtm09.politewave-e57a379c.eastus2.azurecontainerapps.io/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register MCP Plugin
builder.Services.AddSingleton<McpServerPlugin>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    return new McpServerPlugin(httpClient, "https://mcp-azsql-cmtm09.politewave-e57a379c.eastus2.azurecontainerapps.io/mcp");
});

// Register agents
builder.Services.AddScoped<PricingAgent>();
builder.Services.AddScoped<CustomerServiceAgent>();
builder.Services.AddScoped<InventoryAgent>();
builder.Services.AddScoped<SalesAnalyticsAgent>();
builder.Services.AddScoped<OrchestratorAgent>();

// Configure OpenAI
builder.Services.AddSingleton<Kernel>(provider =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    
    kernelBuilder.AddAzureOpenAIChatCompletion(
        "gpt-4", // or your deployment name
        "https://your-openai-endpoint.openai.azure.com/",
        "your-api-key"
    );

    var kernel = kernelBuilder.Build();
    
    // Import plugins
    var mcpPlugin = provider.GetRequiredService<McpServerPlugin>();
    kernel.ImportPluginFromObject(mcpPlugin, "McpServer");
    
    return kernel;
});

var app = builder.Build();

// Example usage
var orchestrator = app.Services.GetRequiredService<OrchestratorAgent>();
var response = await orchestrator.RouteToSpecialistAgent(
    "I need a quote for 20 cubic meters of B25 concrete in Casablanca with pumping service"
);

Console.WriteLine(response.Response);

await app.RunAsync();
```

## Data Models

### Models/BusinessModels.cs
```csharp
public class PriceQuote
{
    public string ProductType { get; set; }
    public string Region { get; set; }
    public decimal Volume { get; set; }
    public string QuoteText { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class RegionalPrice
{
    public string Region { get; set; }
    public string Product { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "MAD";
}

public class OptimizedOrder
{
    public List<OrderItem> Items { get; set; }
    public string OptimizationStrategy { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class OrderItem
{
    public string ProductType { get; set; }
    public decimal Quantity { get; set; }
    public string RequiredRegion { get; set; }
}

public class ProductRecommendation
{
    public string Application { get; set; }
    public string Requirements { get; set; }
    public string Location { get; set; }
    public string Recommendation { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class TechnicalSpecs
{
    public string ProductCode { get; set; }
    public string Explanation { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class AvailabilityStatus
{
    public string Product { get; set; }
    public string Region { get; set; }
    public DateTime RequiredDate { get; set; }
    public string Status { get; set; }
    public DateTime CheckedAt { get; set; }
}

public class AgentResponse
{
    public string HandlingAgent { get; set; }
    public string Response { get; set; }
    public DateTime Timestamp { get; set; }
}

public class WorkflowResult
{
    public string WorkflowType { get; set; }
    public List<string> Results { get; set; }
    public bool Success { get; set; }
    public DateTime ExecutedAt { get; set; }
}
```

This implementation provides a solid foundation for your Agentic AI solution using Semantic Kernel, with direct integration to your MCP server and specialized agents for different business functions.
