# ============================================================================
# Auth - Curl Test Commands
# ============================================================================
# Base URL
# ============================================================================
export BASE_URL="http://localhost:5000"

# ============================================================================
# 1. Register a new user
# ============================================================================
curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"Username":"testuser","Password":"Test1234!","Email":"test@example.com"}'

# ============================================================================
# 2. Login and get JWT token
# ============================================================================
# Save the token for use in other requests
export TOKEN=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"Username":"testuser","Password":"Test1234!"}' \
  | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

echo "Token: $TOKEN"

# ============================================================================
# 3. Use token to access protected endpoint
# ============================================================================
curl -s -X GET "$BASE_URL/api/articles" \
  -H "Authorization: Bearer $TOKEN"

# ============================================================================
# 4. Access protected endpoint without token (expect 401)
# ============================================================================
curl -i -X GET "$BASE_URL/api/articles"

# ============================================================================
# 5. Access protected endpoint with invalid token (expect 401)
# ============================================================================
curl -i -X GET "$BASE_URL/api/articles" \
  -H "Authorization: Bearer invalid_token_here"

# ============================================================================
# 6. Register with validation errors (empty username)
# ============================================================================
curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"Username":"","Password":"Test1234!","Email":"test@example.com"}'

# ============================================================================
# 7. Register with weak password (too short)
# ============================================================================
curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"Username":"shortpw","Password":"123","Email":"test@example.com"}'

# ============================================================================
# 8. Login with wrong credentials
# ============================================================================
curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"Username":"testuser","Password":"WrongPassword!"}'
