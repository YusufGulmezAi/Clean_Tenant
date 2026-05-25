# CleanTenant — Güvenlik & Uyumluluk Whitepaper

> Due-diligence / müşteri güven dokümanı. **Dürüstlük ilkesi:** her madde gerçek
> duruma göre işaretli — ✅ Mevcut · 🟡 Kısmi/devam · ⬜ Yol haritası. Abartılı
> iddia DD'de geri teper; dürüst "yol haritası" güven yaratır.
>
> ⚠️ Aşağıdaki işaretler ilk taslaktır; her birini koddan/altyapıdan **doğrula**,
> "kanıt" sütununa gerçek artefaktı (test adı, rapor, config) yaz.

## 1. Kimlik & Erişim
| Özellik | Durum | Kanıt / Not |
|---|---|---|
| Parola politikası (min 8 + complexity) + lockout | ✅ | Identity config; tenant-bazlı lockout politikası |
| 2FA (TOTP + recovery code), System için zorunlu | ✅ | Recovery 12 kod; enrollment akışı |
| Scope-bazlı yetki (System/Tenant/Company/Unit) | ✅ | `[RequirePermission]` + AuthorizationBehavior + izin kataloğu (sayfa-bazlı) |
| Çok-bağlamlı oturum (tab başına bağlam/token) | ✅ | Bağlam başına token; canlı izin tazeleme |
| Destek oturumu / impersonation (audit'li) | ✅ | Yazma-aksiyon sayacı + audit işaretleme |

## 2. Çok-Kiracılı İzolasyon
| Özellik | Durum | Kanıt / Not |
|---|---|---|
| `ITenantScoped` global query filter (tenant_id) | ✅ | Shared-DB modunda otomatik filtre |
| Cross-tenant izolasyon test paketi | 🟡 | DB-kısıt testleri var; **kapsamlı izolasyon suite'i genişletilmeli** |
| Dedicated-DB tenant yolu | 🟡 | Mimari hazır; runtime resolver ertelendi |

## 3. Veri Koruması
| Özellik | Durum | Kanıt / Not |
|---|---|---|
| In-transit şifreleme (TLS) | 🟡 | Prod TLS terminasyonu — doğrula/belgele |
| At-rest şifreleme (disk/DB) | 🟡/⬜ | Altyapıya bağlı — netleştir |
| PII etiketleme + audit redaksiyonu | ✅ | `[Sensitive]` + PII alan listesi → audit'te `[REDACTED]` |
| Secret yönetimi (repoda yok; prod vault) | 🟡 | Per-env secret; prod vault planı netleştir |

## 4. Denetim & İzlenebilirlik
| Özellik | Durum | Kanıt / Not |
|---|---|---|
| Ayrı Audit DB (kim/ne/ne zaman + delta) | ✅ | FullAuditInterceptor; Dapper batch insert |
| Yapısal loglama (Serilog → Log DB) | ✅ | Ayrı Log DB |
| Soft-delete + optimistic concurrency (xmin) | ✅ | BaseEntity |
| Dağıtık izleme (OpenTelemetry) | ⬜ | Yol haritası |

## 5. Uygulama Güvenliği
| Özellik | Durum | Kanıt / Not |
|---|---|---|
| Girdi doğrulama (FluentValidation) | ✅ | ValidationBehavior |
| DB-katmanı savunma (CHECK/unique/FK) | ✅ | Migration'larda |
| Rate limiting / brute-force | 🟡 | Lockout var; uç-bazlı rate limit netleştir |
| Bağımlılık taraması (SCA) CI'da | ⬜ | `dotnet list package --vulnerable` CI'a ekle |
| Secret-scan (gitleaks) CI'da | ⬜ | Yol haritası |
| Sızma testi (pentest) | ⬜ | Henüz yok — planla (DD için değerli) |
| OWASP ASVS / ISO 27001 / SOC 2 | ⬜ | Yol haritası |

## 6. Uyumluluk (Türkiye)
| Düzenleme | Durum | Not |
|---|---|---|
| KVKK — veri envanteri + saklama/silme + rıza + DSAR | 🟡 | Audit/PII altyapısı var; **resmi DSAR + retention süreci** belgelenmeli |
| TDHP (tek düzen hesap planı) | ✅ | Otomatik provizyon (275 kod), çift taraflı yevmiye |
| KMK 634 (kat mülkiyeti) | 🟡 | Domain uyumlu; uyum notu yaz |
| e-Fatura / e-Arşiv / e-Defter | ⬜/🟡 | Kapsamdaysa GİB entegrasyonu netleştir |

## 7. Süreklilik
| Özellik | Durum | Not |
|---|---|---|
| Yedekleme (RPO) | 🟡 | Politika + otomasyon belgele |
| DR / restore tatbikatı (RTO) | ⬜ | Planla + tarih kaydet |

## 8. Açık Riskler & Yol Haritası (dürüst liste)
- Pentest + SCA/secret-scan CI'a alınacak.
- Cross-tenant izolasyon test kapsamı genişletilecek.
- KVKK DSAR/retention süreci resmileştirilecek.
- (Teknik) Domain event dağıtımı (Outbox) bağlanacak — şu an event üretiliyor,
  dağıtım altyapısı eklenecek.
- Bus factor = 1 → dokümantasyon + ekip büyütme ile azaltılıyor.
