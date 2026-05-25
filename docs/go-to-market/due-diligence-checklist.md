# CleanTenant — Due-Diligence Hazırlık Kontrol Listesi

> Bir alıcı/yatırımcı/teknik danışman projeye baktığında neye bakar? Bunları
> önceden hazırlamak hem değeri yükseltir hem süreci hızlandırır. Durum: ✅ / 🟡 / ⬜.

## 1. Kod & Repo
- [ ] README ile **<30 dk kurulum** (yeni biri sıfırdan ayağa kaldırabiliyor)
- [ ] `git` geçmişi temiz, anlamlı conventional commit'ler
- [ ] Repoda **secret yok** (gitleaks ile doğrulanmış)
- [ ] Branch/worktree düzeni belgeli (paralel çalışma koordinasyonu)
- [ ] Ölü kod / yarım bırakılmış dal yok (ör. açık refactor branch'leri kapat)

## 2. Test & Kalite
- [ ] **Tüm testler yeşil** (CI çıktısıyla kanıtlı) — ⚠️ parked `Senaryo11` kapatılmalı
- [ ] Test piramidi dolu (domain/app/integration/UI/api)
- [ ] Mimari testler (katman ihlali = 0)
- [ ] Coverage raporu (Domain/Application hedef ≥ %80)
- [ ] CI/CD pipeline (build+test+SCA+secret-scan) çalışır halde

## 3. Güvenlik
- [ ] Güvenlik whitepaper (bkz. `security-whitepaper.md`) dolu ve **dürüst**
- [ ] Tenant izolasyon testleri mevcut ve yeşil
- [ ] 2FA / yetki / audit kanıtları gösterilebilir
- [ ] (varsa) pentest raporu + kapatılan bulgular
- [ ] Bağımlılıklarda bilinen kritik CVE yok

## 4. Uyumluluk & Hukuk
- [ ] KVKK: veri envanteri + aydınlatma + saklama/silme + DSAR süreci
- [ ] Kullanım şartları + gizlilik politikası metinleri
- [ ] TDHP/KMK 634/(varsa) e-fatura uyum durumu yazılı
- [ ] Veri işleme sözleşmeleri (müşteri/işleyen) şablonu

## 5. Fikri Mülkiyet (IP)
- [ ] Kodun sahipliği net (tek geliştirici → temiz; dış katkı varsa sözleşme)
- [ ] Üçüncü parti **lisans uyumu** (paketlerin lisansları ticari kullanıma uygun)
- [ ] Marka/isim (CleanTenant) tescil durumu
- [ ] Açık kaynak yükümlülüğü (copyleft) ihlali yok

## 6. Dokümantasyon
- [ ] Mimari haritalar (Mermaid+PNG, faz bazlı) güncel
- [ ] ADR'ler (önemli kararların gerekçesi)
- [ ] Onboarding + wiki
- [ ] Starter-kit/standartlar (süreç olgunluğu kanıtı) — ✅ mevcut

## 7. İş & Finans
- [ ] Net hedef müşteri + değer önermesi (`pitch.md`)
- [ ] Fiyat modeli + birim ekonomi
- [ ] Pazar büyüklüğü (TAM/SAM/SOM) tahmini + kaynak
- [ ] Traction: pilot/müşteri sayısı, kullanım, referans
- [ ] Gelir/gider tablosu (varsa), runway

## 8. Operasyon & Süreklilik
- [ ] Ortam mimarisi (Dev/Test/Demo/Prod) belgeli
- [ ] Yedekleme + DR planı (RPO/RTO) ve son tatbikat tarihi
- [ ] İzleme/alerting (health check, log, metric)
- [ ] **Bus factor** azaltma planı (dokümantasyon + ekip/ortak)

---

## En kritik 5 (önce bunlar)
1. Testleri yeşile al (parked test dahil).
2. Çalışan demo + seed (`demo-plan.md`).
3. Güvenlik whitepaper'ı dürüstçe doldur (`security-whitepaper.md`).
4. Pitch one-pager netleştir (`pitch.md`).
5. 1-3 pilot / referans.
