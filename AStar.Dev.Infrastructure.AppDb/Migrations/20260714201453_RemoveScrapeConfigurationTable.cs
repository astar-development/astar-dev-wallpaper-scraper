using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class RemoveScrapeConfigurationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConnectionStrings_ScrapeConfiguration_ScrapeConfigurationEntityId",
                table: "ConnectionStrings");

            migrationBuilder.DropForeignKey(
                name: "FK_ScrapeDirectories_ScrapeConfiguration_ScrapeConfigurationEntityId",
                table: "ScrapeDirectories");

            migrationBuilder.DropForeignKey(
                name: "FK_SearchConfiguration_ScrapeConfiguration_ScrapeConfigurationEntityId",
                table: "SearchConfiguration");

            migrationBuilder.DropForeignKey(
                name: "FK_UserConfiguration_ScrapeConfiguration_ScrapeConfigurationEntityId",
                table: "UserConfiguration");

            migrationBuilder.DropTable(
                name: "ScrapeConfiguration");

            migrationBuilder.DropIndex(
                name: "IX_UserConfiguration_ScrapeConfigurationEntityId",
                table: "UserConfiguration");

            migrationBuilder.DropIndex(
                name: "IX_SearchConfiguration_ScrapeConfigurationEntityId",
                table: "SearchConfiguration");

            migrationBuilder.DropIndex(
                name: "IX_ScrapeDirectories_ScrapeConfigurationEntityId",
                table: "ScrapeDirectories");

            migrationBuilder.DropIndex(
                name: "IX_ConnectionStrings_ScrapeConfigurationEntityId",
                table: "ConnectionStrings");

            migrationBuilder.DropColumn(
                name: "ScrapeConfigurationEntityId",
                table: "UserConfiguration");

            migrationBuilder.DropColumn(
                name: "ScrapeConfigurationEntityId",
                table: "SearchConfiguration");

            migrationBuilder.DropColumn(
                name: "ScrapeConfigurationEntityId",
                table: "ScrapeDirectories");

            migrationBuilder.DropColumn(
                name: "ScrapeConfigurationEntityId",
                table: "ConnectionStrings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScrapeConfigurationEntityId",
                table: "UserConfiguration",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScrapeConfigurationEntityId",
                table: "SearchConfiguration",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScrapeConfigurationEntityId",
                table: "ScrapeDirectories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScrapeConfigurationEntityId",
                table: "ConnectionStrings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ScrapeConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeConfiguration", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserConfiguration_ScrapeConfigurationEntityId",
                table: "UserConfiguration",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchConfiguration_ScrapeConfigurationEntityId",
                table: "SearchConfiguration",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScrapeDirectories_ScrapeConfigurationEntityId",
                table: "ScrapeDirectories",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionStrings_ScrapeConfigurationEntityId",
                table: "ConnectionStrings",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectionStrings_ScrapeConfiguration_ScrapeConfigurationEntityId",
                table: "ConnectionStrings",
                column: "ScrapeConfigurationEntityId",
                principalTable: "ScrapeConfiguration",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScrapeDirectories_ScrapeConfiguration_ScrapeConfigurationEntityId",
                table: "ScrapeDirectories",
                column: "ScrapeConfigurationEntityId",
                principalTable: "ScrapeConfiguration",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SearchConfiguration_ScrapeConfiguration_ScrapeConfigurationEntityId",
                table: "SearchConfiguration",
                column: "ScrapeConfigurationEntityId",
                principalTable: "ScrapeConfiguration",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserConfiguration_ScrapeConfiguration_ScrapeConfigurationEntityId",
                table: "UserConfiguration",
                column: "ScrapeConfigurationEntityId",
                principalTable: "ScrapeConfiguration",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
