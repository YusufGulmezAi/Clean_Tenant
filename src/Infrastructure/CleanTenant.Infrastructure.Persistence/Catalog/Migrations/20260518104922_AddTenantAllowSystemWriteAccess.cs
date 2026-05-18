using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanTenant.Infrastructure.Persistence.Catalog.Migrations
{
    /// <summary>
    /// v0.2.3.c — Support Mode v2: Sistem kullanıcısının bir Yönetim'e destek
    /// erişiminde yazma yetkisinin olup olmadığını taşıyan boolean kolonu.
    /// Default <c>true</c> — YönetimAdmin parametreyi mail link onayıyla kapatabilir.
    /// </summary>
    /// <remarks>
    /// EF auto-generate sırasında snapshot'a Companies/AuditEntries
    /// configuration'ları sızmış görünüyordu (assembly filter eksikliği).
    /// v0.2.3.c'de filter eklendi; bu tablolar Catalog'da hiç yaratılmadığı için
    /// DropTable adımları kaldırıldı — snapshot temizliği Designer.cs üzerinden
    /// otomatik geri sarıldı.
    /// </remarks>
    public partial class AddTenantAllowSystemWriteAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_system_write_access",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_system_write_access",
                table: "tenants");
        }
    }
}
