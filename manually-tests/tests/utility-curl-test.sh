# ============================================================================
# Utility Endpoints - Curl Test Commands
# ============================================================================
export BASE_URL="http://localhost:5000"

# ============================================================================
# ROUTES
# ============================================================================

# 1. List all registered API routes
curl -s -X GET "$BASE_URL/routes"

# 2. List routes with verbose output
curl -v -X GET "$BASE_URL/routes"

# ============================================================================
# HOME / WELCOME
# ============================================================================

# 3. Welcome page
curl -s -X GET "$BASE_URL/"

# 4. Welcome page with verbose output
curl -v -X GET "$BASE_URL/"

# ============================================================================
# ERROR ENDPOINTS
# ============================================================================

# 5. 404 error page (JSON)
curl -s -X GET "$BASE_URL/404"

# 6. 500 error page (JSON)
curl -s -X GET "$BASE_URL/500"

# 7. 404 error page with verbose output
curl -v -X GET "$BASE_URL/404"

# ============================================================================
# STATIC FILES
# ============================================================================

# 8. Favicon PNG
curl -i -X GET "$BASE_URL/favicon.png"

# 9. Favicon ICO
curl -i -X GET "$BASE_URL/favicon.ico"

# 10. Check favicon response headers
curl -s -I -X GET "$BASE_URL/favicon.png"

# ============================================================================
# CORS
# ============================================================================

# 11. Test CORS preflight (OPTIONS) on a public endpoint
curl -i -X OPTIONS "$BASE_URL/api/health" \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: GET" \
  -H "Access-Control-Request-Headers: Authorization"

# 12. Test CORS with Origin header on a protected endpoint
curl -i -X GET "$BASE_URL/api/articles" \
  -H "Origin: http://localhost:3000"

# ============================================================================
# RATE LIMITING
# ============================================================================

# 13. Rapid requests to test rate limiting (watch for 429 responses)
for i in $(seq 1 20); do
  echo "Request $i:"
  curl -s -o /dev/null -w "  HTTP Status: %{http_code}\n" \
    -X GET "$BASE_URL/api/health"
done

# ============================================================================
# COMPRESSION
# ============================================================================

# 14. Request with gzip encoding
curl -s -H "Accept-Encoding: gzip, deflate" \
  -D - -o /dev/null \
  -X GET "$BASE_URL/api/articles" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# ============================================================================
# SECURITY HEADERS
# ============================================================================

# 15. Check response headers for security headers
curl -s -I -X GET "$BASE_URL/api/health"

# ============================================================================
# NON-EXISTENT ROUTE
# ============================================================================

# 16. Hit a route that does not exist (expect 404)
curl -s -X GET "$BASE_URL/api/nonexistent"

# 17. Hit a route that does not exist with POST (expect 405)
curl -i -X POST "$BASE_URL/api/nonexistent" \
  -H "Content-Type: application/json" \
  -d '{"test":"data"}'
