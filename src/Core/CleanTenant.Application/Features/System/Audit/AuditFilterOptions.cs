namespace CleanTenant.Application.Features.System.Audit;

/// <summary>
/// Audit Explorer filtre panelinin autocomplete kaynakları. Sayfa ilk
/// yüklendiğinde DB'den distinct olarak yüklenir; kullanıcının görmediği
/// kayıtlardaki değerleri de aratabilmesini sağlar.
/// </summary>
public sealed record AuditFilterOptions(
    IReadOnlyList<string> UserFullNames,
    IReadOnlyList<string> EntityTypes,
    IReadOnlyList<string> TenantNames);
