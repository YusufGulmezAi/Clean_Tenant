# Faz v0.0 — Obsidian ve Claude Code Kullanım Eğitimi

**Amaç:** CleanTenant projesinde iki aracı birlikte verimli kullanmak
**Seviye:** Başlangıç → İleri
**Süre:** ~30 dakika okuma + alıştırmalar

---

## Araçların Rolleri

| Araç | Ne Yapar | Ne Yapmaz |
|---|---|---|
| **Obsidian** | Notlar, kararlar, faz geçmişi, bağlantı haritası | Kod yazar, analiz yapar |
| **Claude Code** | Kod yazar, karar verir, analiz eder, açıklar | Notları otomatik saklar |

> **Temel kural:** Obsidian hafıza, Claude Code akıl yürütme motorudur.

---

## BÖLÜM 1 — Obsidian Temelleri

### 1.1 Vault Nedir?

Vault = projenin tüm notlarının durduğu klasör.
Bu projedeki vault: `d:\Projeler\CleanTenant\docs\wiki\CleanTenant\`

Her not bir `.md` (Markdown) dosyasıdır. Bulut yok, hesap yok — her şey bilgisayarında.

### 1.2 Temel Kısayollar

| Kısayol | İşlev |
|---|---|
| `Ctrl+O` | Not arama ve açma |
| `Ctrl+N` | Yeni not oluştur |
| `Ctrl+G` | Graph View (bağlantı haritası) |
| `Ctrl+Shift+F` | Vault genelinde metin arama |
| `Ctrl+P` | Komut paleti (her şeye erişim) |
| `Ctrl+E` | Düzenleme ↔ Okuma modu geçişi |

### 1.3 Notlar Arası Bağlantı

`[[not adı]]` yazarak notları birbirine bağlarsın.

```markdown
Bu karar [[ADR-001 Hibrit JWT + Redis Session]] notunda detaylı açıklanmıştır.
```

- `[[` yazmaya başlayınca otomatik tamamlama açılır
- `Ctrl+tıkla` → o nota gider
- Sağ panelde "Backlinks" → o notu hangi notlar referans veriyor gösterir

### 1.4 Graph View Kullanımı

`Ctrl+G` → Tüm notların bağlantı ağı görünür.

**Pratik filtreler:**
- Sağ üst **Filters** → arama kutusuna `ADR` yaz → sadece karar notları
- **Groups** → klasöre göre renk ata
- Bir notun üzerine tıkla → sadece o notun bağlantıları vurgulanır

---

## BÖLÜM 2 — Claude Code Temelleri

### 2.1 Claude Code Nedir?

Projenin kaynak kodunu, git geçmişini ve bellek dosyalarını bilen bir AI asistan.
Her oturumda `CLAUDE.md` ve `memory/` klasöründen bağlamı otomatik yükler.

### 2.2 Ne Zaman Kullanılır?

```
Soru sormak          → "Bu mimari karar neden verildi?"
Kod yazmak           → "CompanyService için CreateCommand yaz"
Karar almak          → "Bu özelliği hangi katmana koymalıyım?"
Analiz               → "Bu faz için hangi riskler var?"
Not taslağı          → "Bu kararı ADR formatında yaz"
```

### 2.3 Etkili Soru Sorma

**Kötü soru:**
```
Kod yaz.
```

**İyi soru:**
```
Application katmanında bir CreateBudgetCommand handler yazıyorum.
EF Core mu Dapper mı kullanmalıyım? Neden?
```

**En iyi soru (vault notu ekleyerek):**
```
Şu notuma göre karar vermem gerekiyor:

[ADR-003 notunun içeriği]

Sorum: Bütçe raporlama sorgusu bu karara göre Dapper mı olmalı?
```

### 2.4 Claude Code'un Sınırları

- Yaptığı değişiklikleri **otomatik Obsidian'a yazmaz** — sen kaydetmen gerekir
- Her oturum başında bağlamı yeniden yükler — önemli kararları vault'a not al
- UI/UX kararlarında tek başına karar vermez — senden onay ister

---

## BÖLÜM 3 — Birlikte Kullanım Akışları

### Akış A — Yeni Mimari Karar

```
1. Obsidian → Ctrl+N → "Kararlar/ADR-005 XYZ" notu oluştur
2. Claude Code'a sor: "Bu konuda hangi yaklaşım daha doğru?"
3. Yanıtı Obsidian notuna yapıştır
4. [[ilgili notlara]] bağlantı ekle
5. Hoş geldiniz.md → ADR listesine ekle
```

### Akış B — Faz Başlangıcı

```
1. Claude Code: "v0.X fazı için plan öner"
2. Planı Obsidian → Fazlar/v0.X/README.md olarak kaydet
3. Faz süresince kararları ADR notlarına ekle
4. Faz kapanışında: Claude Code → mimari harita üret
5. Haritayı Fazlar/v0.X/FINAL-ARCHITECTURE-MAP.md olarak kaydet
```

### Akış C — Günlük Çalışma

```
Sabah:
  Obsidian → Hoş geldiniz.md → açık kararlar / devam eden fazı kontrol et

Çalışırken:
  Karar gerekince → Claude Code'a sor → Obsidian'a kaydet

Akşam:
  Obsidian → kısa oturum notu → ne yapıldı, ne kaldı, ne öğrenildi
```

### Akış D — Vault'taki Notla Soru Sormak

```
1. Obsidian'da ilgili notu aç
2. Ctrl+A → Ctrl+C (tümünü kopyala)
3. Claude Code'a gel
4. Şablonu kullan:

   Şu notuma göre sorum var:
   [yapıştır]
   Sorum: ...
```

---

## BÖLÜM 4 — Bu Vault'un Yapısı

```
CleanTenant Wiki/
├── _prompts/              ← Sistem bağlamı (LLM için)
├── Fazlar/
│   ├── v0.0/              ← Bu eğitim (şu an buradasın)
│   ├── v0.1/              ← Backend temeli
│   └── v0.2/              ← UI + özellikler
├── Kararlar/              ← ADR'ler (mimari kararlar)
├── Mimari/                ← Katman ve kural notları
├── Kimlik & Auth/         ← Auth akışları, token yapısı
├── Specs/                 ← Özellik spesifikasyonları
├── Keşif/                 ← Vizyon, keşif notları
└── Fikirler/              ← Henüz kararlaştırılmamış fikirler
```

---

## BÖLÜM 5 — Pratik Alıştırmalar

### Alıştırma 1 — Not Bul ve Oku (2 dakika)
1. `Ctrl+O` bas
2. "ADR-002" yaz
3. Notu aç, içeriğini oku
4. Sayfanın altındaki `[[ADR-003]]` linkine `Ctrl+tıkla`

### Alıştırma 2 — Yeni Not Oluştur (3 dakika)
1. `Ctrl+N` → başlık: `Fikirler/Redis Pub-Sub Fikirleri`
2. İçine şunu yaz:
   ```
   ## Fikir
   Lokalizasyon cache invalidation için Redis pub-sub kullanılabilir.
   
   ## İlgili
   [[ADR-002 Dört Veritabanı Mimarisi]]
   ```
3. `Ctrl+S` kaydet

### Alıştırma 3 — Graph View (2 dakika)
1. `Ctrl+G` bas
2. Sağ üst Filters → "Kararlar" yaz
3. ADR notlarının birbirine nasıl bağlı olduğunu gözlemle

### Alıştırma 4 — Claude Code ile Çalış (5 dakika)
1. `ADR-003 EF Core ve Dapper Hibrit Okuma.md` notunu aç
2. `Ctrl+A` → `Ctrl+C`
3. Claude Code'a git, şunu yaz:
   ```
   Şu notuma göre sorum var:
   [yapıştır]
   Sorum: Bütçe modülünün aylık özet raporu Dapper mı EF Core mı olmalı?
   ```
4. Yanıtı değerlendir, beğenirsen yeni bir not olarak kaydet

---

## BÖLÜM 6 — İleri Kullanım İpuçları

### Etiket Sistemi
Notlara etiket ekleyerek filtreleme yapabilirsin:
```markdown
---
tags: [karar, auth, v0.2]
---
```
Graph View'de etikete göre renklendirme yapılabilir.

### Şablon Kullanımı
Vault'ta `_templates/` klasörü oluşturursan yeni notları şablondan açabilirsin.
Örnek: Her ADR için aynı başlık yapısını otomatik doldur.

### Arama Operatörleri
`Ctrl+Shift+F` gelişmiş arama:
```
tag:#karar          → etiketli notlar
path:Kararlar       → sadece Kararlar klasörü
line:(EF Core)      → satır içeren notlar
```

---

## Özet

```
Obsidian  →  hafıza, bağlantı, geçmiş
Claude Code  →  akıl yürütme, kod, analiz

İkisi birlikte  →  hem ne yaptığını hem neden yaptığını bilen bir sistem
```

> Bir karar verildiğinde Obsidian'a yazmazsan, bir sonraki oturumda unutulur.
> Obsidian'a yazılan her karar, projenin kalıcı hafızası olur.

---

*Faz v0.0 — Araç Eğitimi | Oluşturuldu: 2026-05-21*
*Sonraki: [[Fazlar/v0.1/README]]*
