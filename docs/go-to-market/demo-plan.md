# CleanTenant — Demo Senaryosu & Seed Planı

> Amaç: 5 dakikada "para girişinden resmi muhasebeye" tam akışı, gerçekçi Türkçe
> veriyle, tek nefeste göstermek. Alıcı/yatırımcı kodu değil bunu görür.

## Demo öncesi kontrol (her seferinde)
- [ ] Çalışan app'i kapat, temiz başlat (`scripts/env-run.ps1 -Env Demo`).
- [ ] Demo tenant'ı seed'le (aşağıdaki seed planı).
- [ ] **Yedek ekran kaydı** hazır (canlı demo çökerse devreye girer).
- [ ] Test kullanıcısı + 2FA hazır (giriş akışını da gösterebilmek için).
- [ ] Tarayıcı zoom/çözünürlük sunum için ayarlı.

## Seed planı (gerçekçi ama küçük)
- **1 tenant** (yönetim şirketi) + **1 site** ("Papatya Sitesi").
- Bina şeması: 1 ada/parsel → 2 blok (A, B) → ~12 daire (Excel içe aktarımı da
  *canlı* gösterilebilir → "şema dakikada kuruluyor").
- **Malik + kiracı:** birkaç dairede malik, 1-2 dairede kiracı (sorumluluk paylaşımı).
- **Mali yıl + dönem** açık; hesap planı (TDHP) otomatik provizyonlanmış.
- **Bütçe:** yıllık ₺«…», eşit/m²'ye göre dağıtım.
- **Tahakkuk:** 2-3 ay otomatik üretilmiş (biri gecikmiş olsun).
- **Tahsilat:** 1 tam ödeme + **1 fazla ödeme (avans)** + 1 dairede ödenmemiş borç.
- **Gecikme:** gecikmiş tahakkuğa faiz işlemiş.

## 5 dakikalık akış (ne söyle / ne tıkla / ne kanıtlar)

| Dk | Ekran / Aksiyon | Söylenecek | Kanıtladığı |
|---|---|---|---|
| 0:00 | Giriş + 2FA | "Sistem kullanıcılarında 2FA zorunlu." | Güvenlik ciddiyeti |
| 0:30 | Site + bina şeması (Excel import) | "Şema Excel'den dakikada kuruluyor." | Hız / onboarding kolaylığı |
| 1:15 | Malik/Kiracı + Cari Kart | "Kişi merkezli; malik–kiracı sorumluluğu net." | Domain derinliği |
| 2:00 | Bütçe → Tahakkuk | "Aidat bütçeden otomatik tahakkuk ediyor." | Otomasyon |
| 2:45 | **Tahsilat sihirbazı** (fazla ödeme→avans) | "Ödeme en eski borca dağıtılıyor (TBK m.101); fazlası avans." | Muhasebe doğruluğu |
| 3:30 | Gecikme faizi | "Geciken borca faiz otomatik." | Mevzuat/iş kuralı |
| 4:00 | **Yevmiye fişi** (otomatik) | "Her hareket arka planda resmi yevmiyeye düşüyor (TDHP)." | Asıl hendek: muhasebe |
| 4:30 | Cari kart / KPI / rapor | "Malik tek ekranda borç/avans/ödeme görüyor; dışa aktarılabilir." | Şeffaflık |
| 5:00 | (ops.) Audit izi | "Kim ne yaptı, hepsi denetlenebilir." | Kurumsal güven |

## Anlatı yayı
"Para girer → doğru borca dağılır → gecikirse faizlenir → otomatik resmi muhasebeye
düşer → malik şeffaf görür → her şey denetlenir." Tek cümlede: **kaostan denetlenebilir
düzene.**

## Dikkat
- Hata riskini azalt: demo verisi sabit, akış prova edilmiş olsun.
- Teknik jargona girme; **faydayı** göster (zaman tasarrufu, şeffaflık, uyum).
- Soru gelirse mimari/güvenlik whitepaper'ı yedekte tut.
