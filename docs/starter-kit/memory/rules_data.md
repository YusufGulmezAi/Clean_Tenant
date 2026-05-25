---
name: rules_data
description: EF-yaz/Dapper-oku, hibrit multi-tenancy, 4 DB, UrlCode, UTC, migration akışı
metadata:
  type: reference
---

**Yazım EF Core, okuma Dapper** (karmaşık/rapor sorguları). 4 ayrı DB:
Catalog / Main / Log / Audit.

**Multi-tenancy:** `ITenantScoped` → global query filter ile `tenant_id`
izolasyonu (shared-DB default). Her tenant-scoped entity için **cross-tenant
sızıntı testi zorunlu** (bkz. [[rules_security]]).

**Migration akışı:** migration üret → `scripts/env-migrate` ile 4 DB'ye uygula →
ondan sonra app/seeder. Atlanırsa "relation does not exist".

Standartlar: `UrlCode` (9-char Base58) paylaşılabilir kimlik; tüm zaman UTC,
görüntüde yerel TZ; soft-delete + audit alanları `BaseEntity`'de; optimistic
concurrency (xmin/RowVersion); para alanları `HasPrecision(18,4)` + CHECK.
