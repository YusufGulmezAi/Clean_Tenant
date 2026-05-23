using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class AddAccrualDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accruals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<short>(type: "smallint", nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: true),
                    budget_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accounting_period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    receivable_account_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    income_account_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    generated_by = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_accruals", x => x.id);
                    table.CheckConstraint("ck_accruals_month", "month BETWEEN 1 AND 12");
                });

            migrationBuilder.CreateTable(
                name: "accrual_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accrual_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    distribution_share = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    line_breakdown_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accrual_details", x => x.id);
                    table.CheckConstraint("ck_accrual_details_amount", "amount >= 0");
                    table.ForeignKey(
                        name: "fk_accrual_details_accruals_accrual_id",
                        column: x => x.accrual_id,
                        principalTable: "accruals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_accrual_details_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accrual_details_accrual_unit",
                table: "accrual_details",
                columns: new[] { "accrual_id", "unit_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_accrual_details_tenant_id",
                table: "accrual_details",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_accrual_details_unit_id",
                table: "accrual_details",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_accruals_budget_period",
                table: "accruals",
                columns: new[] { "budget_id", "accounting_period_id" },
                unique: true,
                filter: "source = 0 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_accruals_company_id",
                table: "accruals",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_accruals_tenant_id",
                table: "accruals",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accrual_details");

            migrationBuilder.DropTable(
                name: "accruals");
        }
    }
}
