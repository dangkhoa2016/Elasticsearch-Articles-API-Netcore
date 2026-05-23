#!/bin/bash
# ============================================================================
# Elasticsearch Articles API - Manual Test Script
# ============================================================================
# Usage: chmod +x test-api.sh && ./test-api.sh [BASE_URL]
# Example: ./test-api.sh http://localhost:5000
#
# This script tests all API endpoints using curl.
# It is organized into sections:
#   Section 1: Health Checks
#   Section 2: Authentication (Register/Login)
#   Section 3: Authors CRUD
#   Section 4: Categories CRUD
#   Section 5: Articles CRUD + Search + Import
#   Section 6: Comments CRUD
#   Section 7: Authorships CRUD
#   Section 8: DELETE Operations (Cleanup)
#   Section 9: Utility Endpoints (routes, errors)
#   Section 10: Validation & Edge Cases
#
# All comments are written in English as requested.
# ============================================================================

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
BASE_URL="${1:-http://localhost:5000}"
CONTENT_TYPE="application/json"
PASS_COLOR="\033[32m"
FAIL_COLOR="\033[31m"
INFO_COLOR="\033[36m"
RESET_COLOR="\033[0m"
SEPARATOR="============================================================"

# ---------------------------------------------------------------------------
# Helper Functions
# ---------------------------------------------------------------------------
print_section() {
    echo ""
    echo -e "${SEPARATOR}"
    echo -e "${INFO_COLOR} $1${RESET_COLOR}"
    echo -e "${SEPARATOR}"
}

print_test() {
    echo ""
    echo -e "${INFO_COLOR}[TEST]${RESET_COLOR} $1"
    echo "  Method: $2"
    echo "  URL: $3"
}

print_response() {
    local http_code="$1"
    local body="$2"
    if [[ "$http_code" -ge 200 && "$http_code" -lt 300 ]]; then
        echo -e "  Status: ${PASS_COLOR}${http_code}${RESET_COLOR}"
    else
        echo -e "  Status: ${FAIL_COLOR}${http_code}${RESET_COLOR}"
    fi
    if [[ -n "$body" ]]; then
        echo "  Response: ${body:0:200}"
    fi
}

# Perform a curl request and return response + HTTP status code
# Usage: do_request "METHOD" "URL" "DATA" "HEADERS..."
do_request() {
    local method="$1"
    local url="$2"
    local data="$3"
    shift 3
    local headers=("$@")

    local curl_args=("-s" "-w" "\n%{http_code}" "-X" "$method" "-H" "Content-Type: $CONTENT_TYPE")
    for h in "${headers[@]}"; do
        curl_args+=("-H" "$h")
    done
    if [[ -n "$data" ]]; then
        curl_args+=("-d" "$data")
    fi
    curl_args+=("$url")

    local response
    response=$(curl "${curl_args[@]}")

    # Last line is HTTP status code
    local http_code
    http_code=$(echo "$response" | tail -n1)
    # Everything else is body
    local body
    body=$(echo "$response" | sed '$d')

    print_response "$http_code" "$body"
}

# ---------------------------------------------------------------------------
# Global variables to store JWT token and created entity IDs
# ---------------------------------------------------------------------------
TOKEN=""
ARTICLE_ID=""
AUTHOR_ID=""
CATEGORY_ID=""
COMMENT_ID=""
AUTHORSHIP_ID=""

# ============================================================================
# SECTION 1: Health Checks (Public - No Auth Required)
# ============================================================================
print_section "SECTION 1: Health Checks"

# 1.1 Basic Health Check
print_test "Basic Health Check" "GET" "${BASE_URL}/api/health"
do_request "GET" "${BASE_URL}/api/health" ""

# 1.2 Detailed Health Check
print_test "Detailed Health Check" "GET" "${BASE_URL}/api/health/detailed"
do_request "GET" "${BASE_URL}/api/health/detailed" ""

# 1.3 Database Health Check
print_test "Database Health Check" "GET" "${BASE_URL}/api/health/database"
do_request "GET" "${BASE_URL}/api/health/database" ""

# 1.4 Elasticsearch Health Check
print_test "Elasticsearch Health Check" "GET" "${BASE_URL}/api/health/elasticsearch"
do_request "GET" "${BASE_URL}/api/health/elasticsearch" ""

# ============================================================================
# SECTION 2: Authentication (Public - Register & Login)
# ============================================================================
print_section "SECTION 2: Authentication"

# 2.1 Register a new user with a fixed username to avoid race condition
FIXED_USER="testuser_$(date +%s)"
print_test "Register new user: $FIXED_USER" "POST" "${BASE_URL}/api/auth/register"
REG_DATA='{"username":"'$FIXED_USER'","password":"Test1234!","email":"test@example.com"}'
REG_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/auth/register" \
    -H "Content-Type: $CONTENT_TYPE" \
    -d "$REG_DATA")
echo "  Register response: $REG_RESPONSE"

# 2.2 Login to get JWT token
print_test "Login as: $FIXED_USER" "POST" "${BASE_URL}/api/auth/login"
LOGIN_DATA='{"username":"'$FIXED_USER'","password":"Test1234!"}'
AUTH_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/auth/login" \
    -H "Content-Type: $CONTENT_TYPE" \
    -d "$LOGIN_DATA")
echo "  Auth response: $AUTH_RESPONSE"

# Extract JWT token from response (handle both "token":"value" and "token": "value")
TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"token"[[:space:]]*:[[:space:]]*"[^"]*"' | head -1 | sed 's/.*"token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/')
if [[ -z "$TOKEN" ]]; then
    echo -e "${FAIL_COLOR}[ERROR]${RESET_COLOR} Failed to extract JWT token. Authentication may have failed."
    echo "  Make sure the API is running and registration/login works."
    exit 1
fi
echo -e "${PASS_COLOR}[OK]${RESET_COLOR} JWT Token obtained: ${TOKEN:0:20}..."
AUTH_HEADER="Authorization: Bearer $TOKEN"

# 2.3 Test access without token (should return 401)
print_test "Access protected endpoint without token (expect 401)" "GET" "${BASE_URL}/api/articles"
do_request "GET" "${BASE_URL}/api/articles" ""

# ============================================================================
# SECTION 3: Authors CRUD (create authors first for articles)
# ============================================================================
print_section "SECTION 3: Authors CRUD"

# 3.1 Create Author
print_test "Create Author" "POST" "${BASE_URL}/api/authors"
AUTHOR_DATA='{"first_name":"John","last_name":"Doe"}'
AUTHOR_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/authors" \
    -H "Content-Type: $CONTENT_TYPE" \
    -H "$AUTH_HEADER" \
    -d "$AUTHOR_DATA")
AUTHOR_ID=$(echo "$AUTHOR_RESPONSE" | grep -o '"id"[[:space:]]*:[[:space:]]*[0-9]*' | head -1 | grep -o '[0-9]*$')
echo "  Author response: $AUTHOR_RESPONSE"
echo "  Author ID: $AUTHOR_ID"

# 3.2 Get Authors List
print_test "Get Authors List" "GET" "${BASE_URL}/api/authors"
do_request "GET" "${BASE_URL}/api/authors" "" "$AUTH_HEADER"

# 3.3 Get Authors with Filter
print_test "Get Authors with Filter (name=John)" "GET" "${BASE_URL}/api/authors?name=John"
do_request "GET" "${BASE_URL}/api/authors?name=John" "" "$AUTH_HEADER"

# 3.4 Get Single Author
print_test "Get Single Author (ID=$AUTHOR_ID)" "GET" "${BASE_URL}/api/authors/$AUTHOR_ID"
do_request "GET" "${BASE_URL}/api/authors/$AUTHOR_ID" "" "$AUTH_HEADER"

# 3.5 Update Author (PUT)
print_test "Update Author (PUT)" "PUT" "${BASE_URL}/api/authors/$AUTHOR_ID"
UPDATE_AUTHOR_DATA='{"first_name":"Jane","last_name":"Doe"}'
do_request "PUT" "${BASE_URL}/api/authors/$AUTHOR_ID" "$UPDATE_AUTHOR_DATA" "$AUTH_HEADER"

# 3.6 Update Author (PATCH)
print_test "Update Author (PATCH)" "PATCH" "${BASE_URL}/api/authors/$AUTHOR_ID"
PATCH_AUTHOR_DATA='{"first_name":"John","last_name":"Smith"}'
do_request "PATCH" "${BASE_URL}/api/authors/$AUTHOR_ID" "$PATCH_AUTHOR_DATA" "$AUTH_HEADER"

# 3.7 Get Author Articles
print_test "Get Articles for Author" "GET" "${BASE_URL}/api/authors/$AUTHOR_ID/articles"
do_request "GET" "${BASE_URL}/api/authors/$AUTHOR_ID/articles" "" "$AUTH_HEADER"

# ============================================================================
# SECTION 4: Categories CRUD
# ============================================================================
print_section "SECTION 4: Categories CRUD"

# 4.1 Create Category
print_test "Create Category" "POST" "${BASE_URL}/api/categories"
CATEGORY_DATA='{"title":"Technology"}'
CATEGORY_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/categories" \
    -H "Content-Type: $CONTENT_TYPE" \
    -H "$AUTH_HEADER" \
    -d "$CATEGORY_DATA")
CATEGORY_ID=$(echo "$CATEGORY_RESPONSE" | grep -o '"id"[[:space:]]*:[[:space:]]*[0-9]*' | head -1 | grep -o '[0-9]*$')
echo "  Category response: $CATEGORY_RESPONSE"
echo "  Category ID: $CATEGORY_ID"

# 4.2 Get Categories List
print_test "Get Categories List" "GET" "${BASE_URL}/api/categories"
do_request "GET" "${BASE_URL}/api/categories" "" "$AUTH_HEADER"

# 4.3 Get Categories with Filter
print_test "Get Categories with Filter (title=Tech)" "GET" "${BASE_URL}/api/categories?title=Tech"
do_request "GET" "${BASE_URL}/api/categories?title=Tech" "" "$AUTH_HEADER"

# 4.4 Get Single Category
print_test "Get Single Category (ID=$CATEGORY_ID)" "GET" "${BASE_URL}/api/categories/$CATEGORY_ID"
do_request "GET" "${BASE_URL}/api/categories/$CATEGORY_ID" "" "$AUTH_HEADER"

# 4.5 Update Category (PUT)
print_test "Update Category (PUT)" "PUT" "${BASE_URL}/api/categories/$CATEGORY_ID"
UPDATE_CATEGORY_DATA='{"title":"Tech & Science"}'
do_request "PUT" "${BASE_URL}/api/categories/$CATEGORY_ID" "$UPDATE_CATEGORY_DATA" "$AUTH_HEADER"

# 4.6 Update Category (PATCH)
print_test "Update Category (PATCH)" "PATCH" "${BASE_URL}/api/categories/$CATEGORY_ID"
PATCH_CATEGORY_DATA='{"title":"Technology"}'
do_request "PATCH" "${BASE_URL}/api/categories/$CATEGORY_ID" "$PATCH_CATEGORY_DATA" "$AUTH_HEADER"

# 4.7 Get Category Articles
print_test "Get Articles for Category" "GET" "${BASE_URL}/api/categories/$CATEGORY_ID/articles"
do_request "GET" "${BASE_URL}/api/categories/$CATEGORY_ID/articles" "" "$AUTH_HEADER"

# ============================================================================
# SECTION 5: Articles CRUD + Search + Import
# ============================================================================
print_section "SECTION 5: Articles CRUD + Search + Import"

# 5.1 Create Article
print_test "Create Article" "POST" "${BASE_URL}/api/articles"
ARTICLE_DATA='{
    "title":"Test Article About API Testing",
    "content":"This is the full content of the test article.",
    "abstract":"A short abstract for testing purposes.",
    "shares":42
}'
ARTICLE_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/articles" \
    -H "Content-Type: $CONTENT_TYPE" \
    -H "$AUTH_HEADER" \
    -d "$ARTICLE_DATA")
ARTICLE_ID=$(echo "$ARTICLE_RESPONSE" | grep -o '"id"[[:space:]]*:[[:space:]]*[0-9]*' | head -1 | grep -o '[0-9]*$')
echo "  Article response: $ARTICLE_RESPONSE"
echo "  Article ID: $ARTICLE_ID"

# 5.2 Get Articles List
print_test "Get Articles List" "GET" "${BASE_URL}/api/articles"
do_request "GET" "${BASE_URL}/api/articles" "" "$AUTH_HEADER"

# 5.3 Get Articles with Pagination
print_test "Get Articles (skip=0, take=5)" "GET" "${BASE_URL}/api/articles?skip=0&take=5"
do_request "GET" "${BASE_URL}/api/articles?skip=0&take=5" "" "$AUTH_HEADER"

# 5.4 Get Articles with Filter
print_test "Get Articles with Filter (title=Test)" "GET" "${BASE_URL}/api/articles?title=Test"
do_request "GET" "${BASE_URL}/api/articles?title=Test" "" "$AUTH_HEADER"

# 5.5 Get Single Article
print_test "Get Single Article (ID=$ARTICLE_ID)" "GET" "${BASE_URL}/api/articles/$ARTICLE_ID"
do_request "GET" "${BASE_URL}/api/articles/$ARTICLE_ID" "" "$AUTH_HEADER"

# 5.6 Get Article as Indexed JSON (Elasticsearch)
print_test "Get Article as Indexed JSON" "GET" "${BASE_URL}/api/articles/$ARTICLE_ID/as_indexed_json"
do_request "GET" "${BASE_URL}/api/articles/$ARTICLE_ID/as_indexed_json" "" "$AUTH_HEADER"

# 5.7 Get Comments for Article
print_test "Get Comments for Article" "GET" "${BASE_URL}/api/articles/$ARTICLE_ID/comments"
do_request "GET" "${BASE_URL}/api/articles/$ARTICLE_ID/comments" "" "$AUTH_HEADER"

# 5.8 Update Article (PUT)
print_test "Update Article (PUT)" "PUT" "${BASE_URL}/api/articles/$ARTICLE_ID"
UPDATE_ARTICLE_DATA='{
    "title":"Updated Test Article Title",
    "content":"Updated content for the test article.",
    "abstract":"Updated abstract.",
    "shares":100
}'
do_request "PUT" "${BASE_URL}/api/articles/$ARTICLE_ID" "$UPDATE_ARTICLE_DATA" "$AUTH_HEADER"

# 5.9 Update Article (PATCH)
print_test "Update Article (PATCH)" "PATCH" "${BASE_URL}/api/articles/$ARTICLE_ID"
PATCH_ARTICLE_DATA='{
    "title":"Patched Article Title",
    "content":"Patched content.",
    "abstract":"Patched abstract.",
    "shares":200
}'
do_request "PATCH" "${BASE_URL}/api/articles/$ARTICLE_ID" "$PATCH_ARTICLE_DATA" "$AUTH_HEADER"

# 5.10 Bulk Import Articles
print_test "Bulk Import Articles" "POST" "${BASE_URL}/api/articles/import"
do_request "POST" "${BASE_URL}/api/articles/import" "" "$AUTH_HEADER"

# ============================================================================
# SECTION 6: Comments CRUD
# ============================================================================
print_section "SECTION 6: Comments CRUD"

# 6.1 Create Comment
print_test "Create Comment" "POST" "${BASE_URL}/api/comments"
COMMENT_DATA='{
    "body":"This is a test comment.",
    "user":"testuser",
    "article_id":'$ARTICLE_ID'
}'
COMMENT_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/comments" \
    -H "Content-Type: $CONTENT_TYPE" \
    -H "$AUTH_HEADER" \
    -d "$COMMENT_DATA")
COMMENT_ID=$(echo "$COMMENT_RESPONSE" | grep -o '"id"[[:space:]]*:[[:space:]]*[0-9]*' | head -1 | grep -o '[0-9]*$')
echo "  Comment response: $COMMENT_RESPONSE"
echo "  Comment ID: $COMMENT_ID"

# 6.2 Get Comments List
print_test "Get Comments List" "GET" "${BASE_URL}/api/comments"
do_request "GET" "${BASE_URL}/api/comments" "" "$AUTH_HEADER"

# 6.3 Get Single Comment
print_test "Get Single Comment (ID=$COMMENT_ID)" "GET" "${BASE_URL}/api/comments/$COMMENT_ID"
do_request "GET" "${BASE_URL}/api/comments/$COMMENT_ID" "" "$AUTH_HEADER"

# 6.4 Update Comment (PUT)
print_test "Update Comment (PUT)" "PUT" "${BASE_URL}/api/comments/$COMMENT_ID"
UPDATE_COMMENT_DATA='{"body":"Updated comment body.","user":"updateduser","article_id":'$ARTICLE_ID'}'
do_request "PUT" "${BASE_URL}/api/comments/$COMMENT_ID" "$UPDATE_COMMENT_DATA" "$AUTH_HEADER"

# 6.5 Update Comment (PATCH)
print_test "Update Comment (PATCH)" "PATCH" "${BASE_URL}/api/comments/$COMMENT_ID"
PATCH_COMMENT_DATA='{"body":"Patched comment body.","user":"patcheduser","article_id":'$ARTICLE_ID'}'
do_request "PATCH" "${BASE_URL}/api/comments/$COMMENT_ID" "$PATCH_COMMENT_DATA" "$AUTH_HEADER"

# ============================================================================
# SECTION 7: Authorships CRUD
# ============================================================================
print_section "SECTION 7: Authorships CRUD"

# 7.1 Create Authorship (links author to article)
print_test "Create Authorship" "POST" "${BASE_URL}/api/authorships"
AUTHORSHIP_DATA='{
    "article_id":'$ARTICLE_ID',
    "author_id":'$AUTHOR_ID'
}'
AUTHORSHIP_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/authorships" \
    -H "Content-Type: $CONTENT_TYPE" \
    -H "$AUTH_HEADER" \
    -d "$AUTHORSHIP_DATA")
AUTHORSHIP_ID=$(echo "$AUTHORSHIP_RESPONSE" | grep -o '"id"[[:space:]]*:[[:space:]]*[0-9]*' | head -1 | grep -o '[0-9]*$')
echo "  Authorship response: $AUTHORSHIP_RESPONSE"
echo "  Authorship ID: $AUTHORSHIP_ID"

# 7.2 Get Authorships List
print_test "Get Authorships List" "GET" "${BASE_URL}/api/authorships"
do_request "GET" "${BASE_URL}/api/authorships" "" "$AUTH_HEADER"

# 7.3 Get Single Authorship
print_test "Get Single Authorship (ID=$AUTHORSHIP_ID)" "GET" "${BASE_URL}/api/authorships/$AUTHORSHIP_ID"
do_request "GET" "${BASE_URL}/api/authorships/$AUTHORSHIP_ID" "" "$AUTH_HEADER"

# 7.4 Update Authorship (PUT)
print_test "Update Authorship (PUT)" "PUT" "${BASE_URL}/api/authorships/$AUTHORSHIP_ID"
UPDATE_AUTHORSHIP_DATA='{"article_id":'$ARTICLE_ID',"author_id":'$AUTHOR_ID'}'
do_request "PUT" "${BASE_URL}/api/authorships/$AUTHORSHIP_ID" "$UPDATE_AUTHORSHIP_DATA" "$AUTH_HEADER"

# 7.5 Update Authorship (PATCH)
print_test "Update Authorship (PATCH)" "PATCH" "${BASE_URL}/api/authorships/$AUTHORSHIP_ID"
PATCH_AUTHORSHIP_DATA='{"article_id":'$ARTICLE_ID',"author_id":'$AUTHOR_ID'}'
do_request "PATCH" "${BASE_URL}/api/authorships/$AUTHORSHIP_ID" "$PATCH_AUTHORSHIP_DATA" "$AUTH_HEADER"

# ============================================================================
# SECTION 8: DELETE Operations (Cleanup)
# ============================================================================
print_section "SECTION 8: DELETE Operations (Cleanup)"

# 8.1 Delete Comment
print_test "Delete Comment (ID=$COMMENT_ID)" "DELETE" "${BASE_URL}/api/comments/$COMMENT_ID"
do_request "DELETE" "${BASE_URL}/api/comments/$COMMENT_ID" "" "$AUTH_HEADER"

# 8.2 Delete Comment via GET /delete (alternate route)
# Create another comment to test this route
print_test "Create Comment for DELETE via GET route" "POST" "${BASE_URL}/api/comments"
COMMENT2_DATA='{"body":"Comment to delete via GET route.","user":"testuser","article_id":'$ARTICLE_ID'}'
COMMENT2_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/comments" \
    -H "Content-Type: $CONTENT_TYPE" \
    -H "$AUTH_HEADER" \
    -d "$COMMENT2_DATA")
COMMENT2_ID=$(echo "$COMMENT2_RESPONSE" | grep -o '"id"[[:space:]]*:[[:space:]]*[0-9]*' | head -1 | grep -o '[0-9]*$')
echo "  Response: $COMMENT2_RESPONSE"
echo "  Comment2 ID: $COMMENT2_ID"
print_test "Delete Comment via GET /delete route" "GET" "${BASE_URL}/api/comments/$COMMENT2_ID/delete"
do_request "GET" "${BASE_URL}/api/comments/$COMMENT2_ID/delete" "" "$AUTH_HEADER"

# 8.3 Delete Article (DELETE method)
# Create a new article specifically for deletion test
print_test "Create Article for DELETE test" "POST" "${BASE_URL}/api/articles"
DEL_ARTICLE_DATA='{"title":"Article to Delete","content":"Content to be deleted.","abstract":"Delete test","shares":0}'
DEL_ARTICLE_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/articles" \
    -H "Content-Type: $CONTENT_TYPE" \
    -H "$AUTH_HEADER" \
    -d "$DEL_ARTICLE_DATA")
DEL_ARTICLE_ID=$(echo "$DEL_ARTICLE_RESPONSE" | grep -o '"id"[[:space:]]*:[[:space:]]*[0-9]*' | head -1 | grep -o '[0-9]*$')
echo "  Response: $DEL_ARTICLE_RESPONSE"
echo "  Del Article ID: $DEL_ARTICLE_ID"
print_test "Delete Article (DELETE method)" "DELETE" "${BASE_URL}/api/articles/$DEL_ARTICLE_ID"
do_request "DELETE" "${BASE_URL}/api/articles/$DEL_ARTICLE_ID" "" "$AUTH_HEADER"

# 8.4 Delete Article via GET /delete (alternate route)
print_test "Create Article for GET /delete test" "POST" "${BASE_URL}/api/articles"
DEL2_ARTICLE_DATA='{"title":"Article to Delete via GET","content":"Content.","abstract":"Test","shares":0}'
DEL2_ARTICLE_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/articles" \
    -H "Content-Type: $CONTENT_TYPE" \
    -H "$AUTH_HEADER" \
    -d "$DEL2_ARTICLE_DATA")
DEL2_ARTICLE_ID=$(echo "$DEL2_ARTICLE_RESPONSE" | grep -o '"id"[[:space:]]*:[[:space:]]*[0-9]*' | head -1 | grep -o '[0-9]*$')
echo "  Response: $DEL2_ARTICLE_RESPONSE"
echo "  Del2 Article ID: $DEL2_ARTICLE_ID"
print_test "Delete Article via GET /delete route" "GET" "${BASE_URL}/api/articles/$DEL2_ARTICLE_ID/delete"
do_request "GET" "${BASE_URL}/api/articles/$DEL2_ARTICLE_ID/delete" "" "$AUTH_HEADER"

# 8.5 Delete Authorship
print_test "Delete Authorship (ID=$AUTHORSHIP_ID)" "DELETE" "${BASE_URL}/api/authorships/$AUTHORSHIP_ID"
do_request "DELETE" "${BASE_URL}/api/authorships/$AUTHORSHIP_ID" "" "$AUTH_HEADER"

# 8.6 Delete Authorship via GET /delete (alternate route)
print_test "Create Authorship for GET /delete test" "POST" "${BASE_URL}/api/authorships"
DEL2_AUTHORSHIP_DATA='{"article_id":'$ARTICLE_ID',"author_id":'$AUTHOR_ID'}'
DEL2_AUTHORSHIP_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/authorships" \
    -H "Content-Type: $CONTENT_TYPE" \
    -H "$AUTH_HEADER" \
    -d "$DEL2_AUTHORSHIP_DATA")
DEL2_AUTHORSHIP_ID=$(echo "$DEL2_AUTHORSHIP_RESPONSE" | grep -o '"id"[[:space:]]*:[[:space:]]*[0-9]*' | head -1 | grep -o '[0-9]*$')
echo "  Response: $DEL2_AUTHORSHIP_RESPONSE"
echo "  Del2 Authorship ID: $DEL2_AUTHORSHIP_ID"
print_test "Delete Authorship via GET /delete route" "GET" "${BASE_URL}/api/authorships/$DEL2_AUTHORSHIP_ID/delete"
do_request "GET" "${BASE_URL}/api/authorships/$DEL2_AUTHORSHIP_ID/delete" "" "$AUTH_HEADER"

# 8.7 Delete Category
print_test "Delete Category (ID=$CATEGORY_ID)" "DELETE" "${BASE_URL}/api/categories/$CATEGORY_ID"
do_request "DELETE" "${BASE_URL}/api/categories/$CATEGORY_ID" "" "$AUTH_HEADER"

# 8.8 Delete Category via GET /delete (alternate route)
print_test "Create Category for GET /delete test" "POST" "${BASE_URL}/api/categories"
DEL2_CATEGORY_DATA='{"title":"Category to Delete"}'
DEL2_CATEGORY_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/categories" \
    -H "Content-Type: $CONTENT_TYPE" \
    -H "$AUTH_HEADER" \
    -d "$DEL2_CATEGORY_DATA")
DEL2_CATEGORY_ID=$(echo "$DEL2_CATEGORY_RESPONSE" | grep -o '"id"[[:space:]]*:[[:space:]]*[0-9]*' | head -1 | grep -o '[0-9]*$')
echo "  Response: $DEL2_CATEGORY_RESPONSE"
echo "  Del2 Category ID: $DEL2_CATEGORY_ID"
print_test "Delete Category via GET /delete route" "GET" "${BASE_URL}/api/categories/$DEL2_CATEGORY_ID/delete"
do_request "GET" "${BASE_URL}/api/categories/$DEL2_CATEGORY_ID/delete" "" "$AUTH_HEADER"

# 8.9 Delete Author
print_test "Delete Author (ID=$AUTHOR_ID)" "DELETE" "${BASE_URL}/api/authors/$AUTHOR_ID"
do_request "DELETE" "${BASE_URL}/api/authors/$AUTHOR_ID" "" "$AUTH_HEADER"

# 8.10 Delete Author via GET /delete (alternate route)
print_test "Create Author for GET /delete test" "POST" "${BASE_URL}/api/authors"
DEL2_AUTHOR_DATA='{"first_name":"Delete","last_name":"Me"}'
DEL2_AUTHOR_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/authors" \
    -H "Content-Type: $CONTENT_TYPE" \
    -H "$AUTH_HEADER" \
    -d "$DEL2_AUTHOR_DATA")
DEL2_AUTHOR_ID=$(echo "$DEL2_AUTHOR_RESPONSE" | grep -o '"id"[[:space:]]*:[[:space:]]*[0-9]*' | head -1 | grep -o '[0-9]*$')
echo "  Response: $DEL2_AUTHOR_RESPONSE"
echo "  Del2 Author ID: $DEL2_AUTHOR_ID"
print_test "Delete Author via GET /delete route" "GET" "${BASE_URL}/api/authors/$DEL2_AUTHOR_ID/delete"
do_request "GET" "${BASE_URL}/api/authors/$DEL2_AUTHOR_ID/delete" "" "$AUTH_HEADER"

# ============================================================================
# SECTION 9: Utility Endpoints
# ============================================================================
print_section "SECTION 9: Utility Endpoints"

# 9.1 List all registered routes
print_test "List All Routes" "GET" "${BASE_URL}/routes"
do_request "GET" "${BASE_URL}/routes" ""

# 9.2 Welcome page
print_test "Welcome Page" "GET" "${BASE_URL}/"
do_request "GET" "${BASE_URL}/" ""

# 9.3 404 Error endpoint
print_test "404 Error Page" "GET" "${BASE_URL}/404"
do_request "GET" "${BASE_URL}/404" ""

# 9.4 500 Error endpoint
print_test "500 Error Page" "GET" "${BASE_URL}/500"
do_request "GET" "${BASE_URL}/500" ""

# 9.5 Favicon PNG
print_test "Favicon PNG" "GET" "${BASE_URL}/favicon.png"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/favicon.png")
echo "  Status: $HTTP_CODE"

# 9.6 Favicon ICO
print_test "Favicon ICO" "GET" "${BASE_URL}/favicon.ico"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/favicon.ico")
echo "  Status: $HTTP_CODE"

# ============================================================================
# SECTION 10: Validation & Edge Cases
# ============================================================================
print_section "SECTION 10: Validation & Edge Cases"

# 10.1 Create Article with invalid data (empty title)
print_test "Create Article with invalid data (expect validation error)" "POST" "${BASE_URL}/api/articles"
INVALID_ARTICLE_DATA='{"title":"","content":"","abstract":"","shares":-1}'
do_request "POST" "${BASE_URL}/api/articles" "$INVALID_ARTICLE_DATA" "$AUTH_HEADER"

# 10.2 Create Author with invalid data (empty firstName)
print_test "Create Author with invalid data (expect validation error)" "POST" "${BASE_URL}/api/authors"
INVALID_AUTHOR_DATA='{"firstName":"","lastName":""}'
do_request "POST" "${BASE_URL}/api/authors" "$INVALID_AUTHOR_DATA" "$AUTH_HEADER"

# 10.3 Create Category with invalid data (empty title)
print_test "Create Category with invalid data (expect validation error)" "POST" "${BASE_URL}/api/categories"
INVALID_CATEGORY_DATA='{"title":""}'
do_request "POST" "${BASE_URL}/api/categories" "$INVALID_CATEGORY_DATA" "$AUTH_HEADER"

# 10.4 Create Comment with invalid data (empty body)
print_test "Create Comment with invalid data (expect validation error)" "POST" "${BASE_URL}/api/comments"
INVALID_COMMENT_DATA='{"body":"","user":"","article_id":0}'
do_request "POST" "${BASE_URL}/api/comments" "$INVALID_COMMENT_DATA" "$AUTH_HEADER"

# 10.5 Get non-existent article (expect 404)
print_test "Get Non-existent Article (expect 404)" "GET" "${BASE_URL}/api/articles/999999"
do_request "GET" "${BASE_URL}/api/articles/999999" "" "$AUTH_HEADER"

# 10.6 Rate limiting test (send many rapid requests)
print_test "Rate Limiting Test (rapid requests)" "GET" "${BASE_URL}/api/health"
echo "  Sending 20 rapid requests to test rate limiting..."
for i in {1..20}; do
    CODE=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/health")
    echo "    Request $i: HTTP $CODE"
done

# ============================================================================
# SUMMARY
# ============================================================================
print_section "TEST COMPLETE"
echo ""
echo "Base URL: $BASE_URL"
echo "Created entities:"
echo "  Article ID:    $ARTICLE_ID"
echo "  Author ID:     $AUTHOR_ID"
echo "  Category ID:   $CATEGORY_ID"
echo "  Comment ID:    $COMMENT_ID"
echo "  Authorship ID: $AUTHORSHIP_ID"
echo "  User:          $FIXED_USER"
echo ""
echo "Total tests executed: ~50"
echo -e "${INFO_COLOR}Review the output above for any failures.${RESET_COLOR}"
