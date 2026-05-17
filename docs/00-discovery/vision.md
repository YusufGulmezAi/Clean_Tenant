# Toplu Yapı Yönetimi — Vizyon Belgesi

**Belge Sürümü:** 0.1
**Tarih:** 2026-05-16
**Sahibi:** Yusuf Gülmez
**Durum:** Onaylandı

---

## 1. Ürün Adı

**TopluYapiYonetimi** (geçici proje kodu: `CleanTenant`)

---

## 2. Vizyon Beyanı

Türkiye'deki konut sitesi, AVM ve marina yöneticileri ile bağlı oldukları
yönetim firmaları, mali müşavirler ve sakin/kiracılar için tek çatı altında
toplanmış; **e-dönüşüme uyumlu, şeffaf ve hizmet kalitesi yüksek** bir bulut
tabanlı SaaS platformu sunmak.

Platform; muhasebe, aidat, bütçe, finans, ödeme yönetimi, CRM, insan
kaynakları, satın alma ve mali müşavirlik (bordro + e-beyanname) süreçlerini
tek yerden yönetilebilir kılar.

---

## 3. Çözülen Problem

Bugün toplu yapı yönetimi süreçleri **dağıtık** şekilde yürütülüyor:
- Site çalışanları (yönetici, kapıcı, muhasebe)
- Yönetim firmaları
- Mali müşavirlik büroları
- Konusunda uzman 3. taraflar

Bu dağınıklık üç temel acıya yol açıyor:

| # | Acı | Etkisi |
|---|---|---|
| 1 | **E-dönüşüm eksikliği** | Manuel süreçler, hata payı, denetim zorluğu |
| 2 | **Düşük hizmet kalitesi** | Sakin memnuniyetsizliği, yönetim itibarı kaybı |
| 3 | **Şeffaflık eksikliği** | Maliklerin gelir-gider güvenine zarar, KMK uyumu zafiyeti |

---

## 4. Çözümün Farklılaşma Noktaları

Rakipler (Apsiyon, Sen Yönet, Yönetişim) ile karşılaştırıldığında:

- **Banka API entegrasyonu ile otomatik para transferi** *(birincil farklılaştırıcı)*
- Tek platformda mali müşavirlik (bordro + e-beyan) hizmetinin sunulması
- E-dönüşüme tam uyum hedefi
- Sakin/kiracı için şeffaf, kolay erişilebilir bilgi sunumu

---

## 5. Hedef Kullanıcılar

| Persona | Rol | Profil | Erişim |
|---|---|---|---|
| Yönetim Firması Personeli | Birincil operatör | 25–55 yaş, orta tek. okuryazarlığı | Web (pro UX) |
| Site Yönetimi Personeli | Birincil operatör | 35–55 yaş, **düşük** tek. okuryazarlığı | Web (pro UX) |
| Malik (Bağımsız Bölüm Sahibi) | Tüketici | 18–80 yaş, karışık | Web portal (V1) → Mobil (V1.5) |
| Kiracı | Tüketici | 18–80 yaş, karışık | Web portal (V1) → Mobil (V1.5) |
| Mali Müşavir | Hizmet sağlayıcı | Uzman | Web (pro UX) — V2 |

**UX Stratejisi:** "Pro" (yoğun, formlu) ve "Lite" (basit, sakin/kiracı odaklı)
olmak üzere iki ayrı arayüz dili.

---

## 6. Hedef Müşteri (Ödeyen)

- Yönetim firmaları
- Site / AVM / Marina yönetimleri (direkt müşteri olabilir; sistemde
  daima bir "yönetim firması" tenant'ı olarak modellenir)

**Hiyerarşi:** Yönetim Firması (Tenant) → Tesis (Site/AVM/Marina) → Blok →
Bağımsız Birim (Daire/Dükkan/Tekne Yeri)

---

## 7. Kapsam — Sürüm Yol Haritası

### V1 — MVP (3 ay sonra canlı)
1. Tenant + Kullanıcı + Rol/İzin yönetimi
2. Tesis (yalnız **Site** tipi) + Blok + Bağımsız Birim tanımlama
3. Malik & Kiracı yönetimi
4. **Aidat tahakkuku** — birden fazla gider kalemi (KMK uyumlu: olağan,
   olağanüstü, demirbaş, yakıt avansı vb.)
5. **Ödeme takibi + Sanal POS entegrasyonu**
6. Temel gelir-gider muhasebesi (kasa, banka)
7. Toplu duyuru (e-posta + SMS)
8. Basit raporlama (tahsilat, borçlu listesi, kasa-banka)
9. Sakin/Kiracı **Web Portalı**
10. **Çoklu para birimi altyapısı** (V1'de TL + EUR/USD görüntüleme)
11. KVKK uyumu + 634 Sayılı KMK uyumu

### V1.5 — (4–6 ay)
- CRM / Talep & Şikayet yönetimi
- Satın alma süreci
- Bütçe & planlama
- Tam muhasebe (yevmiye, mizan, bilanço)
- **Mobil uygulama** (sakin/kiracı için)
- **Banka API entegrasyonu** (otomatik para transferi — birincil
  farklılaştırıcı; teknik fizibilite gerekiyor)
- Diğer tesis tipleri: AVM

### V2 — (6–12 ay)
- HR / Puantaj / Bordrolama
- E-Bildirge (SGK entegrasyonu)
- e-Beyanname (GİB entegrasyonu)
- Mali Müşavirlik hizmet modülü
- E-fatura / e-arşiv
- Marina, Sosyal Tesis tipleri

### Kapsam Dışı (şimdilik)
- Donanım / IoT entegrasyonları (kapı, plaka tanıma, kamera)
- Mevcut muhasebe yazılımlarıyla entegrasyon

---

## 8. Ölçek Hedefleri (İlk 12 ay)

| Metrik | Hedef |
|---|---|
| Tenant (yönetim firması) sayısı | 5 |
| Yönetilen tesis (toplam) | ~75 (5 × 15) |
| Toplam bağımsız birim | **~4.500** (5 tenant toplamı) |
| Eş zamanlı aktif kullanıcı | ~45 |

---

## 9. Başarı Kriterleri

### 6. ay
- 3 ödeyen tenant
- 10 yönetilen tesis (canlıda)
- %95+ uptime
- Bir kullanıcının aidat tahakkukunu < 5 dk içinde yapabilmesi

### 18. ay
- 10 ödeyen tenant
- Mobil uygulama yayında
- E-fatura entegrasyonu canlıda

---

## 10. Kısıtlar

| Tip | Kısıt |
|---|---|
| **Takvim** | MVP için 3 ay (sert deadline) |
| **Kaynak** | 1 geliştirici (solo) |
| **Yasal — KVKK** | Zorunlu; veri saklama, silme hakkı, açık rıza |
| **Yasal — 634 KMK** | Aidat, karar defterleri, denetim izi gereksinimleri |
| **Yasal — Sanal POS** | BDDK lisanslı PSP üzerinden çalışma |
| **Diğer** | Üçüncü taraf yazılımlarla entegrasyon zorunluluğu **yok** |

---

## 11. Bilinen Riskler

| # | Risk | Olası Etki | Hafifletme |
|---|---|---|---|
| R1 | **Kapsam-Takvim baskısı** | MVP zamanında bitmez | V1 modül sırası katı tutulmalı, "nice-to-have" eklemeler reddedilmeli |
| R2 | **Solo geliştirici tek nokta riski** | Hastalık, tükenmişlik, bilgi siloları | Erken dokümantasyon, otomatize CI/CD, açık kod |
| R3 | **Banka API entegrasyonu BDDK belirsizlik** | Birincil farklılaştırıcı gecikebilir | V1.5'a alındı; fizibilite çalışması ayrı yapılacak |
| R4 | **Düşük tek. okuryazarlığı olan kullanıcı** | Onboarding zor, churn yüksek | Pro/Lite UX ayrımı, video rehberler, sade form akışları |
| R5 | **KVKK + KMK uyumu** | Yasal yaptırım, müşteri güvensizliği | Faz 3'te tehdit modeli (STRIDE) + denetim izi tasarımı |
| R6 | **E-Beyan/SGK entegrasyonu V2** | Mali müşavirlik vaadi gecikir | V2 kapsamı, erken pazarlama yapmamak |

---

## 12. Onaylanmış Kararlar

- ✅ V1'de sadece **Site** tesis tipi
- ✅ V1'de sakin/kiracı için **yalnız web portal** (mobil V1.5)
- ✅ Çoklu para birimi altyapısı V1'de hazır olacak
- ✅ Hiyerarşi her zaman: Yönetim Firması → Tesis → Blok → Birim
  (site bağımsız yönetilse bile sistem yönetim firması altında modeller)
- ✅ Aidat çoklu gider kalemi destekleyecek
- ✅ Banka API entegrasyonu temel farklılaştırıcı olarak konumlandırıldı
- ✅ Ürün adı: **TopluYapiYonetimi**

---

## 13. Açık Sorular (Sonraki Belgelerde Cevaplanacak)

- Hangi sanal POS sağlayıcıları desteklenecek? (iyzico, PayTR, Param, ...)
- Hangi banka API'ları öncelik? (Garanti, İş Bankası, Akbank, Ziraat, ...)
- SMS sağlayıcı? (NetGSM, İletimerkezi, Twilio, ...)
- KVKK için veri saklama süresi politikası ne olacak?
- Çoklu dil desteği V1'de gerekli mi? (sadece TR ile başla?)
- Tenant onboarding'i self-service mi, manuel mi?
- Fiyatlandırma modeli? (Tesis başına? Birim başına? Sabit aylık?)

---

**Belge Durumu:** Onaylandı — `stakeholders.md` ve `scope.md` belgeleri sırada.
