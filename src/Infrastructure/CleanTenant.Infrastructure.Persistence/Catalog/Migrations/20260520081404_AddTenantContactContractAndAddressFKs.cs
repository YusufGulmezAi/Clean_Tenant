using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantContactContractAndAddressFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                table: "tenants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_person",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                table: "tenants",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "contract_end_date",
                table: "tenants",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "contract_start_date",
                table: "tenants",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "district_id",
                table: "tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "neighborhood_id",
                table: "tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "province_id",
                table: "tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "transition_grace_days",
                table: "tenants",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenants_district_id",
                table: "tenants",
                column: "district_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_neighborhood_id",
                table: "tenants",
                column: "neighborhood_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_province_id",
                table: "tenants",
                column: "province_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tenants_district_id",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "ix_tenants_neighborhood_id",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "ix_tenants_province_id",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "contact_email",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "contact_person",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "contact_phone",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "contract_end_date",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "contract_start_date",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "district_id",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "neighborhood_id",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "province_id",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "transition_grace_days",
                table: "tenants");
        }
    }
}
