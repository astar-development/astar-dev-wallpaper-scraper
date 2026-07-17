using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class RemoveKeywordsAndAddIsFieldsToSearchCats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileClassificationKeywords");

            migrationBuilder.AddColumn<bool>(
                name: "IsFamous",
                table: "SearchCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInternet",
                table: "SearchCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFamous",
                table: "SearchCategories");

            migrationBuilder.DropColumn(
                name: "IsInternet",
                table: "SearchCategories");

            migrationBuilder.CreateTable(
                name: "FileClassificationKeywords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFamous = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsInternet = table.Column<bool>(type: "INTEGER", nullable: false),
                    Keyword = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileClassificationKeywords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileClassificationKeywords_FileClassificationCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FileClassificationCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileClassificationKeywords_CategoryId_Keyword",
                table: "FileClassificationKeywords",
                columns: new[] { "CategoryId", "Keyword" },
                unique: true);
        }
    }
}
