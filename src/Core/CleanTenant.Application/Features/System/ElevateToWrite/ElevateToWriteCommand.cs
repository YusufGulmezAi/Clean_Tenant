namespace CleanTenant.Application.Features.System.ElevateToWrite;

/// <summary>
/// Support Mode'da ReadOnly'den WriteEnabled'a yükselme isteği.
/// Mevcut session in-place mutate edilir (JWT yenilenmiyor).
/// </summary>
/// <param name="Reason">Zorunlu sebep (min 20 karakter); audit'e işlenir.</param>
public sealed record ElevateToWriteCommand(string Reason);
