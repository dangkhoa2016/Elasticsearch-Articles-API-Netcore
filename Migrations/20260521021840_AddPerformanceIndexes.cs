using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElasticsearchArticlesApiNetcore.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "index_categories_on_title",
                table: "categories",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "index_authors_on_first_name_last_name",
                table: "authors",
                columns: new[] { "first_name", "last_name" });

            migrationBuilder.CreateIndex(
                name: "index_articles_on_created_at",
                table: "articles",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "index_articles_on_title",
                table: "articles",
                column: "title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "index_categories_on_title",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "index_authors_on_first_name_last_name",
                table: "authors");

            migrationBuilder.DropIndex(
                name: "index_articles_on_created_at",
                table: "articles");

            migrationBuilder.DropIndex(
                name: "index_articles_on_title",
                table: "articles");
        }
    }
}
