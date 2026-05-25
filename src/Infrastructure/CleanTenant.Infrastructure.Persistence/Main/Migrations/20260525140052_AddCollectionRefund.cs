using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionRefund : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "collection_refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    refund_date = table.Column<DateOnly>(type: "date", nullable: false),
                    cash_account_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    advance_account_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<short>(type: "smallint", nullable: false),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refunded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    refunded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collection_refunds", x => x.id);
                    table.CheckConstraint("ck_collection_refunds_amount", "amount > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_collection_refunds_company_id",
                table: "collection_refunds",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_refunds_tenant_id",
                table: "collection_refunds",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_refunds_unit_id",
                table: "collection_refunds",
                column: "unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "collection_refunds");
        }
    }
}
