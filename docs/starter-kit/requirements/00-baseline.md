# İsterler — Baseline (Requirements)

> Öncelik: **MoSCoW** (M=Must, S=Should, C=Could, W=Won't-now). Stack: .NET 10 /
> Blazor / PostgreSQL / Clean Arch, çok-kiracılı (multi-tenant) SaaS. Bu belge
> **canlı**: her faz karşılanan isterleri işaretler, kalanlar yol haritasına düşer.

## 1. Kapsam & Aktörler
- **Ürün:** Çok-kiracılı SaaS platformu; her tenant izole veriyle çalışır, ortak kod.
- **Scope seviyeleri (yetki sınırı):** `System` → `Tenant` → `Company` → `Unit`.
  Kullanıcı tek tarayıcıda birden çok bağlamda olabilir; bağlam başına token.
- **Aktörler:** System Admin, Tenant Admin, Company yöneticisi, son kullanıcı,
  (opsiyonel) destek personeli (impersonation).

## 2. Fonksiyonel İsterler (FR)

| Kod | İster | Öncelik |
|---|---|---|
| FR-TEN-1 | Tenant onboarding: self-servis/admin ile oluşturma + ilk kaynak provizyonu (idempotent) | M |
| FR-TEN-2 | Tenant yaşam döngüsü: askıya alma/kapatma, soft-delete, yeniden aktive | M |
| FR-IAM-1 | Kayıt/giriş, e-posta doğrulama, şifre politikası + sıfırlama | M |
| FR-IAM-2 | 2FA (TOTP + recovery code); System kullanıcıları için **zorunlu** | M |
| FR-IAM-3 | Rol & izin yönetimi (scope-bazlı), built-in + özel roller | M |
| FR-IAM-4 | Çok-bağlamlı oturum (tab başına bağlam), canlı izin tazeleme | S |
| FR-IAM-5 | Destek oturumu / impersonation (tam audit'li, yazma sayacı) | C |
| FR-DATA-1 | Çekirdek domain CRUD'ları (paylaşılabilir `UrlCode` ile) | M |
| FR-DATA-2 | Toplu içe/dışa aktarım (Excel/CSV) + doğrulama raporu | S |
| FR-BILL-1 | Abonelik/paket, kullanım sayacı, fatura, ödeme sağlayıcı entegrasyonu | S |
| FR-NOTIF-1 | Bildirim (e-posta + uygulama-içi), şablonlu, çok dilli | S |
| FR-REP-1 | Raporlama + dışa aktarım (PDF/Excel) | S |
| FR-AUD-1 | Tüm yazma işlemleri için audit izi (kim/ne/ne zaman + delta) | M |
| FR-FILE-1 | Dosya/görsel yükleme (object storage), görsel işleme | S |
| FR-LOC-1 | Çok dil (TR/EN/AR/RU/DE), DB-tabanlı kaynak, Arapça RTL | S |
| FR-ADM-1 | Back-office: tenant/kullanıcı/izin yönetim paneli | M |

> Alana özel FR'ler her modülün `requirements/<modul>.md`'sinde detaylanır.

## 3. Non-Functional İsterler (NFR) — kanıt sütunuyla

> "Banka gibi" iddiası burada yaşar ya da ölür: her satırın **doğrulama** yöntemi olmalı.

| Kod | NFR | Hedef | Doğrulama (nasıl kanıtlanır) | Öncelik |
|---|---|---|---|---|
| NFR-SEC-1 | Kimlik & scope-bazlı yetki | Her uçta yetki | AuthorizationBehavior + negatif yetki testleri | M |
| NFR-SEC-2 | Tenant veri izolasyonu | Sıfır cross-tenant sızıntı | Her tenant-scoped entity için izolasyon testi (CI) | M |
| NFR-SEC-3 | Şifreleme | At-rest + in-transit (TLS 1.2+) | Konfig denetimi + DB/TLS tarama | M |
| NFR-SEC-4 | Secret yönetimi | Repoda secret yok; prod vault | gitleaks CI taraması | M |
| NFR-SEC-5 | OWASP ASVS | Hedef Level 2 | ASVS checklist + pentest raporu | S |
| NFR-SEC-6 | Bağımlılık güvenliği (SCA) | Bilinen CVE yok | `dotnet list package --vulnerable` + Dependabot | M |
| NFR-SEC-7 | Rate limiting & brute-force | Limit + lockout | Yük/abuse testleri | S |
| NFR-PRIV-1 | KVKK uyumu | Saklama/silme/rıza/DSAR | Veri envanteri + retention job + DSAR akışı | M |
| NFR-PRIV-2 | PII koruması | Etiketli + audit'te redakte | `[Sensitive]` denetim testi | M |
| NFR-PERF-1 | API gecikmesi | p95 < 300ms (okuma) | Yük testi (k6/NBomber) raporu | S |
| NFR-PERF-2 | Sayfalama/akış | Liste uçları sayfalı | API kontrat testleri | M |
| NFR-SCAL-1 | Tenant ölçeklenmesi | Shared-DB + dedicated yolu | Mimari ADR + çoklu-tenant yük testi | S |
| NFR-AVAIL-1 | SLA | %99.9 hedef | Health check + uptime izleme | S |
| NFR-AVAIL-2 | Yedekleme & DR | RPO ≤ 24s, RTO ≤ 4s | Otomatik yedek + restore tatbikatı | M |
| NFR-OBS-1 | Gözlemlenebilirlik | Log+metric+trace+audit | Serilog→Log DB, OTel, Audit DB | M |
| NFR-OBS-2 | Uyarı (alerting) | Kritik hatada alarm | Eşik + bildirim kanalı | S |
| NFR-MAINT-1 | Mimari bütünlük | Katman ihlali = 0 | NetArchTest CI'da | M |
| NFR-MAINT-2 | Test kapsamı | Domain/Application ≥ %80 | Coverage raporu + gate | S |
| NFR-DEPLOY-1 | Ortam pariteti | Dev/Test/Demo/Prod compose | IaC + tek-komut kurulum | M |
| NFR-A11Y-1 | Erişilebilirlik (UI) | WCAG 2.1 AA hedef | Otomatik a11y taraması | C |
| NFR-USAB-1 | Kullanıcı dostu UI | Her sayfada açıklama bloğu | UI inceleme + kullanıcı testi | S |
| NFR-INTEG-1 | Veri bütünlüğü | İşlem/idempotency/concurrency | Idempotency + concurrency testleri | M |

## 4. Uyumluluk Matrisi (Türkiye — hendek/moat)

| Düzenleme | Kapsam | Durum |
|---|---|---|
| **KVKK** | Kişisel veri işleme, saklama, rıza, DSAR | M — veri envanteri + politika |
| **e-Fatura / e-Arşiv / e-Defter** | Faturalama varsa GİB entegrasyonu | Modüle göre M/S |
| **TDHP** | Muhasebe modülü varsa tek düzen hesap planı | Modüle göre |
| **Sektörel kanun** (ör. KMK 634) | Alana özel | Alana göre |
| **Tüketici/sözleşme** | Aydınlatma metni + kullanım şartları | M |

## 5. Kabul / İzlenebilirlik
- Her FR/NFR → en az bir **otomatik test** veya **kanıt artefaktı** (rapor/ADR).
- "Bitti" tanımı `CLAUDE.md §10`'a bağlıdır; NFR doğrulaması faz-sonu kapısında denetlenir.
