using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalAuditFieldsToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RemoteModifiedAt",
                table: "SyncedItems",
                newName: "RemoteModifiedAt_Ticks");

            migrationBuilder.AlterColumn<long>(
                name: "RemoteModifiedAt_Ticks",
                table: "SyncedItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AddColumn<long>(
                name: "CreatedAt_Ticks",
                table: "SyncedItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedAt_Ticks",
                table: "SyncedItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CreatedAt_Ticks",
                table: "SyncConflicts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedAt_Ticks",
                table: "SyncConflicts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CreatedAt_Ticks",
                table: "ImageDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedAt_Ticks",
                table: "ImageDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CreatedAt_Ticks",
                table: "FileDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedAt_Ticks",
                table: "FileDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CreatedAt_Ticks",
                table: "FileClassifications",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedAt_Ticks",
                table: "FileClassifications",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CreatedAt_Ticks",
                table: "FileClassificationCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedAt_Ticks",
                table: "FileClassificationCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CreatedAt_Ticks",
                table: "DriveStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedAt_Ticks",
                table: "DriveStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CreatedAt_Ticks",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedAt_Ticks",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt_Ticks",
                table: "SyncedItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt_Ticks",
                table: "SyncedItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt_Ticks",
                table: "SyncConflicts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt_Ticks",
                table: "SyncConflicts");

            migrationBuilder.DropColumn(
                name: "CreatedAt_Ticks",
                table: "ImageDetail");

            migrationBuilder.DropColumn(
                name: "UpdatedAt_Ticks",
                table: "ImageDetail");

            migrationBuilder.DropColumn(
                name: "CreatedAt_Ticks",
                table: "FileDetail");

            migrationBuilder.DropColumn(
                name: "UpdatedAt_Ticks",
                table: "FileDetail");

            migrationBuilder.DropColumn(
                name: "CreatedAt_Ticks",
                table: "FileClassifications");

            migrationBuilder.DropColumn(
                name: "UpdatedAt_Ticks",
                table: "FileClassifications");

            migrationBuilder.DropColumn(
                name: "CreatedAt_Ticks",
                table: "FileClassificationCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt_Ticks",
                table: "FileClassificationCategories");

            migrationBuilder.DropColumn(
                name: "CreatedAt_Ticks",
                table: "DriveStates");

            migrationBuilder.DropColumn(
                name: "UpdatedAt_Ticks",
                table: "DriveStates");

            migrationBuilder.DropColumn(
                name: "CreatedAt_Ticks",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt_Ticks",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "RemoteModifiedAt_Ticks",
                table: "SyncedItems",
                newName: "RemoteModifiedAt");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RemoteModifiedAt",
                table: "SyncedItems",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");
        }
    }
}
