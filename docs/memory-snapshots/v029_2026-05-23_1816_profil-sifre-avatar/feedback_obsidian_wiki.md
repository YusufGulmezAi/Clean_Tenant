---
name: Obsidian Wiki + Claude Code Çalışma Düzeni
description: Obsidian vault'u hafıza, Claude Code akıl yürütme motoru olarak birlikte kullanma yaklaşımı
type: feedback
originSessionId: 7cb8d813-6a13-4e31-acce-05d41c0e01a9
---
Proje için Obsidian vault'u `d:\Projeler\CleanTenant\docs\wiki\CleanTenant\` altında kuruldu.
Claude Code ve Obsidian birlikte iki farklı rol üstlenir: Obsidian hafıza/bağlantı/geçmiş, Claude Code akıl yürütme/kod/analiz.

**Why:** Kullanıcı API anahtarı olmadığı için Obsidian eklentisi (Copilot) yerine manuel yöntemi tercih etti. Bu yöntemde vault notları Claude Code'a kopyalanarak bağlam sağlanır.

**How to apply:**
- Vault yapısı: `_prompts/`, `Fazlar/`, `Kararlar/` (ADR'ler), `Mimari/`, `Specs/`, `Keşif/`, `Fikirler/`
- Yeni mimari karar → önce Obsidian'da ADR notu, sonra kod
- Vault notu bağlamıyla soru sorulursa: notu oku + bağlam olarak ekle
- Faz kapanışında vault'u da güncelle (CHANGELOG, mimari harita linki)
- Eğitim notu: `Fazlar/v0.0/Obsidian ve Claude Code Egitimi.md`
