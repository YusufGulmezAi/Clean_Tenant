namespace CleanTenant.SharedKernel.Time;

/// <summary>
/// <para>
/// <see cref="IClock"/>'un production implementasyonu. Doğrudan
/// <see cref="DateTimeOffset.UtcNow"/> döner. DI'a singleton olarak
/// kaydedilir.
/// </para>
/// <para>
/// Kuralımız gereği proje genelinde <c>DateTime.Now</c> ve
/// <c>DateTime.UtcNow</c> doğrudan kullanılmaz; her yer
/// <see cref="IClock.UtcNow"/>'a bağımlıdır.
/// </para>
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
