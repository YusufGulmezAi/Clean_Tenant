---
kanban-plugin: board
---

## 🗂️ Backlog

- [ ] **v0.2.12 commit + tag** — Bu oturumun çalışmaları commit'lenip tag'lenecek #acil
- [ ] **Bütçe Modülü** — [[Specs/budget-module/README]] spec hazır; implementasyon faz planı yapılacak
- [ ] **Unit / Resident domain modeli** — v0.3 için yeniden planlama bekleniyor
- [ ] **Şifre sıfırlama e-posta şablonu** — Şu an düz metin; HTML template ile geliştirilecek
- [ ] **Onboarding wizard — System scope desteği** — Mevcut wizard Tenant scope'ta çalışıyor; System admin için de açılacak
- [ ] **`Kimlik & Auth/` doldurulması** — [[Kimlik & Auth/Kimlik Mimarisi]] oluşturuldu; genişletilecek
- [ ] **`UI & Blazor/` doldurulması** — Blazor Server pattern'leri dokümante edilecek
- [ ] **`Veri Erişimi/` doldurulması** — EF Core + Dapper hibrit detaylandırılacak
- [ ] **Redis pub-sub localization invalidation** — Multi-instance için (v0.3+ planı)
- [ ] **ADR-005** — Şifre sıfırlama akışı karar belgesi (challenge-store vs. OTP)

## 🔄 Devam Ediyor

*(şu an boş)*

## ✅ Tamamlandı

- [x] **v0.2.12-A** NavMenu yeniden yapılanması — System/Tenant context gating
- [x] **v0.2.12-B** `RequiresPasswordChange` entity + migration + login akışı
- [x] **v0.2.12-C** Redis OTP doğrulama kodu servisi (`IVerificationCodeService`)
- [x] **v0.2.12-D** Şifre sıfırlama akışı (`/forgot-password` + `/reset-password`)
- [x] **v0.2.12-E** `LookupUserByIdentifierQuery` (TCKN/VKN/Phone/Email)
- [x] **v0.2.12-F** `UserOnboardingWizard.razor` — debounced arama + rol atama
- [x] **v0.2.12-G** Scope-bazlı aktif/pasif komutlar (`DeactivateTenantUser` / `ReactivateTenantUser`)
- [x] **v0.2.12-H** `ListUsersQueryHandler` tenant kapsamlı görünüm (Tenant + Company scope birleşimi)
- [x] **v0.2.11** PermissionPicker + TenantForm tab + CompanyForm grupları
- [x] **v0.2.10** Lokalizasyon (TR/EN/AR/RU/DE + RTL + admin sayfası)
- [x] **v0.2.7** PortalApp Shell MVP
- [x] **v0.2.6** Audit Explorer
- [x] **v0.2.5** Permission/Role CRUD + WebApi
- [x] **v0.2.1–v0.2.4** ManagementApp Shell + Auth UI + Company CRUD

