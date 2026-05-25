---
name: rules_architecture
description: Clean Architecture katmanları, CQRS+MediatR, bağımlılık yönü ve zengin domain kuralları
metadata:
  type: reference
---

Clean Architecture: `Core` (Domain/Application/SharedKernel) → `Infrastructure`
(ilgi alanına göre ayrı projeler) → `Presentation`. **Bağımlılık yönü dışarıdan
içeriye**; Core hiçbir Infrastructure'a bakmaz — bu kural **NetArchTest** ile
otomatik denetlenir.

İş davranışları **CQRS + MediatR**: Command/Query + Handler + Validator + Result,
`Features/<Area>/<UseCase>/` dikey diliminde. Cross-cutting concern'ler pipeline
behavior'da (auth/validation/cache/log/idempotency), handler'da tekrarlanmaz.

**Domain zengindir, anemik değil:** private setter + statik fabrika + davranış
metotları; invariant aggregate'te korunur; koleksiyonlar salt-okunur; domain
event'ler üretilir ve Outbox ile dağıtılır. Bkz. [[rules_data]], [[rules_testing]].
