# MCP-MSSQL Azure Container App Server

A production-ready implementation of the Model Context Protocol (MCP) for Microsoft SQL Server, designed to run on Azure Container Apps. This server provides secure, scalable, and intelligent access to SQL Server databases through a standardized HTTP API.

## 🌟 Features

- **🔒 Secure Database Access**: Azure AD authentication with Managed Identity support
- **⚡ High Performance**: Optimized for Azure Container Apps with auto-scaling
- **🤖 AI-Ready**: Full MCP protocol implementation with RESTful HTTP API
- **📊 Enterprise-Grade**: Comprehensive logging, monitoring, and health checks
- **🐳 Container-Native**: Docker-ready with multi-stage builds
- **🔧 Developer-Friendly**: OpenAPI documentation and easy local development
- **🛠️ Complete MCP Tools**: All 7 essential database tools (ListTables, DescribeTable, ReadData, CreateTable, InsertData, UpdateData, DropTable)
- **🔄 Dual Transport**: Supports both stdio and HTTP transport for MCP protocol

## 🏗️ Architecture

```text
┌─────────────────┐    ┌──────────────────────┐    ┌─────────────────┐
│   AI Clients    │───▶│  Azure Container App │───▶│  Azure SQL DB   │
│   VS Code       │    │   (MCP-MSSQL API)    │    │                 │
│   Automation    │    │                      │    │                 │
└─────────────────┘    └──────────────────────┘    └─────────────────┘
                                │
                       ┌────────┴────────┐
                       │  Azure Monitor  │
                       │  App Insights   │
                       │  Log Analytics  │
                       └─────────────────┘
```

## 🚀 Quick Start

### Prerequisites

- Azure subscription
- Azure CLI or Azure Developer CLI (azd)
- Docker Desktop
- .NET 9.0 SDK
- PowerShell 7+

### Local Development

1. **Clone and navigate to the project**:
   ```powershell
   cd MCP-Azsql
   ```

2. **Set up your connection string**:
   ```powershell
   $env:CONNECTION_STRING = "Server=your-server;Database=your-db;..."
   ```

3. **Run locally**:
   ```powershell
   dotnet run
   ```

4. **Test the API**:
   ```powershell
   # Health check
   Invoke-RestMethod http://localhost:5000/health
     # List tables
   Invoke-RestMethod http://localhost:5000/api/mcp/tools/list-tables
   ```

### Azure Deployment

#### Option 1: Using Azure Developer CLI (Recommended)

```powershell
# Initialize and deploy
azd auth login
azd init
azd up
```

#### Option 2: Using PowerShell Script

```powershell
.\scripts\deploy.ps1 `
    -ResourceGroupName "rg-mcp-mssql" `
    -Location "East US 2" `
    -SqlConnectionString "your-connection-string"
```

#### Option 3: Manual Azure CLI

```powershell
# Create resource group
az group create --name rg-mcp-mssql --location "East US 2"

# Deploy infrastructure
az deployment group create `
    --resource-group rg-mcp-mssql `
    --template-file infra/main.bicep `
    --parameters "@infra/main.parameters.json"
```

## 📚 API Reference

### Health Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Detailed health check with database connectivity |
| `/health/live` | GET | Liveness probe for Container Apps |
| `/health/ready` | GET | Readiness probe for Container Apps |

### MCP Tools (Model Context Protocol)

| Endpoint | Method | Description | MCP Tool |
|----------|--------|-------------|----------|
| `/api/mcp/tools` | GET | List available MCP tools | - |
| `/api/mcp/tools/list-tables` | GET | List all tables | ListTables |
| `/api/mcp/tools/describe-table/{name}` | GET | Get table schema | DescribeTable |
| `/api/mcp/tools/read-data` | POST | Execute SELECT queries | ReadData |
| `/api/mcp/tools/create-table` | POST | Create table | CreateTable |
| `/api/mcp/tools/insert-data` | POST | Insert data | InsertData |
| `/api/mcp/tools/update-data` | POST | Update data | UpdateData |
| `/api/mcp/tools/drop-table` | POST | Drop table | DropTable |

### Example API Calls

#### List Tables
```powershell
$response = Invoke-RestMethod -Uri "https://your-app.azurecontainerapps.io/api/mcp/tools/list-tables" -Method GET
$response.Data
```

#### Execute Query
```powershell
$body = @{
    sql = "SELECT TOP 10 * FROM Users"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://your-app.azurecontainerapps.io/api/mcp/tools/read-data" `
    -Method POST -Body $body -ContentType "application/json"
```

#### Insert Data
```powershell
$body = @{
    sql = "INSERT INTO Users (Name, Email) VALUES ('John Doe', 'john@example.com')"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://your-app.azurecontainerapps.io/api/mcp/tools/insert-data" `
    -Method POST -Body $body -ContentType "application/json"
```

## 🔧 Configuration

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `CONNECTION_STRING` | SQL Server connection string | Yes |
| `AZURE_SQL_CONNECTIONSTRING` | Alternative connection string name | No |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights telemetry | No |
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | No |

### Connection String Formats

#### SQL Authentication
```
Server=tcp:your-server.database.windows.net,1433;Database=your-db;User ID=username;Password=password;Encrypt=True;
```

#### Azure AD Authentication (Recommended for Azure)
```
Server=tcp:your-server.database.windows.net,1433;Database=your-db;Authentication=Active Directory Default;Encrypt=True;
```

## 🔒 Security

### Azure AD Integration
- Uses `DefaultAzureCredential` for automatic authentication
- Supports Managed Identity in Azure environments
- Falls back to developer credentials locally

### API Security
- Input validation and SQL injection protection
- Operation restrictions (SELECT-only for queries)
- Structured error handling without data leakage

### Network Security
- HTTPS enforcement in production
- CORS configuration for cross-origin requests
- Container isolation in Azure Container Apps

## 📊 Monitoring & Observability

### Application Insights Integration
- Automatic telemetry collection
- Custom metrics and traces
- Performance monitoring

### Health Checks
- Database connectivity verification
- Startup and readiness probes
- Detailed health status reporting

### Logging
- Structured logging with correlation IDs
- Configurable log levels
- Azure Log Analytics integration

## 🐳 Container Configuration

### Multi-stage Docker Build
- Optimized for Azure Container Registry
- Non-root user for security
- Health check integration

### Container Apps Features
- Auto-scaling based on HTTP requests
- Zero-downtime deployments
- Integrated monitoring and logging

## 🛠️ Development

### Project Structure
```
MCP-Azsql/
├── Controllers/           # API controllers
├── infra/                # Azure infrastructure (Bicep)
├── scripts/              # Deployment scripts
├── Program.cs            # Application entry point
├── Dockerfile            # Container configuration
├── azure.yaml            # Azure Developer CLI config
└── appsettings.json      # Application configuration
```

### Building Locally
```powershell
# Restore packages
dotnet restore

# Build project
dotnet build

# Run tests (if available)
dotnet test

# Build Docker image
docker build -t mcp-mssql-server .
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Update documentation
6. Submit a pull request

## 📝 License

This project is licensed under the MIT License. See the LICENSE file for details.

## 🆘 Support

### Common Issues

#### Connection String Problems
- Ensure firewall rules allow Azure services
- Verify connection string format
- Check Azure AD permissions

#### Container Apps Deployment
- Verify resource quotas in target region
- Check container registry permissions
- Review Application Insights configuration

#### Health Check Failures
- Verify database connectivity
- Check application logs in Azure Portal
- Validate environment variables

### Getting Help

- Check the [Issues](../../issues) section
- Review Azure Container Apps documentation
- Contact the development team

## 🔄 Changelog

### v1.0.0
- Initial release with full MCP support
- Azure Container Apps deployment
- Comprehensive health checks
- Azure AD authentication
- Application Insights integration

---

**Built with ❤️ for the Azure ecosystem**

## 👨‍💻 Author

**Nizar EL Ouarti**  
Sr Cloud Solution Architect AI @ Microsoft
