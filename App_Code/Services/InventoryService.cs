using System;
using System.Collections.Generic;
using System.Linq;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Caching;
using EidUbahle.Repositories;

namespace EidUbahle.Services
{
    /// <summary>
    /// Inventory Service – Phase 4.
    /// Handles: Categories, Brands, UOM (with default seed), Products, Variants,
    /// Warehouses, Stock Movements (Opening/Adjustment/Transfer/Receipt/Issue),
    /// FIFO/AVCO valuation, Batch/Serial tracking, Low Stock Alerts, Reservation Engine.
    /// </summary>
    public class InventoryService
    {
        private readonly InventoryRepository _repo;
        private readonly IAppCache _cache;

        public InventoryService(string connectionString, IAppCache cache)
        {
            _repo = new InventoryRepository(connectionString);
            _cache = cache;
        }

        // ═══════════════════════════════════════════════════════════════════
        // CATEGORIES
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<CategoryDto>> GetCategories(Guid tenantId, bool activeOnly = true)
        {
            var cacheKey = $"inv:categories:{tenantId}:{activeOnly}";
            var data = _cache.GetOrAdd(cacheKey, () => _repo.GetCategories(tenantId, activeOnly),
                TimeSpan.FromMinutes(10));

            // Build tree
            var map = data.ToDictionary(c => c.Id);
            var roots = new List<CategoryDto>();
            foreach (var c in data)
            {
                if (c.ParentId.HasValue && map.ContainsKey(c.ParentId.Value))
                    map[c.ParentId.Value].Children.Add(c);
                else
                    roots.Add(c);
            }
            return ApiResponseDto<List<CategoryDto>>.Ok(roots);
        }

        public ApiResponseDto<List<CategoryDto>> GetCategoriesFlat(Guid tenantId)
        {
            var data = _repo.GetCategories(tenantId, true);
            return ApiResponseDto<List<CategoryDto>>.Ok(data);
        }

        public ApiResponseDto<CategoryDto> GetCategory(Guid id)
        {
            var data = _repo.GetCategory(id);
            if (data == null) return ApiResponseDto<CategoryDto>.Fail("Category not found", "ERR_NOT_FOUND");
            return ApiResponseDto<CategoryDto>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateCategory(Guid tenantId, Guid userId, CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<Guid>.Fail("Category name is required", "ERR_VALIDATION");
            var id = _repo.CreateCategory(tenantId, userId, dto);
            _cache.Remove($"inv:categories:{tenantId}:True");
            _cache.Remove($"inv:categories:{tenantId}:False");
            return ApiResponseDto<Guid>.Ok(id, "Category created");
        }

        public ApiResponseDto<bool> UpdateCategory(Guid id, Guid tenantId, Guid userId, CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<bool>.Fail("Category name is required", "ERR_VALIDATION");
            var ok = _repo.UpdateCategory(id, userId, dto);
            if (!ok) return ApiResponseDto<bool>.Fail("Category not found", "ERR_NOT_FOUND");
            _cache.Remove($"inv:categories:{tenantId}:True");
            _cache.Remove($"inv:categories:{tenantId}:False");
            return ApiResponseDto<bool>.Ok(true, "Category updated");
        }

        public ApiResponseDto<bool> DeleteCategory(Guid id, Guid tenantId)
        {
            var cat = _repo.GetCategory(id);
            if (cat == null) return ApiResponseDto<bool>.Fail("Category not found", "ERR_NOT_FOUND");
            if (cat.ProductCount > 0)
                return ApiResponseDto<bool>.Fail("Cannot delete category with assigned products", "ERR_HAS_PRODUCTS");
            _repo.DeleteCategory(id);
            _cache.Remove($"inv:categories:{tenantId}:True");
            _cache.Remove($"inv:categories:{tenantId}:False");
            return ApiResponseDto<bool>.Ok(true, "Category deleted");
        }

        // ═══════════════════════════════════════════════════════════════════
        // BRANDS
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<BrandDto>> GetBrands(Guid tenantId, bool activeOnly = true)
        {
            var cacheKey = $"inv:brands:{tenantId}:{activeOnly}";
            var data = _cache.GetOrAdd(cacheKey, () => _repo.GetBrands(tenantId, activeOnly),
                TimeSpan.FromMinutes(10));
            return ApiResponseDto<List<BrandDto>>.Ok(data);
        }

        public ApiResponseDto<BrandDto> GetBrand(Guid id)
        {
            var data = _repo.GetBrand(id);
            if (data == null) return ApiResponseDto<BrandDto>.Fail("Brand not found", "ERR_NOT_FOUND");
            return ApiResponseDto<BrandDto>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateBrand(Guid tenantId, Guid userId, CreateBrandDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<Guid>.Fail("Brand name is required", "ERR_VALIDATION");
            var id = _repo.CreateBrand(tenantId, userId, dto);
            _cache.Remove($"inv:brands:{tenantId}:True");
            return ApiResponseDto<Guid>.Ok(id, "Brand created");
        }

        public ApiResponseDto<bool> UpdateBrand(Guid id, Guid tenantId, CreateBrandDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<bool>.Fail("Brand name is required", "ERR_VALIDATION");
            var ok = _repo.UpdateBrand(id, dto);
            if (!ok) return ApiResponseDto<bool>.Fail("Brand not found", "ERR_NOT_FOUND");
            _cache.Remove($"inv:brands:{tenantId}:True");
            return ApiResponseDto<bool>.Ok(true, "Brand updated");
        }

        public ApiResponseDto<bool> DeleteBrand(Guid id, Guid tenantId)
        {
            var brand = _repo.GetBrand(id);
            if (brand == null) return ApiResponseDto<bool>.Fail("Brand not found", "ERR_NOT_FOUND");
            if (brand.ProductCount > 0)
                return ApiResponseDto<bool>.Fail("Cannot delete brand with assigned products", "ERR_HAS_PRODUCTS");
            _repo.DeleteBrand(id);
            _cache.Remove($"inv:brands:{tenantId}:True");
            return ApiResponseDto<bool>.Ok(true, "Brand deleted");
        }

        // ═══════════════════════════════════════════════════════════════════
        // UNITS OF MEASURE
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<UomDto>> GetUoms(Guid tenantId)
        {
            var cacheKey = $"inv:uoms:{tenantId}";
            var data = _cache.GetOrAdd(cacheKey, () => _repo.GetUoms(tenantId), TimeSpan.FromMinutes(30));
            return ApiResponseDto<List<UomDto>>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateUom(Guid tenantId, Guid userId, CreateUomDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<Guid>.Fail("UOM name is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.Symbol))
                return ApiResponseDto<Guid>.Fail("UOM symbol is required", "ERR_VALIDATION");
            if (dto.ConversionFactor <= 0)
                return ApiResponseDto<Guid>.Fail("Conversion factor must be greater than zero", "ERR_VALIDATION");
            var id = _repo.CreateUom(tenantId, userId, dto);
            _cache.Remove($"inv:uoms:{tenantId}");
            return ApiResponseDto<Guid>.Ok(id, "Unit of measure created");
        }

        public ApiResponseDto<bool> UpdateUom(Guid id, Guid tenantId, CreateUomDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<bool>.Fail("UOM name is required", "ERR_VALIDATION");
            var ok = _repo.UpdateUom(id, dto);
            if (!ok) return ApiResponseDto<bool>.Fail("UOM not found", "ERR_NOT_FOUND");
            _cache.Remove($"inv:uoms:{tenantId}");
            return ApiResponseDto<bool>.Ok(true, "Unit of measure updated");
        }

        /// <summary>Seeds the default units of measure for a new tenant.</summary>
        public ApiResponseDto<bool> SeedDefaultUoms(Guid tenantId, Guid userId)
        {
            _repo.SeedDefaultUoms(tenantId, userId);
            _cache.Remove($"inv:uoms:{tenantId}");
            return ApiResponseDto<bool>.Ok(true, "Default units seeded");
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRODUCTS
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<object> GetProducts(Guid tenantId, string search = null,
            Guid? categoryId = null, Guid? brandId = null, string productType = null,
            bool activeOnly = true, int page = 1, int pageSize = 50)
        {
            page = Math.Max(1, page);
            pageSize = Math.Min(200, Math.Max(1, pageSize));
            var skip = (page - 1) * pageSize;
            var items = _repo.GetProducts(tenantId, search, categoryId, brandId, productType, activeOnly, skip, pageSize);
            var total = _repo.GetProductsCount(tenantId, search, categoryId, brandId, productType, activeOnly);
            return ApiResponseDto<object>.Ok(new { items, total, page, pageSize });
        }

        public ApiResponseDto<ProductDetailDto> GetProduct(Guid id)
        {
            var data = _repo.GetProduct(id);
            if (data == null) return ApiResponseDto<ProductDetailDto>.Fail("Product not found", "ERR_NOT_FOUND");
            return ApiResponseDto<ProductDetailDto>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateProduct(Guid tenantId, Guid userId, CreateProductDto dto)
        {
            var errs = ValidateProduct(dto);
            if (errs != null) return ApiResponseDto<Guid>.Fail(errs, "ERR_VALIDATION");
            var id = _repo.CreateProduct(tenantId, userId, dto);
            return ApiResponseDto<Guid>.Ok(id, "Product created");
        }

        public ApiResponseDto<bool> UpdateProduct(Guid id, Guid tenantId, Guid userId, UpdateProductDto dto)
        {
            var errs = ValidateProduct(dto);
            if (errs != null) return ApiResponseDto<bool>.Fail(errs, "ERR_VALIDATION");
            var ok = _repo.UpdateProduct(id, userId, dto);
            if (!ok) return ApiResponseDto<bool>.Fail("Product not found", "ERR_NOT_FOUND");
            return ApiResponseDto<bool>.Ok(true, "Product updated");
        }

        public ApiResponseDto<bool> DeleteProduct(Guid id, Guid tenantId, Guid userId)
        {
            var product = _repo.GetProduct(id);
            if (product == null) return ApiResponseDto<bool>.Fail("Product not found", "ERR_NOT_FOUND");
            if (product.TotalQtyOnHand > 0)
                return ApiResponseDto<bool>.Fail("Cannot delete product with stock on hand. Please adjust stock to zero first.", "ERR_HAS_STOCK");
            _repo.DeleteProduct(id, userId);
            return ApiResponseDto<bool>.Ok(true, "Product deleted");
        }

        private string ValidateProduct(CreateProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SKU)) return "SKU is required";
            if (string.IsNullOrWhiteSpace(dto.Name)) return "Product name is required";
            var validTypes = new[] { "Storable", "Service", "Consumable" };
            if (!validTypes.Contains(dto.ProductType)) return "Invalid product type";
            var validCosting = new[] { "FIFO", "AVCO" };
            if (!validCosting.Contains(dto.CostingMethod)) return "Invalid costing method (use FIFO or AVCO)";
            var validTracking = new[] { "None", "Batch", "Serial" };
            if (!validTracking.Contains(dto.TrackingType)) return "Invalid tracking type";
            if (dto.SalePrice < 0) return "Sale price cannot be negative";
            if (dto.StandardCost < 0) return "Standard cost cannot be negative";
            if (dto.ReorderPoint < 0) return "Reorder point cannot be negative";
            return null;
        }

        // ═══════════════════════════════════════════════════════════════════
        // WAREHOUSES
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<WarehouseDto>> GetWarehouses(Guid tenantId, Guid? companyId = null)
        {
            var cacheKey = $"inv:warehouses:{tenantId}:{companyId}";
            var data = _cache.GetOrAdd(cacheKey, () => _repo.GetWarehouses(tenantId, companyId),
                TimeSpan.FromMinutes(10));
            return ApiResponseDto<List<WarehouseDto>>.Ok(data);
        }

        public ApiResponseDto<WarehouseDto> GetWarehouse(Guid id)
        {
            var data = _repo.GetWarehouse(id);
            if (data == null) return ApiResponseDto<WarehouseDto>.Fail("Warehouse not found", "ERR_NOT_FOUND");
            return ApiResponseDto<WarehouseDto>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateWarehouse(Guid tenantId, Guid userId, CreateWarehouseDto dto)
        {
            if (dto.CompanyId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Company is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.Code))
                return ApiResponseDto<Guid>.Fail("Warehouse code is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<Guid>.Fail("Warehouse name is required", "ERR_VALIDATION");
            var id = _repo.CreateWarehouse(tenantId, userId, dto);
            _cache.Remove($"inv:warehouses:{tenantId}:{dto.CompanyId}");
            _cache.Remove($"inv:warehouses:{tenantId}:");
            return ApiResponseDto<Guid>.Ok(id, "Warehouse created");
        }

        public ApiResponseDto<bool> UpdateWarehouse(Guid id, Guid tenantId, Guid userId, CreateWarehouseDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<bool>.Fail("Warehouse name is required", "ERR_VALIDATION");
            var ok = _repo.UpdateWarehouse(id, userId, dto);
            if (!ok) return ApiResponseDto<bool>.Fail("Warehouse not found", "ERR_NOT_FOUND");
            _cache.Remove($"inv:warehouses:{tenantId}:{dto.CompanyId}");
            _cache.Remove($"inv:warehouses:{tenantId}:");
            return ApiResponseDto<bool>.Ok(true, "Warehouse updated");
        }

        public ApiResponseDto<bool> DeleteWarehouse(Guid id, Guid tenantId)
        {
            var wh = _repo.GetWarehouse(id);
            if (wh == null) return ApiResponseDto<bool>.Fail("Warehouse not found", "ERR_NOT_FOUND");
            if (wh.IsDefault)
                return ApiResponseDto<bool>.Fail("Cannot delete the default warehouse", "ERR_IS_DEFAULT");
            _repo.DeleteWarehouse(id);
            _cache.Remove($"inv:warehouses:{tenantId}:{wh.CompanyId}");
            _cache.Remove($"inv:warehouses:{tenantId}:");
            return ApiResponseDto<bool>.Ok(true, "Warehouse deleted");
        }

        public ApiResponseDto<Guid> CreateWarehouseLocation(Guid tenantId, Guid userId, CreateWarehouseLocationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                return ApiResponseDto<Guid>.Fail("Location code is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResponseDto<Guid>.Fail("Location name is required", "ERR_VALIDATION");
            var id = _repo.CreateWarehouseLocation(tenantId, userId, dto);
            return ApiResponseDto<Guid>.Ok(id, "Location created");
        }

        // ═══════════════════════════════════════════════════════════════════
        // STOCK LEVELS
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<StockLevelDto>> GetStockLevels(Guid tenantId,
            Guid? warehouseId = null, Guid? productId = null)
        {
            var data = _repo.GetStockLevels(tenantId, warehouseId, productId);
            return ApiResponseDto<List<StockLevelDto>>.Ok(data);
        }

        // ═══════════════════════════════════════════════════════════════════
        // STOCK MOVEMENTS (Opening Stock / Adjustment / Transfer)
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<object> GetMovements(Guid tenantId, Guid? companyId = null,
            string movementType = null, string status = null, int page = 1, int pageSize = 50)
        {
            page = Math.Max(1, page);
            pageSize = Math.Min(100, Math.Max(1, pageSize));
            var skip = (page - 1) * pageSize;
            var items = _repo.GetMovements(tenantId, companyId, movementType, status, skip, pageSize);
            return ApiResponseDto<object>.Ok(new { items, page, pageSize });
        }

        public ApiResponseDto<StockMovementDto> GetMovement(Guid id)
        {
            var data = _repo.GetMovement(id);
            if (data == null) return ApiResponseDto<StockMovementDto>.Fail("Movement not found", "ERR_NOT_FOUND");
            return ApiResponseDto<StockMovementDto>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateMovement(Guid tenantId, Guid userId, CreateStockMovementDto dto)
        {
            if (dto.CompanyId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Company is required", "ERR_VALIDATION");
            if (!IsValidMovementType(dto.MovementType))
                return ApiResponseDto<Guid>.Fail("Invalid movement type", "ERR_VALIDATION");
            if (dto.MovementDate == default)
                return ApiResponseDto<Guid>.Fail("Movement date is required", "ERR_VALIDATION");
            if (dto.Lines == null || dto.Lines.Count == 0)
                return ApiResponseDto<Guid>.Fail("At least one line item is required", "ERR_VALIDATION");

            // Validate transfer has both warehouses
            if (dto.MovementType == "TransferOut" || dto.MovementType == "TransferIn")
            {
                if (!dto.FromWarehouseId.HasValue || !dto.ToWarehouseId.HasValue)
                    return ApiResponseDto<Guid>.Fail("Both source and destination warehouses are required for transfers", "ERR_VALIDATION");
                if (dto.FromWarehouseId == dto.ToWarehouseId)
                    return ApiResponseDto<Guid>.Fail("Source and destination warehouses must be different", "ERR_VALIDATION");
            }

            foreach (var line in dto.Lines)
            {
                if (line.ProductId == Guid.Empty)
                    return ApiResponseDto<Guid>.Fail("Product is required on all lines", "ERR_VALIDATION");
                if (line.Qty == 0)
                    return ApiResponseDto<Guid>.Fail("Quantity cannot be zero", "ERR_VALIDATION");
                if (line.UnitCost < 0)
                    return ApiResponseDto<Guid>.Fail("Unit cost cannot be negative", "ERR_VALIDATION");
            }

            var id = _repo.CreateMovement(tenantId, userId, dto);
            return ApiResponseDto<Guid>.Ok(id, "Stock movement created");
        }

        public ApiResponseDto<bool> PostMovement(Guid movementId, Guid tenantId, Guid userId)
        {
            var m = _repo.GetMovement(movementId);
            if (m == null) return ApiResponseDto<bool>.Fail("Movement not found", "ERR_NOT_FOUND");
            if (m.Status != "Draft")
                return ApiResponseDto<bool>.Fail("Only draft movements can be posted", "ERR_INVALID_STATUS");
            if (m.TenantId != tenantId)
                return ApiResponseDto<bool>.Fail("Access denied", "ERR_FORBIDDEN");

            _repo.PostMovement(movementId, userId);
            return ApiResponseDto<bool>.Ok(true, "Movement posted successfully");
        }

        public ApiResponseDto<bool> DeleteMovement(Guid id, Guid tenantId, Guid userId)
        {
            var m = _repo.GetMovement(id);
            if (m == null) return ApiResponseDto<bool>.Fail("Movement not found", "ERR_NOT_FOUND");
            if (m.Status != "Draft")
                return ApiResponseDto<bool>.Fail("Only draft movements can be deleted", "ERR_INVALID_STATUS");
            if (m.TenantId != tenantId)
                return ApiResponseDto<bool>.Fail("Access denied", "ERR_FORBIDDEN");
            _repo.DeleteMovement(id, userId);
            return ApiResponseDto<bool>.Ok(true, "Movement deleted");
        }

        private bool IsValidMovementType(string type)
        {
            var valid = new[] { "Opening", "Adjustment", "TransferOut", "TransferIn", "Receipt", "Issue" };
            return valid.Contains(type);
        }

        // ═══════════════════════════════════════════════════════════════════
        // BATCHES & SERIAL NUMBERS
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<BatchDto>> GetBatches(Guid tenantId, Guid? productId = null)
        {
            var data = _repo.GetBatches(tenantId, productId);
            return ApiResponseDto<List<BatchDto>>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateBatch(Guid tenantId, Guid userId, CreateBatchDto dto)
        {
            if (dto.ProductId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Product is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.BatchNumber))
                return ApiResponseDto<Guid>.Fail("Batch number is required", "ERR_VALIDATION");
            if (dto.ExpiryDate.HasValue && dto.ManufactureDate.HasValue && dto.ExpiryDate <= dto.ManufactureDate)
                return ApiResponseDto<Guid>.Fail("Expiry date must be after manufacture date", "ERR_VALIDATION");
            var id = _repo.CreateBatch(tenantId, userId, dto);
            return ApiResponseDto<Guid>.Ok(id, "Batch created");
        }

        public ApiResponseDto<List<SerialNumberDto>> GetSerialNumbers(Guid tenantId,
            Guid? productId = null, string status = null)
        {
            var data = _repo.GetSerialNumbers(tenantId, productId, status);
            return ApiResponseDto<List<SerialNumberDto>>.Ok(data);
        }

        public ApiResponseDto<Guid> CreateSerialNumber(Guid tenantId, Guid userId, CreateSerialNumberDto dto)
        {
            if (dto.ProductId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Product is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.SerialNumber))
                return ApiResponseDto<Guid>.Fail("Serial number is required", "ERR_VALIDATION");
            var id = _repo.CreateSerialNumber(tenantId, userId, dto);
            return ApiResponseDto<Guid>.Ok(id, "Serial number created");
        }

        // ═══════════════════════════════════════════════════════════════════
        // STOCK RESERVATIONS
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<StockReservationDto>> GetReservations(Guid tenantId,
            Guid? productId = null, string status = "Active")
        {
            var data = _repo.GetReservations(tenantId, productId, status);
            return ApiResponseDto<List<StockReservationDto>>.Ok(data);
        }

        public ApiResponseDto<Guid> ReserveStock(Guid tenantId, Guid userId, CreateReservationDto dto)
        {
            if (dto.ProductId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Product is required", "ERR_VALIDATION");
            if (dto.WarehouseId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Warehouse is required", "ERR_VALIDATION");
            if (dto.SourceId == Guid.Empty)
                return ApiResponseDto<Guid>.Fail("Source document ID is required", "ERR_VALIDATION");
            if (string.IsNullOrWhiteSpace(dto.SourceType))
                return ApiResponseDto<Guid>.Fail("Source type is required", "ERR_VALIDATION");
            if (dto.Qty <= 0)
                return ApiResponseDto<Guid>.Fail("Quantity must be greater than zero", "ERR_VALIDATION");

            var (rid, error) = _repo.CreateReservation(tenantId, userId, dto);
            if (!string.IsNullOrEmpty(error))
                return ApiResponseDto<Guid>.Fail(error, "ERR_INSUFFICIENT_STOCK");
            return ApiResponseDto<Guid>.Ok(rid, "Stock reserved successfully");
        }

        public ApiResponseDto<bool> ReleaseReservation(Guid reservationId, Guid tenantId)
        {
            _repo.ReleaseReservation(reservationId);
            return ApiResponseDto<bool>.Ok(true, "Reservation released");
        }

        // ═══════════════════════════════════════════════════════════════════
        // LOW STOCK ALERTS
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<List<LowStockAlertDto>> GetLowStockAlerts(Guid tenantId,
            string status = "Active")
        {
            var data = _repo.GetLowStockAlerts(tenantId, status);
            return ApiResponseDto<List<LowStockAlertDto>>.Ok(data);
        }

        public ApiResponseDto<bool> AcknowledgeAlert(Guid alertId, Guid tenantId, Guid userId)
        {
            var ok = _repo.AcknowledgeAlert(alertId, userId);
            if (!ok) return ApiResponseDto<bool>.Fail("Alert not found or already acknowledged", "ERR_NOT_FOUND");
            return ApiResponseDto<bool>.Ok(true, "Alert acknowledged");
        }

        // ═══════════════════════════════════════════════════════════════════
        // STOCK VALUATION (FIFO / AVCO)
        // ═══════════════════════════════════════════════════════════════════

        public ApiResponseDto<StockValuationSummaryDto> GetStockValuation(Guid tenantId,
            Guid? warehouseId = null)
        {
            var lines = _repo.GetStockValuation(tenantId, warehouseId);
            var summary = new StockValuationSummaryDto
            {
                Lines        = lines,
                TotalValue   = lines.Sum(l => l.TotalValue),
                ProductCount = lines.Select(l => l.ProductId).Distinct().Count(),
                AsOf         = DateTime.UtcNow.ToString("yyyy-MM-dd")
            };
            return ApiResponseDto<StockValuationSummaryDto>.Ok(summary);
        }
    }
}
