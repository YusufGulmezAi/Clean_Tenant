namespace CleanTenant.SharedKernel.Time;

/// <summary>
/// <para>
/// Test edilebilir zaman erişimi için soyutlama. <see cref="System.DateTime.UtcNow"/>
/// doğrudan kullanılırsa zaman freeze edilemez ve testler güvenilmez olur.
/// </para>
/// <para>
/// <b>DI kaydı:</b> Production'da <see cref="SystemClock"/> singleton olarak
/// kaydedilir. Testlerde NSubstitute ile mock'lanır:
/// <c>clock.UtcNow.Returns(new DateTimeOffset(2026, 5, 17, 0, 0, 0, TimeSpan.Zero));</c>
/// </para>
/// </summary>
public interface IClock
{
    /// <summary>Şu anki UTC zamanı.</summary>
    DateTimeOffset UtcNow { get; }
}
