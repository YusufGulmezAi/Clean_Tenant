---
name: Eğitici Mod ve Kodlama Öncesi Onay Zorunluluğu
description: Her geliştirme öncesi NE / NEDEN / NEDEN ŞİMDİ brifingi ver, brifingten sonra kodlamaya geçmeden önce mutlaka kullanıcı onayını al
type: feedback
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
**Kural:** Herhangi bir geliştirme adımına (kod yazma, dosya oluşturma, şema değişikliği, paket ekleme, infra değişikliği, deploy adımı) **başlamadan önce**:

1. Kısa bir **brifing** ver:
   - **NE** yapılacak — somut adımlar.
   - **NEDEN** yapılacak — bu işin proje hedefine katkısı.
   - **NEDEN ŞİMDİ** yapılacak — sıralama mantığı; neyi unblock ediyor, neye bağımlı.
2. Brifing sonrası **kullanıcının açık onayını bekle**. Onay gelmeden Write/Edit/Bash gibi geliştirme araçlarını çağırma.
3. Onay geldiğinde uygulamaya başla. Uygulama sırasında karşılaşılan **mikro kararlar** için durup soru sorma — makul kararı al, devam et, sonradan rapor et (kullanıcı `loop` modunda "soru sormadan çalış" demişti; bu kural mikro kararlar içindir, faz/iş başlangıç onayı için değil).
4. İş bittiğinde kısa bir **kapanış notu** ver: ne öğrenildi, ne unblock oldu, sıradaki adım ne.

**Neden:** Kullanıcı kendisini proje boyunca eğitmemi istiyor — bitmiş ürün değil, **mantığını da öğrenmek** istiyor. Ayrıca her büyük adımda **kontrolü elinde tutmak** istiyor; sürpriz kod görmek istemiyor.

**Nasıl uygulanır:**
- **Tetikleyen işler (onay gerekir):** yeni dosya, yeni şema, yeni feature, paket seçimi/eklenmesi, mimari karar, güvenlik kodu, deploy/infra adımı, faz başlangıcı.
- **Tetiklemeyen işler (onay gerekmez):** typo/format düzeltmesi, memory yazımı, yalnızca okuma (Read/Grep/Glob), kullanıcının zaten bu turda açıkça istediği küçük düzenlemeler.
- Brifing tonu **mentor/öğretici** olmalı: alternatifleri ve trade-off'ları da kısaca belirt.
- Çoklu adımlı işlerde her büyük adımdan önce mini-brifing tekrar edilebilir.
