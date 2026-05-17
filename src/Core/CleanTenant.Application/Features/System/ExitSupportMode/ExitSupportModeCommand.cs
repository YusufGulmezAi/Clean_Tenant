namespace CleanTenant.Application.Features.System.ExitSupportMode;

/// <summary>
/// Support Mode'dan çıkış. Mevcut Support session silinir, SupportSession DB
/// kaydının <c>EndedAt</c>'i set edilir, operatör orijinal session'ına döner.
/// Orijinal session TTL doldu ise hata döner; operatör tekrar login olmalı.
/// </summary>
public sealed record ExitSupportModeCommand();
