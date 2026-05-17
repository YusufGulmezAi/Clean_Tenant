# Graph Explorer — Feature Idea Notu

| Alan | Değer |
|---|---|
| **Statüs** | İdea / Faz 1+ planlama notu |
| **Atılan tarih** | 2026-05-17 |
| **Hedef faz** | Faz 1 (UI tasarımı) → Faz 2+ (gerçek implementasyon) |
| **Esin kaynağı** | Obsidian Graph View |

---

## 1. Hedef

CleanTenant'ın `Tenant → Company → Building → Unit` hiyerarşisini ve **cross-cutting `User` atamalarını** tek bir görsel ağ üzerinde sergilemek. Tree view sadece hiyerarşiyi gösterir; graph view ikisini birden — hiyerarşik bağı **kalın oklarla**, kullanıcı atamasını **farklı renkte / stilde kenarlarla** ayırarak.

Property management domain'inin doğal bir graph olduğu gözleminden hareketle: bir Tenant Admin'in tüm organizasyonunu, bir System operatörünün incelediği müşterinin yapısını, ya da bir kullanıcının tüm scope'larındaki rollerini **tek bakışta** anlamak için.

---

## 2. Senaryolar

| # | Senaryo | Kullanıcı | Kıymet |
|---|---|---|---|
| 1 | Tenant Admin kendi organizasyonunun haritasını görür | Tenant Admin | Yapıyı bütünsel görme; eksik atamalar / sahipsiz unit'ler hızla fark edilir |
| 2 | System Support operatörü tenant'a girdiğinde graph karşılar | CustomerSupport / TechnicalSupport | Müşterinin yapısını saniyeler içinde kavrar; ticket'taki probleme hızlı yer'leştirme |
| 3 | Bir kullanıcının tüm atamalarını user-centric görsel | SystemAdmin | "Süper-user" tespiti; izin denetimi |
| 4 | Sales ekibi prospect'e demo verirken yapı anlatımı | Sales | Görsel anlatım, kelimelerden daha güçlü |
| 5 | Tenant'ın denetim raporu için yapının ekran görüntüsü | Accountant / Manager | Müşteri sunumları, yıllık raporlar |
| 6 | KVKK / şeffaflık: kullanıcı kendi atamalarını görür | Malik / Sakin / Kiracı | "Sistemde benimle ilgili neler var" sorusuna görsel cevap |

---

## 3. Veri Modeli (Mevcut, Şimdiden Hazır)

Aşağıdaki entity'ler doğrudan graph'a dönüşür — ek bir tablo gerekmez:

**Düğümler (Nodes):**
- `Tenant` — root
- `Company`
- `Building`  *(Faz 1'de Domain'e eklenecek)*
- `Unit`  *(Faz 1'de Domain'e eklenecek)*
- `User`

**Kenarlar (Edges):**
- `Tenant.Id ← Company.TenantId` — hiyerarşi
- `Company.Id ← Building.CompanyId` — hiyerarşi
- `Building.Id ← Unit.BuildingId` — hiyerarşi
- `UserRoleAssignment` (tablo, çok yönlü kenar) — User ↔ Scope: kullanıcının hangi Tenant/Company/Unit'te hangi rolde olduğunu cross-cutting kenar olarak modeller. Edge'in **rolle etiketlenmiş** olması zenginlik katar (örn. User → Unit ◦ "Malik" ◦ "Kiracı").

**Bonus düğümler (Faz 2+):**
- `Invoice`, `Payment` — finansal akışlar User ↔ Unit ekseninde gösterilebilir.
- `SupportSession` — özel "destek erişim" kenarı (geçici, time-bounded).

---

## 4. Teknik Yaklaşım

### Frontend
- **Cytoscape.js** (MIT) — graph odaklı, çok sayıda layout (force-directed / hierarchical / circular), plugin ekosistemi.
- Blazor `<GraphExplorer />` component'i Cytoscape'i `IJSRuntime` ile sarmalar.
- Alternatifler: vis-network (daha basit), Sigma.js (10K+ node'lara WebGL ile çıkar).

### Backend (Faz 2+)
- **`GraphDto`** Application'da: `{ Nodes: GraphNodeDto[], Edges: GraphEdgeDto[] }`
  - `GraphNodeDto`: `Id, UrlCode, Type, Label, ScopeLevel, Metadata`
  - `GraphEdgeDto`: `FromId, ToId, RelationType, Label?`
- **`GET /api/v1/tenants/{urlCode}/graph`** — Tenant Admin için tam graph
- **`GET /api/v1/users/{urlCode}/graph`** — User-centric graph (sadece kullanıcının olduğu node'lar ve direkt komşuları)
- **`GET /api/v1/system/tenants/{urlCode}/graph?supportSessionId=...`** — System operatör Support Mode'da

### Performans
- Property management ölçeğinde tek tenant: 100–500 node tipik, max 2000-3000.
- Cytoscape.js bu aralıkta sorunsuz.
- Sayfalama / lazy expand: büyük tenant'larda "Building'i tıkla → Unit'ler yüklensin" pattern'i.
- Backend tarafta `GraphDto` cache (Redis, 1 dk TTL) — sık ziyaret edilen tenant'lar için.

### Güvenlik
- **Permission filter**: Kullanıcının görme yetkisi olmadığı node'lar response'ta hiç yer almaz (DTO seviyesinde).
- Cross-tenant sızıntı imkansız: Tenant Admin yalnız kendi tenant'ının graph'ını çekebilir.
- Support Mode: `SupportSession` aktif olmadan System operatör başka tenant'ın graph'ını alamaz.
- Audit: her graph çağrısı Log DB'ye yazılır (hangi user, hangi tenant, ne zaman).

---

## 5. Etkileşimler (UX)

| Etkileşim | Davranış |
|---|---|
| Düğüme tıkla | İlgili detay sayfasına git (`/units/{urlCode}` vb.) |
| Düğüme hover | Mini info card (özet + URL kodu) |
| Sürükle-bırak | Düğümü el ile konumlandır (force layout pause) |
| Tip filtresi | Sadece "Unit" göster / sadece "User" göster |
| Scope filtresi | "Sadece Malik atamaları" |
| Arama | Düğüm adıyla highlight |
| Layout seçimi | Force-directed ↔ Hierarchical ↔ Circular toggle |
| Mini-harita | Büyük graph'larda navigation |
| Ekran görüntüsü / PDF export | Rapor için |

---

## 6. Aşamalandırma

| Aşama | Faz | İçerik |
|---|---|---|
| Hazırlık (zaten yapıldı) | v0.1.4 | Entity model + foreign key'ler graph'a uygun |
| Building/Unit entity'leri | Faz 1.1 | Domain'e ekle (henüz yok) |
| UI tasarımı oturumu | Faz 1.x | Tasarım dili + renk + layout kararı (UI danışma kuralı gereği önce konuşulur) |
| Backend GraphDto + endpoint | Faz 2.1 | Application + Infrastructure |
| Cytoscape.js wrapper | Faz 2.2 | Blazor component, JSInterop |
| Tenant Graph sayfası | Faz 2.3 | İlk sürüm — Tenant Admin için |
| User-centric Graph | Faz 2.4 | "Benim atamalarım" |
| System Support Graph | Faz 2.5 | Support Mode entegrasyonu |
| Performans optimizasyonu | Faz 3+ | Cache, lazy expand, WebGL geçişi (gerekirse) |

---

## 7. Açık Sorular (Faz 1 başlangıcında konuşulacak)

1. **Library kararı:** Cytoscape.js mı, vis-network mı, başka? (UI tasarımı oturumunun parçası.)
2. **Layout default'u:** Force-directed (Obsidian benzeri) mu, hierarchical (org chart benzeri) mı? Kullanıcı toggle edebilir mi?
3. **Renk paleti:** Node tipine göre mi (Tenant=mor, Company=mavi, Unit=yeşil, User=turuncu), yoksa scope level'a göre mi?
4. **Edge gösterimi:** Role adları kenarda yazılı mı (görsel olarak yoğunlaşır) yoksa hover'da mı?
5. **Building entity'si:** Var mı yok mu netleşmemişti — Faz 1.1'de Domain'e eklerken bu karar verilecek.
6. **Mobil görünüm:** MAUI Hybrid'de graph nasıl render olacak? (Cytoscape webview'de çalışır ama touch gesture'lar.)
7. **Saklama / favori:** Kullanıcı favori graph view'larını kaydetsin mi (filtre + layout kombinasyonu)?
8. **İhracat:** PNG / SVG / PDF? Hangileri?

---

## 8. İlham ve Referanslar

- **Obsidian Graph View** — force-directed local + global graph; bizim ilham noktamız.
- **Roam Research** — bidirectional links graph'ı.
- **Notion ✱ (databases relations)** — entity-based bağlantı görselleştirme.
- **Linear (issue graph)** — task ilişkileri.
- **AWS Architecture Diagrams** — hiyerarşik network görselleştirme.

---

## 9. Karar İhtiyacı (Şimdi)

Hiçbiri. Bu not bir **ide / öngörü kaydı**. Faz 1 başladığında bu dosyayı tekrar açıp UI tasarımı oturumuna oradan başlarız.

> **Bir sonraki dokunuş:** Faz 1 başlangıcında — UI tasarımı oturumunda library + tasarım dili + renk paleti birlikte konuşulacak.
