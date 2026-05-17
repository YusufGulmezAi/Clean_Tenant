using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <see cref="UserRoleAssignment"/> entity'sinin EF Core eşlemesi.
/// Scope tutarlılığı DB CHECK constraint ile dayatılır;
/// <c>(UserId, RoleId, ScopeLevel, TenantId, CompanyId, UnitId)</c> bileşik
/// unique index ile çift atama engellenir.
/// </summary>
public sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.ScopeLevel)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(a => a.AssignedAt).IsRequired();
        builder.Property(a => a.IsActive).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(a => a.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // CHECK constraint — scope seviyesi ile null/non-null kombinasyonu tutarlı olmalı.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_user_role_assignment_scope_consistency",
            $"""
            (scope_level = {(short)ScopeLevel.System}      AND tenant_id IS NULL     AND company_id IS NULL AND unit_id IS NULL)
            OR (scope_level = {(short)ScopeLevel.Tenant}   AND tenant_id IS NOT NULL AND company_id IS NULL AND unit_id IS NULL)
            OR (scope_level = {(short)ScopeLevel.Company}  AND tenant_id IS NOT NULL AND company_id IS NOT NULL AND unit_id IS NULL)
            OR (scope_level = {(short)ScopeLevel.Unit}     AND tenant_id IS NOT NULL AND company_id IS NOT NULL AND unit_id IS NOT NULL)
            """));

        // Unique: aynı kullanıcı aynı role'u aynı scope'a iki kez alamaz.
        builder.HasIndex(a => new { a.UserId, a.RoleId, a.ScopeLevel, a.TenantId, a.CompanyId, a.UnitId })
            .IsUnique()
            .HasDatabaseName("ix_user_role_assignment_unique");

        // Performans index'i: kullanıcı bazında atamaları hızlı çekmek için.
        builder.HasIndex(a => new { a.UserId, a.IsActive });

        builder.UseXminAsConcurrencyToken();

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
