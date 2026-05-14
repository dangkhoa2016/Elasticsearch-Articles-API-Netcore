# SECURITY HEADERS GUIDE

> 🌐 Language / Ngôn ngữ: **English** | [Tiếng Việt](SECURITY_HEADERS_GUIDE.vi.md)

## Overview

Security Headers have been implemented to protect the API from common attack types such as XSS, clickjacking, MIME sniffing, and other security vulnerabilities.

## Implemented Security Headers

### 1. X-Content-Type-Options: nosniff
**Purpose:** Prevent MIME type sniffing
**Value:** `nosniff`
**Protection against:** Browser automatically detecting content type and executing malicious code

### 2. X-Frame-Options: DENY
**Purpose:** Prevent clickjacking attacks
**Value:** `DENY`
**Protection against:** Website being embedded in iframe to deceive users

### 3. X-XSS-Protection: 1; mode=block
**Purpose:** Enable browser's XSS filter
**Value:** `1; mode=block`
**Protection against:** Cross-Site Scripting (XSS) attacks

### 4. Referrer-Policy: strict-origin-when-cross-origin
**Purpose:** Control referrer information being sent
**Value:** `strict-origin-when-cross-origin`
**Protection:** User privacy, prevents leaking sensitive URLs

### 5. Content-Security-Policy (CSP)
**Purpose:** Prevent XSS and injection attacks
**Value:**
```
default-src 'self'; 
script-src 'self'; 
style-src 'self' 'unsafe-inline'; 
img-src 'self' data: https:; 
font-src 'self'; 
connect-src 'self'; 
frame-ancestors 'none'
```
**Protection against:** XSS, code injection, unauthorized resource loading

### 6. Permissions-Policy
**Purpose:** Control browser features
**Value:** `geolocation=(), microphone=(), camera=()`
**Protection:** Disable unnecessary features

### 7. Server Header Removal
**Purpose:** Hide server information
**Headers removed:** `Server`, `X-Powered-By`
**Protection:** Information disclosure, fingerprinting

### 8. HSTS (HTTP Strict Transport Security)
**Purpose:** Force HTTPS connections
**Configuration:**
- MaxAge: 365 days
- IncludeSubDomains: true
- Preload: true
**Protection against:** Man-in-the-middle attacks, protocol downgrade attacks

## Implementation

### SecurityHeadersMiddleware.cs
```csharp
public class SecurityHeadersMiddleware
{
    // Adds all security headers to every response
    // Removes server identification headers
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
app.UseMiddleware<SecurityHeadersMiddleware>(); // First in pipeline
app.UseHttpsRedirection(); // Production only
app.UseHsts(); // Production only
```

## Testing Security Headers

### Using curl:
```bash
# Test security headers
curl -I http://localhost:5000/api/articles

# Expected output:
# X-Content-Type-Options: nosniff
# X-Frame-Options: DENY
# X-XSS-Protection: 1; mode=block
# Referrer-Policy: strict-origin-when-cross-origin
# Content-Security-Policy: default-src 'self'; ...
# Permissions-Policy: geolocation=(), microphone=(), camera=()
```

### Using Browser DevTools:
1. Open DevTools (F12)
2. Go to Network tab
3. Send request to API
4. Click on the request and view Response Headers
5. Verify all security headers are present

### Online Security Header Scanners:
- **SecurityHeaders.com**: https://securityheaders.com/
- **Mozilla Observatory**: https://observatory.mozilla.org/
- **OWASP ZAP**: Security scanning tool

## Environment-specific Behavior

### Development
- Security headers: ✅ Enabled
- HTTPS Redirection: ❌ Disabled (for easier testing with HTTP)
- HSTS: ❌ Disabled

### Production
- Security headers: ✅ Enabled
- HTTPS Redirection: ✅ Enabled
- HSTS: ✅ Enabled (365 days, includeSubDomains, preload)

## Customizing Security Headers

### Modifying CSP Policy:
If you need to load resources from external domains:
```csharp
context.Response.Headers.Append("Content-Security-Policy", 
    "default-src 'self'; script-src 'self' https://cdn.example.com; img-src 'self' https: data:");
```

### Modifying X-Frame-Options:
If you need to allow iframe from same origin:
```csharp
context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
```

### Adding Additional Security Headers:
```csharp
// Expect-CT: Certificate Transparency
context.Response.Headers.Append("Expect-CT", 
    "max-age=86400, enforce");

// Feature-Policy (older version of Permissions-Policy)
context.Response.Headers.Append("Feature-Policy", 
    "geolocation 'none'; microphone 'none'; camera 'none'");
```

## HSTS Preload

To submit domain to HSTS preload list:
1. Ensure HSTS header has `preload` directive
2. Redirect all HTTP traffic to HTTPS
3. Serve HSTS header on base domain and subdomains
4. Submit at: https://hstspreload.org/

**⚠️ Warning:** HSTS preload is a permanent decision, consider carefully!

## Content Security Policy (CSP) Tuning

### If inline styles are needed:
```csharp
style-src 'self' 'unsafe-inline'
```

### If inline scripts are needed (not recommended):
```csharp
script-src 'self' 'unsafe-inline'
```

### Using nonces for inline scripts (recommended):
```csharp
script-src 'self' 'nonce-{random-value}'
```

### CSP Report-Only Mode (testing):
```csharp
context.Response.Headers.Append("Content-Security-Policy-Report-Only", 
    "default-src 'self'; report-uri /api/csp-report");
```

## Common Issues & Solutions

### Issue 1: CSP blocking legitimate resources
**Solution:** Update CSP policy to allow specific domains
```csharp
img-src 'self' https://trusted-cdn.com data:
```

### Issue 2: CORS errors with CSP
**Solution:** Ensure `connect-src` includes API domains
```csharp
connect-src 'self' https://api.example.com
```

### Issue 3: HSTS causing issues in development
**Solution:** HSTS only enabled in production
```csharp
if (!env.IsDevelopment())
{
    app.UseHsts();
}
```

### Issue 4: Iframe embedding needed
**Solution:** Change X-Frame-Options
```csharp
X-Frame-Options: SAMEORIGIN  // Allow same origin
// or
X-Frame-Options: ALLOW-FROM https://trusted-site.com
```

## Security Headers Checklist

- [x] X-Content-Type-Options: nosniff
- [x] X-Frame-Options: DENY
- [x] X-XSS-Protection: 1; mode=block
- [x] Referrer-Policy: strict-origin-when-cross-origin
- [x] Content-Security-Policy
- [x] Permissions-Policy
- [x] Server header removal
- [x] HSTS (production only)
- [x] HTTPS redirection (production only)

## Best Practices

1. **Test thoroughly:** Test with real browsers and tools
2. **Monitor CSP violations:** Implement CSP reporting endpoint
3. **Start with Report-Only:** Test CSP with report-only mode first
4. **Regular updates:** Review and update security headers periodically
5. **Document exceptions:** Document why relaxed policies are needed
6. **Use HTTPS everywhere:** Especially in production
7. **Keep HSTS max-age reasonable:** Start with shorter duration

## Security Score

With current configuration, the API will achieve:
- **SecurityHeaders.com:** A+ rating
- **Mozilla Observatory:** A rating
- **OWASP ZAP:** Low risk

## Additional Resources

- [OWASP Secure Headers Project](https://owasp.org/www-project-secure-headers/)
- [MDN Web Security](https://developer.mozilla.org/en-US/docs/Web/Security)
- [Content Security Policy Reference](https://content-security-policy.com/)
- [HSTS Preload List](https://hstspreload.org/)

## Monitoring & Maintenance

### Regular checks:
1. Scan with SecurityHeaders.com monthly
2. Review CSP violations logs
3. Update headers when new threats emerge
4. Test after each deployment

### Metrics to track:
- Number of CSP violations
- HSTS header coverage
- Security scanner scores
- Browser compatibility issues
