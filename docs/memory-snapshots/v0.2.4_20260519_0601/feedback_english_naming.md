---
name: İngilizce Kodlama Standardı
description: Tüm kod tanımlayıcıları (entity, property, enum, metod, dosya adı) İngilizce olmalı
type: feedback
originSessionId: 803c1fbf-906f-46b2-92b4-5f57293b82be
---
Entity adları, property adları, enum adları, dosya adları ve namespace'ler her zaman İngilizce olacak.
Yorum, dokümantasyon ve kullanıcı mesajları Türkçe kalır (ayrı kural: `feedback_turkce_dokuman.md`).

**Why:** Kullanıcı "Ben entity ve propertileri Türkçe yazmış olabilirim. Ama sen standart dile uygun hazırla. İngilizce olarak" dedi — kod tanımlayıcılarında dil standardı İngilizce.

**How to apply:** Yeni entity, property, enum, metod veya dosya adı yazarken her zaman İngilizce kullan. Türkçe tanımlayıcı gören eski kodu refactor ederken de İngilizceye çevir.

**Örnekler:**
- `Yapi` → `Building`
- `Ada` → `Block`
- `Parsel` → `Parcel`
- `BagimsizBolum` → `Unit`
- `MetreKare` → `SquareMeters`
- `ArsaPayi` → `LandShare`
- `YapiTipi` → `BuildingType`
- `OdaSalonTipi` → `ApartmentLayout`
