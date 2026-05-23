---
name: NotebookLM Güncelleme Disiplini
description: NotebookLM kaynakları güncellenmeden önce mutlaka /nlm-refresh çağır; bu komut git temizlik kontrolünü zorunlu kılar
type: feedback
originSessionId: 7e3ff30c-d6de-45ab-976e-8a70ec68b74b
---
**Kural:** CleanTenant NotebookLM notebook'larını (Kurallar & Karar Bağlamı, Mimari & Faz Tarihçesi) güncelleme gerekiyorsa, **doğrudan `source_delete` + `source_add` çağırma**. Önce `/nlm-refresh` slash command'ını kullan. Bu komut sırayla: (1) `git status` + `origin/main` karşılaştırması yapar, (2) kirli/push'lanmamış varsa durur, (3) temizse değişen dosyaları kullanıcıya onaylatıp güncellemeyi başlatır.

**Why:** NotebookLM kaynakları local dosyaların **anlık snapshot'ı**. Eğer commit edilmemiş ya da remote'a push edilmemiş içerik varken NotebookLM'i tazelersek, notebook git geçmişinde bulunmayan bir hibrit duruma kilitlenir — gelecekte "şu kural nereden geldi" diye git blame yapan biri (kullanıcı veya başka geliştirici) kaynak bulamaz. NotebookLM ile remote main her zaman senkron olmalı.

**How to apply:**
- **NotebookLM güncelleme tonunda istek →** ("notebook'u tazele", "kaynakları güncelle", "feedback X'i NotebookLM'e ekle") doğrudan MCP source toolarını çağırma; **önce `/nlm-refresh`'i çalıştır** ya da kullanıcıya çağırmasını öner.
- **`/nlm-refresh` "DUR" derse →** kullanıcıya commit/push komutlarını otomatik çalıştırma; sadece sorunu özetle (uncommitted/unpushed listesi) ve kullanıcının kendi temizlemesini bekle. Sonra `/nlm-refresh` yeniden çağrılır.
- **Memory dosyası eklediysen/güncellediysen** ve bu memory NotebookLM Notebook 1'in kaynak listesindeyse (bkz. `reference_notebooklm.md`), iş bitince kullanıcıya hatırlat: "Bu memory NotebookLM'e de yansıtılmalı, `/nlm-refresh` çalıştır."
- **Tek-seferlik istisna yok:** "Bu sefer hızlıca commit etmeden ekleyelim" denirse bile reddet; NotebookLM kaynakları her zaman remote main ile eşit olmalı.
