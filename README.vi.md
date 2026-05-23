# Elasticsearch Articles API - .NET Core

> 🌐 Language / Ngôn ngữ: [English](README.md) | **Tiếng Việt**

ASP.NET Core 10 Web API quản lý bài viết, tác giả, danh mục và bình luận với lưu trữ kép: SQLite (EF Core) cho dữ liệu quan hệ và Elasticsearch 7.x cho tìm kiếm toàn văn bản.

## Các Dự Án Liên Quan

- **Node.js API**: https://github.com/dangkhoa2016/Elasticsearch-Articles-API-Nodejs
- **Vue.js Frontend**: https://github.com/dangkhoa2016/Elasticsearch-Articles-Management-VueJs

## Công Nghệ Sử Dụng

| Thành phần | Công nghệ |
|---|---|
| **Framework** | ASP.NET Core 10.0 |
| **Cơ sở dữ liệu** | SQLite / PostgreSQL / SQL Server (qua EF Core 10) |
| **Công cụ tìm kiếm** | Elasticsearch 7.x (NEST 7.17.5) |
| **Xác thực** | JWT Bearer (HMAC-SHA256) |
| **Validation** | FluentValidation 12 |
| **Mapping** | AutoMapper 16 |
| **Logging** | Serilog (console + rolling file) |
| **Khôi phục lỗi** | Polly (retry, circuit breaker, timeout) |
| **Giới hạn tốc độ** | AspNetCoreRateLimit 5 |
| **Quan sát** | OpenTelemetry (tracing + metrics, OTLP exporter) |
| **Testing** | xUnit |

## Kiến Trúc

```
Controllers -> Services -> Repositories -> UnitOfWork -> EF Core DbContext
                                                          -> Elasticsearch (NEST)
```

- **Repository Pattern**: Generic repository + specialized repositories cho từng entity
- **Unit of Work**: Quản lý transaction xuyên suốt nhiều repository
- **Service Layer**: Nghiệp vụ và điều phối validation
- **Background Queue**: Queue dựa trên BoundedChannel cho các tác vụ bulk import bất đồng bộ
- **Middleware Pipeline**: Security headers, correlation IDs, xử lý ngoại lệ, giới hạn tốc độ, ghi log request, telemetry enrichment

## Yêu Cầu Hệ Thống

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (phiên bản 10.0.300+)
- [Elasticsearch 7.x](https://www.elastic.co/downloads/elasticsearch) chạy trên `http://localhost:9200`
- (Tùy chọn) [Docker](https://www.docker.com/) cho Elasticsearch containerized

## Hướng Dẫn Cài Đặt

### 1. Clone repository

```bash
git clone https://github.com/dangkhoa2016/Elasticsearch-Articles-API-Netcore.git
cd Elasticsearch-Articles-API-Netcore
```

### 2. Khởi động Elasticsearch

```bash
# Sử dụng Docker
docker run -d --name elasticsearch -p 9200:9200 -e "discovery.type=single-node" -e "xpack.security.enabled=false" elasticsearch:7.17.0
```

### 3. Cấu hình ứng dụng

Chỉnh sửa `appsettings.json` hoặc sử dụng file theo môi trường:

```json
{
  "DBSettings": {
    "Provider": "SQLite",
    "SQLite": {
      "DataSource": "./DB/development.sqlite3"
    }
  },
  "ElasticsearchSettings": {
    "url": "http://localhost:9200",
    "index": "articles"
  },
  "JwtSettings": {
    "Secret": "your-super-secret-key-at-least-32-characters",
    "Issuer": "your-issuer",
    "Audience": "your-audience",
    "ExpirationInMinutes": 60
  },
  "AllowedOrigins": ["http://localhost:3000", "http://localhost:8080"]
}
```

### 4. Chạy database migrations

```bash
dotnet tool install --global dotnet-ef  # nếu chưa cài
dotnet ef database update
```

### 5. Chạy ứng dụng

```bash
dotnet run
```

API sẽ khả dụng tại `http://localhost:5000` (hoặc URL đã cấu hình).

## API Endpoints

### Xác Thực

| Method | Endpoint | Mô tả |
|---|---|---|
| POST | `/api/auth/register` | Đăng ký user mới |
| POST | `/api/auth/login` | Đăng nhập và nhận JWT token |

### Bài Viết (hầu hết endpoints yêu cầu `[Authorize]`)

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/api/articles` | Danh sách bài viết (query: `skip`, `take`, `title`, `loadRelation`, `showTotal`) |
| GET | `/api/articles/{id}` | Lấy bài viết theo ID |
| GET | `/api/articles/{id}/as_indexed_json` | Lấy bài viết dưới dạng đã lưu trong Elasticsearch |
| GET | `/api/articles/{id}/comments` | Lấy bình luận của bài viết |
| POST | `/api/articles` | Tạo bài viết mới (JSON body) |
| PUT/PATCH | `/api/articles/{id}` | Cập nhật bài viết |
| DELETE | `/api/articles/{id}` | Xóa bài viết |
| POST | `/api/articles/import` | Kích hoạt bulk import từ SQLite sang Elasticsearch (chạy nền) |

### Tác Giả

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/api/authors` | Danh sách tác giả (query: `skip`, `take`, `name`, `showTotal`) |
| GET | `/api/authors/{id}` | Lấy tác giả theo ID |
| GET | `/api/authors/{id}/articles` | Lấy bài viết theo tác giả |
| POST | `/api/authors` | Tạo tác giả mới |
| PUT/PATCH | `/api/authors/{id}` | Cập nhật tác giả |
| DELETE | `/api/authors/{id}` | Xóa tác giả |

### Danh Mục

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/api/categories` | Danh sách danh mục |
| GET | `/api/categories/{id}` | Lấy danh mục theo ID |
| GET | `/api/categories/{id}/articles` | Lấy bài viết theo danh mục |
| POST | `/api/categories` | Tạo danh mục mới |
| PUT/PATCH | `/api/categories/{id}` | Cập nhật danh mục |
| DELETE | `/api/categories/{id}` | Xóa danh mục |

### Bình Luận

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/api/comments` | Danh sách bình luận |
| GET | `/api/comments/{id}` | Lấy bình luận theo ID |
| POST | `/api/comments` | Tạo bình luận mới |
| PUT/PATCH | `/api/comments/{id}` | Cập nhật bình luận |
| DELETE | `/api/comments/{id}` | Xóa bình luận |

### Authorships (Quan hệ tác giả - bài viết)

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/api/authorships` | Danh sách authorships |
| GET | `/api/authorships/{id}` | Lấy authorship theo ID |
| POST | `/api/authorships` | Tạo authorship mới |
| PUT/PATCH | `/api/authorships/{id}` | Cập nhật authorship |
| DELETE | `/api/authorships/{id}` | Xóa authorship |

### Health & Utilities

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/` | Trang chào mừng |
| GET | `/api/health` | Kiểm tra sức khỏe cơ bản |
| GET | `/api/health/detailed` | Kiểm tra sức khỏe đầy đủ (DB + Elasticsearch) |
| GET | `/api/health/database` | Kiểm tra sức khỏe database |
| GET | `/api/health/elasticsearch` | Kiểm tra sức khỏe Elasticsearch |
| GET | `/routes` | Danh sách tất cả API routes đã đăng ký |

## Mô Hình Dữ Liệu

### Article (Bài viết)

| Trường | Kiểu | Ghi chú |
|---|---|---|
| Id | int | Khóa chính |
| Title | string | Bắt buộc |
| Content | string | Nội dung đầy đủ |
| Abstract | string | Tóm tắt |
| Url | string | URL thân thiện SEO |
| PublishedOn | DateOnly | Ngày xuất bản |
| Shares | int | Số lượt chia sẻ |
| CreatedAt | DateTime | Tự động đặt khi tạo |
| UpdatedAt | DateTime | Tự động cập nhật |

### Author (Tác giả)

| Trường | Kiểu | Ghi chú |
|---|---|---|
| Id | int | Khóa chính |
| FirstName | string | Bắt buộc |
| LastName | string | Bắt buộc |
| CreatedAt / UpdatedAt | DateTime | Tự động quản lý |

### Category (Danh mục)

| Trường | Kiểu | Ghi chú |
|---|---|---|
| Id | int | Khóa chính |
| Title | string | Bắt buộc |
| CreatedAt / UpdatedAt | DateTime | Tự động quản lý |

### Comment (Bình luận)

| Trường | Kiểu | Ghi chú |
|---|---|---|
| Id | int | Khóa chính |
| Body | string | Nội dung bình luận |
| User | string | Tên người bình luận |
| UserLocation | string | Vị trí |
| Stars | int | Đánh giá |
| Pick | bool | Cờ "Biên tập viên chọn" |
| ArticleId | int | Khóa ngoại trỏ tới Article |
| CreatedAt / UpdatedAt | DateTime | Tự động quản lý |

### Quan Hệ

- **Article** có nhiều **Comments**, **Authorships**, **ArticlesCategories**
- **Author** có nhiều **Authorships**
- **Category** có nhiều **ArticlesCategories**
- **Authorship** liên kết Article <-> Author (bảng nối nhiều-nhiều)
- **ArticlesCategory** liên kết Article <-> Category (bảng nối nhiều-nhiều)

## Cấu Hình

### appsettings.json

| Section | Key | Mô tả |
|---|---|---|
| `DBSettings` | `Provider` | Nhà cung cấp CSDL: `SQLite`, `PostgreSQL`, `SqlServer` |
| `DBSettings.SQLite` | `DataSource` | Đường dẫn file SQLite |
| `ElasticsearchSettings` | `url` | URL endpoint Elasticsearch |
| `ElasticsearchSettings` | `index` | Tên index Elasticsearch |
| `JwtSettings` | `Secret` | Khóa ký JWT (tối thiểu 32 ký tự) |
| `JwtSettings` | `ExpirationInMinutes` | Thời hạn JWT token |
| `AllowedOrigins` | array | Origins được phép CORS |
| `QueueCapacity` | int | Kích thước tối đa queue nền (mặc định: 100) |
| `OpenTelemetrySettings` | `Enabled` | Bật/tắt telemetry OTLP |
| `ResilienceSettings` | `RetryCount`, `CircuitBreakerThreshold`, v.v. | Cấu hình Polly resilience |

### Cấu hình theo môi trường

- `appsettings.Development.json` - SQLite, giới hạn tốc độ thoải mái hơn (200/phút), nhiều CORS origins hơn
- `appsettings.Production.json` - PostgreSQL, giới hạn tốc độ nghiêm ngặt hơn (30/phút), resilience tăng cường

### Biến Môi Trường

Tất cả giá trị cấu hình có thể ghi đè qua biến môi trường dùng `__` làm phân cách:

```bash
export DBSettings__Provider=PostgreSQL
export JwtSettings__Secret=my-secret-key
export ElasticsearchSettings__url=http://elasticsearch:9200
```

## Khôi Phục Lỗi & Hiệu Năng

- **Retry**: Exponential backoff với số lần thử lại cấu hình được (mặc định: 3)
- **Circuit Breaker**: Mở ở tỷ lệ lỗi 50%, thời gian ngắt 30 giây
- **Timeout**: 30 giây mặc định cho thao tác database
- **Memory Cache**: ArticleRepository cache kết quả với expiration cấu hình được
- **IMemoryCache**: Sử dụng trong truy xuất bài viết cho khối lượng đọc lớn
- **Response Compression**: Gzip + Brotli

## Bảo Mật

- Xác thực JWT Bearer cho các endpoint được bảo vệ
- Giới hạn tốc độ: 60 request/phút toàn cục, nghiêm ngặt hơn cho auth endpoints (5/phút đăng nhập, 3/phút đăng ký)
- Security headers: X-Content-Type-Options, X-Frame-Options, CSP, Permissions-Policy, Referrer-Policy
- CORS với origins cấu hình được (throw nếu rỗng)
- Mã hóa mật khẩu qua ASP.NET Core Identity `PasswordHasher`

## Tích Hợp Elasticsearch

- Index mapping được tải từ `DB/index.v7.json` khi khởi động
- Tự động tạo index nếu chưa tồn tại (`Helper.InitIndex()`)
- Nested fields: `authors` (full_name), `comments` (body với snowball analyzer)
- Multi-field: `content` và `content_tokenized`, `title` và `title_tokenized`
- Bulk import qua queue nền (`POST /api/articles/import`)
- Circuit breaker và retry logic cho các thao tác ES

## Testing

Xem [ElasticsearchArticlesApiNetcore.Tests/README.md](ElasticsearchArticlesApiNetcore.Tests/README.md) để biết tài liệu testing chi tiết, yêu cầu và thực hành tốt nhất.

```bash
dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj
```

Test project bao gồm:
- **Unit Tests**: Kiểm tra health checks
- **Integration Tests**: API endpoints (articles, authors, categories), database, Elasticsearch, health, telemetry
- **Telemetry Tests**: OpenTelemetry metrics và tracing

## Cấu Trúc Dự Án

```
Elasticsearch-Articles-API-Netcore/
├── Constants/          # Hằng số ứng dụng (kích thước trang, mặc định)
├── Controllers/        # API controllers (11 controllers)
├── DB/                 # File database, ES index mappings (v5, v6, v7)
├── Extensions/         # Elasticsearch DI extension
├── Factories/          # Database provider factory, SQLite PRAGMA interceptor
├── HealthChecks/       # Database & Elasticsearch health checks
├── Helpers/            # ES helper, background worker queue, long-running service
├── Mappings/           # AutoMapper profiles
├── Middleware/         # Security headers, correlation ID, exception handling, telemetry
├── Migrations/         # EF Core migrations
├── Models/             # EF Core entities và DbContext
├── Repositories/       # Data access layer (generic + specialized)
├── Resilience/         # Polly resilience pipeline
├── Services/           # Business logic layer
├── Telemetry/          # OpenTelemetry metrics và middleware
├── Validators/         # FluentValidation validators
├── ViewModels/         # API request/response DTOs
└── appsettings*.json   # Files cấu hình
```

## Migrations

```bash
# Tạo migration mới
dotnet ef migrations add <MigrationName>

# Áp dụng tất cả migrations đang chờ
dotnet ef database update

# Hoàn tác migration cuối
dotnet ef database update <PreviousMigrationName>
```

## Tài Liệu Liên Quan

- Xem [DB/MIGRATIONS.md](DB/MIGRATIONS.md) để biết hướng dẫn migration chi tiết

## Giấy Phép

Dự án này được cấp phép theo Giấy Phép MIT - xem file [LICENSE](LICENSE) để biết chi tiết.
