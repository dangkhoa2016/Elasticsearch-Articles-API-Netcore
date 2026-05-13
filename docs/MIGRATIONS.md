# EF Core Migrations Guide

## Overview

This project uses EF Core Migrations to manage database schema changes for the SQLite development database.

## Prerequisites

- .NET SDK (version matching project target framework)
- EF Core CLI tools (`dotnet ef`)

### Installing EF Core CLI

```bash
# Install as a global tool
dotnet tool install --global dotnet ef

# Upgrade version
dotnet tool update --global dotnet ef

# Verify installation
dotnet ef --version
```

## Configuration

### Design-Time DbContext Factory

The project includes `Models/ElasticsearchDBContextFactory.cs` which implements `IDesignTimeDbContextFactory<ElasticsearchDBContext>`. This allows EF Core CLI tools to create the DbContext at design time without running the full application.

The factory reads connection strings from `appsettings.json` and environment-specific config files.

### Connection Strings

| Environment | File | Default Connection String |
|-------------|------|--------------------------|
| Development | `appsettings.json` | `Data Source=./DB/development.sqlite3;` |
| Testing | `appsettings.Testing.json` | In-memory SQLite |

## Common Commands

All commands should be run from the project root directory.

### Create a New Migration

```bash
dotnet ef migrations add <MigrationName>
```

Example:
```bash
dotnet ef migrations add AddUserEmailIndex
```

This creates a new migration file in the `Migrations/` directory.

### Apply Migrations

```bash
# Apply all pending migrations
dotnet ef database update

# Apply up to a specific migration
dotnet ef database update <MigrationName>
```

### Remove Last Migration

```bash
# Remove the most recent migration (only if not yet applied)
dotnet ef migrations remove
```

### List Migrations

```bash
# Show all migrations and their applied status
dotnet ef migrations list
```

### Generate SQL Script

```bash
# Generate a SQL script for all migrations
dotnet ef migrations script

# Generate SQL script for a specific range
dotnet ef migrations script <FromMigration> <ToMigration>
```

## Migration History

Migrations are tracked in the `__EFMigrationsHistory` table in the database. This table stores:
- `MigrationId`: The migration identifier (timestamp + name)
- `ProductVersion`: The EF Core version used

## Current Migrations

| Migration ID | Description | Date |
|--------------|-------------|------|
| `20260520131627_CreateUserTable` | Initial schema with all tables + Users table | 2026-05-20 |

### Tables Created by CreateUserTable

| Table | Description |
|-------|-------------|
| `articles` | Blog posts with title, content, abstract, shares |
| `authors` | Author information (first name, last name) |
| `categories` | Article categories |
| `comments` | Comments on articles |
| `authorships` | Many-to-many: articles <-> authors |
| `articles_categories` | Many-to-many: articles <-> categories |
| `Users` | Authentication users (username, password hash, email) |

### Indexes

| Table | Index Name | Columns |
|-------|-----------|---------|
| articles | `index_articles_on_title` | title |
| articles | `index_articles_on_created_at` | created_at |
| authors | `index_authors_on_first_name_last_name` | first_name, last_name |
| categories | `index_categories_on_title` | title |
| comments | `index_comments_on_article_id` | article_id |
| authorships | `index_authorships_on_article_id` | article_id |
| authorships | `index_authorships_on_author_id` | author_id |
| articles_categories | `index_articles_categories_on_article_id` | article_id |
| articles_categories | `index_articles_categories_on_category_id` | category_id |

## Workflow

### Adding a New Feature

1. Make changes to your model classes in `Models/`
2. Update `ElasticsearchDBContext.cs` if needed (relationships, constraints)
3. Run `dotnet ef migrations add <DescriptiveName>`
4. Review the generated migration file
5. Run `dotnet ef database update` to apply
6. Test the changes

### Best Practices

- **Use descriptive migration names**: `AddUserEmailIndex` not `Update1`
- **Review generated migrations**: Always check the Up and Down methods
- **One logical change per migration**: Don't bundle unrelated changes
- **Test Down migrations**: Ensure you can rollback if needed
- **Commit migrations**: Always commit migration files with your code
- **Never edit applied migrations**: Create a new migration instead

## Troubleshooting

### "Pending Model Changes" Error

This occurs when your model differs from the last migration snapshot. Fix by:
```bash
dotnet ef migrations add <Name>
```

### "PendingModelChangesWarning: The model changes each time it is built"

This happens when `HasDefaultValue(DateTime.Now)` is used in `OnModelCreating` — `DateTime.Now` is a dynamic value that changes on every build. Fix by using `HasDefaultValueSql` instead:

```csharp
// WRONG — dynamic value, causes model to change each build
entity.Property(e => e.CreatedAt).HasDefaultValue(DateTime.Now);

// CORRECT — static SQL expression, model is stable
entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
```

If a migration was already created with hardcoded `DateTime` values, edit the migration file to use `defaultValueSql` instead of `defaultValue`, then delete any follow-up "fix" migrations that are no longer needed.

### "Table Already Exists" Error

The database may have been created manually or from a previous schema. Delete and recreate:
```bash
rm DB/development.sqlite3
dotnet ef database update
```

### Build Errors with .NET Version Mismatch

If the installed .NET SDK doesn't match the project's target framework, temporarily change `TargetFramework` in the `.csproj` file, run migrations, then change it back.

## Adding Indexes (Post-Migration)

Additional indexes can be added via separate migrations:

```bash
dotnet ef migrations add AddIndexOnArticlesShares
```

Or via raw SQL in a migration:

```csharp
migrationBuilder.Sql("CREATE INDEX index_articles_on_shares ON articles (shares);");
```
