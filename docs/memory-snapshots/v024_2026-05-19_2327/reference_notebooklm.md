---
name: NotebookLM CleanTenant Notebook'ları
description: CleanTenant projesi için NotebookLM kütüphanesinde oluşturulmuş iki notebook'un ID, URL ve amaç eşlemesi
type: reference
originSessionId: 61d8f930-a87d-4a38-a2cf-e582e02a5421
---
CleanTenant projesi için NotebookLM kütüphanesinde **iki notebook** bulunur. Her ikisinde de CleanTenant-özel custom prompt (Türkçe yanıt + mimari sınırlar) set edilmiştir.

## Notebook 1 — Kurallar & Karar Bağlamı

- **ID:** `080c1318-bf21-4d50-b7fe-4862b0cf2c5a`
- **URL:** https://notebooklm.google.com/notebook/080c1318-bf21-4d50-b7fe-4862b0cf2c5a
- **İçerik:** 21 memory dosyası — `MEMORY.md` + 7 `feedback_*.md` + 9 `rules_*.md` + `project_overview.md` + `project_current_state.md` + `user_profile.md` + `reference_notebooklm.md`
- **Kullanım:** Kural, davranış, karar gerekçesi, proje durumu sorgularında.

## Notebook 2 — Mimari & Faz Tarihçesi

- **ID:** `0ff7b233-6076-45ff-a0d0-1b9817ca013f`
- **URL:** https://notebooklm.google.com/notebook/0ff7b233-6076-45ff-a0d0-1b9817ca013f
- **İçerik:** 7 markdown (proje `README.md`, `docs/00-discovery/vision.md`, `docs/feature-ideas/graph-explorer.md`, v0.1 `README.md` + `CHANGELOG.md` + `v0.1-FINAL-ARCHITECTURE-MAP.md`, v0.2 `CHANGELOG.md`) + 13 Mermaid diyagramı (text source olarak — `.mmd` uzantısı reddedildiği için içerik kopyalandı)
- **Kullanım:** Mimari karar, sequence/flow, ER şeması, faz tarihçesi sorgularında.

## Kullanılan MCP toolları

- `mcp__notebooklm-mcp__notebook_query` — tek notebook üzerinde RAG sorgusu
- `mcp__notebooklm-mcp__cross_notebook_query` — her iki notebook'a birlikte sorgu
- `mcp__notebooklm-mcp__source_add` / `source_delete` — kaynak ekle/sil (update için sil+ekle)
- `mcp__notebooklm-mcp__chat_configure` — custom prompt set
- `mcp__notebooklm-mcp__notebook_describe` — AI özet (aynı zamanda hazırlık göstergesi)

## Önemli notlar

- **`.mmd` uzantısı yüklenmiyor:** Mermaid kaynakları text source olarak konulmalı.
- **In-place update yok:** Dosya değişince source_delete + source_add zorunlu.
- **Hazırlık göstergesi yok:** `notebook_describe` başarılı dönerse indeksleme tamam demektir.
- **Auth:** `nlm login` CLI üzerinden Google OAuth. Token kalıcı.

İlk oluşturma + custom prompt yapılandırması: **2026-05-19**.

Son tazeleme:
- **Notebook 1:** 2026-05-19 — `MEMORY.md` güncel hali yeniden yüklendi; `feedback_nlm_guncelleme_disiplini.md` ve `reference_notebooklm.md` (sayım + tazeleme notu) yenilendi.
- **Notebook 2:** 2026-05-19 (ilk oluşturmadan beri değişiklik yapılmadı — bugünkü commit'lerde Notebook 2 kapsamındaki docs/* veya `README.md` değişmedi).
