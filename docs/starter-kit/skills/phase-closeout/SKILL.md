---
name: phase-closeout
description: >
  Bir fazı/sürümü profesyonelce KAPATIR: tüm testler (unit+integration) yeşil +
  güvenlik kapısı + faz-sonu mimari haritası (Mermaid+PNG) + ADR'ler + memory
  snapshot + conventional commit + faz-versiyonlu doküman. "Fazı kapat", "bu
  sürümü bitir", "vX.Y'i tamamla", "release hazırlığı", "milestone kapanışı" gibi
  HER istekte kullan. Bir faz "bitti" denmeden önce bu skill devreye girmeli.
---

# phase-closeout — Faz Kapanış Ritüeli

Neden: tutarlı kapanış = her sürümde aynı kanıt seti (testler, mimari, kararlar).
Bu, satış/yatırım due-diligence'ında "olgun proje" sinyalidir ve gelecekteki
kendine bırakılmış nettir.

## Sıra (atlamadan)
1. **Testler yeşil:** `dotnet test` (tüm projeler). Faz testleri SLICE bazında
   değil, faz bazında toplu gelir (unit + integration birlikte).
2. **Güvenlik kapısı:** `security-gate` skill'ini çalıştır; bulgular kapanmadan
   faz kapanmaz.
3. **Mimari harita:** `docs/phases/vX.Y/vX.Y-FINAL-ARCHITECTURE-MAP.md`
   - 18 bölüm (aşağıdaki iskelet)
   - Mermaid diyagramları + render edilmiş PNG'ler
4. **ADR'ler:** bu fazda alınan önemli kararlar → `adr-new` ile `docs/wiki/adr/`.
5. **CHANGELOG / faz notu:** ne eklendi/değişti/düzeltildi (Türkçe).
6. **Memory snapshot:** `docs/memory-snapshots/vNNN_YYYY-MM-DD_HHmm_<konu>/`
   altına memory dosyalarının versiyonlu kopyası.
7. **Commit:** conventional + faz etiketi. Örn:
   `feat(orders): sipariş modülü tamamlandı (vX.Y)`

## Mimari harita — 18 bölüm iskeleti
Bağlam · Katman diyagramı · Bağımlılık yönü · Domain modeli · Aggregate sınırları ·
CQRS akışı · Pipeline behavior'lar · Veri erişimi (EF/Dapper) · Multi-tenancy ·
DB şeması (4 DB) · Migration tarihçesi · Kimlik & yetki · Güvenlik · Gözlemlenebilirlik ·
Lokalizasyon · DevOps/ortamlar · Test stratejisi · Açık riskler/yol haritası.

## Bitti tanımı
- [ ] Tüm testler yeşil (çıktı gösterildi)
- [ ] Güvenlik kapısı geçti
- [ ] Mimari harita (Mermaid+PNG) güncel
- [ ] ADR'ler yazıldı
- [ ] Memory snapshot alındı
- [ ] Conventional commit atıldı
