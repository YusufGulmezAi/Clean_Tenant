---
name: UI Tasarımı Öncesi Mutlaka Danış
description: Herhangi bir görsel/UX kararı (layout, renk, tema, tipografi, navigasyon, komponent seçimi) öncesinde kullanıcıyla konuş; tek başına UI kararı verme
type: feedback
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
**Kural:** ManagementApp, PortalApp, MobilApp ya da herhangi bir görsel arayüz dosyasında **görsel veya UX kararı** içeren bir değişiklik yapmadan **önce** kullanıcıya danışılır. Tek başına UI kararı verilmez.

**Kapsam (danışılması gerekenler):**
- Genel **tasarım dili** (modern flat, glassmorphism, neumorphism, material, fluent, vb.)
- **Renk paleti** (primary, secondary, accent, neutral; dark/light tema)
- **Tipografi** (font ailesi, hiyerarşi, boyutlar)
- **Tema sistemi** (MudBlazor theme, Razor Components stil, MAUI XAML resource'ları)
- **Layout** kalıpları (sidebar, top-nav, dashboard grid, formlar)
- **Komponent seçimleri** (MudBlazor vs. başka kütüphane, custom komponent yapımı)
- **İkonografi** (MudIcons, Material Symbols, Lucide, FontAwesome vb. seçimi)
- **Görsel yoğunlukta sayfa / ekran tasarımları** (dashboard, listeleme, detay, form ekranları)
- **Animation / micro-interaction** kararları
- **Responsive davranış** kararları (breakpoint'ler, mobil davranış)
- **Erişilebilirlik** (a11y) düzeyi hedefleri

**Kapsam dışı (sormadan ilerleyebilir):**
- Backend kodu, DTO, validator, handler, schema (görsel değil)
- CSS class isimlendirmesi, küçük yerleşim mikro-düzeltmeleri (zaten kararlaştırılmış stilin tutarlı uygulanması)
- Tipo, semantik hata düzeltmesi
- Test ve infra dosyaları

**Neden:** Kullanıcı projenin görünüş ve hissini doğrudan şekillendirmek istiyor. Tek başına seçimler yapılırsa sonradan büyük rework olur; baştan birlikte konuşulursa hem zaman tasarrufu hem net görsel tutarlılık.

**Nasıl uygulanır:**
- UI işi başlamadan önce eğitici mod brifingi'nin **görsel kararlar** bölümünde alternatifleri (örnek görseller, tasarım dili seçenekleri, renk paleti seçenekleri) sun.
- Mümkünse 2-3 alternatif öner, trade-off'ları açıkla, kullanıcı seçsin.
- Seçim yapıldıktan sonra **tema sistemini bir kez merkezi olarak kur** (tek tema dosyası / değişken seti); her ekranda yeniden karar verme.
- Faz 1 başlangıcında "tasarım sistemi / design tokens" oturumu özel olarak planlanır; bu Faz 1 alt fazlarının ilkidir.
- Faz 0'da yalnız Blazor / MAUI **şablon kodu** vardır (hello-world); buna dokunulmaz — gerçek UI işi Faz 1'de başlar.
