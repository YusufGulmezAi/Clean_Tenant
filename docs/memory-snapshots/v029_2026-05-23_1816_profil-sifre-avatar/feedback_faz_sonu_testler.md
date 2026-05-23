---
name: Faz Sonu Testleri Hazırlama Disiplini
description: Her faz kapanışında (FAZ X sonu) testler de hazırlanır — unit + integration. Slice bazında değil, faz kapanırken topluca.
type: feedback
originSessionId: 92894288-df01-4cff-89f3-6fae0e457383
---
Her faz kapanırken (FAZ 5 / FAZ 6 / FAZ 7 / vb. sonu) o fazın testleri de hazırlanır ve birlikte commit edilir. Slice'lar arasında atlanabilir ama faz kapatılmaz.

**Test kapsamı (her faz için):**
- **Unit testler:** Domain entity invariants, validation kuralları, dağıtım/hesaplama motorları (LRM, KMK m.20 tavanı, vb.)
- **Integration testler:** TestContainers + PostgreSQL ile command/query end-to-end + tenant izolasyon

**Why:** Test kapsamı olmadan faz "tamamlandı" sayılmaz. Spec'in FAZ 8'i ayrı bir test fazı olarak tanımlamış olsa da, faz başına test ile gelmek regresyon riskini azaltır ve referans senaryoları her fazda canlı tutar. Kullanıcının 2026-05-22 tarihindeki direktifi: "Fazın sonunda kapanırken testleri de hazırlayalım."

**How to apply:**
- Bir fazın son slice'ını commit etmeden önce, o faza ait testleri ayrı bir commit ile (veya son slice'ın içinde) hazırla
- Test dosyaları: `tests/CleanTenant.Domain.UnitTests/`, `tests/CleanTenant.Application.UnitTests/`, `tests/CleanTenant.Infrastructure.IntegrationTests/`
- Faz mimari haritası (`docs/phases/v0.X/v0.X-FINAL-ARCHITECTURE-MAP.md`) test özetini de içersin (kaç test, hangi kapsamlar)
- FAZ 8'i atlamayalım da: orada uçtan uca 3 referans senaryo (12-BB, 120-BB, mid-year revizyon) ile full E2E test edilir; faz-bazlı testler bunu beslemek için temel oluşturur
