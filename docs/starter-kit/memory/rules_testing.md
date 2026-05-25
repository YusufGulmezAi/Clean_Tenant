---
name: rules_testing
description: Test piramidi, TDD, mimari testler, faz-sonu zorunlu test + güvenlik kapısı
metadata:
  type: reference
---

**TDD zorunlu:** üretim kodu öncesi başarısız test; refactor mevcut testlerle korunur.

**Test piramidi:** Domain unit (en ucuz — invariant'lar burada) → Application unit
→ Infrastructure integration (Testcontainers, gerçek PG) → UI component (bUnit) →
API integration. **Mimari testler** (NetArchTest) katman ihlallerini yakalar.

**Faz-bazlı** zorunlu test + güvenlik kapısı (slice bazında değil, faz kapanışında
unit + integration birlikte). CI yeşil olmadan merge yok; coverage hedefi
Domain/Application ≥ %80. Bkz. [[rules_devops]].
