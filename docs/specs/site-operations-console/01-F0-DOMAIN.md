# F0 — Cari Kart Temel Domain (Onaylı Tasarım)

> Durum: **ONAYLANDI** (2026-05-24). Kapsam: temel (foundation) domain — Kişi (Malik/Kiracı/İletişim) +
> Unit-tenure + Hissedar + tahakkuk sorumluluğu çözümleme (gün-bazlı proration) + BB-merkezli cari ledger.
> Üst bağlam: [00-MASTER-PLAN.md](00-MASTER-PLAN.md). Diğer modüller (Sözleşme, Abonelik, Araç, Talep,
> İcra, Dosya, Bildirim) sonraki fazlarda; her biri `Party`/`Unit` FK ile bu F0'a bağlanır.

## Kesinleşmiş kararlar
1. **CleanTenant'a entegre** — yeni feature klasörleri; mevcut accrual/collection/audit yeniden kullanılır. ShortCode = mevcut `IHasUrlCode` (9-char Base58). Yeni entity'ler Main DB, Company-scoped veri + Tenant-scoped yetki (Budget deseni).
2. **Görsel: hibrit** — sayfa kabuğu (header card, 240px sidebar, KPI, wizard) bespoke CSS (design-lock'a sadık) + form/tablo/dialog MudBlazor. Nordic Minimal tema preset.
3. **Cari = BB-merkezli + sorumlu kişi** — borç BB'de; kart üstte BB'nin borcunu gösterir, sorumlu kişi başlıkta.
4. **Sorumluluk modu bütçe düzeyinde:** `TenantThenOwner` (tahakkukta kiracı varsa kiracı, yoksa malik) | `OwnerOnly` (her zaman aktif malik). Kalem-bazlı bölüşüm YOK (tüm tahakkuk tek tarafa, gün-bazlı bölünebilir).

## Entity'ler (`Domain/Tenant/Parties/`)
- **`Party`** (Individual|Legal) `BaseEntity, IAggregateRoot, ITenantScoped, IHasUrlCode`: `CompanyId`, `UrlCode`, `Kind`, `FullName`/`FirstName?`/`LastName?`/`TradeName?`, `Tckn?`/`Vkn?`/`Phone?` (`[Sensitive]`), `BirthDate?`, `Email?`, `AddressLine?`, `Notes?`, `TagsJson?`, KVKK (`KvkkConsentGiven`/`KvkkConsentAt?`/`KvkkConsentChannel?`), `LinkedUserId?` (portal, F0'da kullanılmaz). `User` login kimliğinden AYRI.
- **`UnitOwnership`** (Malik + Hissedar) `BaseEntity, ITenantScoped`: `UnitId`, `PartyId`, `StartDate`, `EndDate?`, `SharePercent decimal(5,2)`, `IsJointAndSeveral bool`, `Notes?`. CHECK: `EndDate IS NULL OR EndDate>=StartDate`, `SharePercent>0 AND <=100`.
- **`UnitTenancy`** (Kiracı): `UnitId`, `PartyId`, `StartDate`, `EndDate?`, `Notes?`. CHECK tarih. Tek aktif kiracı varsayımı (uygulama seviyesi).
- **`UnitContact`** (İletişim kişisi): `UnitId`, `PartyId`, `ContactRole` enum (`PropertyManager/FamilyMember/Lawyer/Heir/Other`), `StartDate`, `EndDate?`, `Notes?`. **Borçlu olmaz** (sorumluluk çözümlemesine girmez).
- Mevcut `Unit` **değişmez** (saf fiziksel).

## Sorumluluk modeli — gün-bazlı bölünebilir (proration F0'a dahil)
- **`AccrualResponsibilitySplit`** (child, `Domain/Tenant/Parties/`): `AccrualDetailId` (FK→AccrualDetail, Cascade), `PartyId` (FK→Party, Restrict), `Kind` (`Owner|Tenant`), `FromDate`, `ToDate`, `DayCount`, `Amount`. **Σ Amount = AccrualDetail.Amount**. Tek-taraf → tek satır (%100). Index `(party_id)`, `(accrual_detail_id)`.
- **`AccrualDetail`** (mevcut, BB satırı) + `PrimaryResponsiblePartyId Guid?` (denormalize: en yüksek paylı/en uzun süreli taraf — hızlı liste/KPI) + `ResponsibleResolvedNote string(200)?`.
- **`Accrual`** (mevcut) + `ResponsibilityMode smallint?` (üretim anı snapshot).
- **`Budget`** (mevcut) + `ResponsibilityMode smallint NOT NULL DEFAULT 0`.
- Enum `ResponsibilityMode` (`TenantThenOwner=0, OwnerOnly=1`) → `Domain/Tenant/Parties/Enums/`.
- **Çok-malikli (Hissedar):** borç tek kalır; `PrimaryResponsiblePartyId` = en yüksek paylı malik; müteselsil ise tahsilat herhangi co-owner'dan alınabilir. Pay'a BÖLÜNMEZ (proration yalnız **zamansal** taraf değişiminde).

## Prorater (`IResponsibilityResolver`, Application)
`Prorate(unitId, period(Year,Month), totalAmount, mode) → List<Split{PartyId, Kind, FromDate, ToDate, DayCount, Amount}>`
- **Coverage window = tahakkuk dönemi (ay) tam [ayın 1'i – son günü]**. Tutar ayın gün sayısına bölünür; her tenure dilimi aktif gün sayısı oranında pay alır.
- Her gün-dilimi sorumlu tarafa atanır: `OwnerOnly` → aktif malik; `TenantThenOwner` → aktif kiracı varsa kiracı, yoksa (boş gün) aktif malik.
- Bitişik aynı-taraf dilimleri birleştirilir; `Amount = round(totalAmount × DayCount / aydakiGün)`, kuruş artığı son/en büyük dilime (Σ = total).
- Çok-malikli dilimde `PartyId` = birincil malik (pay'a bölme yok).
- **`ProrateBatch(unitIds, period, mode)`** — tenure'lar tek seferde yüklenir (N+1 önleme; GenerateBudgetAccrual toplu-yükleme desenine uyumlu).
- Tek-taraf çözüm referans tarihi (UI "aktif sorumlu" gösterimi) = tahakkuk tarihi.

## GenerateBudgetAccrual entegrasyonu
`GenerateBudgetAccrualCommandHandler` — unitTotals döngüsünde her BB için `ProrateBatch` → her `AccrualDetail` için `AccrualResponsibilitySplit` satırları + `PrimaryResponsiblePartyId` yazılır; `Accrual.ResponsibilityMode = budget.ResponsibilityMode`. (Invoice/DirectCharge handler'ları sonradan aynı resolver'ı çağırır.)

## Reattribution
`ReattributeAccrualResponsibilityCommand` — tenure create/update/delete sonrası aynı transaction'da çağrılır. Etkilenen `AccrualDetail`'lerin `AccrualResponsibilitySplit` satırları silinip yeniden üretilir + `PrimaryResponsiblePartyId` güncellenir. **Borç toplamı (`AccrualDetail.Amount`) ve tahsilat (`CollectionAllocation`) DOKUNULMAZ** — yalnız "kim, ne kadarından sorumlu" değişir.

## BB-merkezli Cari Ledger (`ICurrentAccountReader`, Dapper — `Infrastructure.Caching/Readers/`)
- **Cari Hareketler:** `accrual_details` (borç) UNION `collection_allocations` (alacak) → tarih sıralı + running balance (window fn). `PrimaryResponsiblePartyId` ile kişiye atıf.
- **KPI'lar:** Toplam Tahakkuk / Tahsilat / Net Bakiye / Vadesi Geçmiş (tek sorgu).
- 3000 BB ölçeğinde tek-BB sorgusu önemsiz (mevcut index'ler + yeni `accrual_details(primary_responsible_party_id)`).

## İzinler (`PermissionCatalog`, ScopeLevel.Tenant)
`tenant.party.view`, `tenant.party.edit`, `tenant.tenure.manage`, `tenant.party.pii.view` (maskesiz PII), `tenant.currentaccount.view`.

## PII / KVKK
`[Sensitive]` şu an yalnız audit delta'sını maskeler → **UI maskeleme YENİ**: `tenant.party.pii.view` yoksa DTO'da maskeli döner (handler/projection + `PiiMasker` helper). KVKK consent alanları zorunlu değil (bildirim modülü sonraki fazda kontrol eder).

## Migration
Yeni tablolar: `parties, unit_ownerships, unit_tenancies, unit_contacts, accrual_responsibility_splits` (config'ler `Main/Configurations/Parties/`, otomatik toplanır). Kolon eklemeleri nullable/default → güvenli; mevcut accrual'larda `primary_responsible_party_id` null. Opsiyonel `BackfillResponsiblePartyCommand` (tenure girildikten sonra geçmişi doldurur).

## 13 sekme → F0 kapsamı
✅ F0 (6): Genel Bilgiler · BB'de Oturanlar (Malik aktif→ilk, Kiracı son→ilk) · İletişim Kişileri · Hissedarlar (pay% + müteselsil) · Cari Hareketler · Tahakkuk&Tahsilat.
⛔ Sonraki faz (7): Sözleşme · Abonelik/Sayaç · Araç/Geçiş · Talep/Şikayet · İcra · Dosyalar · İletişim Geçmişi.

## Referans desenler
- Temporal junction + CHECK + partial-unique: `Domain/Tenant/Budgeting/UnitParticipationGroup.cs` + config.
- PII precedent: `Domain/Identity/Users/User.cs`.
- Dapper reader: `Infrastructure.Caching/Readers/AccountingReader.cs`.
- Tahakkuk üretim: `Features/Main/Accruals/GenerateBudgetAccrual/GenerateBudgetAccrualCommandHandler.cs`.
- İzin kataloğu: `Infrastructure.Persistence/Seeding/PermissionCatalog.cs`.

## Varsayılanlar (açık sorulardan)
AS-2 tek aktif kiracı (evet) · AS-4 pay toplamı 100 esnek (hard değil) · AS-6 tek Party çok association · AS-7 `tenant.currentaccount.view` ayrı izin · AS-9 Company scope · AS-10 PII mask handler/DTO seviyesinde (format sonra).

## İmplementasyon slice'ları
S0 spec (bu) · S1 Party domaini · S2 sorumluluk/proration + testler · S3 GenerateBudgetAccrual + Reattribute + E2E · S4 cari ledger · S5 UI (Cari Kart kabuğu + 6 sekme + tahsilat sihirbazı). Her slice: build 0/0 + testler + memory snapshot.
