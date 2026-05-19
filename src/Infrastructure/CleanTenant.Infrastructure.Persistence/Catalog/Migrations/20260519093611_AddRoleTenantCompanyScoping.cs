using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleTenantCompanyScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_role_normalized_name_scope",
                table: "AspNetRoles");

            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                table: "AspNetRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "AspNetRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_company_id",
                table: "AspNetRoles",
                column: "company_id");

            // NULLS NOT DISTINCT — global rollerde (TenantId=NULL, CompanyId=NULL)
            // PostgreSQL'in default davranışı NULL != NULL'dur; bu olmadan iki
            // farklı "global TenantAdmin" satırı oluşturulabilirdi. PG 15+ destekli.
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ""ix_role_normalized_name_scope_tenant_company""
                ON ""AspNetRoles"" (""normalized_name"", ""scope"", ""tenant_id"", ""company_id"")
                NULLS NOT DISTINCT;
            ");

            migrationBuilder.CreateIndex(
                name: "ix_role_tenant_id",
                table: "AspNetRoles",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_role_company_id",
                table: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "ix_role_normalized_name_scope_tenant_company",
                table: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "ix_role_tenant_id",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "AspNetRoles");

            migrationBuilder.CreateIndex(
                name: "ix_role_normalized_name_scope",
                table: "AspNetRoles",
                columns: new[] { "normalized_name", "scope" },
                unique: true);
        }
    }
}
