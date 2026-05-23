# ============================================================================
# Articles - Curl Test Commands
# ============================================================================
# Prerequisite: Set BASE_URL and TOKEN first
# ============================================================================
export BASE_URL="http://localhost:5000"
export TOKEN="your_jwt_token_here"

# ============================================================================
# CREATE
# ============================================================================

# 1. Create a new article
curl -s -X POST "$BASE_URL/api/articles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Test Article","content":"This is the full content of the test article.","abstract":"A short abstract.","shares":42}'

# 2. Create article with all fields
curl -s -X POST "$BASE_URL/api/articles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Complete Test Article","content":"Full content here.","abstract":"Short abstract.","shares":100,"published_on":"2026-05-20T00:00:00"}'

# 3. Create article with minimal fields
curl -s -X POST "$BASE_URL/api/articles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Minimal Article","content":"Content"}'

# 4. Create article without auth (expect 401)
curl -i -X POST "$BASE_URL/api/articles" \
  -H "Content-Type: application/json" \
  -d '{"title":"Unauthorized Article","content":"Content"}'

# 5. Create article with validation errors (empty title)
curl -i -X POST "$BASE_URL/api/articles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"","content":"","shares":-1}'

# ============================================================================
# READ
# ============================================================================

# 6. Get all articles (default pagination)
curl -s -X GET "$BASE_URL/api/articles" \
  -H "Authorization: Bearer $TOKEN"

# 7. Get articles with pagination
curl -s -X GET "$BASE_URL/api/articles?skip=0&take=5" \
  -H "Authorization: Bearer $TOKEN"

# 8. Get articles with title filter
curl -s -X GET "$BASE_URL/api/articles?title=Test" \
  -H "Authorization: Bearer $TOKEN"

# 9. Get articles with pagination and filter combined
curl -s -X GET "$BASE_URL/api/articles?skip=0&take=10&title=Test" \
  -H "Authorization: Bearer $TOKEN"

# 10. Get articles with large page size
curl -s -X GET "$BASE_URL/api/articles?skip=0&take=50" \
  -H "Authorization: Bearer $TOKEN"

# 11. Get articles - page 3 (skip first 10, page size 5)
curl -s -X GET "$BASE_URL/api/articles?skip=10&take=5" \
  -H "Authorization: Bearer $TOKEN"

# 12. Get articles - page 5 (skip first 20, page size 5)
curl -s -X GET "$BASE_URL/api/articles?skip=20&take=5" \
  -H "Authorization: Bearer $TOKEN"

# 13. Get articles - deep pagination (skip 100)
curl -s -X GET "$BASE_URL/api/articles?skip=100&take=10" \
  -H "Authorization: Bearer $TOKEN"

# 14. Get articles - negative skip (should reset to 0)
curl -s -X GET "$BASE_URL/api/articles?skip=-5&take=5" \
  -H "Authorization: Bearer $TOKEN"

# 15. Get articles - zero take (should use default page size)
curl -s -X GET "$BASE_URL/api/articles?skip=0&take=0" \
  -H "Authorization: Bearer $TOKEN"

# 16. Get articles - take exceeds max size (should cap to max)
curl -s -X GET "$BASE_URL/api/articles?skip=0&take=999" \
  -H "Authorization: Bearer $TOKEN"

# 17. Get articles without auth (expect 401)
curl -i -X GET "$BASE_URL/api/articles"

# 18. Get a single article by ID (replace 1 with actual ID)
curl -s -X GET "$BASE_URL/api/articles/1" \
  -H "Authorization: Bearer $TOKEN"

# 19. Get a non-existent article (expect 404)
curl -s -X GET "$BASE_URL/api/articles/999999" \
  -H "Authorization: Bearer $TOKEN"

# 20. Get article as indexed JSON from Elasticsearch
curl -s -X GET "$BASE_URL/api/articles/1/as_indexed_json" \
  -H "Authorization: Bearer $TOKEN"

# 21. Get comments for a specific article
curl -s -X GET "$BASE_URL/api/articles/10/comments" \
  -H "Authorization: Bearer $TOKEN"

# ============================================================================
# UPDATE
# ============================================================================

# 22. Update article with PUT (replace 1 with actual ID)
curl -s -X PUT "$BASE_URL/api/articles/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Updated Article Title","content":"Updated content.","abstract":"Updated abstract.","shares":200}'

# 23. Update article with PATCH (replace 1 with actual ID)
curl -s -X PATCH "$BASE_URL/api/articles/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Patched Article Title","content":"Patched content.","abstract":"Patched abstract.","shares":300}'

# 24. Update article with validation errors
curl -s -X PUT "$BASE_URL/api/articles/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"","content":"","shares":-1}'

# 25. Update non-existent article (expect 404)
curl -s -X PUT "$BASE_URL/api/articles/999999" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Update Non-existent","content":"Content"}'

# ============================================================================
# DELETE
# ============================================================================

# 26. Delete article with DELETE method (replace 1 with actual ID)
curl -s -X DELETE "$BASE_URL/api/articles/1" \
  -H "Authorization: Bearer $TOKEN"

# 27. Delete article via GET /delete alternate route
curl -i -X GET "$BASE_URL/api/articles/1/delete" \
  -H "Authorization: Bearer $TOKEN"

# 28. Delete non-existent article
curl -i -X DELETE "$BASE_URL/api/articles/999999" \
  -H "Authorization: Bearer $TOKEN"

# ============================================================================
# BULK IMPORT
# ============================================================================

# 29. Trigger bulk import of articles into Elasticsearch
curl -s -X POST "$BASE_URL/api/articles/import" \
  -H "Authorization: Bearer $TOKEN"

# ============================================================================
# PERFORMANCE / TIMING
# ============================================================================

# 30. Get articles with response time
curl -s -o /dev/null -w "HTTP Status: %{http_code}\nTime: %{time_total}s\nSize: %{size_download} bytes\n" \
  -X GET "$BASE_URL/api/articles" \
  -H "Authorization: Bearer $TOKEN"

# 31. Verbose request (see full headers)
curl -v -X GET "$BASE_URL/api/articles?skip=0&take=5" \
  -H "Authorization: Bearer $TOKEN"

# 32. Check if response is compressed (gzip/brotli)
curl -s -H "Accept-Encoding: gzip, deflate" \
  -D - -o /dev/null \
  -X GET "$BASE_URL/api/articles" \
  -H "Authorization: Bearer $TOKEN"
