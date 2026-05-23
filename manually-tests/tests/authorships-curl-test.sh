# ============================================================================
# Authorships - Curl Test Commands
# ============================================================================
export BASE_URL="http://localhost:5000"
export TOKEN="your_jwt_token_here"

# ============================================================================
# CREATE
# ============================================================================

# 1. Create a new authorship (replace articleId and authorId with actual IDs)
curl -s -X POST "$BASE_URL/api/authorships" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"article_id":1,"author_id":1}'

# 2. Create authorship without auth (expect 401)
curl -i -X POST "$BASE_URL/api/authorships" \
  -H "Content-Type: application/json" \
  -d '{"article_id":1,"author_id":1}'

# 3. Create duplicate authorship (expect 409 Conflict)
curl -s -X POST "$BASE_URL/api/authorships" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"article_id":1,"author_id":1}'

# ============================================================================
# READ
# ============================================================================

# 4. Get all authorships (default pagination)
curl -s -X GET "$BASE_URL/api/authorships" \
  -H "Authorization: Bearer $TOKEN"

# 5. Get authorships with pagination
curl -s -X GET "$BASE_URL/api/authorships?skip=0&take=5" \
  -H "Authorization: Bearer $TOKEN"

# 6. Get a single authorship by ID (replace 1 with actual ID)
curl -s -X GET "$BASE_URL/api/authorships/1" \
  -H "Authorization: Bearer $TOKEN"

# 7. Get a non-existent authorship (expect 404)
curl -s -X GET "$BASE_URL/api/authorships/999999" \
  -H "Authorization: Bearer $TOKEN"

# 8. Get authorships without auth (expect 401)
curl -i -X GET "$BASE_URL/api/authorships"

# ============================================================================
# UPDATE
# ============================================================================

# 9. Update authorship with PUT (replace 1 with actual ID)
curl -s -X PUT "$BASE_URL/api/authorships/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"article_id":1,"author_id":1}'

# 10. Update authorship with PATCH (replace 1 with actual ID)
curl -s -X PATCH "$BASE_URL/api/authorships/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"article_id":2,"author_id":1}'

# 11. Update non-existent authorship (expect 404)
curl -s -X PUT "$BASE_URL/api/authorships/999999" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"article_id":1,"author_id":1}'

# ============================================================================
# DELETE
# ============================================================================

# 12. Delete authorship with DELETE method (replace 1 with actual ID)
curl -s -X DELETE "$BASE_URL/api/authorships/1" \
  -H "Authorization: Bearer $TOKEN"

# 13. Delete authorship via GET /delete alternate route
curl -s -X GET "$BASE_URL/api/authorships/1/delete" \
  -H "Authorization: Bearer $TOKEN"

# 14. Delete non-existent authorship (expect 404)
curl -s -X DELETE "$BASE_URL/api/authorships/999999" \
  -H "Authorization: Bearer $TOKEN"

# ============================================================================
# PERFORMANCE / TIMING
# ============================================================================

# 15. Get authorships with response time
curl -s -o /dev/null -w "HTTP Status: %{http_code}\nTime: %{time_total}s\n" \
  -X GET "$BASE_URL/api/authorships" \
  -H "Authorization: Bearer $TOKEN"

# 16. Verbose request (see full headers)
curl -v -X GET "$BASE_URL/api/authorships?skip=0&take=5" \
  -H "Authorization: Bearer $TOKEN"
