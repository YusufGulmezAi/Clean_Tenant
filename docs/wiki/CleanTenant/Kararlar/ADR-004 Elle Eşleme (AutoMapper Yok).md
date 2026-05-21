# ADR-004 — Elle Eşleme (AutoMapper / Mapster Yok)

**Durum:** Kabul edildi (v0.1.x)
**Etkilenen katmanlar:** Application, tüm feature klasörleri

---

## Karar

AutoMapper, Mapster veya reflection tabanlı herhangi bir mapper kütüphanesi kullanılmaz.
Tüm eşlemeler **statik extension sınıfları** ile elle yazılır.

## Kural

```csharp
// Dosya adı: CompanyMappingExtensions.cs
// Konum: tüketen feature klasörünün yanında
public static class CompanyMappingExtensions
{
    public static CompanyDto ToDto(this Company company) => new(...);
    public static Company ToDomain(this CreateCompanyCommand cmd) => new(...);
}
```

## Gerekçe

- Reflection tabanlı mapper'lar derleme zamanında sessizce başarısız olabilir
- Explicit eşleme = her alanın kasıtlı olarak eşlendiğinin garantisi
- IDE navigasyonu doğrudan çalışır (F12 her zaman hedefe gider)
- .NET 10 Native AOT uyumluluğu (reflection sorunları yok)
- Daha az sürpriz: "neden bu alan eşlenmedi?" sorusu ortadan kalkar

## İlgili
- [[Mimari/Clean Architecture Katmanları]]
