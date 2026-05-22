using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class AllowDraftBudgetVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_budget_version_dates",
                table: "budget_versions");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "valid_from",
                table: "budget_versions",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "published_at",
                table: "budget_versions",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamptz");

            migrationBuilder.AddCheckConstraint(
                name: "ck_budget_version_dates",
                table: "budget_versions",
                sql: "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from");

            migrationBuilder.AddCheckConstraint(
                name: "ck_budget_version_publish_consistency",
                table: "budget_versions",
                sql: "(published_at IS NULL AND valid_from IS NULL) OR (published_at IS NOT NULL AND valid_from IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_budget_version_dates",
                table: "budget_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_budget_version_publish_consistency",
                table: "budget_versions");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "valid_from",
                table: "budget_versions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "published_at",
                table: "budget_versions",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_budget_version_dates",
                table: "budget_versions",
                sql: "valid_to IS NULL OR valid_to >= valid_from");
        }
    }
}
