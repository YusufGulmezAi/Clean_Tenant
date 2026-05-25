---
name: migration-flow
description: >
  EF Core migration'ı GÜVENLE üretir ve 4 DB'ye (Catalog/Main/Log/Audit) uygular,
  sonra app/seeder'ı çalıştırır — "relation does not exist" çöküşünü önler. "Migration
  al", "şema değiştir", "yeni alan/tablo ekledim DB'yi güncelle", "veritabanını
  migrate et" gibi HER istekte kullan. Entity/EF config değişiminden sonra app'i
  başlatmadan ÖNCE bu skill devreye girmeli. Paralel ajan varsa migration üretimini
  serileştirmek için de bunu kullan.
---

# migration-flow — Migration Üret & Uygula

Neden: bu projede şema 4 ayrı DB'ye bölünür. Migration üretip uygulamadan app'i
başlatırsan seeder "relation does not exist" ile çöker. Ayrıca EF snapshot tek
dosyadır; iki migration aynı anda üretilirse snapshot çakışır.

## Ön koşullar
- Çalışan app'i kapat (DLL kilidi + port çakışması): `scripts/kill-app.ps1`.
- Paralel ajan çalışıyorsa: **migration üretimini serileştir**. Başkasının açık
  migration'ı varsa bekle; aynı anda iki `migrations add` YAPMA (snapshot çakışır).

## Hangi DbContext?
Değişen entity hangi context'e ait → onun migration'ını üret:
- Catalog (identity, lokalizasyon, lookup, şablonlar) → `CatalogDbContext`
- Main (tenant iş varlıkları) → `MainDbContext`
- Log / Audit → ilgili context (nadiren)

## Adımlar
1. **Üret:**
   ```powershell
   dotnet ef migrations add <AciklayiciAd> `
     --project src/Infrastructure/<App>.Persistence `
     --context <Context> `
     --output-dir <Context-klasoru>/Migrations
   ```
2. **İncele:** üretilen `*.cs`'i OKU. Beklenmedik drop/rename/veri-kaybı var mı?
   Snapshot'ta istenmeyen değişiklik var mı? (CHECK/precision/index özellikle.)
3. **Uygula (4 DB):**
   ```powershell
   scripts/env-migrate.ps1 -Env Development
   ```
4. **Ancak ondan sonra** app'i başlat / seeder'ı çalıştır:
   ```powershell
   scripts/env-run.ps1 -Env Development
   ```

## Dikkat
- Migration adı Türkçe-açıklayıcı: `AddCollectionRefund`, `FixAccountCodeFormat`.
  Anlamsız ad (`Update1`) YASAK.
- `Down()` mantıklı mı (geri alınabilirlik) doğrula.
- Para/precision değişimi → veri dönüşümü gerekiyor mu kontrol et.
- Migration başkasının dosyasıysa COMMIT ETME (cross-agent kuralı).

## Bitti tanımı
- [ ] Migration üretildi ve gözle incelendi (sürpriz yok)
- [ ] 4 DB'ye uygulandı (env-migrate yeşil)
- [ ] App/seeder hatasız başladı
