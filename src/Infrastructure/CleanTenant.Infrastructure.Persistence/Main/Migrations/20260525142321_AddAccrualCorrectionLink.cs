using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class AddAccrualCorrectionLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_accrual_details_amount",
                table: "accrual_details");

            migrationBuilder.AddColumn<Guid>(
                name: "corrected_accrual_detail_id",
                table: "accrual_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_accrual_details_corrected_accrual_detail_id",
                table: "accrual_details",
                column: "corrected_accrual_detail_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_accrual_details_corrected_accrual_detail_id",
                table: "accrual_details");

            migrationBuilder.DropColumn(
                name: "corrected_accrual_detail_id",
                table: "accrual_details");

            migrationBuilder.AddCheckConstraint(
                name: "ck_accrual_details_amount",
                table: "accrual_details",
                sql: "amount >= 0");
        }
    }
}
