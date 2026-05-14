# RATE LIMITING GUIDE

> 🌐 Language / Ngôn ngữ: **English** | [Tiếng Việt](RATE_LIMITING_GUIDE.vi.md)

## Overview

Rate Limiting has been implemented using the `AspNetCoreRateLimit` package to protect the API from abuse and DDoS attacks.

## Configuration

### Development Environment
- **General Limit**: 200 requests/minute
- Less restrictive to facilitate development and testing

### Production Environment
- **General Limit**: 30 requests/minute, 500 requests/hour
- **Login Endpoint**: 3 requests/minute
- **Register Endpoint**: 2 requests/minute
- More restrictive to protect production

### Default (appsettings.json)
- **General Limit**: 60 requests/minute, 1000 requests/hour
- **Login Endpoint**: 5 requests/minute
- **Register Endpoint**: 3 requests/minute
- **Search Endpoint**: 30 requests/minute

## How It Works

1. **IP-based Rate Limiting**: Limits based on the client's IP address
2. **Endpoint-specific Rules**: Sensitive endpoints have their own limits
3. **In-Memory Storage**: Uses MemoryCache to store counters (can be switched to Redis for distributed systems)

## Response When Exceeding Limits

When a client exceeds the rate limit, the API will return:
- **HTTP Status Code**: 429 (Too Many Requests)
- **Headers**:
  - `X-Rate-Limit-Limit`: Maximum limit
  - `X-Rate-Limit-Remaining`: Remaining requests
  - `X-Rate-Limit-Reset`: Time until counter reset

## Testing Rate Limiting

### Using curl:
```bash
# Test general endpoint
for i in {1..70}; do curl http://localhost:5000/api/articles; done

# Test login endpoint
for i in {1..10}; do curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"test"}'; done
```

### Using PowerShell:
```powershell
# Test general endpoint
1..70 | ForEach-Object { Invoke-WebRequest -Uri "http://localhost:5000/api/articles" }

# Test login endpoint
1..10 | ForEach-Object {
  Invoke-WebRequest -Uri "http://localhost:5000/api/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body '{"email":"test@test.com","password":"test"}'
}
```

## Customizing Rate Limiting

### Adding a new rule for a specific endpoint:
```json
{
  "Endpoint": "GET:/api/your-endpoint",
  "Period": "1m",
  "Limit": 10
}
```

### Whitelisting IP addresses:
```json
"IpRateLimitPolicies": {
  "IpRules": [
    {
      "Ip": "192.168.1.100",
      "Rules": [
        {
          "Endpoint": "*",
          "Period": "1m",
          "Limit": 1000
        }
      ]
    }
  ]
}
```

### Using Client ID instead of IP:
Change in `appsettings.json`:
```json
"ClientIdHeader": "X-ClientId"
```

Client sends header:
```
X-ClientId: your-unique-client-id
```

## Migrating to Redis (for distributed systems)

1. Install package:
```bash
dotnet add package AspNetCoreRateLimit.Redis
```

2. Update `Startup.cs`:
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration["Redis:ConnectionString"];
});
services.AddRedisRateLimiting();
```

3. Update `appsettings.json`:
```json
"Redis": {
  "ConnectionString": "localhost:6379"
}
```

## Best Practices

1. **Adjust limits based on usage patterns**: Monitor and adjust limits accordingly
2. **Different limits for authenticated vs anonymous users**: Can implement custom logic
3. **Log rate limit violations**: To detect potential attacks
4. **Inform users**: Return clear error messages when hitting rate limits
5. **Use Redis for production**: If you have multiple API instances

## Monitoring

Monitor the following metrics:
- Number of 429 responses
- Top IPs hitting rate limits
- Most frequently rate-limited endpoints
- Patterns of rate limit violations

## Troubleshooting

### Rate limit not working:
1. Check middleware order in `Configure()` - must be placed before routing
2. Verify configuration in appsettings.json
3. Check logs for any errors

### Rate limit too strict:
1. Increase limits in appsettings.json
2. Add whitelist for trusted IPs
3. Implement different tiers for different user types

### Performance issues:
1. Migrate from in-memory to Redis
2. Optimize rules - fewer rules = faster
3. Consider using distributed cache
