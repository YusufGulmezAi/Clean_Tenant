---
name: Faz Sonu Mimari/Durum Haritası
description: Her faz kapanışında diyagramatik üst-bakış raporu (Mermaid + PNG) hazırlanır
type: feedback
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---

Her faz (v0.X) tamamlanır tamamlanmaz **"Mimari/Durum Haritası Sonuç Raporu"** dokümanı hazırlanır. Bu rapor, CHANGELOG.md'nin yanına eklenen ikinci kapanış belgesidir — kronolojik anlatım yerine **diyagramatik üst-bakış** sunar.

**Why:** İlk uygulama Faz 0 v0.1.7 sonunda yapıldı (commit `7a38f2a`). Kullanıcı, Faz 1'e geçmeden önce sistemin görsel haritasını isteyince ortaya çıktı; Faz 1'e başlarken yeni bir geliştiriciye (veya kullanıcının kendisine 6 ay sonra) tek bakışta sistemi anlatması işe yaradı. CHANGELOG kronolojik, bu rapor topluca — ikisi birbirini tamamlar.

**How to apply:**

1. **Tetikleyici:** Bir faz'ın son alt-fazı tamamlandığı ve push'landığı anda. Faz 0'da bu v0.1.7'ydi; Faz 1'de v0.2.x serisinin son alt-fazı olacak.

2. **Dosya konumu:**
   - Ana doküman: `docs/phases/v0.<faz>/v0.<faz>-FINAL-ARCHITECTURE-MAP.md`
   - Diyagram dizini: `docs/phases/v0.<faz>/architecture-diagrams/` (her diyagram için `.mmd` + `.png` ikilisi)

3. **Şablon:** Faz 0'ın raporu ([v0.1-FINAL-ARCHITECTURE-MAP.md](d:/Projeler/CleanTenant/docs/phases/v0.1/v0.1-FINAL-ARCHITECTURE-MAP.md)) **referans şablon**. Sonraki fazların raporları aynı 18 bölümlü iskelet üzerinden ilerler ama her faz'a göre adapte edilir:
   - **0. Yönetici Özeti** — bu faz'da ne kondu, sayısal özet, faz n → faz n+1 sınırı
   - **1-3. Mimari diyagramları** — sistem bağlamı, proje bağımlılığı, katmanlar (Faz 0'dakini güncelle, yeni proje/component ekle)
   - **4-6. ER diyagramları** — yeni tablolar veya değişen kolonlar (Faz 1'de Main DB tabloları eklenir, Faz 1.5'te DataProtection vb.)
   - **7-12. Akış diyagramları** — yeni akışlar (Faz 1'de tenant onboarding wizard, rol-permission map akışı vb.)
   - **13. Endpoint kataloğu** — yeni route'lar tabloya eklenir
   - **14. DB mimarisi** — Main DB Faz 1'de aktif olur, diyagram güncellenir
   - **15. Test piramidi** — yeni test sayıları
   - **16. Sürüm geçmişi** — bu faz'ın alt-fazları + git tag'leri eklenir
   - **17. Açık konular** — Faz n+1'e ertelenen konular güncellenir
   - **18. Faz n+1 brifingi** — bir sonraki faz için NE/NEDEN/NEDEN ŞİMDİ + tasarım soruları + alt-faz taslağı
   - **Appendix A** — gerçek `dotnet build` + `dotnet test` + `git log` çıktıları

4. **Diyagram formatı:** Mermaid kod bloğu (Markdown'a inline, GitHub auto-render) **+** ayrıca `.mmd` dosyası **+** `mmdc` CLI ile PNG export. Her diyagram için bu üçü.
   - PNG üretimi: `npx -p @mermaid-js/mermaid-cli mmdc -i x.mmd -o x.png --theme default --width 1800 --backgroundColor white`

5. **Yeni tag YOK:** Mimari haritası ayrı bir git tag almaz. Faz'ın asıl kod tag'i (örn. `v0.1.7`) kapanış referansı olarak kalır; harita commit'i `docs(phase-X): ...` mesajıyla main'e gider.

6. **Memory snapshot:** Mimari harita push'landıktan sonra ek bir memory snapshot ZORUNLU değil (CHANGELOG güncellemesi sırasında snapshot zaten alındı). İstisna: rapor yazılırken yeni bir feedback/proje kuralı netleşirse standart snapshot kuralı uygulanır.

7. **Tekrar kullanılabilirlik kuralları:**
   - **Tutarlılık** — terminoloji önceki raporlarla uyumlu olmalı (örn. "Catalog DB", "Tenant", "Persona" aynı isim).
   - **Eskimeyen referanslar** — dosya yolları faz değişiminde geçersiz olabilir; rapor her yeniden yazılmaz, **her faz için yeni rapor** (versiyonlu).
   - **TR + EN karışımı** — başlıklar Türkçe; kod tanımlayıcıları + diyagram kutu adları İngilizce (memory `feedback_turkce_dokuman` kuralı).

8. **Brifing zinciri:** Rapor'un bölüm 18'i sonraki faz brifingidir. Bu brifing onaylandığında **ayrı bir konuşmada** detaylı tasarım soruları sorulur — rapor "kapanış + ön bakış", brifing onayı sonrası gerçek tasarım kararları alınır.

**Faz tamamlandı sayma kuralı:** Bir faz'ın son alt-fazı (örn. v0.1.7) push'landığı an. Daha küçük alt-faz commit'leri (v0.1.6 gibi) yalnız CHANGELOG güncellemesi gerektirir, harita değil. Bu raporu yıl başına/sona indirip büyük dönüm noktalarına saklıyoruz.
