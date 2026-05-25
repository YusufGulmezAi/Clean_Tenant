---
name: security-gate
description: >
  Merge/faz öncesi GÜVENLİK KAPISI çalıştırır: SAST/kod incelemesi + bağımlılık
  (SCA) + secret taraması + TENANT İZOLASYON testleri + tehdit gözden geçirme;
  bulgular kapanmadan geçiş yok. "Güvenlik kontrolü", "merge öncesi denetim",
  "pentest hazırlığı", "bu güvenli mi", "izolasyon testi" gibi HER istekte ve para/
  kimlik/yetki/tenant sınırına dokunan her değişiklikte kullan. Banka-seviyesi
  iddiasını KANITA bağlayan adımdır.
---

# security-gate — Güvenlik Kapısı

Neden: "banka gibi güvenli" bir iddia değil, kanıt zinciridir. Multi-tenant +
finansal projede iki ölümcül sınıf: cross-tenant veri sızıntısı ve yetki/para
mantığı hatası. Bu kapı onları yakalar.

## Kontroller
1. **Tenant izolasyonu (en kritik):** değişen her `ITenantScoped` entity için
   "başka tenant'ın verisi görünmüyor/yazılamıyor" testi var mı? Yoksa yaz.
2. **Yetki:** her yeni uç `[RequirePermission]` ile korunuyor mu? Negatif yetki
   testi var mı?
3. **Girdi & PII:** yeni girdi FluentValidation'da; PII alanları `[Sensitive]`
   etiketli ve audit'te redakte mi?
4. **SCA (bağımlılık):**
   ```powershell
   dotnet list package --vulnerable --include-transitive
   ```
   Bilinen CVE → kapat/güncelle.
5. **Secret taraması:** repoda secret yok (gitleaks/`git diff` denetimi).
6. **Kod incelemesi:** `/code-review` (doğruluk) + `/security-review` (güvenlik)
   çalıştır; bulguları çöz.
7. **Tehdit gözden geçirme:** bu değişiklik yeni bir saldırı yüzeyi açtı mı?
   (yeni endpoint, dosya yükleme, dış entegrasyon → kısa not.)

## Çıktı
`docs/wiki/security/gate-vX.Y.md`: kontrol listesi + bulgular + kapanış durumu.
Bu dosya due-diligence kanıtıdır.

## Bitti tanımı
- [ ] İzolasyon + yetki testleri yeşil
- [ ] SCA temiz, secret yok
- [ ] code-review + security-review bulguları kapandı
- [ ] Gate notu yazıldı
