# HƯỚNG DẪN SECURITY HEADERS

> 🌐 Language / Ngôn ngữ: [English](SECURITY_HEADERS_GUIDE.md) | **Tiếng Việt**

## Tổng quan

Security Headers đã được triển khai để bảo vệ API khỏi các loại tấn công phổ biến như XSS, clickjacking, MIME sniffing và các lỗ hổng bảo mật khác.

## Các Security Headers Đã Triển Khai

### 1. X-Content-Type-Options: nosniff
**Mục đích:** Ngăn chặn MIME type sniffing
**Giá trị:** `nosniff`
**Bảo vệ khỏi:** Trình duyệt tự động phát hiện loại nội dung và thực thi mã độc

### 2. X-Frame-Options: DENY
**Mục đích:** Ngăn chặn tấn công clickjacking
**Giá trị:** `DENY`
**Bảo vệ khỏi:** Website bị nhúng vào iframe để lừa dối người dùng

### 3. X-XSS-Protection: 1; mode=block
**Mục đích:** Kích hoạt bộ lọc XSS của trình duyệt
**Giá trị:** `1; mode=block`
**Bảo vệ khỏi:** Tấn công Cross-Site Scripting (XSS)

### 4. Referrer-Policy: strict-origin-when-cross-origin
**Mục đích:** Kiểm soát thông tin referrer được gửi đi
**Giá trị:** `strict-origin-when-cross-origin`
**Bảo vệ:** Quyền riêng tư người dùng, ngăn rò rỉ URL nhạy cảm

### 5. Content-Security-Policy (CSP)
**Mục đích:** Ngăn chặn tấn công XSS và injection
**Giá trị:**
```
default-src 'self'; 
script-src 'self'; 
style-src 'self' 'unsafe-inline'; 
img-src 'self' data: https:; 
font-src 'self'; 
connect-src 'self'; 
frame-ancestors 'none'
```
**Bảo vệ khỏi:** XSS, chèn mã, tải tài nguyên trái phép

### 6. Permissions-Policy
**Mục đích:** Kiểm soát các tính năng trình duyệt
**Giá trị:** `geolocation=(), microphone=(), camera=()`
**Bảo vệ:** Vô hiệu hóa các tính năng không cần thiết

### 7. Xóa Server Header
**Mục đích:** Ẩn thông tin server
**Headers đã xóa:** `Server`, `X-Powered-By`
**Bảo vệ:** Rò rỉ thông tin, fingerprinting

### 8. HSTS (HTTP Strict Transport Security)
**Mục đích:** Bắt buộc kết nối HTTPS
**Cấu hình:**
- MaxAge: 365 ngày
- IncludeSubDomains: true
- Preload: true
**Bảo vệ khỏi:** Tấn công man-in-the-middle, tấn công downgrade giao thức

## Triển Khai

### SecurityHeadersMiddleware.cs
```csharp
public class SecurityHeadersMiddleware
{
    // Thêm tất cả security headers vào mọi response
    // Xóa các header nhận diện server
}
```

### Startup.cs
```csharp
// ConfigureServices
services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

// Configure
app.UseMiddleware<SecurityHeadersMiddleware>(); // Đầu tiên trong pipeline
app.UseHttpsRedirection(); // Chỉ production
app.UseHsts(); // Chỉ production
```

## Kiểm Tra Security Headers

### Sử dụng curl:
```bash
# Kiểm tra security headers
curl -I http://localhost:5000/api/articles

# Kết quả mong đợi:
# X-Content-Type-Options: nosniff
# X-Frame-Options: DENY
# X-XSS-Protection: 1; mode=block
# Referrer-Policy: strict-origin-when-cross-origin
# Content-Security-Policy: default-src 'self'; ...
# Permissions-Policy: geolocation=(), microphone=(), camera=()
```

### Sử dụng Browser DevTools:
1. Mở DevTools (F12)
2. Chuyển sang tab Network
3. Gửi request đến API
4. Nhấp vào request và xem Response Headers
5. Xác minh tất cả security headers đều có mặt

### Công cụ quét Security Headers trực tuyến:
- **SecurityHeaders.com**: https://securityheaders.com/
- **Mozilla Observatory**: https://observatory.mozilla.org/
- **OWASP ZAP**: Công cụ quét bảo mật

## Hành Vi Theo Môi Trường

### Development
- Security headers: ✅ Bật
- HTTPS Redirection: ❌ Tắt (để dễ kiểm tra với HTTP)
- HSTS: ❌ Tắt

### Production
- Security headers: ✅ Bật
- HTTPS Redirection: ✅ Bật
- HSTS: ✅ Bật (365 ngày, includeSubDomains, preload)

## Tùy Chỉnh Security Headers

### Sửa đổi CSP Policy:
Nếu cần tải tài nguyên từ domain bên ngoài:
```csharp
context.Response.Headers.Append("Content-Security-Policy", 
    "default-src 'self'; script-src 'self' https://cdn.example.com; img-src 'self' https: data:");
```

### Sửa đổi X-Frame-Options:
Nếu cần cho phép iframe từ cùng origin:
```csharp
context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
```

### Thêm Security Headers Bổ Sung:
```csharp
// Expect-CT: Certificate Transparency
context.Response.Headers.Append("Expect-CT", 
    "max-age=86400, enforce");

// Feature-Policy (phiên bản cũ của Permissions-Policy)
context.Response.Headers.Append("Feature-Policy", 
    "geolocation 'none'; microphone 'none'; camera 'none'");
```

## HSTS Preload

Để submit domain vào danh sách HSTS preload:
1. Đảm bảo HSTS header có directive `preload`
2. Redirect tất cả HTTP traffic sang HTTPS
3. Phục vụ HSTS header trên domain gốc và các subdomain
4. Submit tại: https://hstspreload.org/

**⚠️ Cảnh báo:** HSTS preload là quyết định vĩnh viễn, hãy cân nhắc kỹ!

## Tinh chỉnh Content Security Policy (CSP)

### Nếu cần inline styles:
```csharp
style-src 'self' 'unsafe-inline'
```

### Nếu cần inline scripts (không khuyến khích):
```csharp
script-src 'self' 'unsafe-inline'
```

### Sử dụng nonces cho inline scripts (khuyến khích):
```csharp
script-src 'self' 'nonce-{random-value}'
```

### Chế độ CSP Report-Only (kiểm tra):
```csharp
context.Response.Headers.Append("Content-Security-Policy-Report-Only", 
    "default-src 'self'; report-uri /api/csp-report");
```

## Các Vấn Đề Thường Gặp & Giải Pháp

### Vấn đề 1: CSP chặn tài nguyên hợp lệ
**Giải pháp:** Cập nhật CSP policy để cho phép các domain cụ thể
```csharp
img-src 'self' https://trusted-cdn.com data:
```

### Vấn đề 2: Lỗi CORS với CSP
**Giải pháp:** Đảm bảo `connect-src` bao gồm các domain API
```csharp
connect-src 'self' https://api.example.com
```

### Vấn đề 3: HSTS gây vấn đề trong development
**Giải pháp:** HSTS chỉ bật trong production
```csharp
if (!env.IsDevelopment())
{
    app.UseHsts();
}
```

### Vấn đề 4: Cần nhúng iframe
**Giải pháp:** Thay đổi X-Frame-Options
```csharp
X-Frame-Options: SAMEORIGIN  // Cho phép cùng origin
// hoặc
X-Frame-Options: ALLOW-FROM https://trusted-site.com
```

## Checklist Security Headers

- [x] X-Content-Type-Options: nosniff
- [x] X-Frame-Options: DENY
- [x] X-XSS-Protection: 1; mode=block
- [x] Referrer-Policy: strict-origin-when-cross-origin
- [x] Content-Security-Policy
- [x] Permissions-Policy
- [x] Xóa server header
- [x] HSTS (chỉ production)
- [x] HTTPS redirection (chỉ production)

## Best Practices

1. **Kiểm tra kỹ lưỡng:** Test với trình duyệt thật và công cụ
2. **Giám sát vi phạm CSP:** Triển khai endpoint báo cáo CSP
3. **Bắt đầu với Report-Only:** Kiểm tra CSP ở chế độ report-only trước
4. **Cập nhật định kỳ:** Xem xét và cập nhật security headers theo định kỳ
5. **Tài liệu hóa ngoại lệ:** Ghi lý do tại sao cần nới lỏng policy
6. **Sử HTTPS mọi nơi:** Đặc biệt trong production
7. **Giữ HSTS max-age hợp lý:** Bắt đầu với thời gian ngắn hơn

## Điểm Bảo Mật

Với cấu hình hiện tại, API sẽ đạt:
- **SecurityHeaders.com:** xếp hạng A+
- **Mozilla Observatory:** xếp hạng A
- **OWASP ZAP:** rủi ro thấp

## Tài Nguyên Bổ Sung

- [OWASP Secure Headers Project](https://owasp.org/www-project-secure-headers/)
- [MDN Web Security](https://developer.mozilla.org/en-US/docs/Web/Security)
- [Content Security Policy Reference](https://content-security-policy.com/)
- [HSTS Preload List](https://hstspreload.org/)

## Giám Sát & Bảo Trì

### Kiểm tra định kỳ:
1. Quét với SecurityHeaders.com hàng tháng
2. Xem xét log vi phạm CSP
3. Cập nhật headers khi có mối đe dọa mới
4. Kiểm tra sau mỗi lần deploy

### Metrics cần theo dõi:
- Số lượng vi phạm CSP
- Phạm vi phủ HSTS header
- Điểm quét bảo mật
- Vấn đề tương thích trình duyệt
