using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class AddPartiesDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url_code = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<short>(type: "smallint", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    trade_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tckn = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    vkn = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tags_json = table.Column<string>(type: "jsonb", nullable: true),
                    kvkk_consent_given = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    kvkk_consent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    kvkk_consent_channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    linked_user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_parties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unit_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_role = table.Column<short>(type: "smallint", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_unit_contacts", x => x.id);
                    table.CheckConstraint("ck_unit_contacts_dates", "end_date IS NULL OR end_date >= start_date");
                    table.ForeignKey(
                        name: "fk_unit_contacts_parties_party_id",
                        column: x => x.party_id,
                        principalTable: "parties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_contacts_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_ownerships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    share_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_joint_and_several = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_unit_ownerships", x => x.id);
                    table.CheckConstraint("ck_unit_ownerships_dates", "end_date IS NULL OR end_date >= start_date");
                    table.CheckConstraint("ck_unit_ownerships_share", "share_percent > 0 AND share_percent <= 100");
                    table.ForeignKey(
                        name: "fk_unit_ownerships_parties_party_id",
                        column: x => x.party_id,
                        principalTable: "parties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_ownerships_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_tenancies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_unit_tenancies", x => x.id);
                    table.CheckConstraint("ck_unit_tenancies_dates", "end_date IS NULL OR end_date >= start_date");
                    table.ForeignKey(
                        name: "fk_unit_tenancies_parties_party_id",
                        column: x => x.party_id,
                        principalTable: "parties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_tenancies_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_parties_company_id",
                table: "parties",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_parties_company_tckn",
                table: "parties",
                columns: new[] { "company_id", "tckn" },
                unique: true,
                filter: "tckn IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_parties_tenant_id",
                table: "parties",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_parties_url_code",
                table: "parties",
                column: "url_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_unit_contacts_party_id",
                table: "unit_contacts",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_contacts_tenant_id",
                table: "unit_contacts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_contacts_unit_id",
                table: "unit_contacts",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_ownerships_party_id",
                table: "unit_ownerships",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_ownerships_tenant_id",
                table: "unit_ownerships",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_ownerships_unit_id",
                table: "unit_ownerships",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_tenancies_party_id",
                table: "unit_tenancies",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_tenancies_tenant_id",
                table: "unit_tenancies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_tenancies_unit_id",
                table: "unit_tenancies",
                column: "unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "unit_contacts");

            migrationBuilder.DropTable(
                name: "unit_ownerships");

            migrationBuilder.DropTable(
                name: "unit_tenancies");

            migrationBuilder.DropTable(
                name: "parties");
        }
    }
}
