# CleanTenant Wiki

Çok kiracılı site/apartman yönetim SaaS platformu — canlı bilgi tabanı.

**Son oturum:** v0.2.12 (2026-05-21) | **Durum:** Commit bekliyor

---

## 🗺️ Hızlı Erişim

| | |
|---|---|
| 📋 **[[Proje Tahtası]]** | Kanban: Backlog / Devam Ediyor / Tamamlandı |
| 📖 **[[Fazlar/v0.2/CHANGELOG]]** | v0.2.x değişiklik günlüğü |
| 🔐 **[[Kimlik & Auth/Kimlik Mimarisi]]** | Scope, login akışı, token yapısı |
| 🏗️ **[[Mimari/Clean Architecture Katmanları]]** | Katman düzeni ve kurallar |
| 💡 **[[Keşif/vision]]** | Vizyon ve hedef kitle |

---

## Başlangıç Noktaları

### Faz Geçmişi
- [[Fazlar/v0.0/Obsidian ve Claude Code Egitimi]] — Faz 0.0: Araç eğitimi (buradan başla)
- [[Fazlar/v0.1/README]] — Faz 0.1: Backend temeli (Auth + 2FA + 146 test)
- [[Fazlar/v0.1/CHANGELOG]] — v0.1.x değişiklik günlüğü
- [[Fazlar/v0.2/CHANGELOG]] — v0.2.x değişiklik günlüğü (güncel)
- [[Fazlar/v0.2/v0.2.12-oturum]] — Son oturum detayı
- [[Fazlar/v0.2/v0.2.11-FINAL-ARCHITECTURE-MAP]] — Son mimari harita

### Mimari Kararlar (ADR)
- [[Kararlar/ADR-001 Hibrit JWT + Redis Session]]
- [[Kararlar/ADR-002 Dört Veritabanı Mimarisi]]
- [[Kararlar/ADR-003 EF Core ve Dapper Hibrit Okuma]]
- [[Kararlar/ADR-004 Elle Eşleme (AutoMapper Yok)]]

### Alan Belgeleri
- [[Kimlik & Auth/Kimlik Mimarisi]] — Scope seviyeleri, login / sıfırlama akışları
- [[Mimari/Clean Architecture Katmanları]] — Katman kuralları

### Özellik Spesifikasyonları
- [[Specs/budget-module/README]] — Bütçe modülü (spec hazır)
- [[Specs/budget-module/01-SDD-v1.0]] — Yazılım tasarım dokümanı
- [[Specs/budget-module/03-DECISIONS-OPEN]] — Açık kararlar

### Fikirler
- [[Fikirler/graph-explorer]] — Graph Explorer fikri

---

## Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Backend | .NET 10, MediatR, FluentValidation |
| Veritabanı | PostgreSQL × 4 (Catalog/Main/Log/Audit) |
| ORM | EF Core (yazma) + Dapper (raporlama) |
| Cache/Session | Redis |
| UI | Blazor Server + MudBlazor |
| Mobil | MAUI Hybrid |

---

## Şablonlar

Yeni not oluştururken: `_templates/` klasörüne bak.
- [[_templates/ADR]] — Yeni mimari karar
- [[_templates/Oturum-Notu]] — Oturum / sprint notu
- [[_templates/Faz-Kapanis]] — Faz kapanış belgesi
