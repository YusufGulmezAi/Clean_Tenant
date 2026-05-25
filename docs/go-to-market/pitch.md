# CleanTenant — Tek Sayfa (Pitch One-Pager)

> Satış / yatırım / ortak görüşmelerinde kullanılacak özet. «...» işaretli yerleri
> kendi sayılarınla doldur (henüz net değilse kaba tahmin yaz, boş bırakma).

## Tek cümle
CleanTenant, Türkiye'deki site ve apartman yönetimi için **mevzuata tam uyumlu,
muhasebe-derinlikli, çok-kiracılı bir yönetim platformudur** — aidat tahakkuku,
tahsilat, gecikme ve resmi muhasebeyi tek yerde, denetlenebilir biçimde yönetir.

## Problem
- Site/apartman yönetiminde aidat takibi, tahsilat ve **muhasebe** dağınık ve hataya açık (Excel + ayrı programlar).
- Maliklere **şeffaflık** verilemiyor; gecikme/borç hesapları tartışma yaratıyor.
- Yönetici değişiminde kayıt/bilgi kayboluyor; denetim (audit) izi yok.
- Genel yazılımlar Türkiye mevzuatına (TDHP, KMK 634, KVKK) uymuyor.

## Çözüm
Tek platform: site kurulumu → bina/daire şeması → malik/kiracı → bütçe → **otomatik
tahakkuk** → **tahsilat sihirbazı** → **gecikme faizi** → **resmi yevmiye/muhasebe**
→ malik bazında **cari kart** ve raporlar. Her hareket denetlenebilir (audit).

## Neden şimdi
- Dijitalleşen yönetim şirketleri + maliklerin şeffaflık beklentisi artıyor.
- KVKK ve e-dönüşüm baskısı genel araçları yetersiz bırakıyor.
- «… kendi pazar gözlemini ekle …»

## Pazar (Türkiye)
- TAM: «… toplam site/apartman + yönetim şirketi sayısı tahmini …»
- SAM: «… profesyonel yönetilen + dijitale yatkın segment …»
- SOM (ilk hedef): «… ilk 12 ayda ulaşılabilir abone …»
- Gelir modeli: site/daire başına aylık abonelik (aidat hacmiyle ölçeklenir).

## Hendek (neden kopyalanması zor)
- **Muhasebe derinliği:** TDHP tek düzen hesap planı, çift taraflı yevmiye,
  TBK m.101 ödeme dağıtımı, gecikme faizi, ters kayıt/düzeltme — tipik rakipler
  bu kadar derine inmez.
- **Mevzuat uyumu** (TDHP/KVKK/KMK 634) = yerel giriş bariyeri.
- **Mimari olgunluk:** Clean Architecture, çok-kiracılı izolasyon, denetim izi,
  2FA — kurumsal müşteriye güven verir.

## Ürün (modüller)
Kimlik & yetki (scope-bazlı, 2FA) · Çok-kiracılı yönetim · Bina/daire şeması
(Excel içe aktarım) · Malik/Kiracı & Cari Kart · Bütçe · Tahakkuk (otomatik,
Hangfire) · Tahsilat sihirbazı + avans · Gecikme faizi · TDHP muhasebe/yevmiye ·
Audit & raporlama · Çok dil (TR/EN/AR/RU/DE).

## Traction
- Pilot/müşteri: «… kaç site, kaç daire, ne kadar süredir …»
- Kullanım/geri bildirim: «… somut örnek/referans cümlesi …»

## İş modeli
- Fiyat: «… daire başına ₺/ay veya site başına paket …»
- Birim ekonomi: «… kazanım maliyeti vs yaşam boyu değer (kaba) …»

## Ekip & risk
- Kurucu: «…» — şu an tek geliştirici (bus factor = 1).
- Risk azaltma: kapsamlı dokümantasyon + mimari haritalar + standart starter-kit
  → devredilebilirlik; aranıyor: «teknik ortak / ekip büyütme».

## Talep (görüşme tipine göre)
- **Yatırım:** «… ₺/$ tutar, ne için (ekip/satış), karşılığında …»
- **Ortak:** «… teknik/ticari ortak, rol ve katkı beklentisi …»
- **Satış/devir:** «… devir kapsamı (kod+IP+müşteri), beklenti …»
