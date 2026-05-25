---
name: ui-page
description: >
  Blazor (MudBlazor) sayfası/formu/listesi/dashboard'u üretir — projenin UI
  konvansiyonlarıyla: ÖNCE kullanıcıya görsel/UX danışma, her sayfada "ne yapar +
  nasıl kullanılır" açıklama bloğu, tema uyumu, lokalizasyon, scope/izin guard'ı.
  "Yeni sayfa/form/liste/detay/dashboard ekle", "şu ekranı yap", "UI oluştur" gibi
  HER istekte kullan. Görsel karar gerektiren her işte bu skill devreye girmeli.
---

# ui-page — Blazor Sayfa Üretici

Neden: tutarlı, kullanıcı-dostu UI satışın vitrini. İki kural kırılmaz:
(1) görsel kararı tek başına verme, (2) her sayfa kendini açıklar.

## ADIM 0 — DUR ve danış (zorunlu)
Layout, renk, tema, komponent seçimi, akış gibi görsel/UX kararı varsa ÖNCE
kullanıcıya seçenekleri sun, onay al. Tek başına UI kararı VERME. (Birden çok
makul tasarım varsa 2-3 seçenek + öneri sun.)

## Açıklama bloğu (her sayfada)
Sayfanın üstünde "**Bu sayfa ne yapar + nasıl kullanılır**" bloğu:
- Liste/detay/dashboard: kalıcı (her zaman görünür, kapatılabilir).
- Form: kullanıcı onayladıktan/işi öğrendikten sonra gizlenebilir (tercih saklanır).

```razor
<MudAlert Severity="Severity.Info" Class="mb-4" ShowCloseIcon="true"
          CloseIconClicked="HideHelp" hidden="@_helpHidden">
    <b>Bu sayfa:</b> ... ne yapar ...<br/>
    <b>Nasıl:</b> ... adım adım kullanım ...
</MudAlert>
```

## Kurallar
- **Tema:** projenin `MudTheme`'lerini kullan; ham renk/inline stil GÖMME.
- **Lokalizasyon:** metinler `IStringLocalizer`'dan; sabit Türkçe string gömme
  (RTL/Arapça desteği için).
- **Yetki:** sayfa/aksiyon `[Authorize]` + izin koduyla korunur; scope'a uygun.
- **DbContext:** UI component'leri `IDbContextFactory`'den TAZE context alır
  (aynı circuit'te paralel kullanım "second operation" hatası verir).
- **Erişilebilirlik:** etiketler, klavye, kontrast (WCAG AA hedefi).

## Üreteceğin dosya
`src/Presentation/<App>.ManagementApp/Components/Pages/<Area>/<Page>.razor`
(+ gerekiyorsa `.razor.cs` code-behind, validator, dialog).

## Bitti tanımı
- [ ] Görsel kararlar kullanıcıyla onaylandı
- [ ] Açıklama bloğu var (form ise gizlenebilir)
- [ ] Tema + lokalizasyon + izin guard'ı uygulandı
- [ ] DbContext factory'den alınıyor
