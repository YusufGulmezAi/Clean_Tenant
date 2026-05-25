---
name: add-entity
description: >
  Domain'e ZENGIN bir aggregate/entity ekler: private setter + statik fabrika +
  davranış metotları + invariant koruması + marker arayüzler + domain event +
  EF Core konfigürasyonu (CHECK/unique/FK) + migration + domain unit testleri.
  "Yeni entity/tablo/aggregate ekle", "şu kavramı modelle", "X varlığını oluştur",
  "domain'e Y ekle" gibi HER istekte kullan. Anemik (public setter'lı) entity
  ÜRETME — bu skill onu engeller. Mevcut anemik bir entity'yi zenginleştirirken de kullan.
---

# add-entity — Zengin Aggregate Üretici

Neden: anemik (public get/set) entity'ler invariant'ı koruyamaz; iş kuralı
handler'lara dağılır ve biri kuralı kazara bozabilir. Zengin aggregate kuralı
YAPISAL olarak bozulamaz kılar.

## Üreteceğin dosyalar
- `src/Core/<App>.Domain/<Area>/<Entity>.cs` (+ varsa child + Enums + ValueObject)
- `src/Core/<App>.Domain/Events/<Area>/<Entity>Created.cs`
- `src/Infrastructure/<App>.Persistence/.../Configurations/<Area>/<Entity>Configuration.cs`
- migration (bkz. `migration-flow`)
- (test) `tests/<App>.Domain.UnitTests/<Area>/<Entity>Tests.cs`

## Aggregate kuralları (ve nedenleri)
- **Setter'lar `private`**, EF için `private` parametresiz ctor. Nesne yalnız
  statik fabrika (`Create`/`Record`) veya davranış metoduyla geçerli kurulur.
- **Koleksiyonlar salt-okunur:** `private readonly List<TChild> _items = [];`
  → `public IReadOnlyCollection<TChild> Items => _items.AsReadOnly();`. Mutasyon
  yalnız aggregate metoduyla. (EF: `Navigation(x => x.Items).UsePropertyAccessMode(Field)`.)
- **Invariant fabrika/metot sonunda doğrulanır** (ör. `SUM(items) == Total`).
- **Marker arayüzler:** `IAggregateRoot` (sınır), `ITenantScoped` (tenant filtresi —
  setter public KALIR, altyapı set eder), `IHasUrlCode` (paylaşılabilir kod).
- **Child entity fabrikası `internal`:** sadece aggregate satır üretir → "serbest"
  satır oluşturulamaz.
- **Domain event ÜRET:** fabrikada `RaiseDomainEvent(new <Entity>Created(...))`.
  (Dağıtım Outbox altyapısında — bkz. CLAUDE.md §2; olay tanımlayıp dağıtmamak yasak.)

## İskelet
```csharp
public sealed class Order : BaseEntity, IAggregateRoot, ITenantScoped, IHasUrlCode
{
    private readonly List<OrderLine> _lines = [];
    private Order() { }                       // EF

    public Guid TenantId { get; set; }        // ITenantScoped → public
    public string UrlCode { get; set; } = ""; // interceptor atar
    public Guid CustomerId { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Order Create(Guid tenantId, Guid customerId, IReadOnlyList<LineSpec> specs)
    {
        if (specs.Count == 0) throw new ArgumentException("En az bir satır gerekli.");
        var o = new Order { TenantId = tenantId, CustomerId = customerId };
        foreach (var s in specs) o._lines.Add(OrderLine.For(tenantId, o.Id, s.Sku, s.Amount));
        o.Total = o._lines.Sum(l => l.Amount);
        o.RaiseDomainEvent(new OrderCreated(o.Id, customerId, o.Total));
        return o;
    }
}
```

## EF konfigürasyonu — unutma
- Tüm para alanları `HasPrecision(18, 4)`; tutar/sayı CHECK constraint'leri;
  unique index'ler (`UrlCode`, doğal anahtarlar); FK davranışı (Restrict/Cascade);
  `UseXminAsConcurrencyToken()`; koleksiyon için backing-field access.

## TDD akışı
1. `<Entity>Tests.cs` → fabrika + invariant + event testleri (kırmızı).
2. Entity'yi yaz (yeşil).
3. EF config + migration; `migration-flow` ile uygula.
4. (varsa) DB-kısıt entegrasyon testi.

## Bitti tanımı
- [ ] Public setter YOK (TenantId/UrlCode hariç); koleksiyon salt-okunur
- [ ] Invariant fabrika/metotta korunuyor + testi var
- [ ] Domain event üretiliyor
- [ ] EF CHECK/unique/FK + migration uygulandı
