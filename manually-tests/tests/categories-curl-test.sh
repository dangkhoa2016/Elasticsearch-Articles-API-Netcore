# ============================================================================
# Categories - Curl Test Commands
# ============================================================================
export BASE_URL="http://localhost:5000"
export TOKEN="your_jwt_token_here"

# ============================================================================
# CREATE
# ============================================================================

# 1. Create a new category
curl -s -X POST "$BASE_URL/api/categories" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Technology"}'

# 2. Create another category
curl -s -X POST "$BASE_URL/api/categories" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Science"}'

# 3. Create category without auth (expect 401)
curl -i -X POST "$BASE_URL/api/categories" \
  -H "Content-Type: application/json" \
  -d '{"title":"NoAuth Category"}'

# 4. Create category with validation errors (empty title)
curl -s -X POST "$BASE_URL/api/categories" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":""}'

# ============================================================================
# READ
# ============================================================================

# 5. Get all categories (default pagination)
curl -s -X GET "$BASE_URL/api/categories" \
  -H "Authorization: Bearer $TOKEN"

# 6. Get categories with pagination
curl -s -X GET "$BASE_URL/api/categories?skip=0&take=5" \
  -H "Authorization: Bearer $TOKEN"

# 7. Get categories with title filter
curl -s -X GET "$BASE_URL/api/categories?title=Tech" \
  -H "Authorization: Bearer $TOKEN"

# 8. Get a single category by ID (replace 1 with actual ID)
curl -s -X GET "$BASE_URL/api/categories/1" \
  -H "Authorization: Bearer $TOKEN"

# 9. Get a non-existent category (expect 404)
curl -s -X GET "$BASE_URL/api/categories/999999" \
  -H "Authorization: Bearer $TOKEN"

# 10. Get articles for a specific category
curl -s -X GET "$BASE_URL/api/categories/1/articles" \
  -H "Authorization: Bearer $TOKEN"

# 11. Get categories without auth (expect 401)
curl -i -X GET "$BASE_URL/api/categories"

# ============================================================================
# UPDATE
# ============================================================================

# 12. Update category with PUT (replace 1 with actual ID)
curl -s -X PUT "$BASE_URL/api/categories/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Tech & Science"}'

# 13. Update category with PATCH (replace 1 with actual ID)
curl -s -X PATCH "$BASE_URL/api/categories/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Information Technology"}'

# 14. Update category with validation errors
curl -s -X PUT "$BASE_URL/api/categories/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":""}'

# 15. Update non-existent category (expect 404)
curl -s -X PUT "$BASE_URL/api/categories/999999" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Ghost Category"}'

# ============================================================================
# DELETE
# ============================================================================

# 16. Delete category with DELETE method (replace 1 with actual ID)
curl -s -X DELETE "$BASE_URL/api/categories/1" \
  -H "Authorization: Bearer $TOKEN"

# 17. Delete category via GET /delete alternate route
curl -s -X GET "$BASE_URL/api/categories/1/delete" \
  -H "Authorization: Bearer $TOKEN"

# 18. Delete non-existent category (expect 404)
curl -s -X DELETE "$BASE_URL/api/categories/999999" \
  -H "Authorization: Bearer $TOKEN"

# ============================================================================
# PERFORMANCE / TIMING
# ============================================================================

# 19. Get categories with response time
curl -s -o /dev/null -w "HTTP Status: %{http_code}\nTime: %{time_total}s\n" \
  -X GET "$BASE_URL/api/categories" \
  -H "Authorization: Bearer $TOKEN"

# 20. Verbose request (see full headers)
curl -v -X GET "$BASE_URL/api/categories?skip=0&take=5" \
  -H "Authorization: Bearer $TOKEN"
