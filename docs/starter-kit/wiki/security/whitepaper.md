# Güvenlik Whitepaper (iskelet)

> Due-diligence / müşteri güven dokümanı. Her başlık altını projenin GERÇEK
> durumuyla doldur — boş/abartılı iddia bırakma; her iddianın kanıtı olsun.

## 1. Kimlik & Erişim
- Kimlik doğrulama: ... (ASP.NET Identity, parola politikası)
- 2FA: ... (System için zorunlu; TOTP + recovery)
- Yetkilendirme: scope-bazlı (System/Tenant/Company/Unit), izin kataloğu
- Oturum: bağlam başına token, ... ; oturum geçersizleştirme

## 2. Çok-Kiracılı İzolasyon
- `ITenantScoped` global query filter
- **Kanıt:** cross-tenant izolasyon test paketi (CI'da koşar) → `...`

## 3. Veri Koruması
- Şifreleme: in-transit (TLS ...), at-rest (...)
- PII: `[Sensitive]` etiketleme + audit redaksiyonu
- Secret yönetimi: prod vault, repoda secret yok (gitleaks CI)

## 4. Denetim & İzlenebilirlik
- Ayrı Audit DB: kim/ne/ne zaman + değişiklik delta'sı
- Log (Serilog → Log DB) + OpenTelemetry trace/metric

## 5. Uygulama Güvenliği
- Girdi doğrulama (FluentValidation), rate limiting, idempotency
- Bağımlılık taraması (SCA), SAST, secret-scan — CI'da
- Sızma testi: ... (tarih, kapsam, bulguların kapanışı)

## 6. Uyumluluk
- KVKK: veri envanteri, saklama/silme, rıza, DSAR akışı
- (varsa) e-Fatura/TDHP/sektörel

## 7. Süreklilik
- Yedekleme (RPO ...), DR (RTO ...), restore tatbikatı tarihleri

## 8. Açık Riskler & Yol Haritası
- ... (dürüst liste — DD'de güven yaratır)
