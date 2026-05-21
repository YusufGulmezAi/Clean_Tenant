# 03 — Karar Bekleyen Sorular

> **Bu sprint'in kodlamasına başlamadan önce aşağıdaki 6 kararın hepsi kapatılmalı.**
> Her karar için: Soru → Önerim (gerekçeli) → Alternatifler → Kullanıcı Kararı (boş, kullanıcı dolduracak).
>
> **Claude Code:** Bir kararı kullanıcı işaretlemeden o konuda kod yazma. Kararsız bir konuda kararsız ilerleme; dur, sor.

---

## KARAR #1 — KapıNo Unique Scope'u

**Soru:** Bir BB'nin `KapiNo` alanı hangi scope'ta unique olmalı?

**Bağlam:** Aynı yerleşkede iki blok varsa, her birinde "Daire 5" olabilir. Aynı blokta iki "Daire 5" olamaz.

**Önerim:** `KapiNo` **Blok scope'unda unique**, eğer Blok yoksa **Yapı scope'unda unique**. Yani:
- BB Blok'a bağlı ise: `(TenantId, BlokId, KapiNo)` UNIQUE.
- BB doğrudan Yapı'ya bağlı ise (villa tipi): `(TenantId, YapiId, KapiNo)` UNIQUE.

**Alternatifler:**
- A) Site scope unique (zor — büyük sitede çok yapı, çakışmalar olası).
- B) Tenant scope unique (anlamsız — farklı sitelerde aynı daire no olabilir).
- C) Hiç unique değil, sadece BB ShortCode unique (önerim daha güvenli).

**Kullanıcı Kararı:**
- [ ] Önerim onaylandı (Blok/Yapı scope unique)
- [ ] Diğer: ___________

---

## KARAR #2 — Yönetim Birimi: Yapı mı, Site mi?

**Soru:** Türk hukukunda KMK'ya göre **yönetim plan birimi yapı düzeyinde** kurulabilir (her yapı kendi kat malikleri kurulu) veya **birleşik yönetim** olabilir (tek site yönetimi tüm yapıları yönetir). Sistem hangisini destekleyecek?

**Bağlam:**
- KMK m.65 ve sonrası "Toplu Yapı Yönetimi" hükümleri: birleşik yönetim de yapı-bazlı yönetim de mümkün.
- Pratikte: küçük apartman → tek yönetim. Büyük site (10 blok, 5 yapı) → genellikle birleşik yönetim ama her yapının kendi karar defteri olabilir.

**Önerim:** **Bu sprint için sadece Site (Company) seviyesinde tek yönetim destekle.** Yani 1 Site = 1 Yönetim = 1 Bütçe. Yapı-bazlı yönetim Wave 3+'a ertele.

**Gerekçe:** Yapı-bazlı yönetim eklemek `Butce.YapiId` mi yoksa `Butce.SiteId` mi sorusunu açar, ve "bütçe kalemini hangi yapıya yansıt" probleminin matematiği değişir. MVP scope'unda gereksiz karmaşıklık.

**Alternatifler:**
- A) Sadece Site-bazlı yönetim (önerim — basit).
- B) Hem Site hem Yapı-bazlı yönetim destekle (karmaşık — Wave 3'e ertele).
- C) Sadece Yapı-bazlı yönetim (anlamsız — küçük apartmanlar için overkill).

**Kullanıcı Kararı:**
- [ ] Önerim onaylandı (sadece Site-bazlı, Yapı-bazlı Wave 3'e)
- [ ] Diğer: ___________

---

## KARAR #3 — ShortCode Üretim Stratejisi

**Soru:** 8 karakter alfanumerik ShortCode üretiminde:
- Hangi alfabe kullanılacak?
- Çakışma (collision) durumunda ne yapılacak?
- Hangi servis üretecek (DB sequence mi, code-side generator mı)?

**Önerim:** 
- **Alfabe:** Crockford Base32 (`0123456789ABCDEFGHJKMNPQRSTVWXYZ` — `I`, `L`, `O`, `U` hariç; çünkü `1`/`l`, `0`/`O` karışıyor).
- **Üretim:** Code-side, `IShortCodeGenerator` interface'i ile. Random 8 char üret + DB'de unique check + çakışma varsa 3 kez tekrar dene. 3 deneme sonrası fail → exception.
- **Scope:** Entity-tipi başına unique (örn: `BagimsizBolum.ShortCode` farklı entity'lerle çakışabilir, sorun değil).
- **Tenant scope:** `(TenantId, ShortCode)` unique constraint (cross-tenant aynı ShortCode olabilir).

**Alternatifler:**
- A) UUID short (Base58 6 char) — kısa ama daha çok çakışma riski.
- B) Sekanslı tenant prefix + counter (`T001-BB-0042`) — okunabilir ama tahmin edilebilir.
- C) Crockford Base32 (önerim).

**Kullanıcı Kararı:**
- [ ] Önerim onaylandı (Crockford Base32, 8 char, code-side generator)
- [ ] Diğer: ___________

---

## KARAR #4 — Mevcut "Site" Entity'sine Yapılacak Müdahale

**Soru:** Mevcut `Site` (= Company) entity'si Yapı Şeması altına entegre olacak. Mevcut yapı korunarak mı genişletilecek yoksa refactor mı?

**Bağlam:** Mevcut Site entity'si User/Tenant modülleriyle birlikte zaten kullanılıyor. Yapı Şeması (Ada/Parsel/Yapı/Blok/BB) Site'ın altına eklenecek.

**Önerim:** **Mevcut Site entity'sine dokunma.** Sadece Ada entity'sinden Site'a FK ekle. Yani:
- `Ada` → `SiteId` (FK to `Site.Id`).
- `Site` entity'sine `Adalar` navigation property eklenebilir (opsiyonel — kullanılacaksa).
- Mevcut `Site` migration'ı değişmez; yeni migration sadece Yapı Şeması tablolarını ekler.

**Alternatifler:**
- A) Mevcut Site'a dokunma (önerim).
- B) Site entity'sine `Adalar`, `ToplamBBSayisi` gibi alanlar ekle, mevcut migration'ı update et (riskli, breaking change olabilir).

**Kullanıcı Kararı:**
- [ ] Önerim onaylandı (Site'a dokunma, sadece FK ekle)
- [ ] Site'a navigation property ekle ama domain logic ekleme
- [ ] Diğer: ___________

---

## KARAR #5 — `BagimsizBolumTipi`: Enum mu, Lookup Table mı?

**Soru:** BB tipleri (`Konut, Dukkan, Ofis, Depo, Otopark, Diger`) C# enum olarak mı yoksa veritabanı lookup tablosu olarak mı?

**Önerim:** **C# enum** + database `INT` kolonu olarak sakla.

**Gerekçe:**
- Sayısı az (< 10), nadiren değişir.
- Code-first değişiklikler kolay (yeni enum value → migration güncelle).
- Lookup tablosu fazla overhead (her query'de JOIN gerekli).
- İhtiyaç çıkarsa Wave 3+'da lookup tablosuna refactor edilebilir.

**Alternatifler:**
- A) Enum (önerim).
- B) Lookup tablo (`BagimsizBolumTipi` tablo, `(Id, Kod, Ad)`).
- C) Hibrit: hem enum hem lookup — her tenant kendi tiplerini ekleyebilir (Wave 3+'da gerekirse).

**Aynı soru `YapiTipi` için de geçerli — aynı karar uygulanacak.**

**Kullanıcı Kararı:**
- [ ] Önerim onaylandı (Enum + INT column)
- [ ] Diğer: ___________

---

## KARAR #6 — Bütçe Onay Akışı: MVP'de Var mı?

**Soru:** Bütçenin "taslak → yayınlandı" geçişinde Onay süreci olacak mı? (Module.Onay daha kurulmadı.)

**Bağlam:** SDD'de Wave 2-3'te detaylı onay süreci öngörülmüştü. MVP'de hızlı ilerlemek için ne yapacağız?

**Önerim:** **MVP'de basit "Yayınla" butonu**, formal onay akışı YOK. Sadece yetki kontrolü: `IButceYayinlanaYetki` policy ile yayınlanabilen rolleri kısıtla.
- Wave 3'te Module.Onay entegrasyonu geldiğinde, `PublishButceCommand` üzerinden onay süreci tetiklenir.
- Şu anki kod o entegrasyonu kolaylaştıracak şekilde yapılsın: `IApprovalService` interface'i Application katmanında yer alsın, ama implementation'ı şimdilik `AutoApproveApprovalService` (her şeyi onaylar) olsun. Wave 3'te gerçek `WorkflowApprovalService` ile replace edilir.

**Alternatifler:**
- A) Basit publish + yetki, gelecek için interface (önerim).
- B) Onay süreci olmadan direkt yayın (gelecekte refactor ağır olur).
- C) Module.Onay'ı önce kur, sonra bütçe (sprint uzar 1+ hafta).

**Kullanıcı Kararı:**
- [ ] Önerim onaylandı (basit publish + yetki + interface hazır)
- [ ] Diğer: ___________

---

## Karar Sayfası Kullanım Talimatı

**Kullanıcı:** Her karar için `[ ]` kutucuğunu `[x]` ile işaretle, alternatif istiyorsan "Diğer" satırına yaz.

**Claude Code:**
1. Bu dosyayı her sprint başlangıcında oku.
2. Karar verilmemiş (`[ ]`) varsa kullanıcıya sor: *"Karar #X henüz açık. Önerim şu, kabul mü?"*
3. Karar verildikten sonra dosyayı git'e commit et (`docs: kararlar güncellendi`).
4. Sonra kodlamaya başla.

---

## Sprint Sırasında Yeni Karar Gerekirse?

**Kullanıcı veya Claude Code bir karar daha gerektiğini fark ederse:**
1. Dur (kod yazma).
2. Bu dosyaya yeni numarayla soru ekle (KARAR #7, #8...).
3. Önerini ve gerekçeni yaz.
4. Kullanıcıya bildir.
5. Cevap geldikten sonra devam et.

> **"Tahmin yürütüp devam etme" anti-pattern'i.** Karar belirsiz iken kod yazılırsa, sonradan refactor maliyetli olur.
