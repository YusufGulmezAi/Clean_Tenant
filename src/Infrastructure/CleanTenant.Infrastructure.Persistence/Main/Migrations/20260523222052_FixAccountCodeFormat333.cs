using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class FixAccountCodeFormat333 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_account_code_format",
                table: "account_codes");

            migrationBuilder.AddCheckConstraint(
                name: "ck_account_code_format",
                table: "account_codes",
                sql: "code ~ '^[1-9][0-9]{2}(\\.[0-9]{3}(\\.[0-9]{3})?)?$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_account_code_format",
                table: "account_codes");

            migrationBuilder.AddCheckConstraint(
                name: "ck_account_code_format",
                table: "account_codes",
                sql: "code ~ '^[1-9][0-9]{2}(\\.[0-9]{2}(\\.[0-9]{3})?)?$'");
        }
    }
}
