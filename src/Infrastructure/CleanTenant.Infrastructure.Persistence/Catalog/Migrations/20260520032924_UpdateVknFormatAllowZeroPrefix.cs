using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVknFormatAllowZeroPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_legal_identity_format",
                table: "tenants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_vkn_format",
                table: "AspNetUsers");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_legal_identity_format",
                table: "tenants",
                sql: "(legal_identity_type = 1 AND legal_identity_number ~ '^[0-9]{10}$') OR (legal_identity_type = 2 AND legal_identity_number ~ '^[1-9][0-9]{10}$') OR (legal_identity_type = 3 AND legal_identity_number ~ '^99[0-9]{9}$')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_vkn_format",
                table: "AspNetUsers",
                sql: "vkn IS NULL OR vkn ~ '^[0-9]{10}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_legal_identity_format",
                table: "tenants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_vkn_format",
                table: "AspNetUsers");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_legal_identity_format",
                table: "tenants",
                sql: "(legal_identity_type = 1 AND legal_identity_number ~ '^[1-9][0-9]{9}$') OR (legal_identity_type = 2 AND legal_identity_number ~ '^[1-9][0-9]{10}$') OR (legal_identity_type = 3 AND legal_identity_number ~ '^99[0-9]{9}$')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_vkn_format",
                table: "AspNetUsers",
                sql: "vkn IS NULL OR vkn ~ '^[1-9][0-9]{9}$'");
        }
    }
}
