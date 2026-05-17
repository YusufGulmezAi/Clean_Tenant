---
name: Tüm Proje Dokümantasyonu Türkçe Yazılır
description: Faz dokümanları, README, ADR, CHANGELOG, açıklamalar, memory notları — hepsi Türkçe; kod ve teknik tanımlayıcılar İngilizce kalır
type: feedback
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
**Kural:** Projede yazılan tüm **dokümantasyon ve açıklayıcı içerikler Türkçe** olmalıdır.

**Kapsam (Türkçe yazılır):**
- Faz dokümanları (`/docs/phases/vX.Y/README.md`, `design.md`, `test-plan.md`, `test-report.md`, `security-report.md`, `CHANGELOG.md`)
- Proje düzeyindeki README dosyaları
- ADR (Architecture Decision Records)
- Sınıf, interface, enum, record üzerindeki `///` XML doc açıklamaları
- Property ve değişken yorumları
- Hata mesajı ve validasyon mesajı çevirilerinin **TR** kültürü
- Memory dosyalarına yazılan açıklamalar
- Commit mesajlarının body kısmı (başlık Conventional Commit standardı gereği İngilizce kalır: `feat:`, `fix:`)
- PR açıklamaları, issue açıklamaları
- Kullanıcıya verilen tüm cevaplar

**Kapsam dışı (İngilizce kalır):**
- Kod tanımlayıcıları (sınıf, metod, değişken, property isimleri) — kod dili İngilizce
- Teknik terim ve framework isimleri (`MediatR`, `Pipeline Behavior`, `Repository` vb.) — Türkçeleştirme zorlaması yok, parantez içinde kısa karşılık verilebilir
- NuGet paket isimleri, dosya/klasör adları
- Conventional Commit başlık prefiksleri (`feat`, `fix`, `chore`, `refactor`, vb.)
- Loglama template'lerindeki property isimleri (`{UserId}`, `{TenantId}` gibi yapısal log alanları)
- HTTP header isimleri, JSON alan adları (API kontratı dış dünyaya açık; İngilizce standart)

**Neden:** Kullanıcı projenin tüm dokümantasyonunu Türkçe görmek istiyor. Yazılan her açıklamayı izliyor; İngilizce metin gördüğünde uyarmak zorunda kalmasın.

**Nasıl uygulanır:**
- Yeni dosya yazarken doküman/açıklama bölümleri doğrudan Türkçe yazılır.
- Mevcut İngilizce dokümanlar görüldüğünde (önceki turlardaki memory dosyaları dahil) **bir sonraki dokunulduğunda Türkçeye çevrilir** — yığın halinde retroactive çeviri yapılmaz, organik geçiş.
- Karışık dil kullanılmaz; bir paragrafta TR ise tüm paragraf TR.
- Teknik terim çevirisi zorlanmaz: "Pipeline Behavior (boru hattı davranışı)" yerine "Pipeline Behavior" demek yeterli; gerekli görülürse parantez içi kısa açıklama eklenir.
