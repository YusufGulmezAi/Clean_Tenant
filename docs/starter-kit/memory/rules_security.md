---
name: rules_security
description: Scope-bazlı yetki, 2FA, secret/vault, PII, KVKK, tenant izolasyon testi, güvenlik kapısı
metadata:
  type: reference
---

Kimlik + **scope-bazlı yetki** (System/Tenant/Company/Unit); System kullanıcıları
için 2FA zorunlu. Her uç `[RequirePermission]` ile korunur; negatif yetki testi yazılır.

Secret'lar repoda değil, prod'da vault; şifreleme at-rest + in-transit. **PII**
alanları `[Sensitive]` ile etiketlenir ve audit'te otomatik redakte edilir.

**KVKK:** veri saklama/silme politikası, açık rıza, veri sahibi talepleri (DSAR).

Her faz kapanışında **güvenlik kapısı**: tenant izolasyon testleri + SAST/kod
incelemesi + bağımlılık (SCA) + secret taraması + tehdit gözden geçirme; bulgular
kapanmadan faz kapanmaz. "Banka-seviyesi" bir iddia değil, kanıt zinciridir.
Bkz. [[rules_testing]].
