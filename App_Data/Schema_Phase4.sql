-- =============================================================================
-- EidUbahle Enterprise ERP – Phase 4 Database Schema
-- Inventory & Products: Categories, Brands, UOM, Products, Warehouses,
-- Stock Movements, FIFO/AVCO Valuation, Batches/Serials, Reservations, Alerts
-- Run this script against EidUbahleDB (after Schema_Phase3.sql).
-- =============================================================================

USE EidUbahleDB;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Product Categories (hierarchical, unlimited depth)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_Categories')
CREATE TABLE inv_Categories (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    ParentId        UNIQUEIDENTIFIER REFERENCES inv_Categories(Id),
    Name            NVARCHAR(200)    NOT NULL,
    Code            NVARCHAR(50),
    Description     NVARCHAR(500),
    ImageUrl        NVARCHAR(500),
    SortOrder       INT              NOT NULL DEFAULT 0,
    Level           INT              NOT NULL DEFAULT 1,
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CONSTRAINT UQ_Category_Code UNIQUE (TenantId, Code)
);
GO
CREATE INDEX IX_Categories_TenantId  ON inv_Categories(TenantId) WHERE IsActive=1;
CREATE INDEX IX_Categories_ParentId  ON inv_Categories(ParentId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Brands
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_Brands')
CREATE TABLE inv_Brands (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    Name            NVARCHAR(200)    NOT NULL,
    Code            NVARCHAR(50),
    Description     NVARCHAR(500),
    LogoUrl         NVARCHAR(500),
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CONSTRAINT UQ_Brand_Code UNIQUE (TenantId, Code)
);
GO
CREATE INDEX IX_Brands_TenantId ON inv_Brands(TenantId) WHERE IsActive=1;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Units of Measure (UOM)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_UOM')
CREATE TABLE inv_UOM (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    Name            NVARCHAR(100)    NOT NULL,
    Symbol          NVARCHAR(20)     NOT NULL,
    BaseUomId       UNIQUEIDENTIFIER REFERENCES inv_UOM(Id),   -- NULL = this IS the base
    ConversionFactor DECIMAL(20,8)   NOT NULL DEFAULT 1,        -- factor to convert to base
    UomType         NVARCHAR(30)     NOT NULL DEFAULT 'Count',  -- Count | Weight | Volume | Length | Area | Time
    IsBase          BIT              NOT NULL DEFAULT 0,
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CONSTRAINT UQ_UOM UNIQUE (TenantId, Symbol)
);
GO
CREATE INDEX IX_UOM_TenantId ON inv_UOM(TenantId) WHERE IsActive=1;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 4. Product Master
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_Products')
CREATE TABLE inv_Products (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    SKU             NVARCHAR(100)    NOT NULL,
    Barcode         NVARCHAR(100),
    Name            NVARCHAR(300)    NOT NULL,
    ShortName       NVARCHAR(100),
    Description     NVARCHAR(2000),
    CategoryId      UNIQUEIDENTIFIER REFERENCES inv_Categories(Id),
    BrandId         UNIQUEIDENTIFIER REFERENCES inv_Brands(Id),
    PurchaseUomId   UNIQUEIDENTIFIER REFERENCES inv_UOM(Id),
    SalesUomId      UNIQUEIDENTIFIER REFERENCES inv_UOM(Id),
    StockUomId      UNIQUEIDENTIFIER REFERENCES inv_UOM(Id),    -- base tracking unit
    ProductType     NVARCHAR(20)     NOT NULL DEFAULT 'Storable', -- Storable | Service | Consumable
    CostingMethod   NVARCHAR(10)     NOT NULL DEFAULT 'AVCO',   -- FIFO | AVCO
    StandardCost    DECIMAL(20,4)    NOT NULL DEFAULT 0,
    SalePrice       DECIMAL(20,4)    NOT NULL DEFAULT 0,
    MinSalePrice    DECIMAL(20,4),
    TaxRate         DECIMAL(8,4)     NOT NULL DEFAULT 0,
    ImageUrl        NVARCHAR(500),
    Notes           NVARCHAR(1000),
    -- Reorder settings
    ReorderPoint    DECIMAL(20,4)    NOT NULL DEFAULT 0,         -- trigger low-stock alert
    ReorderQty      DECIMAL(20,4)    NOT NULL DEFAULT 0,         -- suggested reorder quantity
    -- Batch / Serial settings
    TrackingType    NVARCHAR(10)     NOT NULL DEFAULT 'None',    -- None | Batch | Serial
    -- GL account links (for COGS / Inventory asset posting)
    InventoryAccountId UNIQUEIDENTIFIER REFERENCES acc_Accounts(Id),
    COGSAccountId      UNIQUEIDENTIFIER REFERENCES acc_Accounts(Id),
    RevenueAccountId   UNIQUEIDENTIFIER REFERENCES acc_Accounts(Id),
    -- Flags
    IsSaleable      BIT              NOT NULL DEFAULT 1,
    IsPurchaseable  BIT              NOT NULL DEFAULT 1,
    IsActive        BIT              NOT NULL DEFAULT 1,
    IsDeleted       BIT              NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    UpdatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CONSTRAINT UQ_Product_SKU UNIQUE (TenantId, SKU)
);
GO
CREATE INDEX IX_Products_TenantId    ON inv_Products(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_Products_CategoryId  ON inv_Products(CategoryId) WHERE IsDeleted=0;
CREATE INDEX IX_Products_BrandId     ON inv_Products(BrandId) WHERE IsDeleted=0;
CREATE INDEX IX_Products_SKU         ON inv_Products(TenantId, SKU) WHERE IsDeleted=0;
CREATE INDEX IX_Products_Barcode     ON inv_Products(TenantId, Barcode) WHERE IsDeleted=0 AND Barcode IS NOT NULL;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 5. Product Variants (e.g. Color=Red, Size=M)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_ProductVariants')
CREATE TABLE inv_ProductVariants (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    ProductId       UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Products(Id),
    VariantSKU      NVARCHAR(100)    NOT NULL,
    Barcode         NVARCHAR(100),
    Name            NVARCHAR(300)    NOT NULL,
    Attributes      NVARCHAR(1000),  -- JSON: {"Color":"Red","Size":"M"}
    ExtraCost       DECIMAL(20,4)    NOT NULL DEFAULT 0,
    ExtraPrice      DECIMAL(20,4)    NOT NULL DEFAULT 0,
    ImageUrl        NVARCHAR(500),
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Variant_SKU UNIQUE (TenantId, VariantSKU)
);
GO
CREATE INDEX IX_Variants_ProductId ON inv_ProductVariants(ProductId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 6. Warehouses
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_Warehouses')
CREATE TABLE inv_Warehouses (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    CompanyId       UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    BranchId        UNIQUEIDENTIFIER REFERENCES saas_Branches(Id),
    Code            NVARCHAR(50)     NOT NULL,
    Name            NVARCHAR(200)    NOT NULL,
    Address         NVARCHAR(500),
    ContactPerson   NVARCHAR(200),
    ContactPhone    NVARCHAR(50),
    IsDefault       BIT              NOT NULL DEFAULT 0,
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CONSTRAINT UQ_Warehouse_Code UNIQUE (TenantId, Code)
);
GO
CREATE INDEX IX_Warehouses_TenantId  ON inv_Warehouses(TenantId) WHERE IsActive=1;
CREATE INDEX IX_Warehouses_CompanyId ON inv_Warehouses(CompanyId) WHERE IsActive=1;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 7. Warehouse Locations (bins/racks inside a warehouse)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_WarehouseLocations')
CREATE TABLE inv_WarehouseLocations (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    WarehouseId     UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Warehouses(Id),
    Code            NVARCHAR(50)     NOT NULL,
    Name            NVARCHAR(200)    NOT NULL,
    LocationType    NVARCHAR(30)     NOT NULL DEFAULT 'Storage', -- Storage | Receiving | Shipping | QC | Virtual
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Location_Code UNIQUE (WarehouseId, Code)
);
GO
CREATE INDEX IX_Locations_WarehouseId ON inv_WarehouseLocations(WarehouseId) WHERE IsActive=1;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 8. Stock Levels (materialized current stock per product/variant/warehouse/location)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_StockLevels')
CREATE TABLE inv_StockLevels (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    ProductId       UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Products(Id),
    VariantId       UNIQUEIDENTIFIER REFERENCES inv_ProductVariants(Id),
    WarehouseId     UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Warehouses(Id),
    LocationId      UNIQUEIDENTIFIER REFERENCES inv_WarehouseLocations(Id),
    QtyOnHand       DECIMAL(20,4)    NOT NULL DEFAULT 0,
    QtyReserved     DECIMAL(20,4)    NOT NULL DEFAULT 0,
    QtyAvailable    AS (QtyOnHand - QtyReserved),   -- computed
    QtyOnOrder      DECIMAL(20,4)    NOT NULL DEFAULT 0,   -- pending purchase orders
    AvgCost         DECIMAL(20,4)    NOT NULL DEFAULT 0,   -- running AVCO
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_StockLevel UNIQUE (ProductId, VariantId, WarehouseId, LocationId)
);
GO
CREATE INDEX IX_StockLevels_TenantId    ON inv_StockLevels(TenantId);
CREATE INDEX IX_StockLevels_Product     ON inv_StockLevels(ProductId, WarehouseId);
CREATE INDEX IX_StockLevels_Warehouse   ON inv_StockLevels(WarehouseId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 9. Cost Layers – FIFO/AVCO valuation layers
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_CostLayers')
CREATE TABLE inv_CostLayers (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    ProductId       UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Products(Id),
    VariantId       UNIQUEIDENTIFIER REFERENCES inv_ProductVariants(Id),
    WarehouseId     UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Warehouses(Id),
    MovementLineId  UNIQUEIDENTIFIER,               -- FK set after inv_StockMovementLines exists
    LayerDate       DATETIME2        NOT NULL,
    QtyIn           DECIMAL(20,4)    NOT NULL DEFAULT 0,
    QtyRemaining    DECIMAL(20,4)    NOT NULL DEFAULT 0,   -- for FIFO consumption
    UnitCost        DECIMAL(20,4)    NOT NULL DEFAULT 0,
    TotalCost       AS (QtyRemaining * UnitCost),
    IsFullyConsumed BIT              NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO
CREATE INDEX IX_CostLayers_Product  ON inv_CostLayers(ProductId, WarehouseId, IsFullyConsumed, LayerDate);
CREATE INDEX IX_CostLayers_Tenant   ON inv_CostLayers(TenantId, LayerDate);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 10. Stock Movement Headers (Opening / Adjustment / Transfer / Receipt / Issue)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_StockMovements')
CREATE TABLE inv_StockMovements (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    CompanyId       UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Companies(Id),
    MovementNumber  NVARCHAR(30)     NOT NULL,   -- auto: ADJ-2025-000001 / TRF-2025-000001
    MovementType    NVARCHAR(20)     NOT NULL,   -- Opening | Adjustment | TransferOut | TransferIn | Receipt | Issue
    MovementDate    DATE             NOT NULL,
    Reference       NVARCHAR(100),
    Notes           NVARCHAR(1000),
    FromWarehouseId UNIQUEIDENTIFIER REFERENCES inv_Warehouses(Id),
    ToWarehouseId   UNIQUEIDENTIFIER REFERENCES inv_Warehouses(Id),
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'Draft',  -- Draft | Posted | Cancelled
    PostedAt        DATETIME2,
    PostedBy        UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CancelledAt     DATETIME2,
    CancelledBy     UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    JournalEntryId  UNIQUEIDENTIFIER REFERENCES acc_JournalEntries(Id),  -- GL posting link
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT              NOT NULL DEFAULT 0,
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CONSTRAINT UQ_Movement_Number UNIQUE (CompanyId, MovementNumber)
);
GO
CREATE INDEX IX_StockMovements_TenantId ON inv_StockMovements(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_StockMovements_CompanyId ON inv_StockMovements(CompanyId, MovementDate DESC) WHERE IsDeleted=0;
CREATE INDEX IX_StockMovements_Type ON inv_StockMovements(TenantId, MovementType, Status) WHERE IsDeleted=0;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 11. Stock Movement Lines
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_StockMovementLines')
CREATE TABLE inv_StockMovementLines (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    MovementId      UNIQUEIDENTIFIER NOT NULL REFERENCES inv_StockMovements(Id) ON DELETE CASCADE,
    ProductId       UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Products(Id),
    VariantId       UNIQUEIDENTIFIER REFERENCES inv_ProductVariants(Id),
    FromLocationId  UNIQUEIDENTIFIER REFERENCES inv_WarehouseLocations(Id),
    ToLocationId    UNIQUEIDENTIFIER REFERENCES inv_WarehouseLocations(Id),
    BatchId         UNIQUEIDENTIFIER,               -- FK added after inv_Batches exists
    SerialId        UNIQUEIDENTIFIER,               -- FK added after inv_SerialNumbers exists
    LineNumber      INT              NOT NULL,
    Qty             DECIMAL(20,4)    NOT NULL,
    UomId           UNIQUEIDENTIFIER REFERENCES inv_UOM(Id),
    UnitCost        DECIMAL(20,4)    NOT NULL DEFAULT 0,
    TotalCost       AS (Qty * UnitCost),
    Reason          NVARCHAR(200),
    Notes           NVARCHAR(500),
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO
CREATE INDEX IX_SMLLines_MovementId ON inv_StockMovementLines(MovementId);
CREATE INDEX IX_SMLLines_ProductId  ON inv_StockMovementLines(ProductId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 12. Batches / Lots
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_Batches')
CREATE TABLE inv_Batches (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    ProductId       UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Products(Id),
    VariantId       UNIQUEIDENTIFIER REFERENCES inv_ProductVariants(Id),
    BatchNumber     NVARCHAR(100)    NOT NULL,
    LotNumber       NVARCHAR(100),
    ManufactureDate DATE,
    ExpiryDate      DATE,
    ReceivedDate    DATE             NOT NULL DEFAULT CAST(GETUTCDATE() AS DATE),
    SupplierId      UNIQUEIDENTIFIER,               -- FK to future crm_Contacts
    Notes           NVARCHAR(500),
    IsActive        BIT              NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    CONSTRAINT UQ_Batch UNIQUE (TenantId, ProductId, BatchNumber)
);
GO
CREATE INDEX IX_Batches_Product ON inv_Batches(ProductId);
CREATE INDEX IX_Batches_Expiry  ON inv_Batches(TenantId, ExpiryDate) WHERE ExpiryDate IS NOT NULL;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 13. Serial Numbers
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_SerialNumbers')
CREATE TABLE inv_SerialNumbers (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    ProductId       UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Products(Id),
    VariantId       UNIQUEIDENTIFIER REFERENCES inv_ProductVariants(Id),
    SerialNumber    NVARCHAR(200)    NOT NULL,
    WarehouseId     UNIQUEIDENTIFIER REFERENCES inv_Warehouses(Id),
    LocationId      UNIQUEIDENTIFIER REFERENCES inv_WarehouseLocations(Id),
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'InStock',  -- InStock | Sold | Reserved | Damaged | Returned
    BatchId         UNIQUEIDENTIFIER REFERENCES inv_Batches(Id),
    ReceivedAt      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    SoldAt          DATETIME2,
    Notes           NVARCHAR(500),
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_SerialNumber UNIQUE (TenantId, ProductId, SerialNumber)
);
GO
CREATE INDEX IX_Serials_Product   ON inv_SerialNumbers(ProductId, Status);
CREATE INDEX IX_Serials_Warehouse ON inv_SerialNumbers(WarehouseId, Status);
GO

-- Add batch/serial FKs to movement lines
ALTER TABLE inv_StockMovementLines
    ADD CONSTRAINT FK_SML_Batch  FOREIGN KEY (BatchId)  REFERENCES inv_Batches(Id);
ALTER TABLE inv_StockMovementLines
    ADD CONSTRAINT FK_SML_Serial FOREIGN KEY (SerialId) REFERENCES inv_SerialNumbers(Id);
GO

-- Add MovementLineId FK on cost layers
ALTER TABLE inv_CostLayers
    ADD CONSTRAINT FK_CostLayer_MovementLine FOREIGN KEY (MovementLineId)
        REFERENCES inv_StockMovementLines(Id);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 14. Stock Reservations (reservation engine)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_StockReservations')
CREATE TABLE inv_StockReservations (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    ProductId       UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Products(Id),
    VariantId       UNIQUEIDENTIFIER REFERENCES inv_ProductVariants(Id),
    WarehouseId     UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Warehouses(Id),
    LocationId      UNIQUEIDENTIFIER REFERENCES inv_WarehouseLocations(Id),
    SourceType      NVARCHAR(30)     NOT NULL,   -- SalesOrder | TransferOrder | ManualHold
    SourceId        UNIQUEIDENTIFIER NOT NULL,   -- ID of the source document
    SourceRef       NVARCHAR(100),               -- human-readable reference
    QtyReserved     DECIMAL(20,4)    NOT NULL,
    QtyFulfilled    DECIMAL(20,4)    NOT NULL DEFAULT 0,
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'Active',  -- Active | PartialFulfilled | Fulfilled | Released | Expired
    ExpiresAt       DATETIME2,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       UNIQUEIDENTIFIER REFERENCES sys_Users(Id)
);
GO
CREATE INDEX IX_Reservations_Tenant  ON inv_StockReservations(TenantId, Status);
CREATE INDEX IX_Reservations_Product ON inv_StockReservations(ProductId, WarehouseId, Status);
CREATE INDEX IX_Reservations_Source  ON inv_StockReservations(SourceType, SourceId);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 15. Low Stock Alerts
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='inv_LowStockAlerts')
CREATE TABLE inv_LowStockAlerts (
    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES saas_Tenants(Id),
    ProductId       UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Products(Id),
    WarehouseId     UNIQUEIDENTIFIER NOT NULL REFERENCES inv_Warehouses(Id),
    QtyOnHand       DECIMAL(20,4)    NOT NULL,
    QtyAvailable    DECIMAL(20,4)    NOT NULL,
    ReorderPoint    DECIMAL(20,4)    NOT NULL,
    ReorderQty      DECIMAL(20,4)    NOT NULL DEFAULT 0,
    AlertType       NVARCHAR(20)     NOT NULL DEFAULT 'LowStock',  -- LowStock | OutOfStock | ExpiryWarning
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'Active',    -- Active | Acknowledged | Resolved
    AcknowledgedAt  DATETIME2,
    AcknowledgedBy  UNIQUEIDENTIFIER REFERENCES sys_Users(Id),
    ResolvedAt      DATETIME2,
    CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
GO
CREATE INDEX IX_LowStockAlerts_TenantId  ON inv_LowStockAlerts(TenantId, Status);
CREATE INDEX IX_LowStockAlerts_Product   ON inv_LowStockAlerts(ProductId, Status);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 16. Stored Procedure: Generate movement document number
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_GenerateMovementNumber', 'P') IS NOT NULL DROP PROCEDURE sp_GenerateMovementNumber;
GO
CREATE PROCEDURE sp_GenerateMovementNumber
    @CompanyId      UNIQUEIDENTIFIER,
    @MovementType   NVARCHAR(20),
    @Number         NVARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @prefix NVARCHAR(5);
    SELECT @prefix = CASE @MovementType
        WHEN 'Opening'     THEN 'OPN'
        WHEN 'Adjustment'  THEN 'ADJ'
        WHEN 'TransferOut' THEN 'TRF'
        WHEN 'TransferIn'  THEN 'TRF'
        WHEN 'Receipt'     THEN 'RCP'
        WHEN 'Issue'       THEN 'ISS'
        ELSE 'MOV'
    END;
    DECLARE @year NVARCHAR(4) = CAST(YEAR(GETUTCDATE()) AS NVARCHAR(4));
    DECLARE @seq INT;
    SELECT @seq = COUNT(*) + 1
    FROM inv_StockMovements
    WHERE CompanyId = @CompanyId
      AND MovementType = @MovementType
      AND YEAR(MovementDate) = YEAR(GETUTCDATE());
    SET @Number = @prefix + '-' + @year + '-' + RIGHT('000000' + CAST(@seq AS NVARCHAR), 6);
END;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 17. Stored Procedure: Post stock movement (update StockLevels + CostLayers)
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_PostStockMovement', 'P') IS NOT NULL DROP PROCEDURE sp_PostStockMovement;
GO
CREATE PROCEDURE sp_PostStockMovement
    @MovementId     UNIQUEIDENTIFIER,
    @PostedBy       UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @TenantId UNIQUEIDENTIFIER, @MovementType NVARCHAR(20),
                @FromWH UNIQUEIDENTIFIER, @ToWH UNIQUEIDENTIFIER,
                @Status NVARCHAR(20);

        SELECT @TenantId = TenantId, @MovementType = MovementType,
               @FromWH = FromWarehouseId, @ToWH = ToWarehouseId, @Status = Status
        FROM inv_StockMovements WHERE Id = @MovementId;

        IF @Status <> 'Draft'
        BEGIN
            ROLLBACK; RETURN;
        END

        -- Process each line
        DECLARE @ProductId UNIQUEIDENTIFIER, @VariantId UNIQUEIDENTIFIER,
                @Qty DECIMAL(20,4), @UnitCost DECIMAL(20,4), @LineId UNIQUEIDENTIFIER;

        DECLARE cur CURSOR FOR
            SELECT Id, ProductId, VariantId, Qty, UnitCost
            FROM inv_StockMovementLines WHERE MovementId = @MovementId;

        OPEN cur;
        FETCH NEXT FROM cur INTO @LineId, @ProductId, @VariantId, @Qty, @UnitCost;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @CostingMethod NVARCHAR(10);
            SELECT @CostingMethod = CostingMethod FROM inv_Products WHERE Id = @ProductId;

            -- ── Inbound (Opening / Adjustment+ / TransferIn / Receipt) ──
            IF @MovementType IN ('Opening','Receipt','TransferIn') OR
               (@MovementType = 'Adjustment' AND @Qty > 0)
            BEGIN
                DECLARE @WhId UNIQUEIDENTIFIER = ISNULL(@ToWH, @FromWH);

                -- Upsert stock level
                IF EXISTS (SELECT 1 FROM inv_StockLevels
                           WHERE ProductId=@ProductId AND WarehouseId=@WhId
                             AND (VariantId=@VariantId OR (@VariantId IS NULL AND VariantId IS NULL))
                             AND LocationId IS NULL)
                BEGIN
                    DECLARE @OldQty DECIMAL(20,4), @OldAvg DECIMAL(20,4);
                    SELECT @OldQty=QtyOnHand, @OldAvg=AvgCost FROM inv_StockLevels
                    WHERE ProductId=@ProductId AND WarehouseId=@WhId
                      AND (VariantId=@VariantId OR (@VariantId IS NULL AND VariantId IS NULL))
                      AND LocationId IS NULL;
                    DECLARE @NewAvg DECIMAL(20,4) = CASE WHEN (@OldQty + @Qty) > 0
                        THEN (@OldQty * @OldAvg + @Qty * @UnitCost) / (@OldQty + @Qty)
                        ELSE @UnitCost END;
                    UPDATE inv_StockLevels
                    SET QtyOnHand = QtyOnHand + @Qty,
                        AvgCost   = @NewAvg,
                        UpdatedAt = GETUTCDATE()
                    WHERE ProductId=@ProductId AND WarehouseId=@WhId
                      AND (VariantId=@VariantId OR (@VariantId IS NULL AND VariantId IS NULL))
                      AND LocationId IS NULL;
                END
                ELSE
                BEGIN
                    INSERT INTO inv_StockLevels(TenantId,ProductId,VariantId,WarehouseId,QtyOnHand,AvgCost)
                    VALUES(@TenantId,@ProductId,@VariantId,@WhId,@Qty,@UnitCost);
                END

                -- Insert FIFO cost layer
                IF @CostingMethod = 'FIFO'
                    INSERT INTO inv_CostLayers(TenantId,ProductId,VariantId,WarehouseId,MovementLineId,LayerDate,QtyIn,QtyRemaining,UnitCost)
                    VALUES(@TenantId,@ProductId,@VariantId,@WhId,@LineId,GETUTCDATE(),@Qty,@Qty,@UnitCost);
            END

            -- ── Outbound (Issue / TransferOut / Adjustment-) ──
            IF @MovementType IN ('Issue','TransferOut') OR
               (@MovementType = 'Adjustment' AND @Qty < 0)
            BEGIN
                DECLARE @OutQty DECIMAL(20,4) = ABS(@Qty);
                DECLARE @OutWH  UNIQUEIDENTIFIER = ISNULL(@FromWH, @ToWH);

                UPDATE inv_StockLevels
                SET QtyOnHand = QtyOnHand - @OutQty, UpdatedAt = GETUTCDATE()
                WHERE ProductId=@ProductId AND WarehouseId=@OutWH
                  AND (VariantId=@VariantId OR (@VariantId IS NULL AND VariantId IS NULL))
                  AND LocationId IS NULL;

                -- Consume FIFO layers (oldest first)
                DECLARE @Remaining DECIMAL(20,4) = @OutQty;
                DECLARE @LayerId UNIQUEIDENTIFIER, @LayerQty DECIMAL(20,4);
                DECLARE fc CURSOR FOR
                    SELECT Id, QtyRemaining FROM inv_CostLayers
                    WHERE ProductId=@ProductId AND WarehouseId=@OutWH
                      AND IsFullyConsumed=0
                      AND (VariantId=@VariantId OR (@VariantId IS NULL AND VariantId IS NULL))
                    ORDER BY LayerDate ASC;
                OPEN fc;
                FETCH NEXT FROM fc INTO @LayerId, @LayerQty;
                WHILE @@FETCH_STATUS=0 AND @Remaining>0
                BEGIN
                    IF @LayerQty <= @Remaining
                    BEGIN
                        UPDATE inv_CostLayers SET QtyRemaining=0, IsFullyConsumed=1 WHERE Id=@LayerId;
                        SET @Remaining = @Remaining - @LayerQty;
                    END
                    ELSE
                    BEGIN
                        UPDATE inv_CostLayers SET QtyRemaining=QtyRemaining-@Remaining WHERE Id=@LayerId;
                        SET @Remaining = 0;
                    END
                    FETCH NEXT FROM fc INTO @LayerId, @LayerQty;
                END
                CLOSE fc; DEALLOCATE fc;
            END

            FETCH NEXT FROM cur INTO @LineId, @ProductId, @VariantId, @Qty, @UnitCost;
        END
        CLOSE cur; DEALLOCATE cur;

        -- Mark movement as Posted
        UPDATE inv_StockMovements
        SET Status='Posted', PostedAt=GETUTCDATE(), PostedBy=@PostedBy, UpdatedAt=GETUTCDATE()
        WHERE Id=@MovementId;

        -- Generate low-stock alerts for affected products
        INSERT INTO inv_LowStockAlerts(TenantId,ProductId,WarehouseId,QtyOnHand,QtyAvailable,ReorderPoint,ReorderQty,AlertType)
        SELECT sl.TenantId, sl.ProductId, sl.WarehouseId,
               sl.QtyOnHand, sl.QtyOnHand - sl.QtyReserved,
               p.ReorderPoint, p.ReorderQty,
               CASE WHEN sl.QtyOnHand<=0 THEN 'OutOfStock' ELSE 'LowStock' END
        FROM inv_StockLevels sl
        JOIN inv_Products p ON p.Id = sl.ProductId
        JOIN inv_StockMovementLines sml ON sml.MovementId=@MovementId AND sml.ProductId=sl.ProductId
        WHERE p.ReorderPoint > 0
          AND sl.QtyOnHand <= p.ReorderPoint
          AND NOT EXISTS (
              SELECT 1 FROM inv_LowStockAlerts lsa
              WHERE lsa.ProductId=sl.ProductId AND lsa.WarehouseId=sl.WarehouseId
                AND lsa.Status='Active'
          );

        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 18. Stored Procedure: Reserve stock
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_ReserveStock', 'P') IS NOT NULL DROP PROCEDURE sp_ReserveStock;
GO
CREATE PROCEDURE sp_ReserveStock
    @TenantId       UNIQUEIDENTIFIER,
    @ProductId      UNIQUEIDENTIFIER,
    @VariantId      UNIQUEIDENTIFIER,
    @WarehouseId    UNIQUEIDENTIFIER,
    @Qty            DECIMAL(20,4),
    @SourceType     NVARCHAR(30),
    @SourceId       UNIQUEIDENTIFIER,
    @SourceRef      NVARCHAR(100),
    @CreatedBy      UNIQUEIDENTIFIER,
    @ExpiresAt      DATETIME2 = NULL,
    @ReservationId  UNIQUEIDENTIFIER OUTPUT,
    @Error          NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        SET @ReservationId = NULL;
        SET @Error = NULL;

        -- Check available qty
        DECLARE @Available DECIMAL(20,4);
        SELECT @Available = QtyOnHand - QtyReserved
        FROM inv_StockLevels
        WHERE ProductId=@ProductId AND WarehouseId=@WarehouseId
          AND (VariantId=@VariantId OR (@VariantId IS NULL AND VariantId IS NULL))
          AND LocationId IS NULL;

        IF @Available IS NULL OR @Available < @Qty
        BEGIN
            SET @Error = 'Insufficient available stock';
            ROLLBACK; RETURN;
        END

        SET @ReservationId = NEWID();
        INSERT INTO inv_StockReservations(Id,TenantId,ProductId,VariantId,WarehouseId,
            SourceType,SourceId,SourceRef,QtyReserved,ExpiresAt,CreatedBy)
        VALUES(@ReservationId,@TenantId,@ProductId,@VariantId,@WarehouseId,
            @SourceType,@SourceId,@SourceRef,@Qty,@ExpiresAt,@CreatedBy);

        UPDATE inv_StockLevels
        SET QtyReserved = QtyReserved + @Qty, UpdatedAt = GETUTCDATE()
        WHERE ProductId=@ProductId AND WarehouseId=@WarehouseId
          AND (VariantId=@VariantId OR (@VariantId IS NULL AND VariantId IS NULL))
          AND LocationId IS NULL;

        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        SET @Error = ERROR_MESSAGE();
    END CATCH
END;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 19. Stored Procedure: Release (cancel) reservation
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_ReleaseReservation', 'P') IS NOT NULL DROP PROCEDURE sp_ReleaseReservation;
GO
CREATE PROCEDURE sp_ReleaseReservation
    @ReservationId  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @ProductId UNIQUEIDENTIFIER, @VariantId UNIQUEIDENTIFIER,
                @WarehouseId UNIQUEIDENTIFIER, @Qty DECIMAL(20,4), @Status NVARCHAR(20);

        SELECT @ProductId=ProductId, @VariantId=VariantId, @WarehouseId=WarehouseId,
               @Qty=QtyReserved-QtyFulfilled, @Status=Status
        FROM inv_StockReservations WHERE Id=@ReservationId;

        IF @Status NOT IN ('Active','PartialFulfilled') BEGIN ROLLBACK; RETURN; END

        UPDATE inv_StockReservations
        SET Status='Released', UpdatedAt=GETUTCDATE() WHERE Id=@ReservationId;

        UPDATE inv_StockLevels
        SET QtyReserved = QtyReserved - @Qty, UpdatedAt=GETUTCDATE()
        WHERE ProductId=@ProductId AND WarehouseId=@WarehouseId
          AND (VariantId=@VariantId OR (@VariantId IS NULL AND VariantId IS NULL))
          AND LocationId IS NULL;

        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 20. Stored Procedure: Get stock valuation (FIFO and AVCO)
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_GetStockValuation', 'P') IS NOT NULL DROP PROCEDURE sp_GetStockValuation;
GO
CREATE PROCEDURE sp_GetStockValuation
    @TenantId       UNIQUEIDENTIFIER,
    @WarehouseId    UNIQUEIDENTIFIER = NULL,
    @AsOf           DATE             = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @AsOf IS NULL SET @AsOf = CAST(GETUTCDATE() AS DATE);

    -- AVCO valuation: use materialized AvgCost
    SELECT
        p.Id           AS ProductId,
        p.SKU,
        p.Name         AS ProductName,
        w.Name         AS WarehouseName,
        sl.QtyOnHand,
        sl.AvgCost     AS UnitCost,
        sl.QtyOnHand * sl.AvgCost AS TotalValue,
        'AVCO'         AS Method
    FROM inv_StockLevels sl
    JOIN inv_Products p  ON p.Id = sl.ProductId
    JOIN inv_Warehouses w ON w.Id = sl.WarehouseId
    WHERE sl.TenantId = @TenantId
      AND p.CostingMethod = 'AVCO'
      AND p.IsDeleted = 0
      AND (@WarehouseId IS NULL OR sl.WarehouseId = @WarehouseId)
      AND sl.QtyOnHand <> 0

    UNION ALL

    -- FIFO valuation: sum remaining cost layers
    SELECT
        p.Id           AS ProductId,
        p.SKU,
        p.Name         AS ProductName,
        w.Name         AS WarehouseName,
        sl.QtyOnHand,
        CASE WHEN sl.QtyOnHand > 0
             THEN SUM(cl.QtyRemaining * cl.UnitCost) / sl.QtyOnHand
             ELSE 0 END AS UnitCost,
        SUM(cl.QtyRemaining * cl.UnitCost) AS TotalValue,
        'FIFO'         AS Method
    FROM inv_StockLevels sl
    JOIN inv_Products p  ON p.Id = sl.ProductId
    JOIN inv_Warehouses w ON w.Id = sl.WarehouseId
    LEFT JOIN inv_CostLayers cl ON cl.ProductId=sl.ProductId AND cl.WarehouseId=sl.WarehouseId
        AND cl.IsFullyConsumed=0
    WHERE sl.TenantId = @TenantId
      AND p.CostingMethod = 'FIFO'
      AND p.IsDeleted = 0
      AND (@WarehouseId IS NULL OR sl.WarehouseId = @WarehouseId)
      AND sl.QtyOnHand <> 0
    GROUP BY p.Id, p.SKU, p.Name, w.Name, sl.QtyOnHand

    ORDER BY ProductName;
END;
GO
