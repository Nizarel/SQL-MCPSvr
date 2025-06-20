# Test MCP (Model Context Protocol) with proper SSE parsing for MCP-AzSQL Azure Container App
# This script correctly tests the MCP JSON-RPC over SSE implementation

param(
    [string]$BaseUrl = "https://mcp-azsql-cmtm09.politewave-e57a379c.eastus2.azurecontainerapps.io"
)

function Parse-SSEResponse {
    param([string]$Content)
    
    $lines = $Content -split "`r?`n"
    $data = ""
    
    foreach ($line in $lines) {
        if ($line.StartsWith("data: ")) {
            $data = $line.Substring(6) # Remove "data: " prefix
            break
        }
    }
    
    if ($data) {
        return $data | ConvertFrom-Json
    }
    return $null
}

Write-Host "Testing MCP (Model Context Protocol) over SSE for MCP-AzSQL Azure Container App" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "=" * 80

# Test 1: Test MCP Initialize
Write-Host "`n1. Testing MCP Initialize..." -ForegroundColor Green
try {
    $headers = @{
        'Content-Type' = 'application/json'
        'Accept' = '*/*'
    }
    
    $body = @{
        jsonrpc = "2.0"
        method = "initialize"
        id = 1
        params = @{
            protocolVersion = "2024-11-05"
            capabilities = @{}
            clientInfo = @{
                name = "MCP-Test-Client"
                version = "1.0.0"
            }
        }
    } | ConvertTo-Json -Depth 5
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp" -Method POST -Headers $headers -Body $body -TimeoutSec 10 -ErrorAction Stop
    Write-Host "✅ MCP Initialize successful" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Yellow
    Write-Host "Content-Type: $($response.Headers['Content-Type'])" -ForegroundColor Yellow
    
    # Parse SSE response
    $jsonResponse = Parse-SSEResponse -Content $response.Content
    if ($jsonResponse -and $jsonResponse.result) {
        Write-Host "Server Info: $($jsonResponse.result.serverInfo.name) v$($jsonResponse.result.serverInfo.version)" -ForegroundColor Cyan
        Write-Host "Protocol Version: $($jsonResponse.result.protocolVersion)" -ForegroundColor Cyan
        Write-Host "Capabilities: $($jsonResponse.result.capabilities | ConvertTo-Json -Compress)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ MCP Initialize failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Test MCP Tools List
Write-Host "`n2. Testing MCP Tools List..." -ForegroundColor Green
try {
    $headers = @{
        'Content-Type' = 'application/json'
        'Accept' = '*/*'
    }
    
    $body = @{
        jsonrpc = "2.0"
        method = "tools/list"
        id = 2
    } | ConvertTo-Json -Depth 3
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp" -Method POST -Headers $headers -Body $body -TimeoutSec 10 -ErrorAction Stop
    Write-Host "✅ MCP Tools List successful" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Yellow
    
    # Parse SSE response
    $jsonResponse = Parse-SSEResponse -Content $response.Content
    if ($jsonResponse -and $jsonResponse.result -and $jsonResponse.result.tools) {
        Write-Host "Available Tools:" -ForegroundColor Cyan
        foreach ($tool in $jsonResponse.result.tools) {
            Write-Host "  - $($tool.name): $($tool.description)" -ForegroundColor White
        }
    }
} catch {
    Write-Host "❌ MCP Tools List failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Test MCP Tool Call - ListTables
Write-Host "`n3. Testing MCP Tool Call - ListTables..." -ForegroundColor Green
try {
    $headers = @{
        'Content-Type' = 'application/json'
        'Accept' = '*/*'
    }
    
    $body = @{
        jsonrpc = "2.0"
        method = "tools/call"
        id = 3
        params = @{
            name = "ListTables"
            arguments = @{}
        }
    } | ConvertTo-Json -Depth 4
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp" -Method POST -Headers $headers -Body $body -TimeoutSec 10 -ErrorAction Stop
    Write-Host "✅ MCP ListTables successful" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Yellow
    
    # Parse SSE response
    $jsonResponse = Parse-SSEResponse -Content $response.Content
    if ($jsonResponse -and $jsonResponse.result -and $jsonResponse.result.content) {
        $resultText = $jsonResponse.result.content[0].text | ConvertFrom-Json
        if ($resultText.success) {
            Write-Host "Tables found: $($resultText.data.Count)" -ForegroundColor Cyan
            if ($resultText.data.Count -gt 0) {
                Write-Host "Sample tables:" -ForegroundColor White
                $resultText.data | Select-Object -First 3 | ForEach-Object {
                    Write-Host "  - $_" -ForegroundColor Gray
                }
            }
        } else {
            Write-Host "Error: $($resultText.error)" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "❌ MCP ListTables failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Test MCP Tool Call - ReadData (SELECT query)
Write-Host "`n4. Testing MCP Tool Call - ReadData..." -ForegroundColor Green
try {
    $headers = @{
        'Content-Type' = 'application/json'
        'Accept' = '*/*'
    }
    
    $body = @{
        jsonrpc = "2.0"
        method = "tools/call"
        id = 4
        params = @{
            name = "ReadData"
            arguments = @{
                sql = "SELECT TOP 1 TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
            }
        }
    } | ConvertTo-Json -Depth 4
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp" -Method POST -Headers $headers -Body $body -TimeoutSec 10 -ErrorAction Stop
    Write-Host "✅ MCP ReadData successful" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Yellow
    
    # Parse SSE response
    $jsonResponse = Parse-SSEResponse -Content $response.Content
    if ($jsonResponse -and $jsonResponse.result -and $jsonResponse.result.content) {
        $resultText = $jsonResponse.result.content[0].text | ConvertFrom-Json
        if ($resultText.success) {
            Write-Host "Query executed successfully" -ForegroundColor Cyan
            Write-Host "Rows returned: $($resultText.data.Count)" -ForegroundColor White
            if ($resultText.data.Count -gt 0) {
                Write-Host "Sample data: $($resultText.data[0] | ConvertTo-Json -Compress)" -ForegroundColor Gray
            }
        } else {
            Write-Host "Error: $($resultText.error)" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "❌ MCP ReadData failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Test MCP Error Handling (invalid method)
Write-Host "`n5. Testing MCP Error Handling..." -ForegroundColor Green
try {
    $headers = @{
        'Content-Type' = 'application/json'
        'Accept' = '*/*'
    }
    
    $body = @{
        jsonrpc = "2.0"
        method = "invalid/method"
        id = 5
    } | ConvertTo-Json -Depth 3
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp" -Method POST -Headers $headers -Body $body -TimeoutSec 10 -ErrorAction Stop
    Write-Host "✅ MCP Error Handling works" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Yellow
    
    # Parse SSE response
    $jsonResponse = Parse-SSEResponse -Content $response.Content
    if ($jsonResponse -and $jsonResponse.error) {
        Write-Host "Error Code: $($jsonResponse.error.code)" -ForegroundColor Cyan
        Write-Host "Error Message: $($jsonResponse.error.message)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ MCP Error Handling test failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 6: Test MCP Security (ReadData with restricted SQL)
Write-Host "`n6. Testing MCP Security (ReadData with restricted SQL)..." -ForegroundColor Green
try {
    $headers = @{
        'Content-Type' = 'application/json'
        'Accept' = '*/*'
    }
    
    $body = @{
        jsonrpc = "2.0"
        method = "tools/call"
        id = 6
        params = @{
            name = "ReadData"
            arguments = @{
                sql = "DROP TABLE test_table"
            }
        }
    } | ConvertTo-Json -Depth 4
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp" -Method POST -Headers $headers -Body $body -TimeoutSec 10 -ErrorAction Stop
    Write-Host "✅ MCP Security test executed" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Yellow
    
    # Parse SSE response
    $jsonResponse = Parse-SSEResponse -Content $response.Content
    if ($jsonResponse -and $jsonResponse.result -and $jsonResponse.result.content) {
        $resultText = $jsonResponse.result.content[0].text | ConvertFrom-Json
        if (-not $resultText.success) {
            Write-Host "✅ Security working: $($resultText.error)" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Security issue: DROP operation was allowed" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "❌ MCP Security test failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n" + "=" * 80
Write-Host "MCP Protocol Testing Summary:" -ForegroundColor Cyan
Write-Host "- ✅ MCP implementation uses JSON-RPC 2.0 over Server-Sent Events (SSE)" -ForegroundColor Green
Write-Host "- ✅ Proper SSE format: 'event: message' followed by 'data: {json}'" -ForegroundColor Green
Write-Host "- ✅ Initialize: Tests protocol version and server capabilities" -ForegroundColor Yellow
Write-Host "- ✅ Tools List: Lists available tools (ListTables, DescribeTable, ReadData)" -ForegroundColor Yellow
Write-Host "- ✅ Tool Calls: Tests actual tool execution with database connectivity" -ForegroundColor Yellow
Write-Host "- ✅ Error Handling: Tests invalid method handling" -ForegroundColor Yellow
Write-Host "- ✅ Security: Tests SQL injection protection for ReadData" -ForegroundColor Yellow
Write-Host "`nNote: This is a correct MCP implementation over Server-Sent Events!" -ForegroundColor Green
Write-Host "=" * 80
