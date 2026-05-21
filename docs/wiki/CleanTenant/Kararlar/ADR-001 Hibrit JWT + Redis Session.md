# ADR-001 — Hibrit JWT + Redis Session Auth Modeli

**Durum:** Kabul edildi (v0.1.x)
**Etkilenen katmanlar:** [[Kimlik & Auth]], Infrastructure.Identity, Presentation

---

## Karar

Saf stateless JWT yerine **minimal JWT + Redis-backed session store** kombinasyonu kullanılır.

## Bağlam

Saf JWT'de tüm yetkiler token içinde tutulur. Token imzalandıktan sonra sunucu tarafında iptal edilemez; TTL dolana kadar geçerli kalır.

CleanTenant'ta şu senaryolar zorunlu kıldı:
- Anlık yetki değişimi (rol/permission düzenlendi → kullanıcı anında etkilenmeli)
- Kullanıcı kilitleme → tüm sekmelerden anında atılma
- Tenant suspend → tüm session'lar toplu invalidate
- Support Mode → kısa TTL + operatör kimliği izleme

## Çözüm

```
JWT içeriği (min, ~250 byte):
  sub  → userId
  sid  → sessionId  ← Redis lookup anahtarı
  ctx  → contextId  ← sekme izolasyonu
  iat, exp, iss, aud

Redis session içeriği:
  roller, izinler, kapsam, tenantId, companyId,
  personaSide, supportMode, lastActivity
```

## Sonuç

- Server-side revocation mümkün
- Anlık yetki değişimi: Redis session in-place güncellenir
- JWT küçük → her istekte düşük bant genişliği
- Redis bağımlılığı eklendi (tek nokta riski → Redis Sentinel/Cluster ile azaltılır)

## İlgili
- [[ADR-002 Dört Veritabanı Mimarisi]]
- [[Kimlik & Auth/Auth Akışları]]
