# ADR-003 — EF Core ve Dapper Hibrit Okuma Stratejisi

**Durum:** Kabul edildi (v0.2.3.d revizyonu, 2026-05-18)
**Etkilenen katmanlar:** Infrastructure.Persistence, Application (Query handlers)

---

## Karar

Tek ORM yerine **EF Core (yazma + basit okuma) + Dapper (ağır/raporlama okuma)** kombinasyonu kullanılır.

## Karar Ağacı

```
Yazma işlemi?
  → EF Core (audit interceptor, change tracking, soft delete)

Okuma + şunlardan biri?
  → Dapper
  ✓ GROUP BY / window function / CTE (raporlama)
  ✓ Çapraz kiracı sorgu
  ✓ 500+ satır veya 1000+ sayfalama
  ✓ 3+ tablo JOIN veya DB-specific özellik (citext, jsonb, full-text)
  ✓ Performans kritik path (>100 RPS)

Diğer okumalar:
  → EF Core + reader pattern + ICacheStore
  (by-id, by-urlcode, küçük liste, dropdown, form ön-yükleme)
```

## Neden İkisi Birden?

EF Core: Global Query Filter ile multi-tenancy otomatik (sızıntı riski düşük)
Dapper: Kompleks SQL tam kontrol, raporlama performansı

## Önemli Kural

Dapper'da tenant izolasyonu **OTOMATİK DEĞİL**.
Her Dapper sorgusunda `WHERE tenant_id = @tenantId` zorunlu.
`TenantAwareQueryBuilder` helper veya PR review kontrolü.

## İlgili
- [[ADR-002 Dört Veritabanı Mimarisi]]
- [[Mimari/Clean Architecture Katmanları]]
