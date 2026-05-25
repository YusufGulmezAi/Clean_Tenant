---
name: feedback_conventions
description: Türkçe-doküman/İngilizce-kod, UI danışma, sayfa açıklama bloğu, build/migration refleksleri
metadata:
  type: feedback
---

**Dil:** Tüm doküman/yorum/commit açıklaması/memory **Türkçe**; tüm kod, tanımlayıcı,
entity/property/enum/dosya adı **İngilizce**. Türkçe tanımlayıcı kabul edilmez.

**Why:** Tutarlı dil ayrımı; doküman ekip için okunur, kod uluslararası standartta kalır.
**How to apply:** XML doc Türkçe yaz, sınıf/metot/değişken İngilizce adlandır.

**UI danışma:** Görsel/UX kararı (layout, renk, tema, komponent) tek başına verilmez;
önce kullanıcıya seçenek sun. Her UI sayfasında "ne yapar + nasıl kullanılır" açıklama
bloğu bulunur (formlarda onay sonrası gizlenebilir).

**Refleksler:** Build/run öncesi çalışan app'i kapat (DLL kilidi + port). Yeni EF
migration sonrası app'i başlatmadan `env-migrate` ile 4 DB'ye uygula. Bkz. [[rules_devops]].
