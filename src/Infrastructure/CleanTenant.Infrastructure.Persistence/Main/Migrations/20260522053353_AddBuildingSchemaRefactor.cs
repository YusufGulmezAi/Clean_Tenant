using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingSchemaRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_blocks_companies_company_id",
                table: "blocks");

            migrationBuilder.DropForeignKey(
                name: "fk_parcels_blocks_block_id",
                table: "parcels");

            migrationBuilder.DropIndex(
                name: "ix_units_building_number",
                table: "units");

            migrationBuilder.RenameColumn(
                name: "block_id",
                table: "parcels",
                newName: "land_id");

            migrationBuilder.RenameIndex(
                name: "ix_parcels_block_name",
                table: "parcels",
                newName: "ix_parcels_land_name");

            migrationBuilder.RenameIndex(
                name: "ix_parcels_block_id",
                table: "parcels",
                newName: "ix_parcels_land_id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                table: "blocks",
                newName: "building_id");

            migrationBuilder.RenameIndex(
                name: "ix_blocks_company_name",
                table: "blocks",
                newName: "ix_blocks_building_name");

            migrationBuilder.RenameIndex(
                name: "ix_blocks_company_id",
                table: "blocks",
                newName: "ix_blocks_building_id");

            migrationBuilder.AddColumn<Guid>(
                name: "block_id",
                table: "units",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "gross_square_meters",
                table: "units",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "room_count",
                table: "units",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "lands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url_code = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_lands", x => x.id);
                    table.ForeignKey(
                        name: "fk_lands_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_units_block_id",
                table: "units",
                column: "block_id");

            migrationBuilder.CreateIndex(
                name: "ix_units_block_number",
                table: "units",
                columns: new[] { "block_id", "number" },
                unique: true,
                filter: "is_deleted = false AND block_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_units_building_number",
                table: "units",
                columns: new[] { "building_id", "number" },
                unique: true,
                filter: "is_deleted = false AND block_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_lands_company_id",
                table: "lands",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_lands_company_name",
                table: "lands",
                columns: new[] { "company_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lands_tenant_id",
                table: "lands",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_lands_url_code",
                table: "lands",
                column: "url_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_blocks_buildings_building_id",
                table: "blocks",
                column: "building_id",
                principalTable: "buildings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_parcels_lands_land_id",
                table: "parcels",
                column: "land_id",
                principalTable: "lands",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_units_blocks_block_id",
                table: "units",
                column: "block_id",
                principalTable: "blocks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_blocks_buildings_building_id",
                table: "blocks");

            migrationBuilder.DropForeignKey(
                name: "fk_parcels_lands_land_id",
                table: "parcels");

            migrationBuilder.DropForeignKey(
                name: "fk_units_blocks_block_id",
                table: "units");

            migrationBuilder.DropTable(
                name: "lands");

            migrationBuilder.DropIndex(
                name: "ix_units_block_id",
                table: "units");

            migrationBuilder.DropIndex(
                name: "ix_units_block_number",
                table: "units");

            migrationBuilder.DropIndex(
                name: "ix_units_building_number",
                table: "units");

            migrationBuilder.DropColumn(
                name: "block_id",
                table: "units");

            migrationBuilder.DropColumn(
                name: "gross_square_meters",
                table: "units");

            migrationBuilder.DropColumn(
                name: "room_count",
                table: "units");

            migrationBuilder.RenameColumn(
                name: "land_id",
                table: "parcels",
                newName: "block_id");

            migrationBuilder.RenameIndex(
                name: "ix_parcels_land_name",
                table: "parcels",
                newName: "ix_parcels_block_name");

            migrationBuilder.RenameIndex(
                name: "ix_parcels_land_id",
                table: "parcels",
                newName: "ix_parcels_block_id");

            migrationBuilder.RenameColumn(
                name: "building_id",
                table: "blocks",
                newName: "company_id");

            migrationBuilder.RenameIndex(
                name: "ix_blocks_building_name",
                table: "blocks",
                newName: "ix_blocks_company_name");

            migrationBuilder.RenameIndex(
                name: "ix_blocks_building_id",
                table: "blocks",
                newName: "ix_blocks_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_units_building_number",
                table: "units",
                columns: new[] { "building_id", "number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "fk_blocks_companies_company_id",
                table: "blocks",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_parcels_blocks_block_id",
                table: "parcels",
                column: "block_id",
                principalTable: "blocks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
