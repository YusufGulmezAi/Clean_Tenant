using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantLegalIdentityAndAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "tenants",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_identity_number",
                table: "tenants",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<short>(
                name: "legal_identity_type",
                table: "tenants",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            // Mevcut satırlar (boş legal_identity_number) için VKN placeholder backfill.
            // 1000000001, 1000000002, ... — VKN regex (^[1-9][0-9]{9}$) ile uyumlu ve tekil.
            // Production'da bu placeholder'lar gerçek VKN'lerle güncellenmeli.
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT id, ROW_NUMBER() OVER (ORDER BY id) AS rn
                    FROM tenants
                    WHERE legal_identity_number = ''
                )
                UPDATE tenants t
                SET legal_identity_type = 1,
                    legal_identity_number = LPAD((1000000000 + n.rn)::text, 10, '0')
                FROM numbered n
                WHERE t.id = n.id;");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_legal_identity_number",
                table: "tenants",
                column: "legal_identity_number",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_legal_identity_format",
                table: "tenants",
                sql: "(legal_identity_type = 1 AND legal_identity_number ~ '^[1-9][0-9]{9}$') OR (legal_identity_type = 2 AND legal_identity_number ~ '^[1-9][0-9]{10}$') OR (legal_identity_type = 3 AND legal_identity_number ~ '^99[0-9]{9}$')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tenants_legal_identity_number",
                table: "tenants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_legal_identity_format",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "address",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "legal_identity_number",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "legal_identity_type",
                table: "tenants");
        }
    }
}
