using System;
using System.Collections.Generic;

namespace EidUbahle.Domain.DTOs
{
    // ─── Categories ──────────────────────────────────────────────────────────

    public class CategoryDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? ParentId { get; set; }
        public string ParentName { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int SortOrder { get; set; }
        public int Level { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ProductCount { get; set; }
        public List<CategoryDto> Children { get; set; } = new List<CategoryDto>();
    }

    public class CreateCategoryDto
    {
        public Guid? ParentId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int SortOrder { get; set; }
    }

    // ─── Brands ──────────────────────────────────────────────────────────────

    public class BrandDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ProductCount { get; set; }
    }

    public class CreateBrandDto
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string LogoUrl { get; set; }
    }

    // ─── Units of Measure ────────────────────────────────────────────────────

    public class UomDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public Guid? BaseUomId { get; set; }
        public string BaseUomSymbol { get; set; }
        public decimal ConversionFactor { get; set; }
        public string UomType { get; set; }
        public bool IsBase { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUomDto
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public Guid? BaseUomId { get; set; }
        public decimal ConversionFactor { get; set; } = 1;
        public string UomType { get; set; } = "Count";
        public bool IsBase { get; set; }
    }

    // ─── Products ────────────────────────────────────────────────────────────

    public class ProductListItemDto
    {
        public Guid Id { get; set; }
        public string SKU { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string BrandName { get; set; }
        public string StockUomSymbol { get; set; }
        public string ProductType { get; set; }
        public string CostingMethod { get; set; }
        public string TrackingType { get; set; }
        public decimal StandardCost { get; set; }
        public decimal SalePrice { get; set; }
        public decimal TotalQtyOnHand { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductDetailDto : ProductListItemDto
    {
        public Guid? CategoryId { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? PurchaseUomId { get; set; }
        public string PurchaseUomSymbol { get; set; }
        public Guid? SalesUomId { get; set; }
        public string SalesUomSymbol { get; set; }
        public Guid? StockUomId { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public decimal MinSalePrice { get; set; }
        public decimal TaxRate { get; set; }
        public string ImageUrl { get; set; }
        public string Notes { get; set; }
        public decimal ReorderPoint { get; set; }
        public decimal ReorderQty { get; set; }
        public bool IsSaleable { get; set; }
        public bool IsPurchaseable { get; set; }
        public Guid? InventoryAccountId { get; set; }
        public Guid? COGSAccountId { get; set; }
        public Guid? RevenueAccountId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ProductVariantDto> Variants { get; set; } = new List<ProductVariantDto>();
        public List<StockLevelDto> StockLevels { get; set; } = new List<StockLevelDto>();
    }

    public class CreateProductDto
    {
        public string SKU { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? PurchaseUomId { get; set; }
        public Guid? SalesUomId { get; set; }
        public Guid? StockUomId { get; set; }
        public string ProductType { get; set; } = "Storable";
        public string CostingMethod { get; set; } = "AVCO";
        public decimal StandardCost { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinSalePrice { get; set; }
        public decimal TaxRate { get; set; }
        public string ImageUrl { get; set; }
        public string Notes { get; set; }
        public decimal ReorderPoint { get; set; }
        public decimal ReorderQty { get; set; }
        public string TrackingType { get; set; } = "None";
        public bool IsSaleable { get; set; } = true;
        public bool IsPurchaseable { get; set; } = true;
        public Guid? InventoryAccountId { get; set; }
        public Guid? COGSAccountId { get; set; }
        public Guid? RevenueAccountId { get; set; }
    }

    public class UpdateProductDto : CreateProductDto
    {
        public bool IsActive { get; set; } = true;
    }

    // ─── Product Variants ────────────────────────────────────────────────────

    public class ProductVariantDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string VariantSKU { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public string Attributes { get; set; }
        public decimal ExtraCost { get; set; }
        public decimal ExtraPrice { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateProductVariantDto
    {
        public Guid ProductId { get; set; }
        public string VariantSKU { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public string Attributes { get; set; }
        public decimal ExtraCost { get; set; }
        public decimal ExtraPrice { get; set; }
        public string ImageUrl { get; set; }
    }

    // ─── Warehouses ──────────────────────────────────────────────────────────

    public class WarehouseDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public Guid? BranchId { get; set; }
        public string BranchName { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string ContactPerson { get; set; }
        public string ContactPhone { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int LocationCount { get; set; }
        public List<WarehouseLocationDto> Locations { get; set; } = new List<WarehouseLocationDto>();
    }

    public class CreateWarehouseDto
    {
        public Guid CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string ContactPerson { get; set; }
        public string ContactPhone { get; set; }
        public bool IsDefault { get; set; }
    }

    public class WarehouseLocationDto
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string LocationType { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateWarehouseLocationDto
    {
        public Guid WarehouseId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string LocationType { get; set; } = "Storage";
    }

    // ─── Stock Levels ────────────────────────────────────────────────────────

    public class StockLevelDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSKU { get; set; }
        public Guid? VariantId { get; set; }
        public string VariantName { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public Guid? LocationId { get; set; }
        public string LocationName { get; set; }
        public decimal QtyOnHand { get; set; }
        public decimal QtyReserved { get; set; }
        public decimal QtyAvailable { get; set; }
        public decimal QtyOnOrder { get; set; }
        public decimal AvgCost { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ─── Stock Movements ─────────────────────────────────────────────────────

    public class StockMovementDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string MovementNumber { get; set; }
        public string MovementType { get; set; }
        public DateTime MovementDate { get; set; }
        public string Reference { get; set; }
        public string Notes { get; set; }
        public Guid? FromWarehouseId { get; set; }
        public string FromWarehouseName { get; set; }
        public Guid? ToWarehouseId { get; set; }
        public string ToWarehouseName { get; set; }
        public string Status { get; set; }
        public DateTime? PostedAt { get; set; }
        public string PostedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; }
        public decimal TotalLines { get; set; }
        public decimal TotalValue { get; set; }
        public List<StockMovementLineDto> Lines { get; set; } = new List<StockMovementLineDto>();
    }

    public class StockMovementLineDto
    {
        public Guid Id { get; set; }
        public Guid MovementId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSKU { get; set; }
        public Guid? VariantId { get; set; }
        public string VariantName { get; set; }
        public Guid? BatchId { get; set; }
        public string BatchNumber { get; set; }
        public Guid? SerialId { get; set; }
        public string SerialNumber { get; set; }
        public int LineNumber { get; set; }
        public decimal Qty { get; set; }
        public Guid? UomId { get; set; }
        public string UomSymbol { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
    }

    public class CreateStockMovementDto
    {
        public Guid CompanyId { get; set; }
        public string MovementType { get; set; }
        public DateTime MovementDate { get; set; }
        public string Reference { get; set; }
        public string Notes { get; set; }
        public Guid? FromWarehouseId { get; set; }
        public Guid? ToWarehouseId { get; set; }
        public List<CreateStockMovementLineDto> Lines { get; set; } = new List<CreateStockMovementLineDto>();
    }

    public class CreateStockMovementLineDto
    {
        public Guid ProductId { get; set; }
        public Guid? VariantId { get; set; }
        public Guid? BatchId { get; set; }
        public Guid? SerialId { get; set; }
        public decimal Qty { get; set; }
        public Guid? UomId { get; set; }
        public decimal UnitCost { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
    }

    // ─── Batches ─────────────────────────────────────────────────────────────

    public class BatchDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSKU { get; set; }
        public Guid? VariantId { get; set; }
        public string BatchNumber { get; set; }
        public string LotNumber { get; set; }
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal QtyOnHand { get; set; }
    }

    public class CreateBatchDto
    {
        public Guid ProductId { get; set; }
        public Guid? VariantId { get; set; }
        public string BatchNumber { get; set; }
        public string LotNumber { get; set; }
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string Notes { get; set; }
    }

    // ─── Serial Numbers ───────────────────────────────────────────────────────

    public class SerialNumberDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSKU { get; set; }
        public Guid? VariantId { get; set; }
        public string SerialNumber { get; set; }
        public Guid? WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public Guid? LocationId { get; set; }
        public string LocationName { get; set; }
        public string Status { get; set; }
        public Guid? BatchId { get; set; }
        public string BatchNumber { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime? SoldAt { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateSerialNumberDto
    {
        public Guid ProductId { get; set; }
        public Guid? VariantId { get; set; }
        public string SerialNumber { get; set; }
        public Guid? WarehouseId { get; set; }
        public Guid? LocationId { get; set; }
        public Guid? BatchId { get; set; }
        public string Notes { get; set; }
    }

    // ─── Stock Reservations ───────────────────────────────────────────────────

    public class StockReservationDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSKU { get; set; }
        public Guid? VariantId { get; set; }
        public string VariantName { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public string SourceType { get; set; }
        public Guid SourceId { get; set; }
        public string SourceRef { get; set; }
        public decimal QtyReserved { get; set; }
        public decimal QtyFulfilled { get; set; }
        public decimal QtyOutstanding { get; set; }
        public string Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateReservationDto
    {
        public Guid ProductId { get; set; }
        public Guid? VariantId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid? LocationId { get; set; }
        public string SourceType { get; set; }
        public Guid SourceId { get; set; }
        public string SourceRef { get; set; }
        public decimal Qty { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    // ─── Low Stock Alerts ─────────────────────────────────────────────────────

    public class LowStockAlertDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSKU { get; set; }
        public string CategoryName { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public decimal QtyOnHand { get; set; }
        public decimal QtyAvailable { get; set; }
        public decimal ReorderPoint { get; set; }
        public decimal ReorderQty { get; set; }
        public string AlertType { get; set; }
        public string Status { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string AcknowledgedByName { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─── Stock Valuation ─────────────────────────────────────────────────────

    public class StockValuationDto
    {
        public Guid ProductId { get; set; }
        public string SKU { get; set; }
        public string ProductName { get; set; }
        public string WarehouseName { get; set; }
        public decimal QtyOnHand { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string Method { get; set; }
    }

    public class StockValuationSummaryDto
    {
        public decimal TotalValue { get; set; }
        public int ProductCount { get; set; }
        public string AsOf { get; set; }
        public List<StockValuationDto> Lines { get; set; } = new List<StockValuationDto>();
    }
}
