---
name: new-slice
description: >
  CQRS dikey dilim (vertical slice) üretir: Command/Query + Handler + Validator +
  Result + testleri, projenin Clean Architecture + MediatR konvansiyonlarına
  birebir uyumlu. "Yeni use-case / komut / sorgu / endpoint / özellik ekle",
  "şunu kaydeden işlem yap", "X listesini getiren sorgu yaz" gibi HER istekte
  kullan — kullanıcı "slice" demese bile yeni bir iş davranışı ekleniyorsa bu
  skill devreye girmeli. Ayrıca mevcut bir slice'ı bu desene uydururken de kullan.
---

# new-slice — CQRS Dikey Dilim Üretici

Amaç: her use-case'in AYNI şekle sahip olması. Tutarlı şekil = bulması, test
etmesi, gözden geçirmesi kolay kod. Tek kişilik ekipte en değerli şey budur.

## Önce karar ver
1. **Command mı, Query mi?** Durum değiştiriyorsa Command, sadece okuyorsa Query.
2. **Area:** `Features/<Area>/<UseCase>/` — Area = modül/bağlam.
3. **Yetki:** genelde gerekli → `[RequirePermission("<scope>.<area>.<action>")]`.

## Üreteceğin dosyalar
`src/Core/<App>.Application/Features/<Area>/<UseCase>/`
- `<UseCase>Command.cs` — `record` + `Result<TResult>` dönüş tipi
- `<UseCase>CommandHandler.cs` — `IRequestHandler<,>`
- `<UseCase>CommandValidator.cs` — FluentValidation
- (test) `tests/<App>.Application.UnitTests/.../<UseCase>HandlerTests.cs`

## Kurallar (ve nedenleri)
- **Handler ince kalır:** yalnız I/O + orkestrasyon. İş kuralı/invariant DOMAIN'de
  (aggregate metodu). Mantık entity'de olunca DB'siz, milisaniyede test edilir.
- **Cross-cutting'i handler'da TEKRARLAMA:** auth/validation/log/cache/idempotency
  pipeline behavior'da çalışır. Handler sadece kendi işini yapar.
- **Hata = Result + katalog kodu.** Beklenen hatalar için exception fırlatma:
  `Error.Failure("REC-001", "Tutar sıfırdan büyük olmalı.")`. Kod prefix'i Area'dan.
- **Girdi doğrulama Validator'da.** Domain invariant guard'ı ayrıdır (son savunma).

## İskelet
```csharp
// <UseCase>Command.cs
[RequirePermission("tenant.order.create")]
public sealed record CreateOrderCommand(
    Guid TenantId, Guid CustomerId, decimal Amount)
    : IRequest<Result<OrderResult>>;

public sealed record OrderResult(Guid OrderId, string UrlCode);
```
```csharp
// <UseCase>CommandHandler.cs
public sealed class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, Result<OrderResult>>
{
    private readonly IMainDbContext _db;
    private readonly IClock _clock;
    public CreateOrderCommandHandler(IMainDbContext db, IClock clock)
        => (_db, _clock) = (db, clock);

    public async Task<Result<OrderResult>> Handle(
        CreateOrderCommand request, CancellationToken ct)
    {
        // 1) Orkestrasyon: gerekli kayıtları yükle, uygulama politikalarını uygula
        // 2) Domain kararı: var order = Order.Create(...);  // invariant aggregate'te
        // 3) Persist: _db.Orders.Add(order); await _db.SaveChangesAsync(ct);
        // 4) return Result<OrderResult>.Success(new(order.Id, order.UrlCode));
        throw new NotImplementedException();
    }
}
```
```csharp
// <UseCase>CommandValidator.cs
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}
```

## TDD akışı (zorunlu)
1. Handler testini yaz → **kırmızı** gör (`dotnet test` ile doğrula).
2. Handler'ı minimal yaz → **yeşil**.
3. Validator testi + Validator.
4. Refactor; testler yeşil kalsın.

## Bitti tanımı
- [ ] Test önce kırmızı görüldü, şimdi yeşil
- [ ] `[RequirePermission]` var
- [ ] Hata yolları `Result` + katalog kodu
- [ ] Türkçe XML doc, İngilizce tanımlayıcı
