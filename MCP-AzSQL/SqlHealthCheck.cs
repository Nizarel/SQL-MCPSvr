// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MCP_Azsql;

public class SqlHealthCheck : IHealthCheck
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlHealthCheck(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.GetOpenConnectionAsync();
            return HealthCheckResult.Healthy("SQL Server connection successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server connection failed", ex);
        }
    }
}
