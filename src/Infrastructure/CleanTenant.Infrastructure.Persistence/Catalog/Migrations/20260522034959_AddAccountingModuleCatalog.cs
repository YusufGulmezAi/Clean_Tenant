using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingModuleCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chart_of_accounts_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 12, nullable: false),
                    parent_code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    level = table.Column<short>(type: "smallint", nullable: false),
                    account_class = table.Column<short>(type: "smallint", nullable: false),
                    account_type = table.Column<short>(type: "smallint", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_detail = table.Column<bool>(type: "boolean", nullable: false),
                    is_monetary = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_chart_of_accounts_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inflation_indexes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    index_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
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
                    table.PrimaryKey("pk_inflation_indexes", x => x.id);
                    table.CheckConstraint("ck_inflation_index", "index_value > 0");
                    table.CheckConstraint("ck_inflation_month", "month BETWEEN 1 AND 12");
                });

            migrationBuilder.CreateIndex(
                name: "ix_chart_of_accounts_templates_code",
                table: "chart_of_accounts_templates",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inflation_indexes_year_month",
                table: "inflation_indexes",
                columns: new[] { "year", "month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chart_of_accounts_templates");

            migrationBuilder.DropTable(
                name: "inflation_indexes");
        }
    }
}
