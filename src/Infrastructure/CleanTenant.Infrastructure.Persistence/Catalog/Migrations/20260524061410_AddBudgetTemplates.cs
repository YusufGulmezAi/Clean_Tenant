using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "budget_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    visibility = table.Column<short>(type: "smallint", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    url_code = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    source_label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("pk_budget_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "budget_template_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_category_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    line_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    line_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    line_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    payment_schedule = table.Column<short>(type: "smallint", nullable: false),
                    distribution_model = table.Column<short>(type: "smallint", nullable: false),
                    distribution_config = table.Column<string>(type: "text", nullable: true),
                    due_day_of_month = table.Column<int>(type: "integer", nullable: false),
                    participation_group_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    participation_group_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    installment_interval_months = table.Column<int>(type: "integer", nullable: true),
                    installment_count = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_budget_template_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_budget_template_lines_budget_templates_budget_template_id",
                        column: x => x.budget_template_id,
                        principalTable: "budget_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_budget_template_lines_template_line",
                table: "budget_template_lines",
                columns: new[] { "budget_template_id", "line_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_budget_templates_owner_tenant_id",
                table: "budget_templates",
                column: "owner_tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_templates_type",
                table: "budget_templates",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_budget_templates_url_code",
                table: "budget_templates",
                column: "url_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_template_lines");

            migrationBuilder.DropTable(
                name: "budget_templates");
        }
    }
}
