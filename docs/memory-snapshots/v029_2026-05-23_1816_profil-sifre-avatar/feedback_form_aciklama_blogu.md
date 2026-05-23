---
name: Form Açıklama Bloğu (UI Form Tasarımı Kuralı)
description: Her form sayfasının üstünde, onay alınana kadar görünür kalan bir açıklama bloğu olmalı — alan bilgileri, sayfa mantığı, ilişkili formlar. Onaylandığında gizlenir.
type: feedback
originSessionId: 92894288-df01-4cff-89f3-6fae0e457383
---
UI tasarımında, özellikle form sayfalarında: form için kullanıcı onayı alınana kadar **formun üstünde bir açıklama bloğu (information panel)** bulunmalı. İçerik:

1. **Sayfanın mantığı** — bu form ne işe yarar, hangi süreçte yer alır?
2. **Alan bazlı açıklamalar** — hangi alana hangi tür bilgi girilir, format/zorunluluk
3. **İlişkili formlar** — bu formdan önce/sonra hangi sayfalara gidilir, hangi varlıkları tetikler

Kullanıcı **"Formu onayladım / Anladım"** dediği anda açıklama bloğu **gizlenir**; yalnız form kalır. Onay durumu kullanıcı/oturum bazlı (localStorage veya kullanıcı tercihi — uygulama tarafında karar verilecek).

**Why:** Yeni formlar tasarlanırken kullanıcı, formun nasıl çalışacağını anladıktan SONRA onay verebilsin. Açıklama panelı, formu üretim modunda tekrar gördüğünde gereksiz yer kaplamamalı. Kullanıcı 2026-05-22'de bu kuralı verdi; Bütçe fazında (FAZ 5+ UI'ları) ve sonraki tüm form sayfalarında uygulanmalı.

**How to apply:**
- Her form sayfasının üstünde `MudExpansionPanel` veya `MudAlert Severity="Severity.Info"` ile açıklama bloğu
- "Anladım, formu onaylıyorum" / "Açıklamayı tekrar göster" toggle butonu
- State persist: `IJSRuntime` ile localStorage, veya kullanıcı profili tercihine bağlı `IUserPreferenceService` (ileride genişletilebilir)
- Açıklama içeriği lokalize edilebilir (i18n key'ler)
- Bütçe modülü özelinde: Bütçe Oluştur / Bütçe Kalemi Ekle / Tahakkuk Üret / Tahsilat Kaydet / Muafiyet Tanımla / Katılım Grubu Düzenle gibi tüm formlara uygulanmalı
- Açıklama panelı tasarımı (görsel detay, ikon, renk) için UI Tasarımı Öncesi Danışma kuralı işler — tek başına karar verme
