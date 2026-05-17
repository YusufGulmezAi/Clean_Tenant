using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddUserVknColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "vkn",
                table: "AspNetUsers",
                type: "character(10)",
                fixedLength: true,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "vkn_verified",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_vkn",
                table: "AspNetUsers",
                column: "vkn",
                unique: true,
                filter: "vkn IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_vkn_format",
                table: "AspNetUsers",
                sql: "vkn IS NULL OR vkn ~ '^[1-9][0-9]{9}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_vkn",
                table: "AspNetUsers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_vkn_format",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "vkn",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "vkn_verified",
                table: "AspNetUsers");
        }
    }
}
