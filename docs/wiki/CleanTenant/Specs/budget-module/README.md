# Bütçe Modülü — Spec Paketi (Claude Code'a Devir İçin)

> Bu klasör, **VS Code'taki Claude Code**'a verilecek 5 doküman içerir.
> Repo'ya `docs/specs/budget-module/` altına koyman önerilir.

## Dosya Sırası ve Amacı

| # | Dosya | Boy | Amaç |
|---|-------|-----|------|
| 1 | `00-CONTEXT.md` | ~6 KB | Proje bağlamı — Claude Code'un ilk okuyacağı |
| 2 | `01-SDD-v1.0.md` | ~80 KB | Master tasarım dokümanı (önceki çıktımız) |
| 3 | `02-PHASE-CARDS.md` | ~20 KB | Bu sprint'in 8 fazlık iş paketi |
| 4 | `03-DECISIONS-OPEN.md` | ~8 KB | Karar bekleyen 6 soru (sen dolduracaksın) |
| 5 | `04-CLAUDE-CODE-PLAYBOOK.md` | ~11 KB | Claude Code'a özel çalışma yönergesi |

## VS Code'a Aktarım Adımları

### 1. Repo'da klasör oluştur
```bash
mkdir -p docs/specs/budget-module
```

### 2. 5 dosyayı klasöre kopyala
Bu klasördeki tüm `.md` dosyalarını `docs/specs/budget-module/` altına yerleştir.

### 3. Claude Code için `.claude/instructions.md` (önerilen)
Repo köküne:
```bash
mkdir -p .claude
cat > .claude/instructions.md << 'EOF'
# Claude Code için Proje Talimatları

Her yeni oturumda aşağıdaki dosyaları sırayla oku:

1. `docs/specs/budget-module/00-CONTEXT.md` — Proje bağlamı (zorunlu)
2. `docs/specs/budget-module/04-CLAUDE-CODE-PLAYBOOK.md` — Çalışma protokolü (zorunlu)
3. `docs/specs/budget-module/03-DECISIONS-OPEN.md` — Açık kararlar (zorunlu — karar verilmemiş varsa kullanıcıya sor)
4. `docs/specs/budget-module/02-PHASE-CARDS.md` — Mevcut sprint (zorunlu)
5. `docs/specs/budget-module/01-SDD-v1.0.md` — Master spec (gerektikçe referans)

Bu dokümanlar bağlayıcıdır. Çelişki gördüğünde dur, sor.
EOF
```

### 4. İlk komutu Claude Code'a ver
VS Code'da Claude Code'u aç ve şu komutu ver:

```
@workspace docs/specs/budget-module/ klasöründeki tüm dosyaları sırayla oku 
(00 → 04 → 03 → 02 → 01). Önce okuduğunu özetle. Sonra 03-DECISIONS-OPEN.md'deki 
6 karar için önerilerini söyle ve benim kararımı bekle. Karar almadan kod yazma.
```

### 5. NotebookLM güncelleme
Aynı 5 dosyayı NotebookLM'e de yükle. Faz sonlarında güncellersin.

## Sonraki Adım

Claude Code "Karar #1 hakkındaki önerin nedir?" diye soracak.
6 kararı verir vermez **FAZ 1** (Yapı Şeması Domain Modeli) başlar.

## Süreç Beklentisi

| Aşama | Süre |
|-------|------|
| 6 karar verme | 1-2 saat (bir oturum) |
| FAZ 1-4: Yapı Şeması | ~1 hafta |
| FAZ 5-6: Bütçe + Tahakkuk | ~1 hafta |
| FAZ 7: Tahsilat + UI | ~3 gün |
| FAZ 8: Test + Senaryolar | ~2 gün |
| **Toplam** | **~3 hafta** |

## Genel Tavsiyeler

- **Her faz sonunda dur.** Yusuf demo et, NotebookLM güncelle, sonra sonraki faza geç.
- **Scope büyütme yasak.** "Bunu da hızlıca ekleyeyim" diyenler genellikle başaramaz.
- **Claude Code soru sorduğunda iyi haber.** Demek ki belirsizliği farkında, körlemesine yazmıyor.
- **Karar verirken hızlı ol.** Karar gecikirse Claude Code beklemek zorunda kalır.

## Bu Paket Yetersiz Kalırsa?

Sprint ortasında ek spec gerekirse, aynı pattern'de mikro-spec dosyaları üret:
- `05-FORMULA-DSL-SPEC.md` (Wave 3'te)
- `06-EAV-MODEL-SPEC.md` (Wave 3'te)
- vs.

Her zaman: önce karar netleştir → sonra mikro-spec yaz → sonra Claude Code'a ver.

---

**Hazırlayan:** SDD ekibi
**Tarih:** 20 Mayıs 2026
**Versiyon:** 1.0
