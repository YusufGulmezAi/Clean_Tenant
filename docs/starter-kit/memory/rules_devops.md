---
name: rules_devops
description: Docker/ortam, CI/CD, conventional commit, faz dokümanı + mimari harita + memory snapshot
metadata:
  type: reference
---

Docker; ortam başına compose + secret (Development/Test/Demo/Production). **CI/CD:**
her PR'da build + test + SAST + SCA + secret-scan; yeşil olmadan merge yok.

**Conventional commits** + semver. Commit mesajı açıklaması Türkçe.

Her faz kapanışında: `docs/phases/vX.Y/` mimari harita (Mermaid + PNG, 18 bölüm) +
o fazın ADR'leri + `docs/memory-snapshots/vNNN_.../` memory snapshot.

Build/run öncesi çakışan portu/süreci kapat; `scripts/env-run` ile başlat.
Bkz. [[rules_testing]], [[feedback_conventions]].
