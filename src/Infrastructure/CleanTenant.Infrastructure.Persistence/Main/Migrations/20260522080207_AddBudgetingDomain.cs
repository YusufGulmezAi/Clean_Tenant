using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetingDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "budget_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url_code = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_budget_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expense_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_expense_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_expense_categories_expense_categories_parent_category_id",
                        column: x => x.parent_category_id,
                        principalTable: "expense_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "participation_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_participation_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "budget_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    previous_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    published_by = table.Column<Guid>(type: "uuid", nullable: true),
                    revision_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_budget_versions", x => x.id);
                    table.CheckConstraint("ck_budget_version_dates", "valid_to IS NULL OR valid_to >= valid_from");
                    table.CheckConstraint("ck_budget_version_number", "version_number > 0");
                    table.ForeignKey(
                        name: "fk_budget_versions_budgets_budget_id",
                        column: x => x.budget_id,
                        principalTable: "budget_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "budget_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_budget_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_budget_lines_expense_categories_expense_category_id",
                        column: x => x.expense_category_id,
                        principalTable: "expense_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_participation_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participation_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_unit_participation_groups", x => x.id);
                    table.CheckConstraint("ck_upg_dates", "valid_to IS NULL OR valid_to >= valid_from");
                    table.ForeignKey(
                        name: "fk_unit_participation_groups_participation_groups_participatio",
                        column: x => x.participation_group_id,
                        principalTable: "participation_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_unit_participation_groups_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "budget_line_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    payment_schedule = table.Column<short>(type: "smallint", nullable: false),
                    distribution_model = table.Column<short>(type: "smallint", nullable: false),
                    participation_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    distribution_config = table.Column<string>(type: "jsonb", nullable: true),
                    is_manual_override = table.Column<bool>(type: "boolean", nullable: false),
                    override_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    due_day_of_month = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_budget_line_versions", x => x.id);
                    table.CheckConstraint("ck_blv_due_day", "due_day_of_month BETWEEN 1 AND 31");
                    table.CheckConstraint("ck_blv_planned_amount", "planned_amount >= 0");
                    table.ForeignKey(
                        name: "fk_budget_line_versions_budget_lines_budget_line_id",
                        column: x => x.budget_line_id,
                        principalTable: "budget_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_budget_line_versions_budget_versions_budget_version_id",
                        column: x => x.budget_version_id,
                        principalTable: "budget_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_budget_line_versions_participation_groups_participation_gro",
                        column: x => x.participation_group_id,
                        principalTable: "participation_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exemption_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("pk_exemption_rules", x => x.id);
                    table.CheckConstraint("ck_exemption_dates", "valid_to IS NULL OR valid_to >= valid_from");
                    table.ForeignKey(
                        name: "fk_exemption_rules_budget_lines_budget_line_id",
                        column: x => x.budget_line_id,
                        principalTable: "budget_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exemption_rules_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_blv_version_line",
                table: "budget_line_versions",
                columns: new[] { "budget_version_id", "budget_line_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_budget_line_versions_budget_line_id",
                table: "budget_line_versions",
                column: "budget_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_line_versions_participation_group_id",
                table: "budget_line_versions",
                column: "participation_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_line_versions_tenant_id",
                table: "budget_line_versions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_lines_company_code",
                table: "budget_lines",
                columns: new[] { "company_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_budget_lines_company_id",
                table: "budget_lines",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_lines_expense_category_id",
                table: "budget_lines",
                column: "expense_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_lines_tenant_id",
                table: "budget_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_company_fy",
                table: "budget_plans",
                columns: new[] { "company_id", "fiscal_year_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_company_id",
                table: "budget_plans",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_tenant_id",
                table: "budget_plans",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_url_code",
                table: "budget_plans",
                column: "url_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_budget_versions_budget_id",
                table: "budget_versions",
                column: "budget_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_versions_budget_version",
                table: "budget_versions",
                columns: new[] { "budget_id", "version_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_budget_versions_tenant_id",
                table: "budget_versions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_exemption_rules_budget_line_id",
                table: "exemption_rules",
                column: "budget_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_exemption_rules_company_id",
                table: "exemption_rules",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_exemption_rules_tenant_id",
                table: "exemption_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_exemption_rules_unit_id",
                table: "exemption_rules",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_categories_company_code",
                table: "expense_categories",
                columns: new[] { "company_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_expense_categories_company_id",
                table: "expense_categories",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_categories_parent_category_id",
                table: "expense_categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_categories_tenant_id",
                table: "expense_categories",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_participation_groups_company_code",
                table: "participation_groups",
                columns: new[] { "company_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_participation_groups_company_id",
                table: "participation_groups",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_participation_groups_tenant_id",
                table: "participation_groups",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_participation_groups_tenant_id",
                table: "unit_participation_groups",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_participation_groups_unit_id",
                table: "unit_participation_groups",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_upg_group_unit",
                table: "unit_participation_groups",
                columns: new[] { "participation_group_id", "unit_id" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_line_versions");

            migrationBuilder.DropTable(
                name: "exemption_rules");

            migrationBuilder.DropTable(
                name: "unit_participation_groups");

            migrationBuilder.DropTable(
                name: "budget_versions");

            migrationBuilder.DropTable(
                name: "budget_lines");

            migrationBuilder.DropTable(
                name: "participation_groups");

            migrationBuilder.DropTable(
                name: "budget_plans");

            migrationBuilder.DropTable(
                name: "expense_categories");
        }
    }
}
