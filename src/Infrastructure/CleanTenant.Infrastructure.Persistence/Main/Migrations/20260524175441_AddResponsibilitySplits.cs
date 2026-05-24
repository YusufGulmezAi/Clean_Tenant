using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class AddResponsibilitySplits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "responsibility_mode",
                table: "budget_plans",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "responsibility_mode",
                table: "accruals",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "primary_responsible_party_id",
                table: "accrual_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "responsible_resolved_note",
                table: "accrual_details",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "accrual_responsibility_splits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accrual_detail_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<short>(type: "smallint", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: false),
                    day_count = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_accrual_responsibility_splits", x => x.id);
                    table.CheckConstraint("ck_ars_amount", "amount >= 0");
                    table.CheckConstraint("ck_ars_dates", "to_date >= from_date");
                    table.CheckConstraint("ck_ars_daycount", "day_count > 0");
                    table.ForeignKey(
                        name: "fk_accrual_responsibility_splits_accrual_details_accrual_detai",
                        column: x => x.accrual_detail_id,
                        principalTable: "accrual_details",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_accrual_responsibility_splits_parties_party_id",
                        column: x => x.party_id,
                        principalTable: "parties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accrual_details_primary_responsible_party_id",
                table: "accrual_details",
                column: "primary_responsible_party_id");

            migrationBuilder.CreateIndex(
                name: "ix_accrual_responsibility_splits_accrual_detail_id",
                table: "accrual_responsibility_splits",
                column: "accrual_detail_id");

            migrationBuilder.CreateIndex(
                name: "ix_accrual_responsibility_splits_party_id",
                table: "accrual_responsibility_splits",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_accrual_responsibility_splits_tenant_id",
                table: "accrual_responsibility_splits",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_accrual_details_parties_primary_responsible_party_id",
                table: "accrual_details",
                column: "primary_responsible_party_id",
                principalTable: "parties",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_accrual_details_parties_primary_responsible_party_id",
                table: "accrual_details");

            migrationBuilder.DropTable(
                name: "accrual_responsibility_splits");

            migrationBuilder.DropIndex(
                name: "ix_accrual_details_primary_responsible_party_id",
                table: "accrual_details");

            migrationBuilder.DropColumn(
                name: "responsibility_mode",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "responsibility_mode",
                table: "accruals");

            migrationBuilder.DropColumn(
                name: "primary_responsible_party_id",
                table: "accrual_details");

            migrationBuilder.DropColumn(
                name: "responsible_resolved_note",
                table: "accrual_details");
        }
    }
}
