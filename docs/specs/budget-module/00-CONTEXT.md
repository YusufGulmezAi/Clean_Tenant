# 00 — Proje Bağlamı (Claude Code İlk Okuma)

> **Bu dokümanı her oturum başında oku.** Bu dosyayı okumadan kod yazmaya başlama.

## 1. Proje Kimliği

**Proje adı:** [Henüz konmadı — kodda neyse o]
**Ürün konumu:** Türkiye'de toplu yapı yönetim firmalarına SaaS olarak hizmet veren multi-tenant platform.
**Hedef kullanıcılar:**
- Site yönetim firmaları (Tenant)
- Bu firmaların yönettiği siteler/AVM'ler/marinalar (Site = Company)
- Bağımsız bölüm sahipleri ve kiracılar (Sakin)

**İş hedefi:** Her sitenin yönetim planı, yapısal özellikleri ve yasal yükümlülükleri (Kat Mülkiyeti Kanunu, KVKK, TBK, TTK) farklı olduğu için **tek tip yazılım yetmez**. Sistem her sitenin kuralını UI üzerinden (kod değiştirmeden) kurgulayabilmeli.

## 2. Mevcut Durum (Bu Dilim Öncesi Tamamlananlar)

| Modül | Durum | Teknoloji |
|-------|-------|-----------|
| User Management | ✅ Tamamlandı | Blazor + MudBlazor + ASP.NET Identity |
| Tenant Management | ✅ Tamamlandı | Blazor + MudBlazor |
| Site (= Company) Management | ✅ Tamamlandı | Blazor + MudBlazor |
| **Yapı Şeması (Ada/Parsel/Yapı/BB)** | ⏳ Bu sprint | — |
| **Bütçe + Tahakkuk MVP** | ⏳ Bu sprint | — |

## 3. Mimari Stack

- **.NET:** .NET 10 (veya mevcut sürüm — repo'dan teyit et)
- **Mimari:** Clean Architecture, **katmanlı** (modül-bazlı DEĞİL)
  ```
  src/
  ├── [ProjectName].Domain/         (entity, value object, domain event, business rule)
  ├── [ProjectName].Application/    (use case, CQRS, DTO, validation, interface)
  ├── [ProjectName].Infrastructure/ (EF Core, repo impl, external service, Hangfire)
  └── [ProjectName].WebUI/          (Blazor pages, MudBlazor components, DI)
  ```
- **Database:** PostgreSQL
- **ORM:** EF Core (write), Dapper (read için karmaşık sorgu)
- **UI:** Blazor Server + MudBlazor
- **Background jobs:** Hangfire
- **Logging:** Serilog (Console + File + PostgreSQL)
- **Validation:** FluentValidation
- **CQRS:** MediatR
- **Patterns:** Repository, UnitOfWork, Outbox

## 4. Çok Kiracılı (Multi-Tenant) Yapı

**Hiyerarşi:**
```
Tenant (yönetim firması)
  └── Site (= Company, hukuki tüzel kişilik, KMK kapsamında yönetim birimi)
       └── Ada (tapu kütüğü ada no)
            └── Parsel (tapu kütüğü parsel no)
                 └── Yapı (Apartman, AVM, İş Merkezi, Sosyal Tesis, vb.)
                      └── Blok (Yapı bir veya çok bloktan oluşabilir)
                           └── Bağımsız Bölüm (BB)
```

**Önemli notlar:**
- Bir site (Company) **birden çok ada/parsel** içerebilir (büyük sitelerde gerçek durum).
- Yapı **bir parsele bağlıdır** (bir parsel üzerinde bir veya birden çok yapı olabilir).
- BB **bir bloka veya doğrudan bir yapıya** bağlıdır (apartmanda blok varsa bloka, villa tipinde direkt yapıya).
- **Tüm tablolarda `TenantId` (Guid) mandatory** — EF Core global query filter otomatik filtreleme yapacak.
- Cross-tenant data leak = **P0 incident** (kabul edilemez).

## 5. Kimlik (ID) Standartları

- **Internal ID:** Guid (PK, FK, audit kolonları).
- **External (UI/Export) ID:** ShortCode — 8 karakter alfanumerik (sistem üretir, unique).
- **Hangi alanda hangisi?** Guid asla URL'de, export'ta, email/SMS'te görünmez. Sadece ShortCode görünür.

## 6. Türk Hukuku Bağlamı (Mutlaka Uyulacak Kanunlar)

| Mevzuat | İlgili Konu |
|---------|-------------|
| **KMK (Kat Mülkiyeti Kanunu) 634** | m.18 (gider katılımı), m.20 (gecikme tavanı %5/ay + yedek akçe), m.32 (Yönetim Planı önceliği) |
| **TBK (Borçlar Kanunu) 6098** | m.88 (yasal faiz), m.101 (kısmi ödeme tahsis sırası) |
| **TTK (Ticaret Kanunu) 6102** | m.66-70, m.82 (defter 10 yıl saklama) |
| **KVKK 6698** | m.7 (silme/anonimleştirme), m.12 (veri güvenliği) |

Sistem bu kanunların gereklerini **kod düzeyinde** yerine getirmeli (mevzuat sabitleri kilit, kullanıcı değiştiremez).

## 7. Bu Sprint'in Hedefi

**Tek cümlede:** "Site yönetimleri altına Yapı Şeması (Ada/Parsel/Yapı/Blok/BB) ekle; ardından bu BB'lere ilk bütçe taslağı oluşturup aylık tahakkuk üretebileceğin temel yapıyı kur."

**Out-of-scope (Bu sprint'te yapılmayacaklar):**
- ❌ Kullanıcı tanımlı formül motoru (Wave 3+)
- ❌ Hesaplama bağımlılık grafiği (Wave 3+)
- ❌ Banka mutabakat (Wave 4+)
- ❌ Gelir yönetimi / Net İşletme Sonucu (Wave 3+)
- ❌ Yedek akçe (Wave 3+)
- ❌ Senaryo motoru (Wave 4+)
- ❌ Portföy analitiği (Wave 6+)
- ❌ eFatura/eDefter (Wave 7+)
- ❌ Çoklu para birimi (Wave 5+)
- ❌ Manuel düzeltme/override (Wave 3+ — şimdilik kayıt-yalnız yaklaşımı)

**In-scope (Bu sprint'te yapılacaklar):** `02-PHASE-CARDS.md` dosyasında listelendi.

## 8. Referans Dokümanlar (Aynı Klasörde)

| Dosya | Amaç | Ne zaman okumalısın? |
|-------|------|----------------------|
| `00-CONTEXT.md` | (Bu dosya) Proje bağlamı | Her oturum başı |
| `01-SDD-v1.0.md` | Master tasarım dokümanı (1960 satır, tüm modüller için referans) | Spesifik bir konuda derinleşmek gerekirse (örn: FR-15 detayı) |
| `02-PHASE-CARDS.md` | Bu sprint'in iş paketleri (faz kartları) | Sprint başında zorunlu |
| `03-DECISIONS-OPEN.md` | Karar bekleyen sorular | Kullanıcıdan karar almadan kodlama BAŞLAMA |
| `04-CLAUDE-CODE-PLAYBOOK.md` | Çalışma protokolün | Her oturum başı |

## 9. NotebookLM Entegrasyonu

Kullanıcı her faz sonunda NotebookLM'i bu dokümanlarla günceller. Eğer kullanıcı "NotebookLM'e sor" derse veya bağlam dışı bir bilgi gerekiyorsa, NotebookLM MCP üzerinden sorgulanabilir. **Ama önce bu klasördeki dokümanlarda ara — büyük ihtimalle cevap burada.**

## 10. Bu Doküman İçin Kritik Uyarı

> **Bu dokümanlar bağlayıcıdır. Kendi yorumlarınla çelişki yaratacak karar verme. Çelişki gördüğünde dur, kullanıcıya sor.**

> **Belirsizlik durumunda tahmin yürütme — `03-DECISIONS-OPEN.md`'ye yeni soru ekleyip kullanıcıyı uyar.**
