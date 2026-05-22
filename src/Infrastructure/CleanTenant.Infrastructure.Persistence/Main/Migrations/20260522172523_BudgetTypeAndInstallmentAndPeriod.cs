using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class BudgetTypeAndInstallmentAndPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_budget_plans_company_fy",
                table: "budget_plans");

            migrationBuilder.AddColumn<Guid>(
                name: "income_account_code_id",
                table: "budget_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "period_end_month",
                table: "budget_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "period_end_year",
                table: "budget_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "period_start_month",
                table: "budget_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "period_start_year",
                table: "budget_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "receivable_account_code_id",
                table: "budget_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "type",
                table: "budget_plans",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "installment_end_month",
                table: "budget_line_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "installment_end_year",
                table: "budget_line_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "installment_interval_months",
                table: "budget_line_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "installment_start_month",
                table: "budget_line_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "installment_start_year",
                table: "budget_line_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "budget_line_installments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_line_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installment_number = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_manually_edited = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_budget_line_installments", x => x.id);
                    table.CheckConstraint("ck_bli_amount", "amount >= 0");
                    table.CheckConstraint("ck_bli_installment_number", "installment_number > 0");
                    table.CheckConstraint("ck_bli_month", "month BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "fk_budget_line_installments_budget_line_versions_budget_line_v",
                        column: x => x.budget_line_version_id,
                        principalTable: "budget_line_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_company_fy_type_title",
                table: "budget_plans",
                columns: new[] { "company_id", "fiscal_year_id", "type", "title" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddCheckConstraint(
                name: "ck_budget_plans_period_months",
                table: "budget_plans",
                sql: "period_start_month BETWEEN 1 AND 12 AND period_end_month BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "ck_budget_plans_period_order",
                table: "budget_plans",
                sql: "(period_end_year * 12 + period_end_month) >= (period_start_year * 12 + period_start_month)");

            migrationBuilder.CreateIndex(
                name: "ix_bli_version_year_month",
                table: "budget_line_installments",
                columns: new[] { "budget_line_version_id", "year", "month" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_budget_line_installments_tenant_id",
                table: "budget_line_installments",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_line_installments");

            migrationBuilder.DropIndex(
                name: "ix_budget_plans_company_fy_type_title",
                table: "budget_plans");

            migrationBuilder.DropCheckConstraint(
                name: "ck_budget_plans_period_months",
                table: "budget_plans");

            migrationBuilder.DropCheckConstraint(
                name: "ck_budget_plans_period_order",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "income_account_code_id",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "period_end_month",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "period_end_year",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "period_start_month",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "period_start_year",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "receivable_account_code_id",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "type",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "installment_end_month",
                table: "budget_line_versions");

            migrationBuilder.DropColumn(
                name: "installment_end_year",
                table: "budget_line_versions");

            migrationBuilder.DropColumn(
                name: "installment_interval_months",
                table: "budget_line_versions");

            migrationBuilder.DropColumn(
                name: "installment_start_month",
                table: "budget_line_versions");

            migrationBuilder.DropColumn(
                name: "installment_start_year",
                table: "budget_line_versions");

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_company_fy",
                table: "budget_plans",
                columns: new[] { "company_id", "fiscal_year_id" },
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
