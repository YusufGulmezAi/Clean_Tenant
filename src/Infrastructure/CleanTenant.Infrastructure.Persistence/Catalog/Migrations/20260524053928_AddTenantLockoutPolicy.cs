using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantLockoutPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "lockout_duration_minutes",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddColumn<bool>(
                name: "lockout_enabled",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "lockout_max_failed_attempts",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lockout_duration_minutes",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "lockout_enabled",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "lockout_max_failed_attempts",
                table: "tenants");
        }
    }
}
