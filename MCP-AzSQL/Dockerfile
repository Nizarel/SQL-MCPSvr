# Use the official .NET 9.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

# Create non-root user for security
RUN addgroup --system --gid 1001 mcpuser && \
    adduser --system --uid 1001 --gid 1001 mcpuser

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore dependencies
COPY ["MCP-Azsql.csproj", "./"]
RUN dotnet restore "MCP-Azsql.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "MCP-Azsql.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MCP-Azsql.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Set ownership to non-root user
RUN chown -R mcpuser:mcpuser /app
USER mcpuser

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Set environment variables for Container Apps
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "MCP-Azsql.dll"]
