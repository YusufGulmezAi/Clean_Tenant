namespace CleanTenant.SharedKernel.Entities;

/// <summary>
/// <para>
/// Agregat kök (DDD <em>aggregate root</em>) işaretleyici arabirimidir.
/// Agregat kökü, sistem tarafından bağımsız bir bütün olarak kaydedilen,
/// dışarıdan referans verilebilen ve invariant'larından sorumlu olan
/// entity'dir. İç (detail) entity'ler bu arabirimi implement etmez.
/// </para>
/// <para>
/// Repository'ler yalnız agregat köklerine yönelik tasarlanır; child
/// entity'lere doğrudan erişim agregat köküyle yapılır.
/// </para>
/// </summary>
public interface IAggregateRoot : IEntity
{
}
