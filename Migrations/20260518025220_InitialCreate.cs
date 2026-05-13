using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElasticsearchArticlesApiNetcore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    @abstract = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "datetime('now')"),
                    published_on = table.Column<DateTime>(type: "date", nullable: true),
                    shares = table.Column<long>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "varchar", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "datetime('now')"),
                    url = table.Column<string>(type: "varchar", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "authors",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    first_name = table.Column<string>(type: "varchar", nullable: false),
                    last_name = table.Column<string>(type: "varchar", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    title = table.Column<string>(type: "varchar", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    article_id = table.Column<long>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    pick = table.Column<bool>(type: "boolean", nullable: true),
                    stars = table.Column<long>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    user = table.Column<string>(type: "varchar", nullable: false),
                    user_location = table.Column<string>(type: "varchar", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_comments_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "authorships",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    article_id = table.Column<long>(type: "integer", nullable: false),
                    author_id = table.Column<long>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorships", x => x.id);
                    table.ForeignKey(
                        name: "FK_authorships_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_authorships_authors_author_id",
                        column: x => x.author_id,
                        principalTable: "authors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "articles_categories",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    article_id = table.Column<long>(type: "integer", nullable: false),
                    category_id = table.Column<long>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_articles_categories_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_articles_categories_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "index_articles_categories_on_article_id",
                table: "articles_categories",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "index_articles_categories_on_category_id",
                table: "articles_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "index_authorships_on_article_id",
                table: "authorships",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "index_authorships_on_author_id",
                table: "authorships",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "index_comments_on_article_id",
                table: "comments",
                column: "article_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "articles_categories");
            migrationBuilder.DropTable(name: "authorships");
            migrationBuilder.DropTable(name: "comments");
            migrationBuilder.DropTable(name: "Users");
            migrationBuilder.DropTable(name: "authors");
            migrationBuilder.DropTable(name: "categories");
            migrationBuilder.DropTable(name: "articles");
        }
    }
}
