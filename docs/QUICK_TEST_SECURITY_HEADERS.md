# QUICK TEST GUIDE - SECURITY HEADERS

> 🌐 Language / Ngôn ngữ: **English** | [Tiếng Việt](QUICK_TEST_SECURITY_HEADERS.vi.md)

## Testing with curl

```bash
# Test security headers
curl -I http://localhost:5000/api/articles

# Test with verbose output
curl -v http://localhost:5000/api/articles
```

## Expected Response Headers

```
HTTP/1.1 200 OK
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'
Permissions-Policy: geolocation=(), microphone=(), camera=()
```

## Testing with Browser DevTools

1. Run application: `dotnet run`
2. Open browser and navigate to: `http://localhost:5000/api/articles`
3. Open DevTools (F12)
4. Go to Network tab
5. Refresh page
6. Click on the request
7. View Response Headers
8. Verify all security headers are present

## Testing with Online Scanners

### SecurityHeaders.com
1. Deploy API to public server
2. Visit: https://securityheaders.com/
3. Enter API URL
4. Click "Scan"
5. Expected: A+ rating

### Mozilla Observatory
1. Visit: https://observatory.mozilla.org/
2. Enter API URL
3. Click "Scan Me"
4. Expected: A rating

## Verify HTTPS Redirection (Production)

```bash
# Set environment to Production
export ASPNETCORE_ENVIRONMENT=Production

# Run application
dotnet run

# Test HTTP request (should redirect to HTTPS)
curl -I http://localhost:5000/api/articles

# Expected: 307 Temporary Redirect or 301 Moved Permanently
# Location: https://localhost:5001/api/articles
```

## Verify HSTS Header (Production)

```bash
# In production, HSTS header should be present
curl -I https://your-domain.com/api/articles

# Expected header:
# Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
```

## PowerShell Testing

```powershell
# Test security headers
Invoke-WebRequest -Uri "http://localhost:5000/api/articles" -Method HEAD | Select-Object -ExpandProperty Headers

# Test with full response
$response = Invoke-WebRequest -Uri "http://localhost:5000/api/articles"
$response.Headers
```

## Checklist

- [ ] X-Content-Type-Options: nosniff
- [ ] X-Frame-Options: DENY
- [ ] X-XSS-Protection: 1; mode=block
- [ ] Referrer-Policy: strict-origin-when-cross-origin
- [ ] Content-Security-Policy present
- [ ] Permissions-Policy present
- [ ] Server header removed
- [ ] X-Powered-By header removed
- [ ] HTTPS redirection works (production)
- [ ] HSTS header present (production)

## Common Issues

### Headers not appearing
- Verify middleware is first in pipeline
- Check if middleware is registered in Startup.cs
- Restart application

### HTTPS redirection not working
- Verify environment is Production
- Check if UseHttpsRedirection() is called
- Ensure HTTPS is configured

### HSTS not working
- Verify environment is Production
- Check if UseHsts() is called
- Ensure HSTS is configured in ConfigureServices
