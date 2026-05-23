# ============================================================================
# Authors - Curl Test Commands
# ============================================================================
export BASE_URL="http://localhost:5000"
export TOKEN="your_jwt_token_here"

# ============================================================================
# CREATE
# ============================================================================

# 1. Create a new author
curl -s -X POST "$BASE_URL/api/authors" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"first_name":"John","last_name":"Doe"}'

# 2. Create author with only first name
curl -s -X POST "$BASE_URL/api/authors" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"first_name":"Jane"}'

# 3. Create author without auth (expect 401)
curl -i -X POST "$BASE_URL/api/authors" \
  -H "Content-Type: application/json" \
  -d '{"first_name":"NoAuth","last_name":"User"}'

# 4. Create author with validation errors (empty first_name)
curl -s -X POST "$BASE_URL/api/authors" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"first_name":"","last_name":""}'

# ============================================================================
# READ
# ============================================================================

# 5. Get all authors (default pagination)
curl -s -X GET "$BASE_URL/api/authors" \
  -H "Authorization: Bearer $TOKEN"

# 6. Get authors with pagination
curl -s -X GET "$BASE_URL/api/authors?skip=0&take=5" \
  -H "Authorization: Bearer $TOKEN"

# 7. Get authors with name filter
curl -s -X GET "$BASE_URL/api/authors?name=John" \
  -H "Authorization: Bearer $TOKEN"

# 8. Get a single author by ID (replace 1 with actual ID)
curl -s -X GET "$BASE_URL/api/authors/1" \
  -H "Authorization: Bearer $TOKEN"

# 9. Get a non-existent author (expect 404)
curl -s -X GET "$BASE_URL/api/authors/999999" \
  -H "Authorization: Bearer $TOKEN"

# 10. Get articles for a specific author
curl -s -X GET "$BASE_URL/api/authors/1/articles" \
  -H "Authorization: Bearer $TOKEN"

# 11. Get authors without auth (expect 401)
curl -s -X GET "$BASE_URL/api/authors"

# ============================================================================
# UPDATE
# ============================================================================

# 12. Update author with PUT (replace 1 with actual ID)
curl -s -X PUT "$BASE_URL/api/authors/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"first_name":"Jane","last_name":"Smith"}'

# 13. Update author with PATCH (replace 1 with actual ID)
curl -s -X PATCH "$BASE_URL/api/authors/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"first_name":"Robert","last_name":"Johnson"}'

# 14. Update author with validation errors
curl -s -X PUT "$BASE_URL/api/authors/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"first_name":"","last_name":""}'

# 15. Update non-existent author (expect 404)
curl -s -X PUT "$BASE_URL/api/authors/999999" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"first_name":"Ghost","last_name":"Author"}'

# ============================================================================
# DELETE
# ============================================================================

# 16. Delete author with DELETE method (replace 1 with actual ID)
curl -s -X DELETE "$BASE_URL/api/authors/1" \
  -H "Authorization: Bearer $TOKEN"

# 17. Delete author via GET /delete alternate route
curl -s -X GET "$BASE_URL/api/authors/1/delete" \
  -H "Authorization: Bearer $TOKEN"

# 18. Delete non-existent author (expect 404)
curl -s -X DELETE "$BASE_URL/api/authors/999999" \
  -H "Authorization: Bearer $TOKEN"

# ============================================================================
# PERFORMANCE / TIMING
# ============================================================================

# 19. Get authors with response time
curl -s -o /dev/null -w "HTTP Status: %{http_code}\nTime: %{time_total}s\n" \
  -X GET "$BASE_URL/api/authors" \
  -H "Authorization: Bearer $TOKEN"

# 20. Verbose request (see full headers)
curl -v -X GET "$BASE_URL/api/authors?skip=0&take=5" \
  -H "Authorization: Bearer $TOKEN"
