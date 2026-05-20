using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Main.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVknFormatAllowZeroPrefix : Migration
    {
        // Not: Snapshot'ta url_code_registry tablosu vardı ama önceki migration'lara
        // alınmamıştı; DB tarafında manuel olarak zaten oluşturulmuş. Bu migration
        // yalnız VKN check constraint güncellemesi yapar; CreateTable çağrıları
        // EF'in otomatik diff'inden manuel olarak çıkarıldı (snapshot/DB tutarlı).

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_company_vkn_format",
                table: "companies");

            migrationBuilder.AddCheckConstraint(
                name: "ck_company_vkn_format",
                table: "companies",
                sql: "vkn IS NULL OR vkn ~ '^[0-9]{10}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_company_vkn_format",
                table: "companies");

            migrationBuilder.AddCheckConstraint(
                name: "ck_company_vkn_format",
                table: "companies",
                sql: "vkn IS NULL OR vkn ~ '^[1-9][0-9]{9}$'");
        }
    }
}
