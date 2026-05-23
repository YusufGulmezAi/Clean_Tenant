---
name: NotebookLM Kullanım Refleksi
description: Mimari/kural/karar sorularında önce NotebookLM'i sorgula, sonra koda bak
type: feedback
originSessionId: 61d8f930-a87d-4a38-a2cf-e582e02a5421
---
**Kural:** Kullanıcı CleanTenant ile ilgili "mimari karar nedir", "neden bu yaklaşım seçildi", "kural ne diyor", "Support Mode nasıl çalışır", "ScopeLevel sınırları ne", "v0.1'de neyi neden yaptık" gibi **karar/kural/tarihçe** tonunda sorular sorduğunda, önce ilgili NotebookLM notebook'unu sorgula (bkz. `reference_notebolm.md`), **sonra** kodla doğrula.

**Why:** NotebookLM'e 38 kaynak yüklendi (18 memory + 7 doküman + 13 Mermaid) ve CleanTenant-özel custom prompt set edildi (Türkçe + mimari sınırları zorunlu kıldı). RAG indeksli sorgu, kodu grep'lemekten daha verimli iki bilgiyi sağlar: (1) **karar arkası gerekçe** (kuralın "neden"i), (2) **kaynaklar arası sentez** (örn. rules_identity + Catalog ER + Mermaid 2FA flow birlikte). İlk 38 dosya 2026-05-19'da tek oturumda birlikte konuşulup karar verildi.

**How to apply:**
- **"Mimari/karar/kural/gerekçe/tarihçe" tonunda soru →** önce `notebook_query` (uygun notebook) → sonra gerekirse kod doğrulama (Read/Grep)
- **"Kod davranışı/dosya nerede/şu method ne yapıyor" sorusu →** NotebookLM'i atla, doğrudan Read/Grep/Glob kullan (NotebookLM koddaki son hali bilmez, yalnızca yüklenen snapshot'ı bilir)
- **Notebook seçimi:** kural/davranış/proje durumu → Notebook 1 · mimari/sequence/ER/faz → Notebook 2 · ikisini birden gerektiren senaryo (örn. "kural X'in mimaride karşılığı") → `cross_notebook_query`
- **Notebook ID'leri:** her zaman `reference_notebooklm.md`'den oku — `notebook_list` çağrısından tasarruf
- **Cevap formatı:** NotebookLM döndüğünde önce ilgili kaynağa atıf ver (örn. "rules_identity.md'ye göre..."), sonra Türkçe özetle, somut öneri ver
- **Hata/timeout:** Kaynak hala indeksleniyor olabilir → kodla fallback yap, kullanıcıya "NotebookLM henüz cevap vermedi, koddan yorumladım" diye not düş
- **Stale risk:** NotebookLM kaynakları son `source_add` zamanındaki içerik. Eğer cevap mevcut kodla çelişiyorsa kodu kabul et, kullanıcıya stale kaynak ihtimalini söyle, gerekirse `source_delete` + yeniden `source_add` öner
