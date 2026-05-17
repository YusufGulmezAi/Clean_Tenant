---
name: Memory Snapshot Otomasyonu
description: MEMORY.md veya altındaki dosyalar güncellendiğinde proje altındaki docs/memory-snapshots/ klasörüne versiyonlu kopya alınır
type: feedback
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
**Kural:** Memory dizinindeki herhangi bir dosya (MEMORY.md veya referans verdiği kural/feedback/profile dosyaları) güncellendiğinde, **proje köküne** versiyonlu bir snapshot alınır.

**Hedef konum:** `d:\Projeler\CleanTenant\docs\memory-snapshots\`

**İsimlendirme deseni:** `vNNN_YYYY-MM-DD_HHmm/`
- `NNN`: Üç haneli sıra numarası, ilk snapshot `v001`, sonrasında `v002`, `v003`, ... şeklinde artar.
- `YYYY-MM-DD_HHmm`: Snapshot'ın alındığı yerel tarih ve 24 saatlik zaman damgası.
- Örnek: `v001_2026-05-17_1430/`, `v002_2026-05-17_1605/`

**İçerik:** Snapshot anında memory dizininde bulunan **tüm `.md` dosyalarının tam kopyası**. MEMORY.md (indeks) ve referans verdiği tüm dosyalar aynı klasöre düz şekilde kopyalanır — dosya isimleri korunur.

**Neden:** Kullanıcı, proje kural setinin evrimini git geçmişiyle takip edebilmek istiyor. Bir kuralın ne zaman ve nasıl değiştiğine bakabilmek, ileride ekip arkadaşlarına aktarabilmek için snapshot'lar referans noktası olur.

**Nasıl uygulanır:**
- Her memory yazımı/düzenlemesi sonrası snapshot alınır.
- Aynı oturumda birden fazla memory değişikliği art arda yapılıyorsa, **mantıksal adım sonunda tek snapshot** alınır (her küçük edit için ayrı snapshot şişkinlik yaratır). Mantıksal adımın bittiğine işaret: kullanıcının "devam edebiliriz / tamamdır" benzeri ifadesi veya konunun kapanması.
- Yeni sıra numarası, mevcut snapshot klasörleri taranarak (en yüksek `vNNN` bulunup +1) belirlenir.
- Snapshot oluşturma sadece dosya kopyalamasıdır — kod, şema veya infra değişikliği içermediği için **eğitici mod ön onay zorunluluğunun dışındadır**; otomatik uygulanır.
