// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Data.SqlClient;

namespace MCP_Azsql;

/// <summary>
/// Defines a factory interface for creating SQL database connections.
/// </summary>
public interface ISqlConnectionFactory
{
    Task<SqlConnection> GetOpenConnectionAsync();
}
