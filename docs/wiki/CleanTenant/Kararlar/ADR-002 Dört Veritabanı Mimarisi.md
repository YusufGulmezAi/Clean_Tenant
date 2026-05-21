# ADR-002 — Dört Veritabanı Mimarisi

**Durum:** Kabul edildi (v0.1.x)
**Etkilenen katmanlar:** Infrastructure.Persistence, Domain

---

## Karar

Tek veritabanı yerine dört ayrı PostgreSQL veritabanı kullanılır: **Catalog / Main / Log / Audit**

## Veritabanları

| DB | Amaç | Tenant izolasyonu |
|---|---|---|
| **Catalog** | Global kullanıcılar, kiracı kayıt, roller, izinler | Paylaşımlı (tek) |
| **Main** | İş verileri: şirket, bina, birim, fatura | Büyük → ayrılmış; küçük → paylaşımlı + TenantId |
| **Log** | Serilog sink | Paylaşımlı, TenantId etiketli |
| **Audit** | Append-only denetim izi | Paylaşımlı, TenantId etiketli |

## Bağlam

- Kullanıcı kimliği tenant'tan bağımsız (bir kullanıcı birden çok tenant'ta rol taşıyabilir)
- Log ve Audit büyüme hızı iş verilerinden farklı → ayrı DB = ayrı bakım/yedekleme
- Audit append-only → yazma optimizasyonu, asla hard delete
- Büyük müşteri → dedicated Main DB (bağlantı dizesi Catalog'dan çözümlenir)

## Hibrit Multi-Tenancy

```
Büyük müşteri:  Catalog'da TenantId → ConnectionString (dedicated)
Küçük müşteri:  Catalog'da TenantId → NULL (shared Main DB kullan)

EF Core Global Query Filter: WHERE tenant_id = @current
Dapper: TenantAwareQueryBuilder ile manuel WHERE tenant_id = @tenantId
```

## İlgili
- [[ADR-003 EF Core ve Dapper Hibrit Okuma]]
- [[ADR-001 Hibrit JWT + Redis Session]]
