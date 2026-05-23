# ============================================================================
# Comments - Curl Test Commands
# ============================================================================
export BASE_URL="http://localhost:5000"
export TOKEN="your_jwt_token_here"

# ============================================================================
# CREATE
# ============================================================================

# 1. Create a new comment (replace articleId with actual article ID)
curl -s -X POST "$BASE_URL/api/comments" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"body":"This is a test comment.","user":"testuser","article_id":1}'

# 2. Create comment with all fields
curl -s -X POST "$BASE_URL/api/comments" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"body":"Great article!","user":"john_doe","user_location":"New York","stars":5,"pick":true,"article_id":1}'

# 3. Create comment without auth (expect 401)
curl -i -X POST "$BASE_URL/api/comments" \
  -H "Content-Type: application/json" \
  -d '{"body":"Unauthorized comment.","user":"hacker","article_id":1}'

# 4. Create comment with validation errors (empty body, user)
curl -s -X POST "$BASE_URL/api/comments" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"body":"","user":"","article_id":0}'

# ============================================================================
# READ
# ============================================================================

# 5. Get all comments (default pagination)
curl -s -X GET "$BASE_URL/api/comments" \
  -H "Authorization: Bearer $TOKEN"

# 6. Get comments with pagination
curl -s -X GET "$BASE_URL/api/comments?skip=0&take=5" \
  -H "Authorization: Bearer $TOKEN"

# 7. Get a single comment by ID (replace 1 with actual ID)
curl -s -X GET "$BASE_URL/api/comments/1" \
  -H "Authorization: Bearer $TOKEN"

# 8. Get a non-existent comment (expect 404)
curl -s -X GET "$BASE_URL/api/comments/999999" \
  -H "Authorization: Bearer $TOKEN"

# 9. Get comments without auth (expect 401)
curl -i -X GET "$BASE_URL/api/comments"

# ============================================================================
# UPDATE
# ============================================================================

# 10. Update comment with PUT (replace 1 with actual ID)
curl -s -X PUT "$BASE_URL/api/comments/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"body":"Updated comment body.","user":"updateduser","article_id":1}'

# 11. Update comment with PATCH (replace 1 with actual ID)
curl -s -X PATCH "$BASE_URL/api/comments/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"body":"Patched comment.","user":"patcheduser","user_location":"London","stars":4,"pick":false,"article_id":1}'

# 12. Update comment with validation errors
curl -s -X PUT "$BASE_URL/api/comments/1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"body":"","user":"","article_id":0}'

# 13. Update non-existent comment (expect 404)
curl -s -X PUT "$BASE_URL/api/comments/999999" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"body":"Update ghost comment.","user":"user","article_id":1}'

# ============================================================================
# DELETE
# ============================================================================

# 14. Delete comment with DELETE method (replace 1 with actual ID)
curl -s -X DELETE "$BASE_URL/api/comments/1" \
  -H "Authorization: Bearer $TOKEN"

# 15. Delete comment via GET /delete alternate route
curl -s -X GET "$BASE_URL/api/comments/1/delete" \
  -H "Authorization: Bearer $TOKEN"

# 16. Delete non-existent comment (expect 404)
curl -s -X DELETE "$BASE_URL/api/comments/999999" \
  -H "Authorization: Bearer $TOKEN"

# ============================================================================
# PERFORMANCE / TIMING
# ============================================================================

# 17. Get comments with response time
curl -s -o /dev/null -w "HTTP Status: %{http_code}\nTime: %{time_total}s\n" \
  -X GET "$BASE_URL/api/comments" \
  -H "Authorization: Bearer $TOKEN"

# 18. Verbose request (see full headers)
curl -v -X GET "$BASE_URL/api/comments?skip=0&take=5" \
  -H "Authorization: Bearer $TOKEN"
