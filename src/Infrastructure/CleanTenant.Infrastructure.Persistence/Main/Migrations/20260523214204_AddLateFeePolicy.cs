using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class AddLateFeePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "late_fee_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: true),
                    monthly_rate_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_compound = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    grace_days = table.Column<int>(type: "integer", nullable: false),
                    income_account_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("pk_late_fee_policies", x => x.id);
                    table.CheckConstraint("ck_late_fee_policies_grace", "grace_days >= 0");
                    table.CheckConstraint("ck_late_fee_policies_rate", "monthly_rate_percent > 0");
                    table.ForeignKey(
                        name: "fk_late_fee_policies_account_codes_income_account_code_id",
                        column: x => x.income_account_code_id,
                        principalTable: "account_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_late_fee_policies_budget_plans_budget_id",
                        column: x => x.budget_id,
                        principalTable: "budget_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_late_fee_policies_budget_override",
                table: "late_fee_policies",
                column: "budget_id",
                unique: true,
                filter: "budget_id IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_late_fee_policies_company_default",
                table: "late_fee_policies",
                column: "company_id",
                unique: true,
                filter: "budget_id IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_late_fee_policies_income_account_code_id",
                table: "late_fee_policies",
                column: "income_account_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_late_fee_policies_tenant_id",
                table: "late_fee_policies",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "late_fee_policies");
        }
    }
}
