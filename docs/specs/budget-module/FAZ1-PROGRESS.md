# FAZ 1 — Yapı Şeması Domain Modeli — İlerleme Belgesi

> **Bu belge compact/context kayıplarından sonra kaldığı yerden devam etmek için yazıldı.**
> Tarih: 2026-05-22 | Hazırlayan: Claude Code (Sonnet 4.6)

---

## 1. Mevcut Durum: Tamamlananlar

- [x] `docs/specs/budget-module/03-DECISIONS-OPEN.md` — 6 karar işaretlendi, commit `3564cb4`
- [x] Domain analizi tamamlandı
- [ ] **FAZ 1 implementasyonu DEVAM EDİYOR** (paralel ajanlar çalışıyor)

---

## 2. Kararlar (03-DECISIONS-OPEN.md'den)

| # | Karar |
|---|-------|
| #1 KapıNo Unique | Blok scope (BlockId, Number) WHERE BlockId IS NOT NULL; Yapı scope (BuildingId, Number) WHERE BlockId IS NULL — iki partial unique index |
| #2 Yönetim Birimi | Sadece Site-bazlı (1 Company = 1 Bütçe) |
| #3 Kod Formatı | `UrlCode`, 9-char Base58 (mevcut standart — IHasUrlCode) |
| #4 Company Entity | `ICollection<Land> Lands` navigation property eklendi |
| #5 Enum/Lookup | C# Enum + INT kolonu |
| #6 Onay Akışı | Basit Yayınla + `IApprovalService` + `AutoApproveApprovalService` |

---

## 3. Mevcut Kod Yapısı (FAZ 1 Öncesi)

### Domain — BuildingSchema
```
src/Core/CleanTenant.Domain/Tenant/BuildingSchema/
├── Block.cs          ← ADA (tapu Ada) — LAND olarak yeniden adlandırılacak
├── Parcel.cs         ← Parsel
├── Building.cs       ← Yapı
├── Unit.cs           ← BB (Bağımsız Bölüm)
├── BuildingType.cs   ← enum (Residential, ResidentialCommercial, ShoppingMall, Office, Warehouse, Other)
├── UnitType.cs       ← enum (Apartment, Office, Shop, Store, Storage, Parking, Shelter, Other)
├── ApartmentLayout.cs← enum
└── Orientation.cs    ← enum
```

### Mevcut Hiyerarşi (FAZ 1 Öncesi)
```
Company → Block(Ada) → Parcel → Building → Unit
```

### Hedef Hiyerarşi (FAZ 1 Sonrası)
```
Company → Land(Ada) → Parcel → Building → Block(Kule) [OPSİYONEL] → Unit
                                              ↑
                                       YENI ENTITY
```

---

## 4. FAZ 1 Değişiklikleri — Tam Liste

### 4.1 Naming Kararı (Kullanıcı Onaylı)
- Mevcut `Block` entity → **`Land`** (Ada = Land)
- Yeni `Block` entity → **Kule/Blok** (A Blok, B Blok) — Building altında, Unit üstünde

### 4.2 Domain Katmanı (Ajan 1)

**Oluşturulacak dosyalar:**
```
src/Core/CleanTenant.SharedKernel/Events/IDomainEvent.cs      [YENİ]
src/Core/CleanTenant.Domain/Events/BuildingSchema/
    ├── UnitCreated.cs    [YENİ]
    ├── BuildingCreated.cs [YENİ]
    ├── UnitUpdated.cs    [YENİ]
    └── UnitDeleted.cs    [YENİ]
src/Core/CleanTenant.Domain/Tenant/BuildingSchema/Land.cs     [YENİ — Block.cs içeriğinden]
```

**Düzenlenecek dosyalar:**
- `Block.cs` → Komple yeniden yaz (yeni Block entity = kule/blok, BuildingId FK)
- `Parcel.cs` → BlockId → LandId, Block → Land (navigation + property)
- `Building.cs` → `ICollection<Block> Blocks` navigation ekle
- `Unit.cs` → `Guid? BlockId`, `decimal GrossSquareMeters`, `int RoomCount`, `Block? Block` navigation ekle
- `Company.cs` → `ICollection<Land> Lands` navigation ekle

**Yeni Block entity içeriği:**
```csharp
public sealed class Block : BaseEntity, IAggregateRoot, IHasUrlCode, ITenantScoped
{
    public string UrlCode { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid BuildingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Building Building { get; set; } = null!;
    public ICollection<Unit> Units { get; set; } = [];
}
```

**Land entity (mevcut Block'tan):**
```csharp
public sealed class Land : BaseEntity, IAggregateRoot, IHasUrlCode, ITenantScoped
{
    public string UrlCode { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Company Company { get; set; } = null!;
    public ICollection<Parcel> Parcels { get; set; } = [];
}
```

### 4.3 Application Katmanı (Ajan 2)

**IMainDbContext.cs:**
```csharp
// DEĞİŞTİR:
DbSet<Block> Blocks { get; }   → DbSet<Land> Lands { get; }
// EKLE:
DbSet<Block> Blocks { get; }   (yeni Block/kule entity için)
```

**Klasör rename:**
```
Features/Main/BuildingSchema/Blocks/    → Lands/
  CreateBlockCommand.cs                 → CreateLandCommand.cs
  CreateBlockCommandHandler.cs          → CreateLandCommandHandler.cs
  CreateBlockCommandValidator.cs        → CreateLandCommandValidator.cs
  UpdateBlockCommand.cs                 → UpdateLandCommand.cs
  UpdateBlockCommandHandler.cs          → UpdateLandCommandHandler.cs
  UpdateBlockCommandValidator.cs        → UpdateLandCommandValidator.cs
  DeleteBlockCommand.cs                 → DeleteLandCommand.cs
  DeleteBlockCommandHandler.cs          → DeleteLandCommandHandler.cs

Features/Main/BuildingSchema/Reorder/
  ReorderBlocksCommand.cs               → ReorderLandsCommand.cs
  ReorderBlocksCommandHandler.cs        → ReorderLandsCommandHandler.cs
```

**Her dosyada:** `Block` → `Land`, `_db.Blocks` → `_db.Lands`, namespace `Blocks` → `Lands`

**BuildingSchemaDto.cs:**
```csharp
BlockDto → LandDto
IReadOnlyList<BlockDto> Blocks → IReadOnlyList<LandDto> Lands
BuildingSchemaDto param: Blocks → Lands
```

**GetBuildingSchemaQueryHandler.cs:**
- `_db.Blocks` → `_db.Lands`
- Query result mapping: BlockDto → LandDto

**Parcels/CreateParcelCommand.cs:** `BlockId` → `LandId`
**Parcels/CreateParcelCommandHandler.cs:** `_db.Blocks` → `_db.Lands`, `BlockId` → `LandId`, error code `BLOCK-NOT-FOUND` → `LAND-NOT-FOUND`
**Parcels/CreateParcelCommandValidator.cs:** `BlockId` → `LandId`

**Excel files:** Block → Land referansları güncelle

### 4.4 Infrastructure Katmanı (Ajan 3)

**Configurations:**
```
BlockConfiguration.cs → LandConfiguration.cs  (tablo adı: "blocks" → "lands")
  - IEntityTypeConfiguration<Block> → IEntityTypeConfiguration<Land>
  - .ToTable("blocks") → .ToTable("lands")
  - .WithMany() → .WithMany(c => c.Lands)  (Company navigation eklendi)
  - index name: ix_blocks_company_name → ix_lands_company_name

BlockConfiguration.cs [YENİ — kule entity için]
  - IEntityTypeConfiguration<Block>
  - .ToTable("blocks")  [YENI tablo]
  - FK: BuildingId
  - Navigation: HasMany(bu => bu.Units) ile Unit bağlantısı

ParcelConfiguration.cs:
  - p.BlockId → p.LandId
  - .HasOne(p => p.Block) → .HasOne(p => p.Land)
  - .HasForeignKey(p => p.BlockId) → .HasForeignKey(p => p.LandId)
  - .WithMany(b => b.Parcels) → .WithMany(l => l.Parcels)
  - ix_parcels_block_name → ix_parcels_land_name

UnitConfiguration.cs — Eklenecekler:
  - builder.Property(u => u.BlockId).HasColumnType("uuid");
  - builder.HasIndex(u => u.BlockId);
  - builder.Property(u => u.GrossSquareMeters).HasColumnType("decimal(10,2)").IsRequired();
  - builder.Property(u => u.RoomCount).IsRequired();
  - Unique index güncelleme (KapiNo = Number):
    - Mevcut: (BuildingId, Number) UNIQUE WHERE is_deleted = false
    - YENİ:
      (BlockId, Number) UNIQUE WHERE is_deleted = false AND block_id IS NOT NULL
      (BuildingId, Number) UNIQUE WHERE is_deleted = false AND block_id IS NULL
  - builder.HasOne(u => u.Block).WithMany(b => b.Units)
      .HasForeignKey(u => u.BlockId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
```

**MainDbContext.cs:**
```csharp
// DEĞİŞTİR:
public DbSet<Block> Blocks => Set<Block>();
// EKLE:
public DbSet<Land> Lands => Set<Land>();
public DbSet<Block> Blocks => Set<Block>();   // yeni Block/kule
```

**BuildingSchemaExcelService.cs:** Block → Land referansları

**TenantForm.razor.cs ve LocalizationCatalog.cs:** Block → Land referansları kontrol

---

## 5. FAZ 1 Sonrası Yapılacaklar

### FAZ 2 — EF Core Migration
Tüm kod değişiklikleri tamamlandıktan ve `dotnet build` başarıyla çalıştıktan sonra:
```powershell
cd src/Infrastructure/CleanTenant.Infrastructure.Persistence
dotnet ef migrations add AddBuildingSchemaRefactor `
  --context MainDbContext `
  --startup-project ../../Presentation/CleanTenant.ManagementApp/
```

Migration'ın yapacakları:
1. Tablo rename: `blocks` → `lands`
2. Kolon rename: `parcels.block_id` → `parcels.land_id`
3. Yeni tablo: `blocks` (kule entity için)
4. Yeni kolonlar: `units.block_id`, `units.gross_square_meters`, `units.room_count`
5. Unique index güncellemeleri

---

## 6. Sonraki FAZ'lar (Özet)

- **FAZ 3:** Application layer — Yapı Şeması CRUD commands + Excel import
- **FAZ 4:** UI (Blazor) — Tree view + CRUD forms + Excel import
- **FAZ 5:** Bütçe MVP Domain + Application
- **FAZ 6:** Tahakkuk Motoru
- **FAZ 7:** Tahsilat + Gecikme + UI
- **FAZ 8:** Test + Referans Senaryolar

---

## 7. Dosya Referansları

| Dosya | Konum |
|-------|-------|
| Spec kararları | `docs/specs/budget-module/03-DECISIONS-OPEN.md` |
| Master SDD | `docs/specs/budget-module/01-SDD-v1.0.md` |
| Faz kartları | `docs/specs/budget-module/02-PHASE-CARDS.md` |
| Playbook | `docs/specs/budget-module/04-CLAUDE-CODE-PLAYBOOK.md` |
| Bu belge | `docs/specs/budget-module/FAZ1-PROGRESS.md` |
| Plan | `C:\Users\yusuf\.claude\plans\workspace-docs-specs-budget-module-klas-humble-steele.md` |
