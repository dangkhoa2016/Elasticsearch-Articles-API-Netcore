# Elasticsearch Articles API - .NET Core

> 🌐 Language / Ngôn ngữ: **English** | [Tiếng Việt](README.vi.md)

ASP.NET Core 10 Web API for managing articles, authors, categories, and comments with dual storage: SQLite (EF Core) for relational data and Elasticsearch 7.x for full-text search.

## Related Projects

- **Node.js API**: https://github.com/dangkhoa2016/Elasticsearch-Articles-API-Nodejs
- **Vue.js Frontend**: https://github.com/dangkhoa2016/Elasticsearch-Articles-Management-VueJs

## Technologies Used

| Component | Technology |
|---|---|
| **Framework** | ASP.NET Core 10.0 |
| **Database** | SQLite / PostgreSQL / SQL Server (via EF Core 10) |
| **Search Engine** | Elasticsearch 7.x (NEST 7.17.5) |
| **Auth** | JWT Bearer (HMAC-SHA256) |
| **Validation** | FluentValidation 12 |
| **Mapping** | AutoMapper 16 |
| **Logging** | Serilog (console + rolling file) |
| **Resilience** | Polly (retry, circuit breaker, timeout) |
| **Rate Limiting** | AspNetCoreRateLimit 5 |
| **Observability** | OpenTelemetry (tracing + metrics, OTLP exporter) |
| **Testing** | xUnit |

## Architecture

```
Controllers -> Services -> Repositories -> UnitOfWork -> EF Core DbContext
                                                          -> Elasticsearch (NEST)
```

- **Repository Pattern**: Generic repository + specialized repositories per entity
- **Unit of Work**: Transaction management across multiple repositories
- **Service Layer**: Business logic and validation orchestration
- **Background Queue**: BoundedChannel-based queue for async bulk import operations
- **Middleware Pipeline**: Security headers, correlation IDs, exception handling, rate limiting, request logging, telemetry enrichment

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (version 10.0.300+)
- [Elasticsearch 7.x](https://www.elastic.co/downloads/elasticsearch) running on `http://localhost:9200`
- (Optional) [Docker](https://www.docker.com/) for containerized Elasticsearch

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/dangkhoa2016/Elasticsearch-Articles-API-Netcore.git
cd Elasticsearch-Articles-API-Netcore
```

### 2. Start Elasticsearch

```bash
# Using Docker
docker run -d --name elasticsearch -p 9200:9200 -e "discovery.type=single-node" -e "xpack.security.enabled=false" elasticsearch:7.17.0
```

### 3. Configure the application

Edit `appsettings.json` or use environment-specific files:

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

### 4. Run database migrations

```bash
dotnet tool install --global dotnet-ef  # if not installed
dotnet ef database update
```

### 5. Run the application

```bash
dotnet run
```

The API will be available at `http://localhost:5000` (or the configured URL).

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Register a new user |
| POST | `/api/auth/login` | Login and receive JWT token |

### Articles (most endpoints require `[Authorize]`)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/articles` | List articles (query: `skip`, `take`, `title`, `loadRelation`, `showTotal`) |
| GET | `/api/articles/{id}` | Get article by ID |
| GET | `/api/articles/{id}/as_indexed_json` | Get article as stored in Elasticsearch |
| GET | `/api/articles/{id}/comments` | Get comments for an article |
| POST | `/api/articles` | Create article (JSON body) |
| PUT/PATCH | `/api/articles/{id}` | Update article |
| DELETE | `/api/articles/{id}` | Delete article |
| POST | `/api/articles/import` | Trigger bulk import from SQLite to Elasticsearch (background) |

### Authors

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/authors` | List authors (query: `skip`, `take`, `name`, `showTotal`) |
| GET | `/api/authors/{id}` | Get author by ID |
| GET | `/api/authors/{id}/articles` | Get articles by author |
| POST | `/api/authors` | Create author |
| PUT/PATCH | `/api/authors/{id}` | Update author |
| DELETE | `/api/authors/{id}` | Delete author |

### Categories

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/categories` | List categories |
| GET | `/api/categories/{id}` | Get category by ID |
| GET | `/api/categories/{id}/articles` | Get articles by category |
| POST | `/api/categories` | Create category |
| PUT/PATCH | `/api/categories/{id}` | Update category |
| DELETE | `/api/categories/{id}` | Delete category |

### Comments

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/comments` | List comments |
| GET | `/api/comments/{id}` | Get comment by ID |
| POST | `/api/comments` | Create comment |
| PUT/PATCH | `/api/comments/{id}` | Update comment |
| DELETE | `/api/comments/{id}` | Delete comment |

### Authorships

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/authorships` | List authorships |
| GET | `/api/authorships/{id}` | Get authorship by ID |
| POST | `/api/authorships` | Create authorship |
| PUT/PATCH | `/api/authorships/{id}` | Update authorship |
| DELETE | `/api/authorships/{id}` | Delete authorship |

### Health & Utilities

| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | Welcome page |
| GET | `/api/health` | Basic health check |
| GET | `/api/health/detailed` | Full health (DB + Elasticsearch) |
| GET | `/api/health/database` | Database health check |
| GET | `/api/health/elasticsearch` | Elasticsearch health check |
| GET | `/routes` | List all registered API routes |

## Data Models

### Article

| Field | Type | Notes |
|---|---|---|
| Id | int | Primary key |
| Title | string | Required |
| Content | string | Full article text |
| Abstract | string | Summary |
| Url | string | SEO-friendly URL |
| PublishedOn | DateOnly | Publication date |
| Shares | int | Share count |
| CreatedAt | DateTime | Auto-set on create |
| UpdatedAt | DateTime | Auto-set on update |

### Author

| Field | Type | Notes |
|---|---|---|
| Id | int | Primary key |
| FirstName | string | Required |
| LastName | string | Required |
| CreatedAt / UpdatedAt | DateTime | Auto-managed |

### Category

| Field | Type | Notes |
|---|---|---|
| Id | int | Primary key |
| Title | string | Required |
| CreatedAt / UpdatedAt | DateTime | Auto-managed |

### Comment

| Field | Type | Notes |
|---|---|---|
| Id | int | Primary key |
| Body | string | Comment text |
| User | string | Commenter name |
| UserLocation | string | Location |
| Stars | int | Rating |
| Pick | bool | Editor's pick flag |
| ArticleId | int | Foreign key to Article |
| CreatedAt / UpdatedAt | DateTime | Auto-managed |

### Relationships

- **Article** has many **Comments**, **Authorships**, **ArticlesCategories**
- **Author** has many **Authorships**
- **Category** has many **ArticlesCategories**
- **Authorship** links Article <-> Author (many-to-many join table)
- **ArticlesCategory** links Article <-> Category (many-to-many join table)

## Configuration

### appsettings.json

| Section | Key | Description |
|---|---|---|
| `DBSettings` | `Provider` | Database provider: `SQLite`, `PostgreSQL`, `SqlServer` |
| `DBSettings.SQLite` | `DataSource` | Path to SQLite database file |
| `ElasticsearchSettings` | `url` | Elasticsearch endpoint URL |
| `ElasticsearchSettings` | `index` | Elasticsearch index name |
| `JwtSettings` | `Secret` | JWT signing secret (min 32 chars) |
| `JwtSettings` | `ExpirationInMinutes` | JWT token lifetime |
| `AllowedOrigins` | array | CORS allowed origins |
| `QueueCapacity` | int | Background queue max size (default: 100) |
| `OpenTelemetrySettings` | `Enabled` | Enable/disable OTLP telemetry |
| `ResilienceSettings` | `RetryCount`, `CircuitBreakerThreshold`, etc. | Polly resilience config |

### Environment-specific configs

- `appsettings.Development.json` - SQLite, relaxed rate limits (200/min), more CORS origins
- `appsettings.Production.json` - PostgreSQL, stricter rate limits (30/min), enhanced resilience

### Environment Variables

All config values can be overridden via environment variables using `__` as separator:

```bash
export DBSettings__Provider=PostgreSQL
export JwtSettings__Secret=my-secret-key
export ElasticsearchSettings__url=http://elasticsearch:9200
```

## Resilience & Performance

- **Retry**: Exponential backoff with configurable retries (default: 3)
- **Circuit Breaker**: Opens at 50% failure rate, 30s break duration
- **Timeout**: 30s default for database operations
- **Memory Cache**: ArticleRepository caches results with configurable expiration
- **IMemoryCache**: Used in article retrieval for read-heavy workloads
- **Response Compression**: Gzip + Brotli

## Security

- JWT Bearer authentication for protected endpoints
- Rate limiting: 60 requests/min global, stricter for auth endpoints (5/min login, 3/min register)
- Security headers: X-Content-Type-Options, X-Frame-Options, CSP, Permissions-Policy, Referrer-Policy
- CORS with configurable origins (throws if empty)
- Password hashing via ASP.NET Core Identity `PasswordHasher`

## Elasticsearch Integration

- Index mapping loaded from `DB/index.v7.json` on startup
- Auto-creates index if not exists (`Helper.InitIndex()`)
- Nested fields: `authors` (full_name), `comments` (body with snowball analyzer)
- Multi-field: `content` and `content_tokenized`, `title` and `title_tokenized`
- Bulk import via background queue (`POST /api/articles/import`)
- Circuit breaker and retry logic for ES operations

## Testing

See [ElasticsearchArticlesApiNetcore.Tests/README.md](ElasticsearchArticlesApiNetcore.Tests/README.md) for detailed test documentation, prerequisites, and best practices.

```bash
dotnet test ElasticsearchArticlesApiNetcore.Tests/ElasticsearchArticlesApiNetcore.Tests.csproj
```

Test project includes:
- **Unit Tests**: Health checks
- **Integration Tests**: API endpoints (articles, authors, categories), database, Elasticsearch, health, telemetry
- **Telemetry Tests**: OpenTelemetry metrics and tracing

## Project Structure

```
Elasticsearch-Articles-API-Netcore/
├── Constants/          # App-wide constants (page sizes, defaults)
├── Controllers/        # API controllers (11 controllers)
├── DB/                 # Database files, ES index mappings (v5, v6, v7)
├── Extensions/         # Elasticsearch DI extension
├── Factories/          # Database provider factory, SQLite PRAGMA interceptor
├── HealthChecks/       # Database & Elasticsearch health checks
├── Helpers/            # ES helper, background worker queue, long-running service
├── Mappings/           # AutoMapper profiles
├── Middleware/         # Security headers, correlation ID, exception handling, telemetry
├── Migrations/         # EF Core migrations
├── Models/             # EF Core entities and DbContext
├── Repositories/       # Data access layer (generic + specialized)
├── Resilience/         # Polly resilience pipeline
├── Services/           # Business logic layer
├── Telemetry/          # OpenTelemetry metrics and middleware
├── Validators/         # FluentValidation validators
├── ViewModels/         # API request/response DTOs
└── appsettings*.json   # Configuration files
```

## Migrations

```bash
# Create a new migration
dotnet ef migrations add <MigrationName>

# Apply all pending migrations
dotnet ef database update

# Revert last migration
dotnet ef database update <PreviousMigrationName>
```

## Related Documentation

- See [DB/MIGRATIONS.md](DB/MIGRATIONS.md) for detailed migration guide

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
