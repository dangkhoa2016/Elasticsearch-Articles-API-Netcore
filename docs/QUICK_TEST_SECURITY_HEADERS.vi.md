# HƯỚNG DẪN KIỂM TRA NHANH - SECURITY HEADERS

> 🌐 Language / Ngôn ngữ: [English](QUICK_TEST_SECURITY_HEADERS.md) | **Tiếng Việt**

## Kiểm tra bằng curl

```bash
# Kiểm tra security headers
curl -I http://localhost:5000/api/articles

# Kiểm tra với verbose output
curl -v http://localhost:5000/api/articles
```

## Response Headers Mong Đợi

```
HTTP/1.1 200 OK
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'
Permissions-Policy: geolocation=(), microphone=(), camera=()
```

## Kiểm tra bằng Browser DevTools

1. Chạy ứng dụng: `dotnet run`
2. Mở trình duyệt và truy cập: `http://localhost:5000/api/articles`
3. Mở DevTools (F12)
4. Chuyển sang tab Network
5. Tải lại trang
6. Nhấp vào request
7. Xem Response Headers
8. Xác minh tất cả security headers đều có mặt

## Kiểm tra bằng Công cụ Quét Trực Tuyến

### SecurityHeaders.com
1. Deploy API lên server công cộng
2. Truy cập: https://securityheaders.com/
3. Nhập URL API
4. Nhấp "Scan"
5. Kết quả mong đợi: xếp hạng A+

### Mozilla Observatory
1. Truy cập: https://observatory.mozilla.org/
2. Nhập URL API
3. Nhấp "Scan Me"
4. Kết quả mong đợi: xếp hạng A

## Xác minh HTTPS Redirection (Production)

```bash
# Đặt môi trường là Production
export ASPNETCORE_ENVIRONMENT=Production

# Chạy ứng dụng
dotnet run

# Kiểm tra HTTP request (sẽ redirect sang HTTPS)
curl -I http://localhost:5000/api/articles

# Mong đợi: 307 Temporary Redirect hoặc 301 Moved Permanently
# Location: https://localhost:5001/api/articles
```

## Xác minh HSTS Header (Production)

```bash
# Trong production, HSTS header phải có mặt
curl -I https://your-domain.com/api/articles

# Header mong đợi:
# Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
```

## Kiểm tra bằng PowerShell

```powershell
# Kiểm tra security headers
Invoke-WebRequest -Uri "http://localhost:5000/api/articles" -Method HEAD | Select-Object -ExpandProperty Headers

# Kiểm tra với full response
$response = Invoke-WebRequest -Uri "http://localhost:5000/api/articles"
$response.Headers
```

## Checklist

- [ ] X-Content-Type-Options: nosniff
- [ ] X-Frame-Options: DENY
- [ ] X-XSS-Protection: 1; mode=block
- [ ] Referrer-Policy: strict-origin-when-cross-origin
- [ ] Content-Security-Policy có mặt
- [ ] Permissions-Policy có mặt
- [ ] Server header đã được xóa
- [ ] X-Powered-By header đã được xóa
- [ ] HTTPS redirection hoạt động (production)
- [ ] HSTS header có mặt (production)

## Các Vấn Đề Thường Gặp

### Headers không xuất hiện
- Xác minh middleware đứng đầu trong pipeline
- Kiểm tra middleware đã được đăng ký trong Startup.cs chưa
- Khởi động lại ứng dụng

### HTTPS redirection không hoạt động
- Xác minh môi trường là Production
- Kiểm tra UseHttpsRedirection() đã được gọi chưa
- Đảm bảo HTTPS đã được cấu hình

### HSTS không hoạt động
- Xác minh môi trường là Production
- Kiểm tra UseHsts() đã được gọi chưa
- Đảm bảo HSTS đã được cấu hình trong ConfigureServices
