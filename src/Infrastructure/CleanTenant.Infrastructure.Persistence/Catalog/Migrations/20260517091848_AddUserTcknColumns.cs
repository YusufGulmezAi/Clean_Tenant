using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTcknColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tckn",
                table: "AspNetUsers",
                type: "character(11)",
                fixedLength: true,
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "tckn_verified",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_tckn",
                table: "AspNetUsers",
                column: "tckn",
                unique: true,
                filter: "tckn IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_tckn_format",
                table: "AspNetUsers",
                sql: "tckn IS NULL OR tckn ~ '^[0-9]{11}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_tckn",
                table: "AspNetUsers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_tckn_format",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "tckn",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "tckn_verified",
                table: "AspNetUsers");
        }
    }
}
