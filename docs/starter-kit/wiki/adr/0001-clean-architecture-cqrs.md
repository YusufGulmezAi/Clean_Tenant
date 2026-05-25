# 0001 — Clean Architecture + CQRS (MediatR)

- **Durum:** Kabul edildi
- **Tarih:** YYYY-MM-DD
- **Karar verenler:** <kurucu>

## Bağlam
Çok-kiracılı, uzun ömürlü ve büyümesi beklenen bir SaaS. Tek geliştiriciyle
sürdürülebilirlik, test edilebilirlik ve net sınırlar kritik. İş mantığının
UI/altyapıya sızması ileride bakımı imkânsızlaştırır.

## Karar
Clean Architecture katmanları (Core/Infrastructure/Presentation) + bağımlılık
yönü içeriye + her iş davranışı için CQRS dikey dilim (MediatR). Cross-cutting
concern'ler pipeline behavior'da.

## Alternatifler
- **Katmanlı (klasik N-tier):** sınırlar zayıf, iş mantığı sızar — reddedildi.
- **Transaction Script / fat service:** hızlı başlar ama büyüyünce test edilemez —
  reddedildi.
- **Tam mikroservis:** tek geliştirici için aşırı operasyon yükü — şimdilik reddedildi.

## Sonuçlar
- Olumlu: test edilebilirlik, net sınırlar (NetArchTest ile zorlanır), tutarlı slice.
- Olumsuz/ödün: başlangıçta daha çok dosya/tören; küçük işler için fazladan yapı.
- Etkilenen alanlar: tüm `Features/*`, pipeline, proje referans grafiği.
