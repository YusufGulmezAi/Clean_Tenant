---
name: adr-new
description: >
  Mimari Karar Kaydı (ADR) oluşturur: bağlam, karar, alternatifler, sonuçlar.
  "ADR yaz", "bu kararı kaydet", "neden böyle yaptık dokümante et", "mimari karar
  aldık", "X yerine Y'yi seçtik" gibi HER istekte kullan. Geri döndürülmesi zor
  veya kafa karıştırıcı her teknik karardan sonra bu skill devreye girmeli —
  gelecekteki "neden?" sorusunun cevabıdır.
---

# adr-new — Mimari Karar Kaydı

Neden: kararların GEREKÇESİ kodda görünmez. ADR, "6 ay sonra neden böyle yaptık?"
sorusunu ve due-diligence'taki "bu seçim bilinçli mi?" sorusunu cevaplar.

## Dosya
`docs/wiki/adr/NNNN-kebab-baslik.md` — NNNN sıralı (0001, 0002...). Mevcut en
yüksek numarayı bul, bir artır.

## Şablon (birebir kullan)
```markdown
# NNNN — <Karar Başlığı>

- **Durum:** Önerildi | Kabul edildi | Reddedildi | Yerini aldı (→ NNNN)
- **Tarih:** YYYY-MM-DD
- **Karar verenler:** <kim>

## Bağlam
Hangi problem/güç bu kararı gerektirdi? Kısıtlar neler?

## Karar
Ne yapmaya karar verdik? (net, tek cümlede özetlenebilir)

## Alternatifler
Değerlendirilen diğer seçenekler ve neden seçilmedikleri.

## Sonuçlar
- Olumlu: ...
- Olumsuz / ödünler: ...
- Etkilenen alanlar: ...
```

## İlkeler
- Kısa tut (1 sayfa). ADR roman değil, karar kaydıdır.
- Bir kararı değiştirirsen eskiyi silme; "Yerini aldı → NNNN" işaretle (tarihçe).
- Faz kapanışında (`phase-closeout`) o fazın ADR'lerini topla.

## Bitti tanımı
- [ ] Sıralı numara + kebab başlık
- [ ] Bağlam + karar + alternatifler + sonuçlar dolu
- [ ] Durum işaretli
