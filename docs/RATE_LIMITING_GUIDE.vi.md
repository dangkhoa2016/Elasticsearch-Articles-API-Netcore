# HƯỚNG DẪN RATE LIMITING

> 🌐 Language / Ngôn ngữ: [English](RATE_LIMITING_GUIDE.md) | **Tiếng Việt**

## Tổng quan

Rate Limiting đã được implement sử dụng package `AspNetCoreRateLimit` để bảo vệ API khỏi abuse và DDoS attacks.

## Cấu hình

### Development Environment
- **General Limit**: 200 requests/phút
- Ít hạn chế hơn để thuận tiện cho việc development và testing

### Production Environment
- **General Limit**: 30 requests/phút, 500 requests/giờ
- **Login Endpoint**: 3 requests/phút
- **Register Endpoint**: 2 requests/phút
- Nghiêm ngặt hơn để bảo vệ production

### Default (appsettings.json)
- **General Limit**: 60 requests/phút, 1000 requests/giờ
- **Login Endpoint**: 5 requests/phút
- **Register Endpoint**: 3 requests/phút
- **Search Endpoint**: 30 requests/phút

## Cách hoạt động

1. **IP-based Rate Limiting**: Giới hạn dựa trên IP address của client
2. **Endpoint-specific Rules**: Các endpoint nhạy cảm có giới hạn riêng
3. **In-Memory Storage**: Sử dụng MemoryCache để lưu trữ counters (có thể chuyển sang Redis cho distributed systems)

## Response khi vượt giới hạn

Khi client vượt quá rate limit, API sẽ trả về:
- **HTTP Status Code**: 429 (Too Many Requests)
- **Headers**: 
  - `X-Rate-Limit-Limit`: Giới hạn tối đa
  - `X-Rate-Limit-Remaining`: Số requests còn lại
  - `X-Rate-Limit-Reset`: Thời gian reset counter

## Testing Rate Limiting

### Sử dụng curl:
```bash
# Test general endpoint
for i in {1..70}; do curl http://localhost:5000/api/articles; done

# Test login endpoint
for i in {1..10}; do curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"test"}'; done
```

### Sử dụng PowerShell:
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

## Tùy chỉnh Rate Limiting

### Thêm rule mới cho endpoint cụ thể:
```json
{
  "Endpoint": "GET:/api/your-endpoint",
  "Period": "1m",
  "Limit": 10
}
```

### Whitelist IP addresses:
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

### Sử dụng Client ID thay vì IP:
Thay đổi trong `appsettings.json`:
```json
"ClientIdHeader": "X-ClientId"
```

Client gửi header:
```
X-ClientId: your-unique-client-id
```

## Chuyển sang Redis (cho distributed systems)

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

1. **Adjust limits dựa trên usage patterns**: Monitor và điều chỉnh limits phù hợp
2. **Different limits cho authenticated vs anonymous users**: Có thể implement custom logic
3. **Log rate limit violations**: Để phát hiện potential attacks
4. **Inform users**: Trả về clear error messages khi hit rate limit
5. **Use Redis cho production**: Nếu có multiple instances của API

## Monitoring

Theo dõi các metrics sau:
- Số lượng 429 responses
- Top IPs hitting rate limits
- Endpoints bị rate limit nhiều nhất
- Pattern của rate limit violations

## Troubleshooting

### Rate limit không hoạt động:
1. Kiểm tra middleware order trong `Configure()` - phải đặt trước routing
2. Verify configuration trong appsettings.json
3. Check logs để xem có errors không

### Rate limit quá strict:
1. Tăng limits trong appsettings.json
2. Thêm whitelist cho trusted IPs
3. Implement different tiers cho different user types

### Performance issues:
1. Chuyển từ in-memory sang Redis
2. Optimize rules - ít rules hơn = faster
3. Consider using distributed cache
