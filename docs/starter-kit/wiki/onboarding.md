# Onboarding — Geliştirici Başlangıç

Yeni katılan (insan ya da AI) için 30 dakikada çalışır ortam.

## Ön koşullar
- .NET 10 SDK (`global.json` ile sabit)
- Docker Desktop (PostgreSQL/Redis/MinIO + integration testleri için)
- (Windows) PowerShell 7+

## Kurulum
```powershell
# 1) Bağımlılıklar + ortam
scripts/env-up.ps1 -Env Development        # docker compose (PG/Redis/MinIO)
scripts/env-migrate.ps1 -Env Development   # 4 DB'ye migration
scripts/env-seed.ps1 -Env Development      # seed data

# 2) Çalıştır
scripts/env-run.ps1 -Env Development
```

## İlk işine başlamadan oku
1. `CLAUDE.md` — bağlayıcı kurallar (dil, mimari, TDD, güvenlik).
2. `docs/wiki/architecture-overview.md` — büyük resim.
3. `docs/starter-kit/skills/` (veya `.claude/skills/`) — iş akışı skill'leri.

## Günlük akış (skill'lerle)
- Yeni özellik → `new-slice`
- Yeni kavram/tablo → `add-entity` → `migration-flow`
- Yeni ekran → `ui-page`
- Merge öncesi → `security-gate`
- Faz kapanışı → `phase-closeout`

## Altın kurallar (özet)
- Doküman Türkçe, kod İngilizce.
- UI kararı tek başına verme.
- Test önce kırmızı, sonra kod.
- "Bitti" demeden önce çalıştır + çıktıyı göster.
