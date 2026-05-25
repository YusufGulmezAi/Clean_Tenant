# Starter-Kit — Yeni Proje Başlangıç Paketi

Bu klasör, **yeni bir projeyi** CleanTenant'ın zor kazanılmış konvansiyonlarıyla
(+ bugün keşfedilen boşluklar 1. günden kapalı + endüstri standartları) hızlıca
başlatmak için hazırlanmış **şablon**dur.

> ⚠️ **Bu klasör tamamen referans/şablondur.** CleanTenant'ın çalışan kodunu,
> `CLAUDE.md`'sini, `memory/` dosyalarını ya da yapılandırmasını **etkilemez**.
> Buradaki hiçbir dosya proje köküne kopyalanmadan devreye girmez.

## İçindekiler

| Klasör/Dosya | Ne | Karşılık |
|---|---|---|
| `CLAUDE.md` | Yeni projenin işletim kılavuzu (talimatlar) | A |
| `requirements/00-baseline.md` | Fonksiyonel + NFR + uyumluluk isterleri | F |
| `skills/<ad>/SKILL.md` | 7 iş-akışı skill'i | B |
| `skeleton/STRUCTURE.md` | Boş çözüm ağacı + temel config dosyaları | D |
| `memory/` | `MEMORY.md` + `rules_*` hafıza tohumları | C |
| `wiki/` | ADR şablonu, mimari/onboarding/güvenlik iskeletleri | E |

## Yeni projede nasıl kullanılır

1. Yeni bir repo aç (ör. `D:\Projeler\YeniProje`), `git init`.
2. `skeleton/STRUCTURE.md`'deki ağacı kur (veya `dotnet new` + projeleri ekle).
3. `CLAUDE.md`'yi proje köküne kopyala; `Acme.Saas` → kendi adınla değiştir.
4. `skills/` içeriğini `.claude/skills/` (proje) veya `~/.claude/skills/` (global)
   altına taşı; yolları (`<App>.Application` vb.) kendi namespace'ine uyarla.
5. `memory/` içeriğini projenin memory dizinine tohumla; `MEMORY.md` indeksini
   güncelle.
6. `wiki/` şablonlarını `docs/wiki/` altına al.

## Placeholder'lar

- `Acme.Saas` → kök namespace (kendi ürün adın).
- `<App>` → kısa proje adı.
- `<Area>`, `<UseCase>`, `<Entity>` → somut isimlerle değiştirilir.

## CleanTenant'tan taşınanlar / eklenenler

- ✅ **Taşınan:** Clean Architecture, Türkçe-doküman/İngilizce-kod, CQRS+MediatR
  pipeline, response envelope + hata kataloğu, hibrit multi-tenancy, EF-yaz/Dapper-oku,
  UrlCode, faz-sonu test/güvenlik kapıları, conventional commits, mimari harita +
  memory snapshot disiplini.
- 🔧 **1. günden düzeltilen boşluklar:** zengin aggregate (anemik değil), domain
  event üretimi **+ Outbox dağıtımı**, tenant izolasyon testleri.
- ➕ **Eklenen standartlar:** CI/CD + güvenlik taramaları, mimari sınır testleri
  (NetArchTest), OpenTelemetry, health check, rate limiting, idempotency, ADR'ler,
  KVKK veri yaşam döngüsü, Definition of Done.
