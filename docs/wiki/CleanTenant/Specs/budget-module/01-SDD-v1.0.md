# Bütçe Platformu — Sistem Tasarım Dokümanı (SDD)

| Alan | Değer |
|------|-------|
| **Belge tipi** | Sistem Tasarım Dokümanı (Software Design Document) |
| **Sürüm** | 1.0 (Nihai konsolidasyon) |
| **Durum** | Draft → Mimari onaya hazır |
| **Tarih** | 20 Mayıs 2026 |
| **Proje** | MultiTenant — Mali Domain |
| **Kapsanan modüller** | Module.Aidat, Module.Butce, Module.Formul (yeni) |
| **Üst referans** | `SYSTEM_PROMPT-v1.7.2.md` (mimari Single Source of Truth) |
| **Bu doküman bağlayıcı mıdır?** | Hayır — *önerilen* tasarım. Mimari onay (ONAY) gerektirir. |
| **Hedef okurlar** | Sistem Mimarları, Yazılım Mühendisleri, İş Analistleri, Test Mühendisleri |
| **Önceki çıktılar** | 3 bağımsız AI değerlendirmesi (CA, CG, DS) — Ek B'de özet |

---

## İçindekiler

**Bölüm I — Üst Bakış**
1. Yönetici Özeti
2. Doküman Kullanım Kılavuzu (Hangi paydaş hangi bölümü okumalı)
3. Vizyon ve Kapsam (In-scope / Out-of-scope)
4. Üst-Düzey Mimari (Genel resim)

**Bölüm II — İş Analisti Katmanı**
5. Domain Sözlüğü
6. Paydaşlar, Roller ve Yetki Matrisi
7. Fonksiyonel Gereksinimler (FR-01 ... FR-32)
8. İş Kuralları ve Hesaplama Politikaları
9. Operasyonel Yaşam Döngüleri

**Bölüm III — Mimar Katmanı**
10. Mimari Karar Kayıtları (ADR-01 ... ADR-15)
11. Modül Sınırları ve Etkileşim
12. NFR (Non-Functional Requirements)
13. Güvenlik, Yetki ve Uyum (KVKK, KMK, TBK, TTK)

**Bölüm IV — Geliştirici Katmanı**
14. Domain Modeli ve Entity Şeması
15. Formül DSL Spesifikasyonu (v0.9 taslak)
16. Hesaplama Bağımlılık Grafiği
17. Domain Event Kontratları
18. Geliştirme Standartları (MultiTenant kurallarıyla uyumlu)

**Bölüm V — Test Mühendisi Katmanı**
19. Test Stratejisi
20. Referans Veri Seti (15 Senaryo)
21. Kabul Kriterleri Şablonu
22. Definition of Done

**Bölüm VI — Yönetim**
23. Açık Tasarım Soruları (Mimari Karar Bekleyen)
24. Yol Haritası ve Fazlama
25. Risk Kaydı

**Ekler**
- Ek A: Mevzuat Referansları
- Ek B: Önceki Değerlendirmelerin Sentezi
- Ek C: Sözlük

---

# BÖLÜM I — ÜST BAKIŞ

## 1. Yönetici Özeti

Bu doküman, **1000+ farklı toplu yapının (site/apartman/AVM/marina/sosyal tesis) bütçe ve gider yönetimini tek platformda, kod değiştirmeden, UI üzerinden kurgulayabilen** bir mali alt-sistemin tasarımını tanımlar.

**Çekirdek tasarım kararları:**
- Gider tanımı, dağıtım kuralı, katılım kapsamı ve formül mantığı birbirinden bağımsız nesneler olarak modellenir.
- Bütçe ileriye-dönük versiyonlanır; geçmiş tahakkuklar dokunulmaz kalır.
- Şablon + istisna yaklaşımı ile yeni site dakikalar içinde devreye girer.
- Kullanıcı tanımlı formüller (no-code), kontrollü bir sandbox'ta çalıştırılır.
- Portföy verisi zamanla kurumsal hafızaya dönüşür; Faz 3'te öneri motoru bu hafızayı kullanır.

**Bu doküman üç bağımsız AI değerlendirmesinin sentezidir.** Üç değerlendirme de aynı sonuca ulaşmıştır: orijinal iş analizi belgesi güçlü bir vizyon ortaya koyar ancak doğrudan geliştirmeye başlamak için 4 hafta civarı ek tasarım gerekir. Bu doküman, eksiklerin tümünü doldurarak geliştirmeye-hazır seviyeye taşır.

**Geliştirmeye başlamadan önce mimari onay (ONAY) bekleyen 15 açık tasarım kararı vardır** (Bölüm 23). Bu kararlar verilmeden geliştiriciye iş paketi açılmamalıdır.

---

## 2. Doküman Kullanım Kılavuzu

| Paydaş | Okuması Zorunlu | Bilgi Amaçlı | Atlamalı |
|--------|-----------------|---------------|----------|
| **Sistem Mimarı** | Bölüm 1, 3, 4, 10, 11, 12, 13, 14, 16, 23, 24, 25 | 5, 7, 8, 9, 15 | 19, 20, 21, 22 |
| **Yazılım Mühendisi** | Bölüm 1, 4, 5, 14, 15, 16, 17, 18, 23 | 7, 8, 9, 10 | 6, 25 |
| **İş Analisti** | Bölüm 1, 3, 5, 6, 7, 8, 9, 20, 21 | 4, 11, 24 | 14, 15, 16, 17 |
| **Test Mühendisi** | Bölüm 1, 5, 7, 8, 19, 20, 21, 22 | 9, 17, 18 | 10, 14, 15, 16 |
| **Ürün Sahibi** | Bölüm 1, 3, 7, 23, 24, 25 | 6, 8, 9 | Geri kalan |

**Tipik okuma sırası:**
1. Bölüm 1-4 — herkes
2. Kendi ana bölümünüz
3. Açık tasarım soruları (Bölüm 23) — herkes
4. Yol haritası (Bölüm 24) — herkes

---

## 3. Vizyon ve Kapsam

### 3.1. Hedef

> "1000+ farklı yapısal özelliklere sahip toplu yapının bütçe süreçlerini, **tek yazılım değiştirmeden, UI üzerinden kurgu ile** yöneten; geçmiş bütçelerden öğrenen, kullanıcı tanımlı formüllerle genişleyebilen, dönem ortası revizyonları güvenle yöneten kurumsal mali platform."

### 3.2. Kapsam İçi (In-Scope)

**Wave 0-2:**
- Yerleşke, blok, bağımsız bölüm (BB), yapı bileşeni yönetimi
- Gider ağacı + gider paylaşım grubu + dağıtım modeli
- Katılım grubu + kapsam/muafiyet kuralları
- Bütçe oluşturma, versiyonlama, revizyon
- Kullanıcı tanımlı formül motoru (kontrollü DSL)
- Dönemsel tahakkuk üretimi, BB başına dağıtım
- Tahsilat kaydı, kısmi ödeme, kalan borç takibi
- Vade kuralları, gecikme cezası (KMK m.20 uyumlu)
- Şablon ve katalog kütüphanesi
- Audit izi (tüm işlemler için)
- Onay süreci (Module.Onay entegrasyonu)
- Çok kiracılı izolasyon (tüm tablolar TenantId)

**Wave 3-5:**
- Gelir yönetimi (aidat-dışı: kira, reklam, faiz)
- Net İşletme Sonucu raporu
- Yedek akçe yönetimi (KMK m.20 zorunlu)
- Banka mutabakatı
- Senaryo motoru (what-if simülasyonu)
- Veri standardizasyonu disiplini (auto-suggest, mapping)

**Wave 6-8:**
- Portföy analitiği
- Benzer site eşleştirme
- Öneri motoru (kural-tabanlı → istatistiksel → ML)
- eFatura/eDefter entegrasyonu (Module.Mali ile)
- Çok para birimi desteği

### 3.3. Kapsam Dışı (Out-of-Scope)

- KAT mülkiyet devri / tapu işlemleri (ayrı domain)
- Üretim/SatınAlma/Stok (Ajan B kapsamı)
- İcra takibi (Module.İcra ayrı modül)
- Kamera, geçiş, ziyaretçi yönetimi (Ajan C operasyonel modüller)
- Sözleşme yönetimi (Module.Sozlesme — bütçeye girdi sağlar)

### 3.4. Başarı Kriterleri

Sistem aşağıdaki sorulara "evet" cevabı verebildiğinde başarılı sayılır:

| Kriter | Hedef |
|--------|-------|
| Yeni site açılışı | < 30 dk (şablonla < 10 dk) |
| Yeni gider kalemi tanımlama | 100% UI, kod gerektirmez |
| Yeni dağıtım modeli (mevcut tip) | 100% UI |
| Yeni formül tanımı | 100% UI (DSL içinde) |
| Mid-year revizyon | Geçmiş bozulmadan ileri etkili |
| 1000 site × 200 BB × tahakkuk üretimi | < 1 saat (paralel) |
| Bütçe sapma raporu çekme | < 10 saniye |
| Mevzuat değişikliği (örnek: gecikme tavanı) | < 1 gün (parametre güncelleme, kod yok) |

---

## 4. Üst-Düzey Mimari

```
┌──────────────────────────────────────────────────────────────────┐
│                    BLAZOR + MUDBLAZOR FRONTEND                    │
│  [Bütçe UI] [Aidat UI] [Formül Editörü] [Rapor] [Onay] [Audit]   │
└─────────────────────────────┬────────────────────────────────────┘
                              │ HTTPS / SignalR
┌─────────────────────────────▼────────────────────────────────────┐
│              MODULAR MONOLITH (.NET 10 / ASP.NET Core)            │
│                                                                     │
│  ┌──────────────────── MALİ DOMAIN ────────────────────────────┐ │
│  │  Module.Butce    Module.Aidat    Module.Formul (yeni)      │ │
│  │  Module.Cari     Module.Tahsilat Module.Odeme              │ │
│  └─────────────────────────┬──────────────────────────────────┘ │
│                            │ MediatR + Outbox                   │
│  ┌─────────────────────────▼──────────────────────────────────┐ │
│  │              PLATFORM SERVİSLERİ                            │ │
│  │  Audit  Onay  Bildirim  Dosya  Identity  Tenant  Rapor    │ │
│  └────────────────────────────────────────────────────────────┘ │
└────────────────────────────┬───────────────────────────────────────┘
                             │ EF Core (write) + Dapper (read)
┌────────────────────────────▼───────────────────────────────────────┐
│   POSTGRESQL — Schema-per-module                                    │
│   aidat.* | butce.* | formul.* | sablon.* | onay.* | audit.* | ... │
│   Tüm tablolar: TenantId (Guid) + ShortCode (8 char)                │
└────────────────────────────────────────────────────────────────────┘
                             │
┌────────────────────────────▼───────────────────────────────────────┐
│  ARKA PLAN (Hangfire)                                               │
│  TahakkukUreticisi | GecikmeHesapci | OutboxDispatcher | RaporOzet │
│  KVKKAnonymization | DataRetentionCleaner | InvalidationProcessor  │
└────────────────────────────────────────────────────────────────────┘
```

**Mimari prensipler (özet):**
- **Modüler monolit** (mikroservisler hedef değil)
- **Şema-per-modül** (cross-schema FK yasak, logical reference + app-level integrity)
- **Tüm tablolarda `TenantId`** (EF global query filter ile otomatik)
- **ShortCode dışa görünüm, Guid içsel ID** (export, email, SMS'lerde Guid yok)
- **Outbox pattern** (modüller arası iletişim için)
- **Forward-only versioning** (geçmiş kayıt değiştirilmez)
- **CQRS-light** (write için EF, read için Dapper)

---

# BÖLÜM II — İŞ ANALİSTİ KATMANI

## 5. Domain Sözlüğü

| Terim | Tanım |
|-------|-------|
| **Tenant** | Yönetim şirketi (örnek: "ABC Site Yönetim AŞ"). Birden çok Company barındırır. |
| **Company** | Hukuki tüzel kişilik. Her yerleşke (site, AVM, marina) ayrı bir Company'dir. |
| **Yerleşke** | Fiziki yerleşim. Bir Company tek bir yerleşke içerir (birebir eşleşme). |
| **Blok** | Yerleşke içindeki yapı (Apartman A, B Blok, vs.) |
| **Bağımsız Bölüm (BB)** | KMK'ya göre ayrı tapu kütüğüne kayıtlı birim (daire, dükkan, ofis, villa, vs.) |
| **Yapı Bileşeni** | Ortak alan veya tesis (havuz, asansör grubu, sosyal tesis, otopark) |
| **Gider Kalemi** | Bir gider tipi (Asansör Bakım, Elektrik, Güvenlik) |
| **Gider Grubu** | Gider kalemlerinin mantıksal toplamı (Asansör Giderleri = Bakım + Onarım + Sigorta) |
| **Gider Paylaşım Grubu** | Gider kaleminin nasıl/kime dağıtılacağını belirleyen ayar (bir Dağıtım Modeli + bir Katılım kümesi) |
| **Dağıtım Modeli** | Matematiksel paylaştırma yöntemi (m², arsa payı, eşit, oda sayısı, özel formül) |
| **Katılım Grubu** | BB'lerin gidere katılım kümesi (havuz kullanıcıları, ticari alanlar, vs.) |
| **Kapsam Kuralı** | Bir Gider Paylaşım Grubunun bir Katılım Grubuna "Dahil" veya "Hariç" bağlanması |
| **Muafiyet Kuralı** | Belirli bir BB veya BB tipinin spesifik bir giderden muaf tutulması |
| **Tahakkuk** | Bir gider için belirli bir döneme ait, tüm BB'lere dağıtılmadan önce hesaplanan toplam |
| **Tahakkuk Detayı** | Bir tahakkuğun bir BB'ye düşen payı (vade tarihi ile birlikte) |
| **Tahsilat** | Bir BB'nin tahakkuk detayına karşılık yaptığı ödeme |
| **Borç Durumu** | Bir BB'nin tahakkuk - tahsilat farkı + işlenmiş gecikme |
| **Gerçekleşme** | Gerçek dünyada oluşan harcama (faturalanmış) tutarı |
| **Bütçe Versiyonu** | Bütçenin bir zaman aralığında geçerli olan kar/zarar planı |
| **Bütçe Kalemi Versiyonu** | Bir bütçe kaleminin spesifik bir bütçe versiyonu altındaki değeri |
| **Formül Şablonu** | Yeniden kullanılabilir kullanıcı tanımlı hesaplama mantığı |
| **Formül Versiyonu** | Bir formül şablonunun belirli bir geçerlilik aralığında olan hali |
| **Site Şablonu** | Bir yerleşke tipinin (konut sitesi, AVM, vs.) varsayılan kurguları |
| **Yedek Akçe** | KMK m.20 gereği zorunlu rezerv fon |
| **Dönem** | Mali periyot (genellikle bir ay). `DonemKaydi` ile temsil edilir. |
| **Senaryo** | What-if simülasyonu (resmi değil) |
| **ShortCode** | 8-karakter alfanumerik, sistem üretimi unique kod (export ve UI'da gösterilen) |
| **EnterSystemContext** | SuperAdmin'in tenant izolasyonu bypass'ı (explicit, audited) |

---

## 6. Paydaşlar, Roller ve Yetki Matrisi

### 6.1. Roller

| Rol | Açıklama |
|-----|----------|
| **SystemAdmin** | Anthropic/MultiTenant operatörü. Global katalog, mevzuat sabitleri. |
| **TenantAdmin** | Yönetim şirketi yöneticisi. Tenant katalog, çoklu Company yönetimi. |
| **CompanyAdmin (Yerleşke Yöneticisi)** | Tek yerleşkenin yöneticisi. Bütçe oluşturma, formül tanımı. |
| **Onaylayici** | Belirli işlemleri onaylayan rol (genellikle yönetim kurulu üyesi). |
| **Muhasebeci** | Tahakkuk, tahsilat, mutabakat işlemleri. |
| **Denetci** | Sadece okuma + audit log erişimi. |
| **Sakin (BB Sahibi/Kiracı)** | Kendi BB'sinin tahakkuk, tahsilat, borç durumunu görür. |
| **RaporKullanicisi** | Operasyonel raporlama. |

### 6.2. Yetki Matrisi

| İşlem | SistemAdm | TenantAdm | CompanyAdm | Onaylayıcı | Muhasebeci | Denetçi | Sakin |
|-------|-----------|-----------|------------|------------|-----------|---------|-------|
| Global katalog yönet | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Tenant katalog yönet | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Yerleşke açma | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Bütçe taslağı oluştur | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Bütçe yayınla | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Bütçe revize et (taslak) | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Bütçe revizyon yayınla | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Gider kalemi tanımla | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Formül oluştur | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Formül onayla & yayınla | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Tahakkuk üret | ✅ | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ |
| Tahakkuk iptal | ✅ | ❌ | ❌ | ✅ | ✅ (gerekçe) | ❌ | ❌ |
| Manuel override | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Tahsilat kaydet | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Tahsilat sil/düzelt | ✅ | ❌ | ❌ | ✅ | ✅ (gerekçe + audit) | ❌ | ❌ |
| Yedek akçe çekiş | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Audit log görüntüle | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Kendi BB borç durumu | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Diğer BB tahakkukları | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Portföy raporu | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ |
| Mevzuat kuralı değiştir | ❌ (kilit) | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

---

## 7. Fonksiyonel Gereksinimler

Her FR şu formattadır: **ID — Başlık — Açıklama — Ana kabul kriteri.** Detaylı kabul kriterleri Bölüm 21'de.

### FR-01 — Yerleşke Kurulumu (Şablonlu)
Kullanıcı bir Site Şablonu seçerek dakikalar içinde yeni yerleşke açabilir. Şablon: gider ağacı, dağıtım kuralları, katılım grupları varsayılan olarak gelir; istisnalar üzerine eklenir.
**Kabul:** Boş bir tenant'ta, "Sosyal Tesisli Büyük Site" şablonuyla yerleşke 10 dk içinde kurulabilir; ilk bütçe taslağı oluşturulabilir.

### FR-02 — Bağımsız Bölüm Yönetimi
BB'ler manual veya import ile eklenir. Standart öznitelikler: brüt m², net m², arsa payı, oda sayısı, kullanım tipi. **Özel öznitelikler EAV ile eklenebilir** (FR-03).
**Kabul:** Excel'den 200 BB'lik import < 30 sn'de hatasız tamamlanır; hatalı satırlar rapor olarak indirilebilir.

### FR-03 — Özel BB Öznitelikleri (EAV)
SystemAdmin/TenantAdmin/CompanyAdmin yeni öznitelik tanımı yapabilir (örnek: "Balkonlu mu", "Asansör katı mı", "Deniz manzaralı mı"). Tanımlanan öznitelik dağıtım kurallarında ve formüllerde kullanılabilir.
**Kabul:** Yeni bir bool öznitelik tanımlanıp 200 BB'ye atanabilir; ardından "Balkonlu BB'lere %30 daha az dağıt" kuralı UI'dan tanımlanabilir.

### FR-04 — Gider Ağacı Yönetimi
Hiyerarşik gider kalemleri (3-4 seviye optimal, max 6 önerilir). Closure table ile saklanır.
**Kabul:** "Sosyal Tesis Giderleri / Havuz / Kimyasal" 3 seviye hiyerarşi oluşturulup tüm alt-ağacı listelenebilir (< 100 ms).

### FR-05 — Gider Paylaşım Grubu
Bir gider kalemi bir paylaşım grubuna bağlanır; grup bir dağıtım modeli ve bir veya birden fazla katılım grubuna sahiptir.
**Kabul:** "Havuz İşletmesi" grubu (eşit dağıtım + "Havuz Kullanıcıları" katılım grubu) tanımlanıp bir gider kalemi atanabilir.

### FR-06 — Katılım Grubu Tanımı
BB'lerin "şu gidere katılır" mantıksal gruplandırması. Bir BB birden fazla gruba ait olabilir. N-N ilişki + tarih aralığı.
**Kabul:** "Havuz Kullanıcıları" grubuna 50 BB atanıp 25 Mayıs sonrası 5 BB ayrılabilir.

### FR-07 — Kapsam ve Muafiyet Kuralları
Bir paylaşım grubunun bir katılım grubuna "Dahil" veya "Hariç" olarak bağlanması. Belirli BB veya BB tipleri için ayrı muafiyet tanımı.
**Kabul:** "Asansör Bakım" grubu için "Tüm konutlar dahil + Zemin kat dükkanları hariç" kuralı UI'dan tanımlanabilir.

### FR-08 — Kural Çakışma Çözümü
Aynı BB için birden fazla kural varsa, deterministik öncelik algoritması (Bölüm 8.3) ile sonuç belirlenir. Kullanıcı UI'da "Bu BB neden hariç kaldı?" sorusunun cevabını görebilir.
**Kabul:** Çakışan iki kural senaryosunda sistem her zaman aynı sonucu üretir; sebep açıklaması UI'da gösterilir.

### FR-09 — Standart Dağıtım Modelleri
Önceden tanımlı modeller: m², arsa payı, eşit, oda sayısı, BB tipine göre, katsayılı.
**Kabul:** Her standart model için aynı gider 100 BB arası dağıtılır; toplam ± yuvarlama artığı dışında girilen miktarla eşittir.

### FR-10 — Kullanıcı Tanımlı Formül
DSL ile yazılan, parametre alan, sandbox'ta çalışan formül. Detay: Bölüm 15.
**Kabul:** Yönetici, geliştirici çağırmadan "(KisiSayisi × GunSayisi × BirimFiyat) × SezonKatsayisi" formülünü tanımlar, doğrular, önizler, onaylar, yayınlar.

### FR-11 — Formül Versiyonlama
Bir formülün birden fazla versiyonu olur; her versiyon geçerlilik tarih aralığı taşır. Eski versiyonla hesaplanmış tahakkuklar geçmişte korunur.
**Kabul:** Formül V1 ile Ocak-Haziran hesabı, V2 ile Temmuz-Aralık hesabı yapılır; yıl sonu raporu ikisini birleşik gösterir.

### FR-12 — Hesaplama Bağımlılık Grafiği
Bir formül başka formülü/kalemi referans edebilir. Sistem dairesel bağımlılığı tespit eder ve reddeder. Tek girdi değişiminde sadece etkilenenler yeniden hesaplanır.
**Kabul:** A formülü B'yi, B de A'yı referans ederse sistem kaydetmez ve gerekçe gösterir. C'nin girdisi değişince yalnız C ve C'ye bağlı olanlar invalide olur.

### FR-13 — Bütçe Oluşturma ve Versiyonlama
Bütçe bir yerleşke ve dönem (genellikle 12 ay) için oluşturulur. İlk taslak, onay, yayın akışı. Yayınlanan bütçe versiyonlanır.
**Kabul:** 2026 yılı bütçesi taslak olarak oluşturulur, 30 kalem eklenir, onaya gönderilir, onaylanır, yayınlanır.

### FR-14 — Bütçe Revizyon (Forward-Only)
Yayınlanmış bütçeye dönem ortasında revizyon. Geçmiş tahakkuklar değiştirilmez; yeni versiyon belirli bir tarihten itibaren etki eder.
**Kabul:** Ocak-Temmuz tahakkukları aynen kalır; Ağustos-Aralık için yeni tutar/formülle yeni tahakkuklar üretilir. Yıl sonu raporu iki versiyonu birleştirir.

### FR-15 — Ödeme Planı / Tahakkuk Zamanlaması
Her gider kaleminin ne zaman tahakkuk edileceği parametrik: aylık-eşit, yıllık-tek-seferde, fatura-bazlı, sezonluk, taksitli.
**Kabul:** "Asansör Periyodik Kontrol" yıllık 15.000 TL kalemi, kullanıcı seçimi ile (a) 12 eşit takside bölünür, (b) sadece Ocak'ta tek seferde tahakkuk eder, (c) faturanın geldiği ayda tahakkuk eder şeklinde kurgulanabilir.

### FR-16 — Tahakkuk Üretimi
Belirli bir dönem için ilgili bütçe versiyonundan tahakkukların üretilmesi. Bağımlılık grafiği sırasında topolojik sıralama. Idempotent (aynı dönem 2 kez çalıştırılırsa aynı sonuç).
**Kabul:** Şubat 2026 tahakkukları üretildikten sonra aynı işlem tekrarlanırsa veri değişmez (rerun-safe).

### FR-17 — Tahsilat Kaydetme
Bir BB'nin bir tahakkuk detayına karşılık yaptığı ödemenin kaydı. Tam, kısmi, fazla ödeme.
**Kabul:** Bir tahakkuk detayına 3 ayrı kısmi ödeme yapılabilir; toplam borç buna göre güncellenir.

### FR-18 — Kısmi Ödeme Tahsis Sırası
Kısmi ödeme önce hangi borç kalemine, hangi sırayla yansıtılır? Politika seçilebilir: önce-anapara, önce-faiz, en-eski-vade, vs.
**Kabul:** "Önce gecikme cezası" politikası seçilince 600 TL ödeme bir BB'nin 1000 TL borcuna ve 150 TL gecikmesine: önce 150 TL gecikme silinir, kalan 450 TL anapara'dan düşülür.

### FR-19 — Gecikme Cezası Hesaplama
Vade geçen tahakkuk detayları için KMK m.20 uyumlu (aylık %5 tavan), basit veya bileşik faiz. Hangfire job ile günlük çalışır.
**Kabul:** Vadesi 30 gün geçmiş 1000 TL borcun gecikme cezası aylık %3 oranıyla 30 TL hesaplanır; tavan kontrol ile uyumlu.

### FR-20 — Manuel Tahakkuk İptal / Düzeltme
Yanlış tahakkuğun iptali veya düzeltilmesi. Audit zorunlu, onay gerekir.
**Kabul:** Yanlış BB'ye çıkmış tahakkuk iptal edilince ters kayıt oluşur; audit kaydı "kim, ne zaman, neden" bilgisini taşır.

### FR-21 — Manuel Override (Kilit)
Sistem hesabını ezerek yöneticinin manuel tutar girmesi. Onay zorunlu; "OverrideSebebi" mandatory.
**Kabul:** Sistem 500 TL hesaplarken yönetici "350 TL olsun (sözleşmeden indirim)" override yapar; bu kalem yeniden hesap çalıştığında değişmez.

### FR-22 — Gelir Yönetimi
Aidat-dışı gelirlerin (kira, reklam, faiz, ceza) kaydı ve raporu.
**Kabul:** Yıllık 12.000 TL'lik baz istasyonu kira sözleşmesi tanımlanır; aylık 1.000 TL otomatik gelir tahakkuku üretilir.

### FR-23 — Net İşletme Sonucu
`Toplam Gelir - Toplam Gider = Sonuç` raporu, BB başına ve aylık kırılım ile.
**Kabul:** Herhangi bir dönem için Net İşletme Sonucu < 5 sn içinde gösterilir; gelir ve gider kalemlerine drill-down imkanı vardır.

### FR-24 — Yedek Akçe Yönetimi
KMK m.20 zorunlu yedek akçe için ayrı muhasebe. Birikim oranı, çekiş onayı, karşılık hesabı.
**Kabul:** Aidatın %10'u otomatik yedek akçeye akar; çekiş için onay gerekir; "yedek akçe yeterlilik" raporu çekilebilir.

### FR-25 — Banka Mutabakatı
Banka ekstresinden otomatik eşleştirme + askıdaki kayıtlar.
**Kabul:** CSV ekstresinden 100 hareket import edilir; %80'i auto-match, %20'si manual eşleştirme UI'sında çözülür.

### FR-26 — Dönem Kapama
Belirli bir döneminin kapatılması; sonra retro işlem için özel onay gerekir.
**Kabul:** Kapatılmış bir döneme yeni tahakkuk eklenmek istenirse sistem uyarır ve "DonemAcmaOnayi" zorunlu kılar.

### FR-27 — Site Şablonu Kütüphanesi
Standart yapı tipleri için hazır kurgular: konut sitesi, karma kullanım, AVM, marina, vs.
**Kabul:** 5 farklı şablon mevcuttur; her şablon en az 20 gider kalemi + 5 katılım grubu + 3 dağıtım kuralı içerir.

### FR-28 — Veri Yönetişimi (Katalog Hiyerarşisi)
Global → Tenant → Yerleşke kademeli katalog. Standartlaştırma, auto-suggest, mapping ekranı.
**Kabul:** Yeni kullanıcı "Elek..." yazınca sistem "Elektrik - Genel" katalog önerir.

### FR-29 — Audit İzi
Tüm değişiklikler audit log'a yazılır. 5 yıl saklama. SuperAdmin EnterSystemContext de audit'lenir.
**Kabul:** Herhangi bir kayıt için "değişiklik geçmişi" UI'dan gösterilir; "kim, ne zaman, ne değişti, eski/yeni değer" bilgisi vardır.

### FR-30 — KVKK Anonimleştirme
Bir kullanıcının silinme talebi gelince PII anonimleştirilir; mali kayıtlar (UserId, audit) korunur.
**Kabul:** Anonimleştirilen kullanıcının email/ad bilgisi "deleted_xxx@anonymized.invalid" olur; geçmiş tahakkukları görülmeye devam eder ama kişi tespit edilemez.

### FR-31 — Çoklu Yerleşke (Tenant Migration) — Wave 9+
Bir yerleşkenin bir tenant'tan diğerine taşınması.
**Kabul:** Wave 9 sonrası tasarlanacak. Wave 0 outbox şemasında `CompanyMigratedTo` field rezerve edilmiş.

### FR-32 — Portföy Analitiği — Wave 6+
Benzer yerleşke kümeleri, geçmişten öğrenme, öneri motoru.
**Kabul:** Wave 6 sonrası. Faz 6'da rule-based, Faz 7'de istatistiksel, Faz 8+ ML.

---

## 8. İş Kuralları ve Hesaplama Politikaları

### 8.1. Yuvarlama Politikası

**Sorun:** 1000 TL'nin 7 BB'ye m² oranında dağıtımında 0.02 TL artık.

**Çözüm — UI'dan seçilebilir politika:**

| Politika | Davranış |
|----------|----------|
| **Largest Remainder Method (LRM)** | Artık en büyük ondalık kalan'a sahip BB'lere eklenir. (Varsayılan önerilir) |
| **Anchor BB** | Her bütçe kalemine bir "anchor BB" atanır; artık ona kalır. |
| **Yönetim Hesabı** | Artık yönetim alacak/borç hesabına kaydedilir. |
| **Sıralı** | İlk BB'den son BB'ye 1 kuruşluk artık dağıtılır. |

**Karar:** `Tenant` seviyesinde varsayılan, `Yerleşke` seviyesinde override.

### 8.2. Sıfır-Koruma

- Bir BB'nin m²'si 0 ise m² dağıtımında dahil edilmez (otomatik hariç, audit kaydı).
- Toplam katsayı 0'a yakın ise (< 0.0001) hesap reddedilir, UI'da hata gösterilir.
- Negatif katsayı/indirim mümkün ama toplam negatif çıkamaz (sistem reddeder).

### 8.3. Kural Çakışma Çözümü (Deterministik Algoritma)

Kurallar şu sıraya göre değerlendirilir:

1. **Kaynak önceliği:**
   - Mevzuat (kilit, değişmez)
   - Yönetim Planı (Yerleşke'nin tüzüğü)
   - Karar Defteri (yönetim kurulu kararları)
   - Sistem varsayılan

2. **Aynı kaynakta — spesifiklik puanı:**
   ```
   Puan = (BB referansı ? 100 : 0) + 
          (BB tipi referansı ? 50 : 0) + 
          (Blok referansı ? 25 : 0) + 
          (Yapı bileşeni referansı ? 10 : 0) + 
          (Tarih spesifikliği ? 5 : 0)
   ```

3. **Aynı puan — tarih önceliği:** Daha yeni yürürlüğe giren kural baskındır.

4. **Aynı tarih — manuel salience:** Kullanıcı belirlediği "öncelik puanı" (varsayılan 50, 0-100 aralığında).

5. **Manuel kilit:** ManuelKilit varsa, otomatik hesap dokunmaz.

**Determinizm garantisi:** Aynı veri + aynı tarih → her zaman aynı sonuç. Test edilmeli.

### 8.4. Vade ve Gecikme

- **Vade:** `VadeKurali.GunSayisi` veya `SabitGun` ile her ay vade tarihi hesaplanır.
- **Gecikme:** Vadeden sonraki gün başlar.
- **Gecikme oranı:** Aylık % veya günlük % (parametrik). KMK m.20 gereği aylık %5 tavan.
- **Tavan kontrolü:** Sistem-genelinde "MevzuatTavanlari" kataloğu (kilit). UI değiştiremez, sadece SystemAdmin günceller.
- **Bileşik mi basit mi:** `GecikmeKurali.BilesikMi` flag; bileşikse aylık compound.
- **Geri-tarihli ödeme:** Dekont tarihine göre değil, sistem giriş tarihine göre işlem. Geri-tarihli düzeltme manuel düzeltme akışından geçer.

### 8.5. Kısmi Ödeme Tahsis Politikası

TBK m.101 esas (anaparaya değil önce faize). Ancak UI'dan değiştirilebilir:

| Politika | Davranış |
|----------|----------|
| **En eski vade + Faiz öncelikli** (varsayılan, TBK uyumlu) | En eski tahakkuk → önce gecikme cezası → sonra anapara |
| **En eski vade + Anapara öncelikli** | En eski tahakkuk → önce anapara → sonra gecikme |
| **FIFO + Anapara** | Tahakkuk tarihine göre eski → anapara → gecikme |
| **Manuel atama** | Muhasebeci her ödemeyi manuel hangi tahakkuğa yazacağına karar verir |

### 8.6. Dönem Kapama Kuralları

| Durum | Davranış |
|-------|----------|
| Dönem `KapandiMi = true` | Yeni tahakkuk, gerçekleşme, düzeltme reddedilir. |
| Geri açma | Yalnızca `Onaylayici` + sebep + audit. |
| Retro işlem (kapatılmış döneme yeni gerçekleşme) | Özel "GeçGelenFatura" akışı; ayrı entity. Geçmiş tahakkuk değişmez, ayrı düzeltme kaydı yapılır. |
| Kapama → bütçe versiyonu | Dönem kapatılınca o döneme ait `ButceVersiyonu` da kilitlenir. |

### 8.7. Yedek Akçe Kuralları (KMK m.20)

- Zorunlu yedek akçe oranı: KMK gereği genellikle aidatın belli yüzdesi (parametrik, ama 0 olamaz).
- Her tahsilattan otomatik bir oran yedek akçeye akar.
- Yedek akçe çekiş: Onaylayıcı + Karar Defteri kayıt zorunlu.
- "Yedek Akçe Yeterlilik" metriği: `Yedek Akçe / (Aylık Ort. Gider × 3)` (3 ay önerilen).

---

## 9. Operasyonel Yaşam Döngüleri

### 9.1. Yerleşke Açılışı

```
1. CompanyAdmin "Yeni Yerleşke" sihirbazını başlatır
2. Site Şablonu seçer (örn: "Sosyal Tesisli Büyük Site")
3. BB envanterini Excel'den import eder (200 BB)
   3a. Sistem hatalı satırları raporlar
   3b. Kullanıcı düzeltir, tekrar import
4. Şablondan gelen gider ağacını gözden geçirir, istisnaları işaretler
5. Katılım gruplarını oluşturur, BB'leri atar
6. İlk bütçe taslağını oluşturur
7. Onaya gönderir
8. Onaylanır → Yayınlanır
9. İlk tahakkuk üretilir
```

### 9.2. Yeni Formül Yaşam Döngüsü

```
1. Yönetici "Yeni Formül" başlatır
2. Şablondan başlar veya boş başlar
3. Parametre tanımlar (KisiSayisi, BirimFiyat, vs.)
4. DSL ile ifadeyi yazar
5. "Validate" → sistem syntax + bağımlılık + güvenlik kontrolü yapar
6. "Önizle" → test verisinde sonucu görür
7. Onaya gönderir (Onaylayici rol)
8. Onay → Versiyon 1.0 olarak yayınlanır, GecerlilikTarihi belirlenir
9. İlgili tahakkuklar bu formülle üretilir
```

### 9.3. Bütçe Revizyon Yaşam Döngüsü

```
1. Mid-year: Yönetici "Bütçe Revize Et" başlatır
2. Sebep yazar (örnek: "Elektrik fiyatları %40 arttı")
3. Etkilenecek kalemleri seçer (sadece elektrik, sadece güvenlik, vs.)
4. Yeni tutar/formülü girer
5. "Önizleme" → kalan dönemdeki tahakkukların yeni hali
6. Onaya gönderir
7. Onay → Yeni bütçe versiyonu yayınlanır (GecerlilikTarihi belli)
8. Geçmiş tahakkuklar değişmez
9. Yeni dönem tahakkukları yeni versiyonla üretilir
```

### 9.4. Tahsilat Yaşam Döngüsü

```
1. Banka ekstresi import edilir
2. Sistem auto-match yapar (referans no, BB kodu, tutar)
3. Eşleşmeyenler "Askıdaki Tahsilatlar" sayfasında listelenir
4. Muhasebeci manuel eşler
5. Eşleştirme sonrası tahsilat kayıtları oluşur
6. Tahsis politikasına göre borç düşülür
7. Eğer fazla ödeme → "Avans" olarak kaydedilir, sonraki tahakkuğa mahsup
```

### 9.5. Manuel Düzeltme Yaşam Döngüsü

```
1. Hata tespit edilir (örnek: yanlış BB'ye tahakkuk)
2. Muhasebeci "Düzeltme Talebi" başlatır
3. Sebep + gerekçe yazar
4. "Hesabı önizle" → düzeltmenin etkisini görür
5. Onaya gönderir
6. Onay → eski tahakkuk iptal + yeni tahakkuk + audit kaydı
7. Karşı kayıt (ters kayıt) otomatik oluşur
```

---

# BÖLÜM III — MİMAR KATMANI

## 10. Mimari Karar Kayıtları (ADR)

Her ADR formatı: **Karar — Bağlam — Seçim — Sonuçlar — Alternatifler.**

### ADR-01 — Modüler Monolit (Mikroservis Değil)
- **Bağlam:** 1000+ tenant ölçeği. Geliştirme ekibi sınırlı.
- **Karar:** Modüler monolit. Tek deployment, tek DB, modül sınırları kod düzeyinde.
- **Sonuç:** Deployment basit, transactional consistency kolay. Trade-off: bağımsız scale yok.
- **Reddedilenler:** Mikroservis (ekipler için erken), serverless (state yönetimi zor).

### ADR-02 — PostgreSQL + Schema-per-Module
- **Karar:** Tek DB, modül başına ayrı schema (`aidat.*`, `butce.*`, vs.).
- **Cross-schema FK:** Yasak. Logical reference + app-level integrity.

### ADR-03 — TenantId Mandatory, Global Query Filter
- **Karar:** Tüm tablolarda `TenantId Guid` zorunlu. EF Core global query filter otomatik filtreler.
- **SuperAdmin bypass:** Sadece `EnterSystemContext()` explicit scope ile.

### ADR-04 — Guid Internal, ShortCode External
- **Karar:** PK/FK/audit Guid. Her entity ayrıca 8-char ShortCode.
- **Dışa görünür:** Sadece ShortCode (URL, export, email, SMS).

### ADR-05 — Forward-Only Versioning
- **Karar:** Bütçe, formül, dağıtım kuralı, gecikme oranı versiyonlu. Geçmiş asla değiştirilmez.
- **Audit:** Her versiyon değişikliği audit'lenir.

### ADR-06 — Outbox Pattern (Modüller arası iletişim)
- **Karar:** Business modüllerden business modüllere doğrudan çağrı yok. MediatR `INotification` + Transactional Outbox.
- **Idempotency:** Inbox dedup tablosu.

### ADR-07 — Closure Table (Hiyerarşi)
- **Karar:** GiderKalemiHiyerarsi closure table. Self-reference değil.
- **Avantaj:** O(1) ancestor query, multi-level fast.

### ADR-08 — EAV-Light for Custom BB Attributes
- **Karar:** `OzellikTanimi` + `BagimsizBolumOzellik` tabloları. EAV ama tip-güvenli (sayısal, metin, bool, seçim ayrı kolonlar).

### ADR-09 — Formül DSL Strategy: AST-based JSON + ANTLR Parser
- **Karar:** Formüller AST JSON olarak saklanır; ANTLR grammar parse eder; özel sandbox execute eder.
- **Reddedilenler:** Roslyn (güvenlik), NCalc (sınırlı), Excel-tarzı string (parse pahalı).

### ADR-10 — DAG-Based Computation Dependency Graph
- **Karar:** Formüller arası bağımlılık DAG ile yönetilir. Cycle detection (Tarjan algorithm) write-time'da.
- **Yeniden hesap:** Topological sort + invalidation kuyruğu.

### ADR-11 — Hangfire for Background Jobs
- **Karar:** Tahakkuk üretimi, gecikme hesabı, outbox dispatch, KVKK anonimleştirme tümü Hangfire.

### ADR-12 — Range Partition for Large Tables
- **Karar:** `TahakkukDetayi`, `Tahsilat`, `Gerceklesme` tabloları yıl bazında range partition. PostgreSQL native.
- **Eşik:** Tablo 50M satıra ulaşmadan önce partition aktive edilir.

### ADR-13 — Read-Optimized View Layer
- **Karar:** Operasyonel sorgular EF Core. Raporlama materialized view + Dapper.
- **Refresh:** Hangfire job ile gece refresh; gün içinde event-driven incremental update.

### ADR-14 — Hybrid Envelope Response Pattern
- **Karar:** Tüm API yanıtları RFC 7807 Problem Details + success/data wrapper.

### ADR-15 — Resource Limits on Formula Execution
- **Karar:** Her formül çalıştırma: max 100ms CPU, max 10MB memory, max 50 fonksiyon çağrısı, max recursion depth 10.
- **Aşılırsa:** Hata + audit.

---

## 11. Modül Sınırları ve Etkileşim

### 11.1. Mali Domain Modülleri (Bu SDD kapsamında)

| Modül | Schema | Sorumluluğu |
|-------|--------|------------|
| **Module.Butce** | `butce.*` | Bütçe oluşturma, versiyonlama, revizyon, gelir kalemleri, net işletme sonucu |
| **Module.Aidat** | `aidat.*` | BB, gider yönetimi, dağıtım, tahakkuk üretimi, vade, gecikme |
| **Module.Formul** (yeni) | `formul.*` | Formül DSL, versiyonlama, sandbox, bağımlılık grafiği |
| **Module.Tahsilat** | `tahsilat.*` | Tahsilat kaydı, kısmi ödeme tahsisi, banka mutabakatı |
| **Module.Cari** | `cari.*` | BB başına cari hesap, borç durumu, alacak |

### 11.2. Etkileşim Diyagramı (Domain Event akışı)

```
[Module.Aidat]                    [Module.Butce]
     │                                  │
     │ <─── ButceYayinlandi ──────────  │
     │                                  │
     │ ──── TahakkukUretildi ──────►    │
     │                                  │
     │                              [Module.Bildirim]
     │ ──── TahakkukUretildi ─────► (SMS/Email)
     │
     │ ──── TahakkukUretildi ────────► [Module.Audit]
     │
     ▼
[Module.Tahsilat]
     │
     │ ──── BorcOdendi ──────────► [Module.Bildirim]
     │ ──── BorcOdendi ──────────► [Module.Cari]
     │ ──── BorcOdendi ──────────► [Module.Audit]
```

### 11.3. Platform Servisleri (Dependency)

| Platform Servisi | Bu domain'in kullanımı |
|------------------|------------------------|
| `IAuditService` | Tüm değişiklikler audit'lenir |
| `INotificationService` | Tahakkuk, ödeme, gecikme bildirimi |
| `IApprovalService` (Module.Onay) | Bütçe yayını, formül onayı, manuel override |
| `IFileService` | Bütçe PDF, rapor export, fatura PDF |
| `IExportService<T>` | Excel/CSV/PDF export |
| `ITenantContext` | Tenant scope yönetimi |
| `IIdentityService` | Kullanıcı, BB sahip eşleştirme |
| `IReportService` | Konsolide raporlar (Module.Rapor) |

---

## 12. NFR (Non-Functional Requirements)

### 12.1. Performans

| Operasyon | p50 | p95 | p99 |
|-----------|-----|-----|-----|
| Bütçe listele (200 kalem) | 200ms | 500ms | 1s |
| Tek tahakkuk üretimi (200 BB, 30 kalem) | 5s | 15s | 30s |
| Toplu tahakkuk (1 tenant, 50 yerleşke paralel) | — | < 60dk | — |
| Portföy raporu (100 yerleşke) | 3s | 10s | 20s |
| Formül validation | 50ms | 100ms | 200ms |
| Formül execute (tek BB) | 10ms | 100ms | 200ms |
| Sakin görüş "Borç Durumum" | 100ms | 300ms | 500ms |
| Banka ekstresi import (100 hareket) | 5s | 15s | 30s |

### 12.2. Ölçek (Wave 5 hedef)

- 5,000 tenant
- 50,000 yerleşke
- 10,000,000 bağımsız bölüm
- 100,000,000+ tahakkuk satırı (5 yıllık tarihsel)
- 1,000 eşzamanlı aktif kullanıcı

### 12.3. Saklama ve Backup

| Veri | Aktif Saklama | Arşiv | Toplam |
|------|---------------|-------|--------|
| Mali (Tahakkuk, Tahsilat, Gerçekleşme) | 10 yıl | + | TTK 82 |
| Karar Defteri, Toplantı | süresiz | — | KMK |
| Bordro, SGK | 10 yıl | — | — |
| KVKK Onay | Onay + 3y | — | — |
| Audit | 5 yıl | — | — |
| Outbox dispatch | Dispatch + 7 gün | — | hard delete |
| Application log | 30 gün DB | 1 yıl compressed file | — |
| Backup günlük | 30 gün | — | RPO 24sa |
| Backup haftalık | 12 hafta | — | — |
| Backup aylık | 12 ay | — | — |
| Backup yıllık snapshot | 7 yıl | — | — |
| RPO | — | — | 24 saat |
| RTO | — | — | 4 saat |

### 12.4. Güvenlik

- TLS 1.3 (HTTPS only)
- Şifreler: Argon2id hash
- Session: JWT refresh + cookie auth (httpOnly, sameSite)
- Audit immutable (append-only, başka bir DB user write yetkisinden)
- KVKK uyumu: anonimleştirme + retention policy
- Penetration test: Wave 5 öncesi zorunlu
- OWASP Top 10 uyum check

### 12.5. Erişilebilirlik

- WCAG 2.1 AA seviye
- Klavye navigasyonu
- Ekran okuyucu desteği (NVDA, JAWS test)
- Yüksek kontrast tema
- Çoklu dil: tr-TR (varsayılan), en-US (Wave 2 sonrası)

### 12.6. Çok Kiracılı İzolasyon

- Tenant verisi başka tenant'tan görülemez (kanıtlanmalı, test edilmeli).
- TenantId filtre bypass sadece `EnterSystemContext` + audit.
- Cross-tenant data leak = P0 incident.

---

## 13. Güvenlik, Yetki ve Uyum

### 13.1. KVKK Uyumu

- **PII alanları:** email, ad, soyad, telefon, TC kimlik no, fotoğraf, adres.
- **Anonimleştirme mekanizması:** PII alanları `null` veya `"deleted_<uuid>@anonymized.invalid"`; flag `IsAnonymized=true`; `AnonymizedAtUtc` damgalı.
- **Mali immutability ile uyum:** UserId korunur (anonim hesaba point eder); mali kayıtlar (tahakkuk, tahsilat) değişmez; sadece kullanıcı PII anonimleşir.
- **Backup restore:** Restore sonrası `anonymization_ledger` tablosundaki kayıtlar Hangfire job ile yeniden uygulanır.
- **Ledger temizliği:** En eski backup (7 yıl) yok edildiğinde ilgili ledger kaydı silinir.

### 13.2. KMK Uyumu

- KMK m.20 (gecikme tazminatı tavanı %5/ay): Sistem-genelinde "MevzuatTavanlari" tablosu; UI değiştiremez.
- KMK m.20 (yedek akçe): Zorunlu, oran parametrik ama 0 olamaz.
- KMK m.32 (yönetim planı önceliği): Yönetim Planı kuralları sistem varsayılanı baskındır.

### 13.3. TBK Uyumu

- TBK m.101 (kısmi ödeme tahsis sırası): Default önce-faiz-sonra-anapara; UI'dan değiştirilebilir ama default uyumlu.
- TBK m.88 (yasal faiz): Mevzuat sabiti; manuel override ile değiştirilebilir ama audit'li.

### 13.4. TTK Uyumu

- TTK m.82 (defter saklama): Mali kayıtlar 10 yıl saklama zorunlu.
- TTK m.66-70 (defter tutma): Module.Muhasebe + Module.eDefter ile entegrasyon (Wave 7+).

---

# BÖLÜM IV — GELİŞTİRİCİ KATMANI

## 14. Domain Modeli ve Entity Şeması

### 14.1. Çekirdek Entity'ler (Şema-bazlı gruplama)

#### Schema: `aidat`

```
BagimsizBolum
- BagimsizBolumId      uuid PK
- TenantId             uuid (NOT NULL, global filter)
- CompanyId            uuid FK
- ShortCode            varchar(8) UNIQUE
- BlokId               uuid FK NULL
- BagimsizBolumTipiId  uuid FK
- Kod                  varchar(50)
- KapiNo               varchar(20)
- BrutMetrekare        decimal(10,2)
- NetMetrekare         decimal(10,2)
- ArsaPayi             decimal(10,4)  // 1234/45678 gibi pay
- OdaSayisi            integer
- KullanimTipi         varchar(50)
- AktifMi              boolean
- OlusturmaTarihi      timestamptz
- GuncellemeTarihi     timestamptz
- (audit fields)

OzellikTanimi  // EAV
- OzellikTanimiId      uuid PK
- TenantId             uuid (NULL = global)
- CompanyId            uuid (NULL = tenant-wide)
- Kod                  varchar(50)
- Ad                   varchar(200)
- VeriTipi             enum(sayi, metin, bool, secim)
- SecimDegerleri       jsonb (VeriTipi=secim için)
- DagitimaUygunMu      boolean
- AktifMi              boolean

BagimsizBolumOzellik  // EAV value
- Id                   uuid PK
- TenantId             uuid
- BagimsizBolumId      uuid FK
- OzellikTanimiId      uuid FK
- DegerSayisal         decimal(18,4) NULL
- DegerMetin           varchar(500) NULL
- DegerBoolean         boolean NULL
- DegerSecim           varchar(100) NULL
- GecerlilikBaslangic  date
- GecerlilikBitis      date NULL

KatilimGrubu
- KatilimGrubuId       uuid PK
- TenantId             uuid
- CompanyId            uuid
- ShortCode            varchar(8)
- Kod                  varchar(50)
- Ad                   varchar(200)
- AktifMi              boolean

BagimsizBolumKatilimGrubu
- Id                   uuid PK
- TenantId             uuid
- BagimsizBolumId      uuid FK
- KatilimGrubuId       uuid FK
- GecerlilikBaslangic  date
- GecerlilikBitis      date NULL

KapsamKurali
- KapsamKuraliId       uuid PK
- TenantId             uuid
- GiderPaylasimGrubuId uuid FK
- KatilimGrubuId       uuid FK
- KuralTipi            enum(Dahil, Haric)
- OncelikSirasi        integer
- KuralKaynagi         enum(Mevzuat, YonetimPlani, KararDefteri, Sistem)
- GecerlilikBaslangic  date
- GecerlilikBitis      date NULL

MuafiyetKurali
- MuafiyetKuraliId     uuid PK
- TenantId             uuid
- GiderPaylasimGrubuId uuid FK
- BagimsizBolumId      uuid FK NULL
- BagimsizBolumTipiId  uuid FK NULL
- Aciklama             text
- KuralKaynagi         enum
- GecerlilikBaslangic  date
- GecerlilikBitis      date NULL

GiderGrubu
- GiderGrubuId         uuid PK
- TenantId             uuid
- ShortCode            varchar(8)
- Kod, Ad, Aciklama

GiderKalemi
- GiderKalemiId        uuid PK
- TenantId             uuid
- CompanyId            uuid
- GiderGrubuId         uuid FK
- ShortCode            varchar(8)
- GlobalKatalogKodu    varchar(50) NULL  // Global katalog eşlemesi
- Kod, Ad
- GiderTipi            enum
- HesaplamaTipi        enum(Sabit, Formul, Hibrit)
- ZorunluMu            boolean
- SezonluMu            boolean
- AktifMi              boolean

GiderKalemiHiyerarsi  // Closure table
- Ancestor             uuid FK
- Descendant           uuid FK
- Depth                integer

GiderPaylasimGrubu
- GiderPaylasimGrubuId uuid PK
- TenantId             uuid
- CompanyId            uuid
- ShortCode            varchar(8)
- Kod, Ad
- DagitimModeliId      uuid FK
- AktifMi              boolean

DagitimModeli
- DagitimModeliId      uuid PK
- TenantId             uuid (NULL = global standart)
- Kod, Ad
- ModelTipi            enum(M2, ArsaPayi, Esit, OdaSayisi, BBTipi, Katsayili, Formul)
- FormulVersiyonuId    uuid FK NULL  // Formul tipinde
- YuvarlamaPolitikasi  enum(LRM, AnchorBB, Yonetim, Sirali)

OdemePlani  // Yeni
- OdemePlaniId         uuid PK
- TenantId             uuid
- GiderKalemiId        uuid FK
- PlanTipi             enum(AylikEsit, YillikTekSeferde, FaturaBazli, SezonAylari, Taksitli, KullanimaBagli)
- TaksitSayisi         integer NULL
- BaslangicAyi         integer NULL  // 1-12
- VadeGunu             integer NULL  // ayın hangi günü
- PeriyotAylik         integer  // 1=aylık, 3=üç aylık, 12=yıllık
- SezonBaslangicAy     integer NULL
- SezonBitisAy         integer NULL

VadeKurali
- VadeKuraliId         uuid PK
- TenantId             uuid
- Kod, Ad
- VadeTipi             enum(TahakkukSonrasiGun, AyinSabitGunu, FaturaBazli)
- GunSayisi            integer NULL
- SabitGun             integer NULL  // 1-31
- GecerlilikBaslangic  date
- GecerlilikBitis      date NULL

GecikmeKurali
- GecikmeKuraliId      uuid PK
- TenantId             uuid
- Kod, Ad
- OranTipi             enum(GunlukYuzde, AylikYuzde, SabitTutar)
- OranDegeri           decimal(10,4)
- BilesikMi            boolean
- BaslangicKuralTipi   enum(VadeIzleyenGun, VadeIzleyenAy)
- TavanTutari          decimal(18,2) NULL
- KuralKaynagi         enum
- GecerlilikBaslangic  date
- GecerlilikBitis      date NULL
```

#### Schema: `butce`

```
Butce
- ButceId              uuid PK
- TenantId             uuid
- CompanyId            uuid
- ShortCode            varchar(8)
- Kod, Ad
- ButceBaslangicTarihi date
- ButceBitisTarihi     date
- ButceTipi            enum(Yillik, AraDonem, Olaganustu)
- Durum                enum(Taslak, Onayda, Yayinlandi, Iptal)

ButceVersiyonu
- ButceVersiyonuId     uuid PK
- TenantId             uuid
- ButceId              uuid FK
- VersiyonNo           integer
- GecerlilikBaslangic  date
- GecerlilikBitis      date
- RevizyonNedeni       text
- OnayDurumu           enum
- OncekiVersiyonId     uuid FK NULL

ButceKalemi
- ButceKalemiId        uuid PK
- TenantId             uuid
- ButceId              uuid FK
- GiderKalemiId        uuid FK  (logical, aidat schema)
- OdemePlaniId         uuid FK
- VarsayilanDagitimModeliId uuid FK

ButceKalemiVersiyonu
- ButceKalemiVersiyonuId uuid PK
- TenantId             uuid
- ButceKalemiId        uuid FK
- ButceVersiyonuId     uuid FK
- PlanlananTutar       decimal(18,2)
- HesaplananTutar      decimal(18,2)
- ManuelMi             boolean  // override edildi mi
- ManuelKilit          boolean  // ileri hesaplara dokundurma
- OverrideSebebi       text NULL
- VadeKuraliId         uuid FK NULL
- GecikmeKuraliId      uuid FK NULL

DonemKaydi
- DonemKaydiId         uuid PK
- TenantId             uuid
- CompanyId            uuid
- Yil, Ay              integer
- DonemBaslangic, DonemBitis date
- KapandiMi            boolean
- KapatanId            uuid NULL
- KapatmaTarihi        timestamptz NULL

Tahakkuk
- TahakkukId           uuid PK
- TenantId             uuid
- ShortCode            varchar(8)
- ButceKalemiVersiyonuId uuid FK
- DonemKaydiId         uuid FK
- ToplamTutar          decimal(18,2)
- HesaplamaTarihi      timestamptz
- Durum                enum(Hesaplandi, Yayinlandi, Iptal)

TahakkukDetayi
- TahakkukDetayiId     uuid PK (partition by year)
- TenantId             uuid
- ShortCode            varchar(8)
- TahakkukId           uuid FK
- BagimsizBolumId      uuid FK
- DagitimPayi          decimal(18,6)
- TahakkukTutari       decimal(18,2)
- VadeTarihi           date
- IptalEdildi          boolean DEFAULT false

DuzeltmeIslemi  // Yeni
- Id                   uuid PK
- TenantId             uuid
- KaynakTahakkukDetayiId uuid FK
- YeniTutar            decimal(18,2)
- EskiTutar            decimal(18,2)
- Sebep                text
- OnaylayanId          uuid
- OnayTarihi           timestamptz
- KarsiKayitId         uuid FK NULL  // ters kayıt

Gerceklesme
- GerceklesmeId        uuid PK
- TenantId             uuid
- GiderKalemiId        uuid FK
- DonemKaydiId         uuid FK
- GerceklesenTutar     decimal(18,2)
- BelgeNo, Aciklama

GelirKalemi  // Yeni
- GelirKalemiId        uuid PK
- TenantId             uuid
- CompanyId            uuid
- ShortCode            varchar(8)
- Kod, Ad
- GelirTipi            enum(Aidat, Kira, Faiz, Reklam, GecikmeCezasi, Diger)

KiraSozlesmesi  // Yeni
- KiraSozlesmesiId     uuid PK
- TenantId             uuid
- CompanyId            uuid
- KiraciAd             varchar(200)
- OrtakAlanReferansi   text  // hangi alan kiralanmış
- BaslangicTarihi      date
- BitisTarihi          date
- AylikTutar           decimal(18,2)
- VadeKuraliId         uuid FK

YedekAkce  // Yeni
- YedekAkceId          uuid PK
- TenantId             uuid
- CompanyId            uuid
- Donem                varchar(7)  // YYYY-MM
- BirikenTutar         decimal(18,2)
- CekilenTutar         decimal(18,2)
- KalanTutar           decimal(18,2)
- KararDefteriReferansi text NULL  // çekiş için karar referansı
```

#### Schema: `formul`

```
FormulSablonu
- FormulSablonuId      uuid PK
- TenantId             uuid (NULL = global)
- CompanyId            uuid (NULL = tenant-wide)
- ShortCode            varchar(8)
- Kod, Ad, Aciklama
- CiktiTipi            enum(Sayi, Para, Oran, Bool)
- YenidenKullanilabilirMi boolean
- AktifMi              boolean

FormulVersiyonu
- FormulVersiyonuId    uuid PK
- TenantId             uuid
- FormulSablonuId      uuid FK
- VersiyonNo           integer
- IfadeAST             jsonb  // AST as JSON
- IfadeMetin           text   // human-readable form
- GecerlilikBaslangic  date
- GecerlilikBitis      date NULL
- OnayDurumu           enum
- OnceVersiyonId       uuid FK NULL

FormulParametresi
- FormulParametresiId  uuid PK
- TenantId             uuid
- FormulVersiyonuId    uuid FK
- Kod, Ad
- ParametreTipi        enum(Sayi, Tarih, Metin, Bool, Referans)
- VarsayilanDeger      text NULL
- ZorunluMu            boolean
- SiraNo               integer

FormulGirdiSeti
- FormulGirdiSetiId    uuid PK
- TenantId             uuid
- FormulVersiyonuId    uuid FK
- CompanyId            uuid NULL
- Donem                varchar(7) NULL
- GirdiKaynakTipi      enum

FormulGirdiDegeri
- Id                   uuid PK
- TenantId             uuid
- FormulGirdiSetiId    uuid FK
- FormulParametresiId  uuid FK
- Deger                text  // serialize edilmiş
- BagimsizBolumId      uuid FK NULL

FormulBagimliligi  // Yeni - DAG yönetimi
- Id                   uuid PK
- TenantId             uuid
- KaynakFormulVersiyonuId uuid FK
- HedefFormulVersiyonuId uuid FK NULL  // başka formül
- HedefGiderKalemiId   uuid FK NULL    // veya gider kalemi
- ReferansTipi         enum(FormulSonucu, GiderToplami, BBOzelligi)

HesapInvalidasyonKuyrugu  // Yeni
- Id                   uuid PK
- TenantId             uuid
- EtkilenenObjeTipi    enum
- EtkilenenObjeId      uuid
- TetikleyenSebep      varchar(200)
- DamgaTarihi          timestamptz
- IslemTarihi          timestamptz NULL  // null = bekliyor
```

#### Schema: `sablon`

```
SiteSablonu
- SiteSablonuId        uuid PK
- TenantId             uuid (NULL = global)
- ShortCode            varchar(8)
- Kod, Ad
- SablonTipi           enum(KonutSitesi, KarmaSite, Villalı, Sosyal, AVM, Marina, Plaza, Ofis)
- AktifMi              boolean

GiderSablonu
- Id                   uuid PK
- TenantId             uuid
- SiteSablonuId        uuid FK
- GiderKalemiId        uuid FK
- VarsayilanMi         boolean

DagitimSablonu, FormulSablonuKutuphanesi, SablonUyarlamaKaydi
// Benzer yapı
```

### 14.2. İndeks Stratejisi

| Tablo | Birincil İndeks | Ek İndeksler |
|-------|-----------------|--------------|
| `TahakkukDetayi` | (TahakkukId, BagimsizBolumId) UNIQUE | (TenantId, VadeTarihi), (TenantId, BagimsizBolumId, VadeTarihi) |
| `Tahsilat` | TahsilatId | (TenantId, TahakkukDetayiId), (TenantId, TahsilatTarihi) |
| `BagimsizBolum` | BagimsizBolumId | (TenantId, ShortCode), (CompanyId, BlokId) |
| `Tahakkuk` | TahakkukId | (TenantId, DonemKaydiId), (ButceKalemiVersiyonuId) |
| `FormulVersiyonu` | FormulVersiyonuId | (FormulSablonuId, VersiyonNo) |

### 14.3. Partition Stratejisi

- `TahakkukDetayi`: RANGE PARTITION BY (Yıl), 1 yıl bir partition.
- `Tahsilat`: RANGE PARTITION BY (Yıl).
- `Gerceklesme`: RANGE PARTITION BY (Yıl).
- `audit.AuditLog`: RANGE PARTITION BY (Ay), aktif 12 ay + arşiv.

---

## 15. Formül DSL Spesifikasyonu (v0.9 Taslak)

### 15.1. Tasarım Prensipleri

- **Güvenli** — kullanıcı doğrudan kod çalıştıramaz; sadece izinli operatörler ve fonksiyonlar.
- **Açık** — AST JSON formatı, IfadeMetin formatı paralelinde saklanır.
- **Sınırlı** — Resource limits (ADR-15).
- **Test edilebilir** — Önizleme ortamı zorunlu.
- **Versiyonlu** — Eski versiyonla yapılan hesap tekrar üretilebilir.

### 15.2. Grammar (Excel-benzeri syntax)

```
Expression  := Literal | Reference | FunctionCall | BinaryOp | UnaryOp | Conditional
Literal     := Number | String | Boolean
Reference   := Identifier ('.' Identifier)*
                 // Örnek: BB.BrutMetrekare, Parametre.KisiSayisi, Donem.Ay
FunctionCall := Identifier '(' Args ')'
BinaryOp    := Expression Operator Expression
UnaryOp     := '-' Expression | 'NOT' Expression
Conditional := 'IF' '(' Expression ',' Expression ',' Expression ')'

Operator    := '+' | '-' | '*' | '/' | '%' | '^' 
             | '=' | '<>' | '<' | '<=' | '>' | '>='
             | 'AND' | 'OR'
```

### 15.3. Veri Tipleri

| Tip | Açıklama | Hassasiyet |
|-----|----------|-----------|
| `Sayi` | Genel sayısal | decimal(28,10) |
| `Para` | Para birimi | decimal(18,2) |
| `Oran` | Yüzde/oran | decimal(10,6) |
| `Tarih` | Tarih | date |
| `Donem` | YYYY-MM | varchar(7) |
| `Metin` | String | varchar |
| `Bool` | Boolean | bool |

### 15.4. Standart Fonksiyon Kütüphanesi (v1)

**Matematik (8):**
- `MIN(a, b, ...)`, `MAX(a, b, ...)`, `ABS(x)`, `SUM(list)`, `AVG(list)`, `POW(x, y)`, `SQRT(x)`, `MOD(x, y)`

**Yuvarlama (2):**
- `ROUND(x, n)`, `CEILING(x, n)`, `FLOOR(x, n)`

**Koşul (2):**
- `IF(condition, a, b)`, `IFS(c1, a1, c2, a2, ..., default)`

**Agregasyon (4):**
- `SUM_OVER(group, expression)` — bir grup üzerinden topla
- `COUNT(group, condition)` — koşula uyan say
- `LAG(period_offset, reference)` — önceki dönem değeri
- `LOOKUP(table, key)` — tablodan değer al

**Tarih (3):**
- `MONTH(date)`, `YEAR(date)`, `DAYS_BETWEEN(d1, d2)`

**Yardımcı (3):**
- `IS_SEASON(date, start_month, end_month)`, `BB_PROPERTY(bb_id, property_code)`, `CLAMP(x, min, max)`

### 15.5. Reference Resolution

| Referans | Çözümleme |
|----------|----------|
| `Parametre.X` | Formülün kendi parametresinden |
| `BB.X` | Tahakkuk üretimi sırasında her BB için ayrı çözülür (BrutMetrekare, ArsaPayi, ...) |
| `BB.Ozellik.X` | EAV özniteliği (FR-03) |
| `Donem.X` | Mevcut dönem (Yil, Ay, BaslangicTarihi) |
| `Yerleske.X` | Yerleşke parametresi (BBSayisi, BlokSayisi, HavuzVar) |
| `Mevzuat.X` | Sistem-genelinde kilitli sabit (KMK_M20_TAVAN, vs.) |
| `Formul.X` | Başka formülün sonucu — DAG'a kaydedilir |

### 15.6. Güvenlik Sınırları

| Sınır | Değer |
|-------|-------|
| Max formül uzunluğu | 10,000 karakter |
| Max parametre sayısı | 50 |
| Max fonksiyon çağrısı | 100 (per execution) |
| Max recursion depth | 10 |
| Max CPU time | 100 ms |
| Max memory | 10 MB |
| Yasak: | Loop, GoTo, side effect, network/disk I/O |

### 15.7. Hata Mesajları (Türkçe + Stadnardize Kod)

| Kod | Mesaj |
|-----|-------|
| F001 | Sıfıra bölme |
| F002 | Tanımsız değişken: `{var}` |
| F003 | Çıktı tipi uyumsuz: beklenen `{X}`, alınan `{Y}` |
| F004 | Dairesel bağımlılık tespit edildi: `{A → B → A}` |
| F005 | Resource limit aşıldı (CPU/memory/recursion) |
| F006 | Geçersiz operatör: `{op}` `{tip}` üzerinde uygulanamaz |
| F007 | Fonksiyon `{X}` mevcut değil veya bu versiyonda yasak |
| F008 | Aşırı uç değer: sonuç `{X}` (sınır `{max}`) |

### 15.8. Örnek Formül

**Senaryo:** Havuz cankurtaran ücreti.

**Parametreler:**
- `KisiSayisi`: Sayı (zorunlu)
- `AylikUcret`: Para (zorunlu)
- `SezonBaslangic`: Tarih
- `SezonBitis`: Tarih

**İfade Metin:**
```
IF(
  IS_SEASON(Donem.BaslangicTarihi, MONTH(Parametre.SezonBaslangic), MONTH(Parametre.SezonBitis)),
  Parametre.KisiSayisi * Parametre.AylikUcret,
  0
)
```

**AST JSON:**
```json
{
  "type": "Conditional",
  "condition": {
    "type": "FunctionCall",
    "name": "IS_SEASON",
    "args": [
      { "type": "Reference", "path": ["Donem", "BaslangicTarihi"] },
      { "type": "FunctionCall", "name": "MONTH", "args": [{"type": "Reference", "path": ["Parametre", "SezonBaslangic"]}] },
      { "type": "FunctionCall", "name": "MONTH", "args": [{"type": "Reference", "path": ["Parametre", "SezonBitis"]}] }
    ]
  },
  "thenBranch": {
    "type": "BinaryOp",
    "op": "*",
    "left": { "type": "Reference", "path": ["Parametre", "KisiSayisi"] },
    "right": { "type": "Reference", "path": ["Parametre", "AylikUcret"] }
  },
  "elseBranch": { "type": "Literal", "value": 0 }
}
```

---

## 16. Hesaplama Bağımlılık Grafiği

### 16.1. Veri Modeli

`FormulBagimliligi` tablosu (Bölüm 14.1, schema formul). Her formül versiyonu için referans verdiği diğer formüller/giderler listelenir.

### 16.2. Cycle Detection Algoritması

Yeni formül kaydedilirken DFS-tabanlı cycle check:

```
boolean HasCycle(FormulVersiyonuId yeniFormul, IList<Reference> referanslar) {
    var graph = LoadDependencyGraph();  // tüm mevcut formul bağımlılıkları
    graph.AddEdges(yeniFormul, referanslar);
    return TarjanSCC(graph).Any(scc => scc.Count > 1);  // güçlü bağlantılı bileşen >1 = cycle
}
```

Cycle bulunursa: F004 hatası, kayıt reddedilir.

### 16.3. Topological Sort

Tahakkuk üretimi öncesi tüm bağımlı formüller sıralanır:

```
List<FormulVersiyonu> SortForExecution(List<FormulVersiyonu> formuller) {
    return TopologicalSort(formuller, this.bagimlilikGrafigi);
}
```

### 16.4. Invalidation Cascade

Bir formülün girdisi değiştiğinde:

```
void OnFormulOrInputChanged(FormulVersiyonuId degisti) {
    var etkilenenler = GetTransitiveDependents(degisti);
    foreach (var e in etkilenenler) {
        HesapInvalidasyonKuyrugu.Add(new {
            EtkilenenObjeTipi = "FormulVersiyonu",
            EtkilenenObjeId = e,
            TetikleyenSebep = $"FormulVersiyonu {degisti} degisti",
            DamgaTarihi = DateTime.UtcNow
        });
    }
    // Hangfire job kuyruktan tek tek alıp yeniden hesaplar
}
```

### 16.5. Idempotency ve Re-run Safety

- Tahakkuk üretimi `(DonemKaydiId, ButceKalemiVersiyonuId)` UNIQUE constraint ile korunur.
- Aynı kombinasyonla tekrar üretilmek istenirse mevcut kayıt güncellenir, dublike olmaz.
- Sonuç değişikliği varsa audit'lenir.

---

## 17. Domain Event Kontratları

Tüm event'ler `/contracts/notifications/Mali/` klasöründe versiyonlanır.

### 17.1. Mali Domain Event'leri

```csharp
public record TahakkukUretildi(
    Guid TahakkukId,
    Guid TenantId,
    Guid CompanyId,
    Guid ButceKalemiVersiyonuId,
    Guid DonemKaydiId,
    decimal ToplamTutar,
    int EtkilenenBBSayisi,
    DateTime HesaplamaTarihi
) : IDomainEvent;

public record TahakkukIptalEdildi(
    Guid TahakkukId,
    Guid TenantId,
    Guid IptalEdenId,
    string IptalSebebi,
    Guid? KarsiKayitId,
    DateTime IptalTarihi
) : IDomainEvent;

public record BorcOdendi(
    Guid TahsilatId,
    Guid TahakkukDetayiId,
    Guid TenantId,
    Guid BagimsizBolumId,
    decimal OdenenTutar,
    decimal KalanBorc,
    DateTime OdemeTarihi
) : IDomainEvent;

public record ButceYayinlandi(
    Guid ButceVersiyonuId,
    Guid ButceId,
    Guid TenantId,
    int VersiyonNo,
    DateTime GecerlilikBaslangic,
    Guid OnaylayanId
) : IDomainEvent;

public record ButceRevizeEdildi(
    Guid YeniVersiyonId,
    Guid OncekiVersiyonId,
    Guid ButceId,
    Guid TenantId,
    string RevizyonNedeni,
    List<Guid> EtkilenenKalemler,
    DateTime GecerlilikBaslangic
) : IDomainEvent;

public record FormulOnaylandi(
    Guid FormulVersiyonuId,
    Guid TenantId,
    int VersiyonNo,
    DateTime GecerlilikBaslangic,
    Guid OnaylayanId
) : IDomainEvent;

public record ManuelOverrideUygulandi(
    Guid ButceKalemiVersiyonuId,
    Guid TenantId,
    decimal EskiTutar,
    decimal YeniTutar,
    string OverrideSebebi,
    Guid OnaylayanId
) : IDomainEvent;

public record GecikmeHesaplandi(
    Guid BorcDurumuId,
    Guid TenantId,
    Guid BagimsizBolumId,
    decimal IslenmisGecikmeTutari,
    DateTime HesaplamaTarihi
) : IDomainEvent;

public record YedekAkceCekisYapildi(
    Guid YedekAkceId,
    Guid TenantId,
    decimal CekilenTutar,
    string KararDefteriReferansi,
    Guid OnaylayanId
) : IDomainEvent;

public record DonemKapatildi(
    Guid DonemKaydiId,
    Guid TenantId,
    int Yil,
    int Ay,
    Guid KapatanId
) : IDomainEvent;
```

### 17.2. Outbox Sözleşmesi

Her event:
- `event_id` (Guid, unique)
- `tenant_id` (zorunlu)
- `event_type` (string, FQN)
- `payload` (jsonb)
- `created_at` (timestamptz)
- `dispatched_at` (timestamptz NULL)
- `retry_count` (int)
- `last_error` (text NULL)
- `idempotency_key` (string, indexed)
- `company_migrated_to` (Guid NULL) — Wave 9+ rezerve

---

## 18. Geliştirme Standartları (MultiTenant Uyumlu)

Bu standartlar `SYSTEM_PROMPT-v1.7.2.md`'nin bütçe domain'ine uyarlamasıdır.

### 18.1. Modül Yapısı

```
src/Modules/Mali/Butce/
├── Butce.Application/        # use case, command/query, DTO
├── Butce.Domain/             # entity, value object, domain event
├── Butce.Infrastructure/     # EF Core, Dapper repository
├── Butce.Api/                # controller / endpoint
├── Butce.SystemOperations/   # SuperAdmin (IgnoreQueryFilters)
└── Butce.Contracts/          # public DTO, event contract
```

### 18.2. Adlandırma

- Tablo adları: PascalCase Türkçe (`ButceKalemiVersiyonu`)
- Kolon adları: PascalCase Türkçe (`ToplamTutar`, `OnayDurumu`)
- Şema adları: lowercase Türkçe (`butce`, `aidat`, `formul`)
- Entity sınıfları: Türkçe PascalCase (C# içinde de)
- Service sınıfları: İngilizce PascalCase (`TahakkukGeneratorService`)
- Interface: `I` prefix (`ITahakkukGenerator`)

### 18.3. Logging

Üç-katmanlı zorunlu (Serilog):
- Console (geliştirme)
- File (rolling daily, 30 gün)
- PostgreSQL (`audit.AppLog` tablosu, 30 gün aktif)

Her log:
- TenantId (varsa)
- UserId (varsa)
- TraceId (correlation)
- LogLevel
- Message
- Exception (varsa)

### 18.4. Kod Stili

- C# 12+ özelliklerden yararlanılır (primary constructor, collection expression)
- Async/await tüm I/O için
- CancellationToken her async metoda
- Nullable reference types açık (`<Nullable>enable</Nullable>`)
- Result<T> pattern (exception throw değil)
- MediatR ile CQRS

### 18.5. Migration

- EF Core code-first
- Her migration ayrı PR
- Migration adı: `[YYYYMMDD]_[Description]` (örn: `20260520_AddOdemePlani`)
- Up + Down zorunlu
- Migration runner: deploy öncesi otomatik (`dotnet ef database update`)

### 18.6. Test Standartları

- Unit test: xUnit + FluentAssertions + NSubstitute
- Integration test: TestContainers (PostgreSQL)
- Test coverage hedef: %80 (domain logic için %95)
- Her PR test geçmeli (CI)

---

# BÖLÜM V — TEST MÜHENDİSİ KATMANI

## 19. Test Stratejisi

### 19.1. Test Piramidi

```
              ┌────────┐
              │   E2E   │  ~5% (kritik akışlar)
              ├────────┤
              │  Integ  │  ~25% (modül arası, DB ile)
              ├────────┤
              │  Unit   │  ~70% (domain logic)
              └────────┘
```

### 19.2. Test Kategorileri

| Kategori | Kapsam | Sorumluluğu |
|----------|--------|------------|
| **Unit Test** | Domain logic, formül execute, dağıtım matematiği | Developer |
| **Integration Test** | Repository, modül entegrasyonu | Developer |
| **Contract Test** | Domain event uyumluluğu | Developer |
| **Acceptance Test** | Bölüm 21 senaryoları | QA |
| **Performance Test** | NFR 12.1 hedefleri | QA + DevOps |
| **Security Test** | OWASP, tenant izolasyonu | Security |
| **Penetration Test** | Üçüncü taraf | Yıllık |
| **Smoke Test** | Deploy sonrası temel sağlık | DevOps |

### 19.3. Kritik Test Alanları (Mutlaka Test Edilmeli)

1. **Tenant izolasyonu:** Bir tenant'ın verisi başka tenant'tan ASLA görülmemeli.
2. **Forward-only versioning:** Geçmiş tahakkuk hiçbir senaryoda değişmemeli.
3. **Dağıtım toplamı:** Tüm BB'lerin payları toplandığında giriş tutarı ± yuvarlama hassasiyeti dışında eşit olmalı.
4. **Cycle detection:** Dairesel formül kabul edilmemeli.
5. **Resource limits:** Aşırı yük formülü sandbox çökmemeli.
6. **Audit immutability:** Audit log silme/güncelleme reddedilmeli.
7. **Determinizm:** Aynı input → aynı output her zaman.
8. **Idempotency:** Tahakkuk rerun aynı sonucu vermeli.
9. **Concurrency:** Aynı bütçe versiyonu eşzamanlı düzenlenmemeli (optimistic lock).

---

## 20. Referans Veri Seti (15 Senaryo)

Geliştirme ve UAT için kullanılacak referans yerleşke profilleri. Her senaryo için: kurulum verisi (CSV/JSON), beklenen tahakkuk sonuçları, beklenen raporlar hazırlanır.

| # | Profil | BB Sayısı | Özellikler | Test Hedefi |
|---|--------|-----------|------------|-------------|
| 1 | Küçük Apartman | 12 | 1 blok, asansör yok, otopark açık | Temel akış |
| 2 | Orta Site | 120 | 4 blok, 4 asansör, kapalı otopark, güvenlik | Çok-bloklu dağıtım |
| 3 | Karma Konut+Dükkan | 92 | 80 konut + 12 dükkan, asansör | Muafiyet kuralı |
| 4 | Sosyal Tesisli Büyük | 350 | Havuz, spor salonu, sosyal tesis | Çoklu paylaşım grubu |
| 5 | Villalı Site | 40 | Açık otopark, peyzaj ağır, ortak alan sınırlı | Düşük ortak alan |
| 6 | AVM | 200 | Sadece ticari, ortak alan ağırlık, baz istasyonu kira | Gelir yönetimi |
| 7 | Marina | 80 yat + 50 depo | Sezonluk yoğun, döviz girişi | Para birimi (Wave 5+) |
| 8 | Plaza/Ofis | 60 | Ofis ağırlık, asansör yoğun, güvenlik | Yüksek kullanım profili |
| 9 | Mixed-use | 200 | 120 konut + 50 ofis + 30 dükkan, 3 farklı katılım grubu | Çakışan kapsamkuralları |
| 10 | Otopark-Yoğun | 60 + 200 araç yeri | Otopark satılan/kiralanan ayrı modeli | Özel öznitelik gerek (EAV) |
| 11 | Revizyonlu Bütçe | 150 | Yıl ortasında elektrik %30 zam | Forward-only versioning |
| 12 | Çoklu Muafiyet | 200 | 4 farklı muafiyet, 6 katılım grubu | Çakışma çözüm algoritması |
| 13 | Yedek Akçe Çekişi | 100 | Olağanüstü çatı tamiri ile yedek akçe çekiş | KMK m.20 |
| 14 | KVKK Silme | 200 | Bir BB sahibinin anonimleştirme talebi | KVKK uyum |
| 15 | Çok Karmaşık Formül | 250 | Birden çok formül birbirine bağlı | DAG, cycle detection |

Her senaryo için Test Coordinator dokümanları (input verisi + beklenen output):
- `/tests/fixtures/scenario-01/input.json`
- `/tests/fixtures/scenario-01/expected-tahakkuk.json`
- `/tests/fixtures/scenario-01/expected-reports/*.json`

---

## 21. Kabul Kriterleri Şablonu

Her fonksiyonel gereksinim için kabul kriterleri şu formatta yazılır:

```
GHERKIN STILI:
  Verili (Given): Başlangıç durumu
  Yapıldığında (When): İşlem
  Beklenen (Then): Sonuç

ÖRNEK FR-08 (Kural Çakışma Çözümü):

Senaryo 1: Spesifik kural genel kuralı geçer
  Verili: 
    - Yerleşke A'da "Tüm Konutlar Dahil" katılım kuralı tanımlı
    - Aynı yerleşkede "A Blok 1. kat Konutlar Hariç" muafiyet kuralı tanımlı
  Yapıldığında:
    - "Havuz Bakım" gideri için tahakkuk üretildiğinde
  Beklenen:
    - A Blok 1. kat konutları tahakkuktan hariç kalmalı
    - Sebep açıklaması "A Blok 1. kat konutları muafiyet kuralı baskın" olmalı

Senaryo 2: Mevzuat kuralı Yönetim Planı kuralını geçer
  Verili:
    - Yönetim Planı'nda "tüm BB'ler ortak gidere katılır" yazılı
    - Mevzuat sabiti: "0 m² alanlı BB'ler m² dağıtımında hariç"
  Yapıldığında:
    - 0 m² alanlı bir BB için tahakkuk hesaplandığında
  Beklenen:
    - BB m² dağıtımına dahil olmamalı
    - Audit: "Mevzuat kuralı baskın"
```

### 21.1. Kabul Kriterleri Checklist

Her FR için sağlanması gerekenler:
- [ ] En az 1 pozitif (happy path) senaryo
- [ ] En az 1 negatif (error path) senaryo
- [ ] En az 1 edge case (sınır değer)
- [ ] Audit kaydının doğrulanması
- [ ] Yetki kontrolünün doğrulanması
- [ ] Tenant izolasyonunun doğrulanması (varsa data-related)
- [ ] Concurrency davranışı (eşzamanlı işlem)
- [ ] Performans hedefi (NFR'a göre)

---

## 22. Definition of Done

Bir özellik "DONE" olarak işaretlenebilir mi?

### 22.1. Kod Düzeyi (Developer)

- [ ] Kod review yapıldı, en az 1 onay
- [ ] Unit testler %80+ coverage
- [ ] Integration testler geçti
- [ ] Migration up + down çalışıyor
- [ ] Statik analiz uyarı yok
- [ ] Build CI'da yeşil

### 22.2. Fonksiyonel Düzey (QA)

- [ ] Tüm acceptance criteria geçti
- [ ] Pozitif + negatif + edge case test edildi
- [ ] Referans veri seti üzerinde test edildi (uygun senaryoda)
- [ ] Yetki matrisi (Bölüm 6.2) test edildi
- [ ] Tenant izolasyonu test edildi (eğer data-related)
- [ ] Audit kayıtları doğrulandı
- [ ] UI'da hata mesajları Türkçe ve anlaşılır

### 22.3. NFR Düzeyi (QA + DevOps)

- [ ] Performans hedefi karşılandı (NFR 12.1)
- [ ] Memory leak yok
- [ ] DB query'leri N+1 değil
- [ ] Log seviyesi uygun
- [ ] Erişilebilirlik check (WCAG 2.1 AA) — UI varsa

### 22.4. Dokümantasyon Düzeyi (Tech Writer)

- [ ] API endpoint dokümanı güncel
- [ ] Domain event kontratı güncel (varsa)
- [ ] Migration notu yazıldı (breaking change varsa)
- [ ] Kullanıcı kılavuzu güncellendi (UI değişikliği varsa)
- [ ] CHANGELOG güncel

### 22.5. Ürün Düzeyi (Product Owner)

- [ ] Demo yapıldı, kabul edildi
- [ ] User Story → kabul kriteri tam karşılandı
- [ ] Edge case'ler ele alındı

---

# BÖLÜM VI — YÖNETİM

## 23. Açık Tasarım Soruları (Mimari Karar Bekleyen)

Aşağıdaki 15 karar mimari onay (ONAY) bekliyor. Geliştirmeye başlamadan önce her birinin sonuçlanması gerekir.

| # | Karar | Önerilen | Karar Veren | Deadline |
|---|-------|----------|-------------|----------|
| 1 | Portföy ↔ Tenant/Company eşleştirmesi | Belgenin `Portfoy` = MultiTenant `Tenant`; `Site` = `Company`+`Yerleşke` (1:1) | Sistem Mimarı | Hemen |
| 2 | Formül parser teknolojisi | ANTLR custom grammar (Roslyn ve NCalc reddedildi) | Mimar + Tech Lead | 1 hafta |
| 3 | Fonksiyon kütüphanesi v1 listesi | Bölüm 15.4'teki 22 fonksiyon | İş Analisti + Mimar | 1 hafta |
| 4 | Yuvarlama varsayılan politika | LRM (Largest Remainder Method) | İş Analisti | 3 gün |
| 5 | Çakışma çözüm algoritması | Bölüm 8.3'teki 5-seviye algoritma | Mimar + İş Analisti | 3 gün |
| 6 | Kısmi ödeme varsayılan tahsis | Önce-faiz-sonra-anapara (TBK m.101) | İş Analisti + Hukuk | 3 gün |
| 7 | EAV implementasyon stratejisi | Tip-güvenli EAV (sayı/metin/bool/seçim ayrı kolon) | Mimar | 3 gün |
| 8 | Hiyerarşi modeli | Closure table | Mimar | Karar verildi |
| 9 | Partition stratejisi | Year-based range partition (TahakkukDetayi, Tahsilat, Gerceklesme) | DBA + Mimar | 1 hafta |
| 10 | Senaryo entity ayrımı | `Senaryo` ayrı entity, `Versiyon`'dan bağımsız | Mimar + İş Analisti | 3 gün |
| 11 | Sezon entity'si | `SezonTanimi` entity'si, tarih aralığı + tekrarlı yıllar | İş Analisti | 3 gün |
| 12 | Para birimi stratejisi | MVP tek para (TL), Wave 5+ multi-currency | Ürün Sahibi | 3 gün |
| 13 | Banka mutabakat MVP kapsamı | Wave 4'te basit CSV import + manual eşleştirme; otomatik Wave 5+ | Ürün Sahibi | 1 hafta |
| 14 | Öneri motoru MVP yaklaşımı | Wave 6'ya kadar yok; Wave 6'da rule-based | Ürün Sahibi | Wave 5 sonrası |
| 15 | Global katalog yönetim modeli | SystemAdmin yönetir; Tenant özelleştirme katmanı; site override haritalama | Ürün + Mimar | 1 hafta |

---

## 24. Yol Haritası ve Fazlama

### 24.1. Pre-Wave (Tasarım — 4 hafta)

Geliştirmeye başlamadan önce tamamlanmalı:

| Çıktı | Süre | Sorumlu |
|-------|------|---------|
| Bu dokümandaki açık kararların kapatılması (23. bölüm) | 1 hafta | Mimar |
| Formül DSL grammar + parser proof of concept | 1 hafta | Tech Lead |
| Veri modeli ER diyagramı (PG kullanarak) | 3 gün | DBA |
| API spec (OpenAPI) | 5 gün | Backend |
| UI mockup (Figma) — en az 8 ana ekran | 1 hafta | UX |
| Referans veri seti hazırlığı (15 senaryo) | 5 gün | QA + İş Analisti |
| Test stratejisi onayı | 3 gün | QA Lead |

### 24.2. Wave 0 (Temel Altyapı — 6 hafta)

MultiTenant Wave 0'a paralel. Ajan A scope dahilinde:

- Module.Aidat şema kurulumu (aidat.*)
- Module.Butce şema kurulumu (butce.*)
- Module.Formul yeni modül kurulumu (formul.*)
- `BagimsizBolum`, `OzellikTanimi` (EAV), `KatilimGrubu` temel CRUD
- `GiderKalemi`, `GiderPaylasimGrubu`, `DagitimModeli` temel CRUD
- Outbox + Audit + Identity entegrasyonu
- TenantId global filter çalışıyor
- ShortCode üretici servisi

### 24.3. Wave 1-2 (MVP Çekirdek — 12 hafta)

- Standart dağıtım modelleri (m², arsa payı, eşit, oda sayısı, BB tipi)
- Yuvarlama politikası
- Çakışma çözüm motoru
- Kapsam + Muafiyet kuralları
- Bütçe oluşturma, versiyonlama, revizyon
- Tahakkuk üretimi (basit, yıllık-aylık)
- Tahsilat kaydı (manuel)
- Vade + Gecikme cezası (KMK m.20 uyumlu)
- Audit ve onay süreci (Module.Onay entegrasyon)
- Şablon kütüphanesi temel (3 şablon)
- Temel raporlar (sapma, tahsilat, borç durumu)

**MVP Tanımı:** Bir yerleşke için baştan sona bütçe-tahakkuk-tahsilat-rapor akışı çalışmalı.

### 24.4. Wave 3-4 (Genişleme — 10 hafta)

- Kullanıcı tanımlı formül (DSL v1)
- Formül DAG ve invalidation
- Manuel düzeltme + override
- Ödeme planı entity
- Gelir kalemleri + Net İşletme Sonucu
- Yedek akçe yönetimi
- Banka mutabakat (manual + CSV)
- Senaryo motoru (what-if)
- Dönem kapama
- Şablon kütüphanesi genişleme

### 24.5. Wave 5 (Sertleştirme — 6 hafta)

- Performans optimizasyonu (partition, materialized view)
- Veri standardizasyonu disiplin servisleri
- Migrasyon araçları (Excel import)
- Penetration test
- KVKK anonimleştirme tam akışı
- Erişilebilirlik (WCAG 2.1 AA)

### 24.6. Wave 6-8 (Stratejik Katmanlar — Açık uçlu)

- Portföy analitiği
- Öneri motoru (rule-based → istatistiksel)
- eFatura/eDefter entegrasyonu (Module.Mali ile)
- Çoklu para birimi
- Tenant migration (Wave 9+)

---

## 25. Risk Kaydı

| ID | Risk | Olasılık | Etki | Mitigasyon |
|----|------|----------|------|-----------|
| R-01 | Formül DSL'i kullanıcı için karmaşık olur, no-code hedefi kullanılmaz | Yüksek | Yüksek | UX: önizleme, otomatik tamamlama, şablon kütüphanesi; eğitim |
| R-02 | DAG cycle detection performans sorunu | Düşük | Orta | Write-time check + cache; benchmark öncelikli |
| R-03 | Yuvarlama farkları mali denetimde sorun çıkarır | Orta | Yüksek | Politika UI'dan seçilir, audit'lenir, raporda görünür |
| R-04 | Partition stratejisi geri-uyumsuz | Düşük | Yüksek | Wave 0'da doğru kurulum, sonra zor değişir |
| R-05 | Veri migrasyonu pilot sitelerden başarısız | Orta | Yüksek | Pilot öncesi referans veri seti ile test, idempotent import |
| R-06 | KVKK ↔ mali immutability çelişkisi denetim sorunu | Düşük | Yüksek | Anonimleştirme stratejisi belge'de tanımlı, audit'li |
| R-07 | Wave 6'da öneri motoru için yeterli veri birikmemiş | Yüksek | Düşük | Wave 6 zaten 18-24 ay sonra |
| R-08 | Tenant izolasyonu bug'ı → cross-tenant data leak | Düşük | Çok yüksek (P0) | EF global filter + integration test + penetration test |
| R-09 | Formül sandbox bypass (güvenlik) | Düşük | Yüksek | ANTLR + AST validation + resource limits + audit |
| R-10 | Performans 1000+ tenant ölçeğinde çöker | Orta | Yüksek | Wave 5 sertleştirme, partition, materialized view |
| R-11 | Onay süreci karmaşıklaşır, kullanıcı kaçar | Orta | Orta | Module.Onay basit MVP, sonra zenginleştirme |
| R-12 | Şablon kütüphanesi yetersiz → her site özel kurulum | Orta | Orta | Şablon kütüphanesi sürekli zenginleştirme, kullanıcı katkı modeli |

---

# EKLER

## Ek A — Mevzuat Referansları

| Mevzuat | Madde | İlgili Konu |
|---------|-------|-------------|
| **Kat Mülkiyeti Kanunu (KMK) 634** | m.18 | Ortak gider katılım esasları |
| | m.20 | Gecikme tazminatı %5/ay tavanı; zorunlu yedek akçe |
| | m.32 | Yönetim Planı önceliği |
| **Türk Borçlar Kanunu (TBK) 6098** | m.88 | Yasal faiz oranı |
| | m.101 | Kısmi ödeme tahsis sırası |
| **Türk Ticaret Kanunu (TTK) 6102** | m.66-70 | Defter tutma yükümlülüğü |
| | m.82 | Defterlerin 10 yıl saklanması |
| **KVKK 6698** | m.7 | Kişisel verinin silinmesi/anonimleştirilmesi |
| | m.12 | Veri güvenliği |
| **Yargıtay 18. HD** | İçtihatlar | Aidat anlaşmazlıkları emsalleri |

## Ek B — Önceki Değerlendirmelerin Sentezi

Bu doküman, üç bağımsız AI değerlendirmesinin sentezidir:

- **CA değerlendirmesi:** Teknik derinlik, parser/sandbox kararları, Türk hukuku spesifik referansları, MultiTenant projesi entegrasyon haritası.
- **CG değerlendirmesi:** Sistematik kapsam, hesaplama bağımlılık grafiği, manuel düzeltme, veri yönetişimi, rapor metrik sözlüğü.
- **DS değerlendirmesi:** Popüler-anlaşılır, gelir yönetimi, ödeme planı, veri standardizasyonu disiplini.

Üç değerlendirme şu ortak yargıya ulaştı:
1. Vizyon güçlü.
2. Mimari omurga doğru.
3. Doğrudan kodlamaya başlanamaz, ~3-4 hafta ek tasarım gerekli.
4. En kritik 5 eksik: Formül DSL, çakışma çözüm algoritması, yuvarlama politikası, EAV custom attribute, UI ekran akışları.

## Ek C — Sözlük (Kısaltmalar)

| Kısaltma | Açıklama |
|----------|---------|
| ADR | Architecture Decision Record |
| AST | Abstract Syntax Tree |
| BB | Bağımsız Bölüm |
| CQRS | Command Query Responsibility Segregation |
| DAG | Directed Acyclic Graph |
| DSL | Domain Specific Language |
| EAV | Entity-Attribute-Value |
| EF | Entity Framework |
| FR | Functional Requirement |
| KMK | Kat Mülkiyeti Kanunu |
| KVKK | Kişisel Verilerin Korunması Kanunu |
| LRM | Largest Remainder Method |
| NFR | Non-Functional Requirement |
| OLAP | Online Analytical Processing |
| OLTP | Online Transactional Processing |
| PII | Personally Identifiable Information |
| RPO | Recovery Point Objective |
| RTO | Recovery Time Objective |
| SDD | Software Design Document |
| TBK | Türk Borçlar Kanunu |
| TDHP | Tek Düzen Hesap Planı |
| TTK | Türk Ticaret Kanunu |
| WCAG | Web Content Accessibility Guidelines |

---

## Belge Yönetim Bilgisi

**Değişiklik tarihçesi:**
| Sürüm | Tarih | Yazar | Açıklama |
|-------|-------|-------|---------|
| 0.1 | 2026-05-20 | CA | İlk değerlendirme |
| 0.2 | 2026-05-20 | DS | İkinci değerlendirme |
| 0.3 | 2026-05-20 | CG | Üçüncü değerlendirme |
| 0.4 | 2026-05-20 | CA | Revize (DS+CG sentezi) |
| 0.5 | 2026-05-20 | DS, CG | Revize değerlendirmeler |
| **1.0** | **2026-05-20** | **CA (Final)** | **Bu SDD — paydaş onayına sunulan tasarım** |

**Onay zinciri:**
- [ ] Sistem Mimarı
- [ ] Tech Lead
- [ ] Lead Backend Developer
- [ ] QA Lead
- [ ] Product Owner
- [ ] Hukuk Müşaviri (KVKK + KMK + TBK uyum)
- [ ] (Opsiyonel) Dış Mali Müşavir

**Bağlı doküman setleri (oluşturulacak):**
- `BUDGET-API-SPEC-v1.0.yaml` (OpenAPI 3.0)
- `BUDGET-DATA-MODEL-v1.0.dbml` (DB diyagramı)
- `BUDGET-FORMULA-DSL-v1.0.md` (DSL detay)
- `BUDGET-UI-MOCKUPS-v1.0.fig` (Figma)
- `BUDGET-TEST-PLAN-v1.0.md` (Test stratejisi)
- `BUDGET-REFERENCE-DATASETS-v1.0/` (15 senaryo verileri)

---

**Doküman sonu.**

*Bu dokümanın yaşayan bir belge olduğu; revize edilirken sürüm numarası artırılması ve onay zincirinden geçirilmesi gerektiği bilinmelidir.*
