using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EidUbahle.Domain.DTOs;

namespace EidUbahle.Repositories
{
    /// <summary>
    /// Data access for the Inventory module – Phase 4.
    /// Covers: Categories, Brands, UOM, Products, Variants,
    /// Warehouses, Stock Levels, Movements, Batches, Serials,
    /// Reservations, Low Stock Alerts, Valuation.
    /// All queries are tenant-scoped.
    /// </summary>
    public class InventoryRepository
    {
        private readonly string _conn;

        public InventoryRepository(string connectionString)
        {
            _conn = connectionString;
        }

        // ═══════════════════════════════════════════════════════════════════
        // CATEGORIES
        // ═══════════════════════════════════════════════════════════════════

        public List<CategoryDto> GetCategories(Guid tenantId, bool activeOnly = true)
        {
            var list = new List<CategoryDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = @"SELECT c.Id, c.TenantId, c.ParentId, p.Name AS ParentName,
                                c.Name, c.Code, c.Description, c.ImageUrl,
                                c.SortOrder, c.Level, c.IsActive, c.CreatedAt,
                                (SELECT COUNT(*) FROM inv_Products WHERE CategoryId=c.Id AND IsDeleted=0) AS ProductCount
                            FROM inv_Categories c
                            LEFT JOIN inv_Categories p ON p.Id = c.ParentId
                            WHERE c.TenantId=@TenantId";
                if (activeOnly) sql += " AND c.IsActive=1";
                sql += " ORDER BY c.Level, c.SortOrder, c.Name";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapCategory(r));
                }
            }
            return list;
        }

        public CategoryDto GetCategory(Guid id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT c.Id, c.TenantId, c.ParentId, p.Name AS ParentName,
                                c.Name, c.Code, c.Description, c.ImageUrl,
                                c.SortOrder, c.Level, c.IsActive, c.CreatedAt,
                                (SELECT COUNT(*) FROM inv_Products WHERE CategoryId=c.Id AND IsDeleted=0) AS ProductCount
                            FROM inv_Categories c
                            LEFT JOIN inv_Categories p ON p.Id = c.ParentId
                            WHERE c.Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapCategory(r) : null;
                }
            }
        }

        public Guid CreateCategory(Guid tenantId, Guid userId, CreateCategoryDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                int level = 1;
                if (dto.ParentId.HasValue)
                    level = GetCategoryLevel(conn, dto.ParentId.Value) + 1;

                const string sql = @"INSERT INTO inv_Categories
                    (TenantId,ParentId,Name,Code,Description,ImageUrl,SortOrder,Level,IsActive,CreatedBy)
                    OUTPUT INSERTED.Id
                    VALUES(@TenantId,@ParentId,@Name,@Code,@Desc,@Img,@Sort,@Level,1,@CreatedBy)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@ParentId", (object)dto.ParentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Code", (object)dto.Code ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Desc", (object)dto.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Img", (object)dto.ImageUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Sort", dto.SortOrder);
                    cmd.Parameters.AddWithValue("@Level", level);
                    cmd.Parameters.AddWithValue("@CreatedBy", userId);
                    return (Guid)cmd.ExecuteScalar();
                }
            }
        }

        public bool UpdateCategory(Guid id, Guid userId, CreateCategoryDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"UPDATE inv_Categories SET
                    ParentId=@ParentId, Name=@Name, Code=@Code, Description=@Desc,
                    ImageUrl=@Img, SortOrder=@Sort, UpdatedAt=GETUTCDATE()
                    WHERE Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@ParentId", (object)dto.ParentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Code", (object)dto.Code ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Desc", (object)dto.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Img", (object)dto.ImageUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Sort", dto.SortOrder);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteCategory(Guid id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = "UPDATE inv_Categories SET IsActive=0, UpdatedAt=GETUTCDATE() WHERE Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        private int GetCategoryLevel(SqlConnection conn, Guid parentId)
        {
            const string sql = "SELECT Level FROM inv_Categories WHERE Id=@Id";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", parentId);
                var val = cmd.ExecuteScalar();
                return val != null ? Convert.ToInt32(val) : 1;
            }
        }

        private CategoryDto MapCategory(SqlDataReader r) => new CategoryDto
        {
            Id           = r.GetGuid(r.GetOrdinal("Id")),
            TenantId     = r.GetGuid(r.GetOrdinal("TenantId")),
            ParentId     = r["ParentId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("ParentId")),
            ParentName   = r["ParentName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("ParentName")),
            Name         = r.GetString(r.GetOrdinal("Name")),
            Code         = r["Code"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Code")),
            Description  = r["Description"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Description")),
            ImageUrl     = r["ImageUrl"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("ImageUrl")),
            SortOrder    = r.GetInt32(r.GetOrdinal("SortOrder")),
            Level        = r.GetInt32(r.GetOrdinal("Level")),
            IsActive     = r.GetBoolean(r.GetOrdinal("IsActive")),
            CreatedAt    = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            ProductCount = r.GetInt32(r.GetOrdinal("ProductCount"))
        };

        // ═══════════════════════════════════════════════════════════════════
        // BRANDS
        // ═══════════════════════════════════════════════════════════════════

        public List<BrandDto> GetBrands(Guid tenantId, bool activeOnly = true)
        {
            var list = new List<BrandDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = @"SELECT b.Id, b.TenantId, b.Name, b.Code, b.Description, b.LogoUrl,
                                b.IsActive, b.CreatedAt,
                                (SELECT COUNT(*) FROM inv_Products WHERE BrandId=b.Id AND IsDeleted=0) AS ProductCount
                            FROM inv_Brands b
                            WHERE b.TenantId=@TenantId";
                if (activeOnly) sql += " AND b.IsActive=1";
                sql += " ORDER BY b.Name";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapBrand(r));
                }
            }
            return list;
        }

        public BrandDto GetBrand(Guid id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT b.Id, b.TenantId, b.Name, b.Code, b.Description, b.LogoUrl,
                                b.IsActive, b.CreatedAt,
                                (SELECT COUNT(*) FROM inv_Products WHERE BrandId=b.Id AND IsDeleted=0) AS ProductCount
                            FROM inv_Brands b WHERE b.Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapBrand(r) : null;
                }
            }
        }

        public Guid CreateBrand(Guid tenantId, Guid userId, CreateBrandDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"INSERT INTO inv_Brands(TenantId,Name,Code,Description,LogoUrl,CreatedBy)
                    OUTPUT INSERTED.Id VALUES(@TenantId,@Name,@Code,@Desc,@Logo,@CreatedBy)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Code", (object)dto.Code ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Desc", (object)dto.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Logo", (object)dto.LogoUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", userId);
                    return (Guid)cmd.ExecuteScalar();
                }
            }
        }

        public bool UpdateBrand(Guid id, CreateBrandDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"UPDATE inv_Brands SET Name=@Name,Code=@Code,
                    Description=@Desc,LogoUrl=@Logo,UpdatedAt=GETUTCDATE() WHERE Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Code", (object)dto.Code ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Desc", (object)dto.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Logo", (object)dto.LogoUrl ?? DBNull.Value);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteBrand(Guid id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = "UPDATE inv_Brands SET IsActive=0, UpdatedAt=GETUTCDATE() WHERE Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        private BrandDto MapBrand(SqlDataReader r) => new BrandDto
        {
            Id           = r.GetGuid(r.GetOrdinal("Id")),
            TenantId     = r.GetGuid(r.GetOrdinal("TenantId")),
            Name         = r.GetString(r.GetOrdinal("Name")),
            Code         = r["Code"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Code")),
            Description  = r["Description"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Description")),
            LogoUrl      = r["LogoUrl"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("LogoUrl")),
            IsActive     = r.GetBoolean(r.GetOrdinal("IsActive")),
            CreatedAt    = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            ProductCount = r.GetInt32(r.GetOrdinal("ProductCount"))
        };

        // ═══════════════════════════════════════════════════════════════════
        // UNITS OF MEASURE
        // ═══════════════════════════════════════════════════════════════════

        public List<UomDto> GetUoms(Guid tenantId, bool activeOnly = true)
        {
            var list = new List<UomDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = @"SELECT u.Id, u.TenantId, u.Name, u.Symbol, u.BaseUomId, b.Symbol AS BaseUomSymbol,
                                u.ConversionFactor, u.UomType, u.IsBase, u.IsActive, u.CreatedAt
                            FROM inv_UOM u
                            LEFT JOIN inv_UOM b ON b.Id = u.BaseUomId
                            WHERE u.TenantId=@TenantId";
                if (activeOnly) sql += " AND u.IsActive=1";
                sql += " ORDER BY u.UomType, u.IsBase DESC, u.Name";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapUom(r));
                }
            }
            return list;
        }

        public UomDto GetUom(Guid id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT u.Id, u.TenantId, u.Name, u.Symbol, u.BaseUomId, b.Symbol AS BaseUomSymbol,
                                u.ConversionFactor, u.UomType, u.IsBase, u.IsActive, u.CreatedAt
                            FROM inv_UOM u LEFT JOIN inv_UOM b ON b.Id = u.BaseUomId WHERE u.Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var r = cmd.ExecuteReader())
                        return r.Read() ? MapUom(r) : null;
                }
            }
        }

        public Guid CreateUom(Guid tenantId, Guid userId, CreateUomDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"INSERT INTO inv_UOM(TenantId,Name,Symbol,BaseUomId,ConversionFactor,UomType,IsBase,CreatedBy)
                    OUTPUT INSERTED.Id VALUES(@TenantId,@Name,@Symbol,@BaseUomId,@Factor,@Type,@IsBase,@CreatedBy)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Symbol", dto.Symbol);
                    cmd.Parameters.AddWithValue("@BaseUomId", (object)dto.BaseUomId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Factor", dto.ConversionFactor);
                    cmd.Parameters.AddWithValue("@Type", dto.UomType ?? "Count");
                    cmd.Parameters.AddWithValue("@IsBase", dto.IsBase);
                    cmd.Parameters.AddWithValue("@CreatedBy", userId);
                    return (Guid)cmd.ExecuteScalar();
                }
            }
        }

        public bool UpdateUom(Guid id, CreateUomDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"UPDATE inv_UOM SET Name=@Name, Symbol=@Symbol, BaseUomId=@BaseUomId,
                    ConversionFactor=@Factor, UomType=@Type, IsBase=@IsBase WHERE Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Symbol", dto.Symbol);
                    cmd.Parameters.AddWithValue("@BaseUomId", (object)dto.BaseUomId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Factor", dto.ConversionFactor);
                    cmd.Parameters.AddWithValue("@Type", dto.UomType ?? "Count");
                    cmd.Parameters.AddWithValue("@IsBase", dto.IsBase);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool SeedDefaultUoms(Guid tenantId, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Check if already seeded
                const string check = "SELECT COUNT(*) FROM inv_UOM WHERE TenantId=@TenantId";
                using (var cmd = new SqlCommand(check, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    if ((int)cmd.ExecuteScalar() > 0) return false;
                }
                // Seed base UOMs
                var defaults = new[]
                {
                    new { Name="Each", Symbol="ea", Type="Count", IsBase=true, Factor=1m, Base=(string)null },
                    new { Name="Kilogram", Symbol="kg", Type="Weight", IsBase=true, Factor=1m, Base=(string)null },
                    new { Name="Litre", Symbol="L", Type="Volume", IsBase=true, Factor=1m, Base=(string)null },
                    new { Name="Metre", Symbol="m", Type="Length", IsBase=true, Factor=1m, Base=(string)null },
                };
                var baseIds = new System.Collections.Generic.Dictionary<string, Guid>();
                foreach (var d in defaults)
                {
                    const string ins = @"INSERT INTO inv_UOM(TenantId,Name,Symbol,ConversionFactor,UomType,IsBase,CreatedBy)
                        OUTPUT INSERTED.Id VALUES(@T,@N,@S,@F,@TP,@IB,@CB)";
                    using (var cmd = new SqlCommand(ins, conn))
                    {
                        cmd.Parameters.AddWithValue("@T", tenantId);
                        cmd.Parameters.AddWithValue("@N", d.Name);
                        cmd.Parameters.AddWithValue("@S", d.Symbol);
                        cmd.Parameters.AddWithValue("@F", d.Factor);
                        cmd.Parameters.AddWithValue("@TP", d.Type);
                        cmd.Parameters.AddWithValue("@IB", d.IsBase);
                        cmd.Parameters.AddWithValue("@CB", userId);
                        baseIds[d.Symbol] = (Guid)cmd.ExecuteScalar();
                    }
                }
                // Seed derived UOMs
                var derived = new[]
                {
                    new { Name="Dozen", Symbol="dz", Type="Count", Factor=12m, Base="ea" },
                    new { Name="Gram", Symbol="g", Type="Weight", Factor=0.001m, Base="kg" },
                    new { Name="Tonne", Symbol="t", Type="Weight", Factor=1000m, Base="kg" },
                    new { Name="Pound", Symbol="lb", Type="Weight", Factor=0.453592m, Base="kg" },
                    new { Name="Millilitre", Symbol="mL", Type="Volume", Factor=0.001m, Base="L" },
                    new { Name="Gallon", Symbol="gal", Type="Volume", Factor=3.78541m, Base="L" },
                    new { Name="Centimetre", Symbol="cm", Type="Length", Factor=0.01m, Base="m" },
                    new { Name="Foot", Symbol="ft", Type="Length", Factor=0.3048m, Base="m" },
                };
                foreach (var d in derived)
                {
                    if (!baseIds.ContainsKey(d.Base)) continue;
                    const string ins = @"INSERT INTO inv_UOM(TenantId,Name,Symbol,BaseUomId,ConversionFactor,UomType,IsBase,CreatedBy)
                        VALUES(@T,@N,@S,@BID,@F,@TP,0,@CB)";
                    using (var cmd = new SqlCommand(ins, conn))
                    {
                        cmd.Parameters.AddWithValue("@T", tenantId);
                        cmd.Parameters.AddWithValue("@N", d.Name);
                        cmd.Parameters.AddWithValue("@S", d.Symbol);
                        cmd.Parameters.AddWithValue("@BID", baseIds[d.Base]);
                        cmd.Parameters.AddWithValue("@F", d.Factor);
                        cmd.Parameters.AddWithValue("@TP", d.Type);
                        cmd.Parameters.AddWithValue("@CB", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
        }

        private UomDto MapUom(SqlDataReader r) => new UomDto
        {
            Id               = r.GetGuid(r.GetOrdinal("Id")),
            TenantId         = r.GetGuid(r.GetOrdinal("TenantId")),
            Name             = r.GetString(r.GetOrdinal("Name")),
            Symbol           = r.GetString(r.GetOrdinal("Symbol")),
            BaseUomId        = r["BaseUomId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("BaseUomId")),
            BaseUomSymbol    = r["BaseUomSymbol"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("BaseUomSymbol")),
            ConversionFactor = r.GetDecimal(r.GetOrdinal("ConversionFactor")),
            UomType          = r.GetString(r.GetOrdinal("UomType")),
            IsBase           = r.GetBoolean(r.GetOrdinal("IsBase")),
            IsActive         = r.GetBoolean(r.GetOrdinal("IsActive")),
            CreatedAt        = r.GetDateTime(r.GetOrdinal("CreatedAt"))
        };

        // ═══════════════════════════════════════════════════════════════════
        // PRODUCTS
        // ═══════════════════════════════════════════════════════════════════

        public List<ProductListItemDto> GetProducts(Guid tenantId, string search = null,
            Guid? categoryId = null, Guid? brandId = null, string productType = null,
            bool activeOnly = true, int skip = 0, int take = 50)
        {
            var list = new List<ProductListItemDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = @"SELECT p.Id, p.SKU, p.Barcode, p.Name, c.Name AS CategoryName,
                                b.Name AS BrandName, u.Symbol AS StockUomSymbol,
                                p.ProductType, p.CostingMethod, p.TrackingType,
                                p.StandardCost, p.SalePrice, p.IsActive, p.CreatedAt,
                                ISNULL((SELECT SUM(QtyOnHand) FROM inv_StockLevels WHERE ProductId=p.Id),0) AS TotalQtyOnHand
                            FROM inv_Products p
                            LEFT JOIN inv_Categories c ON c.Id = p.CategoryId
                            LEFT JOIN inv_Brands b ON b.Id = p.BrandId
                            LEFT JOIN inv_UOM u ON u.Id = p.StockUomId
                            WHERE p.TenantId=@TenantId AND p.IsDeleted=0";
                if (activeOnly) sql += " AND p.IsActive=1";
                if (!string.IsNullOrEmpty(search)) sql += " AND (p.SKU LIKE @Search OR p.Name LIKE @Search OR p.Barcode LIKE @Search)";
                if (categoryId.HasValue) sql += " AND p.CategoryId=@CategoryId";
                if (brandId.HasValue) sql += " AND p.BrandId=@BrandId";
                if (!string.IsNullOrEmpty(productType)) sql += " AND p.ProductType=@ProductType";
                sql += " ORDER BY p.Name OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                    if (categoryId.HasValue) cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value);
                    if (brandId.HasValue) cmd.Parameters.AddWithValue("@BrandId", brandId.Value);
                    if (!string.IsNullOrEmpty(productType)) cmd.Parameters.AddWithValue("@ProductType", productType);
                    cmd.Parameters.AddWithValue("@Skip", skip);
                    cmd.Parameters.AddWithValue("@Take", take);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapProductListItem(r));
                }
            }
            return list;
        }

        public int GetProductsCount(Guid tenantId, string search = null,
            Guid? categoryId = null, Guid? brandId = null, string productType = null, bool activeOnly = true)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = "SELECT COUNT(*) FROM inv_Products p WHERE p.TenantId=@TenantId AND p.IsDeleted=0";
                if (activeOnly) sql += " AND p.IsActive=1";
                if (!string.IsNullOrEmpty(search)) sql += " AND (p.SKU LIKE @Search OR p.Name LIKE @Search OR p.Barcode LIKE @Search)";
                if (categoryId.HasValue) sql += " AND p.CategoryId=@CategoryId";
                if (brandId.HasValue) sql += " AND p.BrandId=@BrandId";
                if (!string.IsNullOrEmpty(productType)) sql += " AND p.ProductType=@ProductType";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                    if (categoryId.HasValue) cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value);
                    if (brandId.HasValue) cmd.Parameters.AddWithValue("@BrandId", brandId.Value);
                    if (!string.IsNullOrEmpty(productType)) cmd.Parameters.AddWithValue("@ProductType", productType);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public ProductDetailDto GetProduct(Guid id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT p.Id, p.SKU, p.Barcode, p.Name, p.ShortName, p.Description,
                                p.CategoryId, c.Name AS CategoryName,
                                p.BrandId, b.Name AS BrandName,
                                p.PurchaseUomId, pu.Symbol AS PurchaseUomSymbol,
                                p.SalesUomId, su.Symbol AS SalesUomSymbol,
                                p.StockUomId, stu.Symbol AS StockUomSymbol,
                                p.ProductType, p.CostingMethod, p.TrackingType,
                                p.StandardCost, p.SalePrice, p.MinSalePrice, p.TaxRate,
                                p.ImageUrl, p.Notes, p.ReorderPoint, p.ReorderQty,
                                p.IsSaleable, p.IsPurchaseable, p.IsActive,
                                p.InventoryAccountId, p.COGSAccountId, p.RevenueAccountId,
                                p.CreatedAt, p.UpdatedAt,
                                ISNULL((SELECT SUM(QtyOnHand) FROM inv_StockLevels WHERE ProductId=p.Id),0) AS TotalQtyOnHand
                            FROM inv_Products p
                            LEFT JOIN inv_Categories c ON c.Id = p.CategoryId
                            LEFT JOIN inv_Brands b ON b.Id = p.BrandId
                            LEFT JOIN inv_UOM pu ON pu.Id = p.PurchaseUomId
                            LEFT JOIN inv_UOM su ON su.Id = p.SalesUomId
                            LEFT JOIN inv_UOM stu ON stu.Id = p.StockUomId
                            WHERE p.Id=@Id AND p.IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        var dto = MapProductDetail(r);
                        r.Close();
                        dto.StockLevels = GetStockLevelsForProduct(conn, id);
                        return dto;
                    }
                }
            }
        }

        public Guid CreateProduct(Guid tenantId, Guid userId, CreateProductDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"INSERT INTO inv_Products
                    (TenantId,SKU,Barcode,Name,ShortName,Description,CategoryId,BrandId,
                     PurchaseUomId,SalesUomId,StockUomId,ProductType,CostingMethod,
                     StandardCost,SalePrice,MinSalePrice,TaxRate,ImageUrl,Notes,
                     ReorderPoint,ReorderQty,TrackingType,IsSaleable,IsPurchaseable,
                     InventoryAccountId,COGSAccountId,RevenueAccountId,CreatedBy)
                    OUTPUT INSERTED.Id
                    VALUES(@TenantId,@SKU,@Barcode,@Name,@Short,@Desc,@CatId,@BrandId,
                           @PurchUom,@SaleUom,@StockUom,@Type,@Costing,
                           @StdCost,@SalePrice,@MinPrice,@Tax,@Img,@Notes,
                           @Reorder,@ReorderQty,@Tracking,@Saleable,@Purchaseable,
                           @InvAcct,@COGS,@Revenue,@CreatedBy)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    AddProductParams(cmd, tenantId, userId, dto);
                    return (Guid)cmd.ExecuteScalar();
                }
            }
        }

        public bool UpdateProduct(Guid id, Guid userId, UpdateProductDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"UPDATE inv_Products SET
                    SKU=@SKU, Barcode=@Barcode, Name=@Name, ShortName=@Short, Description=@Desc,
                    CategoryId=@CatId, BrandId=@BrandId,
                    PurchaseUomId=@PurchUom, SalesUomId=@SaleUom, StockUomId=@StockUom,
                    ProductType=@Type, CostingMethod=@Costing,
                    StandardCost=@StdCost, SalePrice=@SalePrice, MinSalePrice=@MinPrice, TaxRate=@Tax,
                    ImageUrl=@Img, Notes=@Notes,
                    ReorderPoint=@Reorder, ReorderQty=@ReorderQty, TrackingType=@Tracking,
                    IsSaleable=@Saleable, IsPurchaseable=@Purchaseable,
                    InventoryAccountId=@InvAcct, COGSAccountId=@COGS, RevenueAccountId=@Revenue,
                    IsActive=@IsActive, UpdatedBy=@CreatedBy, UpdatedAt=GETUTCDATE()
                    WHERE Id=@Id AND IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    AddProductParams(cmd, Guid.Empty, userId, dto);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteProduct(Guid id, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"UPDATE inv_Products SET IsDeleted=1, DeletedAt=GETUTCDATE(),
                    UpdatedBy=@UserId, UpdatedAt=GETUTCDATE() WHERE Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        private void AddProductParams(SqlCommand cmd, Guid tenantId, Guid userId, CreateProductDto dto)
        {
            if (tenantId != Guid.Empty) cmd.Parameters.AddWithValue("@TenantId", tenantId);
            cmd.Parameters.AddWithValue("@SKU", dto.SKU ?? "");
            cmd.Parameters.AddWithValue("@Barcode", (object)dto.Barcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Name", dto.Name ?? "");
            cmd.Parameters.AddWithValue("@Short", (object)dto.ShortName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Desc", (object)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CatId", (object)dto.CategoryId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BrandId", (object)dto.BrandId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PurchUom", (object)dto.PurchaseUomId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SaleUom", (object)dto.SalesUomId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StockUom", (object)dto.StockUomId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Type", dto.ProductType ?? "Storable");
            cmd.Parameters.AddWithValue("@Costing", dto.CostingMethod ?? "AVCO");
            cmd.Parameters.AddWithValue("@StdCost", dto.StandardCost);
            cmd.Parameters.AddWithValue("@SalePrice", dto.SalePrice);
            cmd.Parameters.AddWithValue("@MinPrice", dto.MinSalePrice);
            cmd.Parameters.AddWithValue("@Tax", dto.TaxRate);
            cmd.Parameters.AddWithValue("@Img", (object)dto.ImageUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object)dto.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Reorder", dto.ReorderPoint);
            cmd.Parameters.AddWithValue("@ReorderQty", dto.ReorderQty);
            cmd.Parameters.AddWithValue("@Tracking", dto.TrackingType ?? "None");
            cmd.Parameters.AddWithValue("@Saleable", dto.IsSaleable);
            cmd.Parameters.AddWithValue("@Purchaseable", dto.IsPurchaseable);
            cmd.Parameters.AddWithValue("@InvAcct", (object)dto.InventoryAccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@COGS", (object)dto.COGSAccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Revenue", (object)dto.RevenueAccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", userId);
        }

        private ProductListItemDto MapProductListItem(SqlDataReader r) => new ProductListItemDto
        {
            Id             = r.GetGuid(r.GetOrdinal("Id")),
            SKU            = r.GetString(r.GetOrdinal("SKU")),
            Barcode        = r["Barcode"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Barcode")),
            Name           = r.GetString(r.GetOrdinal("Name")),
            CategoryName   = r["CategoryName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("CategoryName")),
            BrandName      = r["BrandName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("BrandName")),
            StockUomSymbol = r["StockUomSymbol"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("StockUomSymbol")),
            ProductType    = r.GetString(r.GetOrdinal("ProductType")),
            CostingMethod  = r.GetString(r.GetOrdinal("CostingMethod")),
            TrackingType   = r.GetString(r.GetOrdinal("TrackingType")),
            StandardCost   = r.GetDecimal(r.GetOrdinal("StandardCost")),
            SalePrice      = r.GetDecimal(r.GetOrdinal("SalePrice")),
            TotalQtyOnHand = r.GetDecimal(r.GetOrdinal("TotalQtyOnHand")),
            IsActive       = r.GetBoolean(r.GetOrdinal("IsActive")),
            CreatedAt      = r.GetDateTime(r.GetOrdinal("CreatedAt"))
        };

        private ProductDetailDto MapProductDetail(SqlDataReader r) => new ProductDetailDto
        {
            Id                  = r.GetGuid(r.GetOrdinal("Id")),
            SKU                 = r.GetString(r.GetOrdinal("SKU")),
            Barcode             = r["Barcode"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Barcode")),
            Name                = r.GetString(r.GetOrdinal("Name")),
            ShortName           = r["ShortName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("ShortName")),
            Description         = r["Description"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Description")),
            CategoryId          = r["CategoryId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("CategoryId")),
            CategoryName        = r["CategoryName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("CategoryName")),
            BrandId             = r["BrandId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("BrandId")),
            BrandName           = r["BrandName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("BrandName")),
            PurchaseUomId       = r["PurchaseUomId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("PurchaseUomId")),
            PurchaseUomSymbol   = r["PurchaseUomSymbol"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("PurchaseUomSymbol")),
            SalesUomId          = r["SalesUomId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("SalesUomId")),
            SalesUomSymbol      = r["SalesUomSymbol"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("SalesUomSymbol")),
            StockUomId          = r["StockUomId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("StockUomId")),
            StockUomSymbol      = r["StockUomSymbol"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("StockUomSymbol")),
            ProductType         = r.GetString(r.GetOrdinal("ProductType")),
            CostingMethod       = r.GetString(r.GetOrdinal("CostingMethod")),
            TrackingType        = r.GetString(r.GetOrdinal("TrackingType")),
            StandardCost        = r.GetDecimal(r.GetOrdinal("StandardCost")),
            SalePrice           = r.GetDecimal(r.GetOrdinal("SalePrice")),
            MinSalePrice        = r["MinSalePrice"] == DBNull.Value ? 0m : r.GetDecimal(r.GetOrdinal("MinSalePrice")),
            TaxRate             = r.GetDecimal(r.GetOrdinal("TaxRate")),
            ImageUrl            = r["ImageUrl"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("ImageUrl")),
            Notes               = r["Notes"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Notes")),
            ReorderPoint        = r.GetDecimal(r.GetOrdinal("ReorderPoint")),
            ReorderQty          = r.GetDecimal(r.GetOrdinal("ReorderQty")),
            IsSaleable          = r.GetBoolean(r.GetOrdinal("IsSaleable")),
            IsPurchaseable      = r.GetBoolean(r.GetOrdinal("IsPurchaseable")),
            IsActive            = r.GetBoolean(r.GetOrdinal("IsActive")),
            InventoryAccountId  = r["InventoryAccountId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("InventoryAccountId")),
            COGSAccountId       = r["COGSAccountId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("COGSAccountId")),
            RevenueAccountId    = r["RevenueAccountId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("RevenueAccountId")),
            TotalQtyOnHand      = r.GetDecimal(r.GetOrdinal("TotalQtyOnHand")),
            CreatedAt           = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            UpdatedAt           = r.GetDateTime(r.GetOrdinal("UpdatedAt"))
        };

        // ═══════════════════════════════════════════════════════════════════
        // WAREHOUSES
        // ═══════════════════════════════════════════════════════════════════

        public List<WarehouseDto> GetWarehouses(Guid tenantId, Guid? companyId = null, bool activeOnly = true)
        {
            var list = new List<WarehouseDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = @"SELECT w.Id, w.TenantId, w.CompanyId, c.Name AS CompanyName,
                                w.BranchId, br.Name AS BranchName,
                                w.Code, w.Name, w.Address, w.ContactPerson, w.ContactPhone,
                                w.IsDefault, w.IsActive, w.CreatedAt,
                                (SELECT COUNT(*) FROM inv_WarehouseLocations WHERE WarehouseId=w.Id AND IsActive=1) AS LocationCount
                            FROM inv_Warehouses w
                            JOIN saas_Companies c ON c.Id = w.CompanyId
                            LEFT JOIN saas_Branches br ON br.Id = w.BranchId
                            WHERE w.TenantId=@TenantId";
                if (activeOnly) sql += " AND w.IsActive=1";
                if (companyId.HasValue) sql += " AND w.CompanyId=@CompanyId";
                sql += " ORDER BY w.IsDefault DESC, w.Name";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    if (companyId.HasValue) cmd.Parameters.AddWithValue("@CompanyId", companyId.Value);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapWarehouse(r));
                }
            }
            return list;
        }

        public WarehouseDto GetWarehouse(Guid id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT w.Id, w.TenantId, w.CompanyId, c.Name AS CompanyName,
                                w.BranchId, br.Name AS BranchName,
                                w.Code, w.Name, w.Address, w.ContactPerson, w.ContactPhone,
                                w.IsDefault, w.IsActive, w.CreatedAt,
                                (SELECT COUNT(*) FROM inv_WarehouseLocations WHERE WarehouseId=w.Id AND IsActive=1) AS LocationCount
                            FROM inv_Warehouses w
                            JOIN saas_Companies c ON c.Id = w.CompanyId
                            LEFT JOIN saas_Branches br ON br.Id = w.BranchId
                            WHERE w.Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        var wh = MapWarehouse(r);
                        r.Close();
                        wh.Locations = GetWarehouseLocations(conn, id);
                        return wh;
                    }
                }
            }
        }

        public Guid CreateWarehouse(Guid tenantId, Guid userId, CreateWarehouseDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                // If IsDefault, unset other defaults for this company
                if (dto.IsDefault)
                    using (var cmd = new SqlCommand("UPDATE inv_Warehouses SET IsDefault=0 WHERE CompanyId=@CId", conn))
                    {
                        cmd.Parameters.AddWithValue("@CId", dto.CompanyId);
                        cmd.ExecuteNonQuery();
                    }
                const string sql = @"INSERT INTO inv_Warehouses
                    (TenantId,CompanyId,BranchId,Code,Name,Address,ContactPerson,ContactPhone,IsDefault,CreatedBy)
                    OUTPUT INSERTED.Id
                    VALUES(@TenantId,@CompanyId,@BranchId,@Code,@Name,@Addr,@Contact,@Phone,@IsDefault,@CreatedBy)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
                    cmd.Parameters.AddWithValue("@BranchId", (object)dto.BranchId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Code", dto.Code ?? "");
                    cmd.Parameters.AddWithValue("@Name", dto.Name ?? "");
                    cmd.Parameters.AddWithValue("@Addr", (object)dto.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Contact", (object)dto.ContactPerson ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", (object)dto.ContactPhone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsDefault", dto.IsDefault);
                    cmd.Parameters.AddWithValue("@CreatedBy", userId);
                    return (Guid)cmd.ExecuteScalar();
                }
            }
        }

        public bool UpdateWarehouse(Guid id, Guid userId, CreateWarehouseDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                if (dto.IsDefault)
                    using (var cmd = new SqlCommand("UPDATE inv_Warehouses SET IsDefault=0 WHERE CompanyId=@CId AND Id<>@Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@CId", dto.CompanyId);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                const string sql = @"UPDATE inv_Warehouses SET
                    BranchId=@BranchId, Code=@Code, Name=@Name, Address=@Addr,
                    ContactPerson=@Contact, ContactPhone=@Phone, IsDefault=@IsDefault,
                    UpdatedAt=GETUTCDATE() WHERE Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@BranchId", (object)dto.BranchId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Code", dto.Code ?? "");
                    cmd.Parameters.AddWithValue("@Name", dto.Name ?? "");
                    cmd.Parameters.AddWithValue("@Addr", (object)dto.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Contact", (object)dto.ContactPerson ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", (object)dto.ContactPhone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsDefault", dto.IsDefault);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteWarehouse(Guid id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = "UPDATE inv_Warehouses SET IsActive=0, UpdatedAt=GETUTCDATE() WHERE Id=@Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public Guid CreateWarehouseLocation(Guid tenantId, Guid userId, CreateWarehouseLocationDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"INSERT INTO inv_WarehouseLocations(TenantId,WarehouseId,Code,Name,LocationType)
                    OUTPUT INSERTED.Id VALUES(@TenantId,@WhId,@Code,@Name,@Type)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@WhId", dto.WarehouseId);
                    cmd.Parameters.AddWithValue("@Code", dto.Code ?? "");
                    cmd.Parameters.AddWithValue("@Name", dto.Name ?? "");
                    cmd.Parameters.AddWithValue("@Type", dto.LocationType ?? "Storage");
                    return (Guid)cmd.ExecuteScalar();
                }
            }
        }

        private List<WarehouseLocationDto> GetWarehouseLocations(SqlConnection conn, Guid warehouseId)
        {
            var list = new List<WarehouseLocationDto>();
            const string sql = @"SELECT Id, WarehouseId, Code, Name, LocationType, IsActive
                                 FROM inv_WarehouseLocations WHERE WarehouseId=@WhId AND IsActive=1 ORDER BY Code";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@WhId", warehouseId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new WarehouseLocationDto
                        {
                            Id           = r.GetGuid(0),
                            WarehouseId  = r.GetGuid(1),
                            Code         = r.GetString(2),
                            Name         = r.GetString(3),
                            LocationType = r.GetString(4),
                            IsActive     = r.GetBoolean(5)
                        });
            }
            return list;
        }

        private WarehouseDto MapWarehouse(SqlDataReader r) => new WarehouseDto
        {
            Id            = r.GetGuid(r.GetOrdinal("Id")),
            TenantId      = r.GetGuid(r.GetOrdinal("TenantId")),
            CompanyId     = r.GetGuid(r.GetOrdinal("CompanyId")),
            CompanyName   = r.GetString(r.GetOrdinal("CompanyName")),
            BranchId      = r["BranchId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("BranchId")),
            BranchName    = r["BranchName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("BranchName")),
            Code          = r.GetString(r.GetOrdinal("Code")),
            Name          = r.GetString(r.GetOrdinal("Name")),
            Address       = r["Address"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Address")),
            ContactPerson = r["ContactPerson"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("ContactPerson")),
            ContactPhone  = r["ContactPhone"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("ContactPhone")),
            IsDefault     = r.GetBoolean(r.GetOrdinal("IsDefault")),
            IsActive      = r.GetBoolean(r.GetOrdinal("IsActive")),
            CreatedAt     = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            LocationCount = r.GetInt32(r.GetOrdinal("LocationCount"))
        };

        // ═══════════════════════════════════════════════════════════════════
        // STOCK LEVELS
        // ═══════════════════════════════════════════════════════════════════

        public List<StockLevelDto> GetStockLevels(Guid tenantId, Guid? warehouseId = null,
            Guid? productId = null)
        {
            var list = new List<StockLevelDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                list = GetStockLevelsFiltered(conn, tenantId, warehouseId, productId);
            }
            return list;
        }

        private List<StockLevelDto> GetStockLevelsForProduct(SqlConnection conn, Guid productId)
        {
            return GetStockLevelsFiltered(conn, Guid.Empty, null, productId);
        }

        private List<StockLevelDto> GetStockLevelsFiltered(SqlConnection conn, Guid tenantId,
            Guid? warehouseId, Guid? productId)
        {
            var list = new List<StockLevelDto>();
            var sql = @"SELECT sl.Id, sl.ProductId, p.Name AS ProductName, p.SKU AS ProductSKU,
                            sl.VariantId, v.Name AS VariantName,
                            sl.WarehouseId, w.Name AS WarehouseName,
                            sl.LocationId, l.Name AS LocationName,
                            sl.QtyOnHand, sl.QtyReserved, sl.QtyOnHand-sl.QtyReserved AS QtyAvailable,
                            sl.QtyOnOrder, sl.AvgCost, sl.QtyOnHand*sl.AvgCost AS TotalValue,
                            sl.UpdatedAt
                        FROM inv_StockLevels sl
                        JOIN inv_Products p ON p.Id = sl.ProductId
                        JOIN inv_Warehouses w ON w.Id = sl.WarehouseId
                        LEFT JOIN inv_ProductVariants v ON v.Id = sl.VariantId
                        LEFT JOIN inv_WarehouseLocations l ON l.Id = sl.LocationId
                        WHERE 1=1";
            if (tenantId != Guid.Empty) sql += " AND sl.TenantId=@TenantId";
            if (warehouseId.HasValue) sql += " AND sl.WarehouseId=@WarehouseId";
            if (productId.HasValue) sql += " AND sl.ProductId=@ProductId";
            sql += " ORDER BY p.Name, w.Name";
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (tenantId != Guid.Empty) cmd.Parameters.AddWithValue("@TenantId", tenantId);
                if (warehouseId.HasValue) cmd.Parameters.AddWithValue("@WarehouseId", warehouseId.Value);
                if (productId.HasValue) cmd.Parameters.AddWithValue("@ProductId", productId.Value);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new StockLevelDto
                        {
                            Id            = r.GetGuid(r.GetOrdinal("Id")),
                            ProductId     = r.GetGuid(r.GetOrdinal("ProductId")),
                            ProductName   = r.GetString(r.GetOrdinal("ProductName")),
                            ProductSKU    = r.GetString(r.GetOrdinal("ProductSKU")),
                            VariantId     = r["VariantId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("VariantId")),
                            VariantName   = r["VariantName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("VariantName")),
                            WarehouseId   = r.GetGuid(r.GetOrdinal("WarehouseId")),
                            WarehouseName = r.GetString(r.GetOrdinal("WarehouseName")),
                            LocationId    = r["LocationId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("LocationId")),
                            LocationName  = r["LocationName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("LocationName")),
                            QtyOnHand     = r.GetDecimal(r.GetOrdinal("QtyOnHand")),
                            QtyReserved   = r.GetDecimal(r.GetOrdinal("QtyReserved")),
                            QtyAvailable  = r.GetDecimal(r.GetOrdinal("QtyAvailable")),
                            QtyOnOrder    = r.GetDecimal(r.GetOrdinal("QtyOnOrder")),
                            AvgCost       = r.GetDecimal(r.GetOrdinal("AvgCost")),
                            TotalValue    = r.GetDecimal(r.GetOrdinal("TotalValue")),
                            UpdatedAt     = r.GetDateTime(r.GetOrdinal("UpdatedAt"))
                        });
            }
            return list;
        }

        // ═══════════════════════════════════════════════════════════════════
        // STOCK MOVEMENTS
        // ═══════════════════════════════════════════════════════════════════

        public List<StockMovementDto> GetMovements(Guid tenantId, Guid? companyId = null,
            string movementType = null, string status = null, int skip = 0, int take = 50)
        {
            var list = new List<StockMovementDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = @"SELECT m.Id, m.TenantId, m.CompanyId, c.Name AS CompanyName,
                                m.MovementNumber, m.MovementType, m.MovementDate, m.Reference, m.Notes,
                                m.FromWarehouseId, fw.Name AS FromWarehouseName,
                                m.ToWarehouseId, tw.Name AS ToWarehouseName,
                                m.Status, m.PostedAt, pu.FullName AS PostedByName,
                                m.CreatedAt, cu.FullName AS CreatedByName,
                                (SELECT COUNT(*) FROM inv_StockMovementLines WHERE MovementId=m.Id) AS TotalLines,
                                ISNULL((SELECT SUM(Qty*UnitCost) FROM inv_StockMovementLines WHERE MovementId=m.Id),0) AS TotalValue
                            FROM inv_StockMovements m
                            JOIN saas_Companies c ON c.Id = m.CompanyId
                            LEFT JOIN inv_Warehouses fw ON fw.Id = m.FromWarehouseId
                            LEFT JOIN inv_Warehouses tw ON tw.Id = m.ToWarehouseId
                            LEFT JOIN sys_Users pu ON pu.Id = m.PostedBy
                            LEFT JOIN sys_Users cu ON cu.Id = m.CreatedBy
                            WHERE m.TenantId=@TenantId AND m.IsDeleted=0";
                if (companyId.HasValue) sql += " AND m.CompanyId=@CompanyId";
                if (!string.IsNullOrEmpty(movementType)) sql += " AND m.MovementType=@Type";
                if (!string.IsNullOrEmpty(status)) sql += " AND m.Status=@Status";
                sql += " ORDER BY m.MovementDate DESC, m.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    if (companyId.HasValue) cmd.Parameters.AddWithValue("@CompanyId", companyId.Value);
                    if (!string.IsNullOrEmpty(movementType)) cmd.Parameters.AddWithValue("@Type", movementType);
                    if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@Skip", skip);
                    cmd.Parameters.AddWithValue("@Take", take);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapMovement(r));
                }
            }
            return list;
        }

        public StockMovementDto GetMovement(Guid id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"SELECT m.Id, m.TenantId, m.CompanyId, c.Name AS CompanyName,
                                m.MovementNumber, m.MovementType, m.MovementDate, m.Reference, m.Notes,
                                m.FromWarehouseId, fw.Name AS FromWarehouseName,
                                m.ToWarehouseId, tw.Name AS ToWarehouseName,
                                m.Status, m.PostedAt, pu.FullName AS PostedByName,
                                m.CreatedAt, cu.FullName AS CreatedByName,
                                (SELECT COUNT(*) FROM inv_StockMovementLines WHERE MovementId=m.Id) AS TotalLines,
                                ISNULL((SELECT SUM(Qty*UnitCost) FROM inv_StockMovementLines WHERE MovementId=m.Id),0) AS TotalValue
                            FROM inv_StockMovements m
                            JOIN saas_Companies c ON c.Id = m.CompanyId
                            LEFT JOIN inv_Warehouses fw ON fw.Id = m.FromWarehouseId
                            LEFT JOIN inv_Warehouses tw ON tw.Id = m.ToWarehouseId
                            LEFT JOIN sys_Users pu ON pu.Id = m.PostedBy
                            LEFT JOIN sys_Users cu ON cu.Id = m.CreatedBy
                            WHERE m.Id=@Id AND m.IsDeleted=0";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        var m = MapMovement(r);
                        r.Close();
                        m.Lines = GetMovementLines(conn, id);
                        return m;
                    }
                }
            }
        }

        public Guid CreateMovement(Guid tenantId, Guid userId, CreateStockMovementDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Generate document number
                var number = "";
                using (var cmd = new SqlCommand("sp_GenerateMovementNumber", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
                    cmd.Parameters.AddWithValue("@MovementType", dto.MovementType ?? "Adjustment");
                    var outParam = cmd.Parameters.Add("@Number", SqlDbType.NVarChar, 30);
                    outParam.Direction = ParameterDirection.Output;
                    cmd.ExecuteNonQuery();
                    number = outParam.Value?.ToString() ?? "MOV-DRAFT";
                }

                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        Guid movId;
                        const string ins = @"INSERT INTO inv_StockMovements
                            (TenantId,CompanyId,MovementNumber,MovementType,MovementDate,Reference,Notes,
                             FromWarehouseId,ToWarehouseId,Status,CreatedBy)
                            OUTPUT INSERTED.Id
                            VALUES(@TenantId,@CompanyId,@Number,@Type,@Date,@Ref,@Notes,@FromWH,@ToWH,'Draft',@CreatedBy)";
                        using (var cmd = new SqlCommand(ins, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@TenantId", tenantId);
                            cmd.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
                            cmd.Parameters.AddWithValue("@Number", number);
                            cmd.Parameters.AddWithValue("@Type", dto.MovementType ?? "Adjustment");
                            cmd.Parameters.AddWithValue("@Date", dto.MovementDate.Date);
                            cmd.Parameters.AddWithValue("@Ref", (object)dto.Reference ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Notes", (object)dto.Notes ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@FromWH", (object)dto.FromWarehouseId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ToWH", (object)dto.ToWarehouseId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@CreatedBy", userId);
                            movId = (Guid)cmd.ExecuteScalar();
                        }
                        int lineNum = 1;
                        foreach (var line in dto.Lines)
                        {
                            const string insl = @"INSERT INTO inv_StockMovementLines
                                (TenantId,MovementId,ProductId,VariantId,BatchId,SerialId,
                                 LineNumber,Qty,UomId,UnitCost,Reason,Notes)
                                VALUES(@TenantId,@MovId,@ProdId,@VarId,@BatchId,@SerialId,
                                       @LineNum,@Qty,@UomId,@Cost,@Reason,@Notes)";
                            using (var cmd = new SqlCommand(insl, conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                                cmd.Parameters.AddWithValue("@MovId", movId);
                                cmd.Parameters.AddWithValue("@ProdId", line.ProductId);
                                cmd.Parameters.AddWithValue("@VarId", (object)line.VariantId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@BatchId", (object)line.BatchId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SerialId", (object)line.SerialId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@LineNum", lineNum++);
                                cmd.Parameters.AddWithValue("@Qty", line.Qty);
                                cmd.Parameters.AddWithValue("@UomId", (object)line.UomId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Cost", line.UnitCost);
                                cmd.Parameters.AddWithValue("@Reason", (object)line.Reason ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Notes", (object)line.Notes ?? DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                        return movId;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool PostMovement(Guid movementId, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("sp_PostStockMovement", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@MovementId", movementId);
                    cmd.Parameters.AddWithValue("@PostedBy", userId);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }

        public bool DeleteMovement(Guid id, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"UPDATE inv_StockMovements
                    SET IsDeleted=1, UpdatedAt=GETUTCDATE() WHERE Id=@Id AND Status='Draft'";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        private List<StockMovementLineDto> GetMovementLines(SqlConnection conn, Guid movementId)
        {
            var list = new List<StockMovementLineDto>();
            const string sql = @"SELECT ml.Id, ml.MovementId, ml.ProductId, p.Name AS ProductName, p.SKU,
                                ml.VariantId, v.Name AS VariantName,
                                ml.BatchId, b.BatchNumber,
                                ml.SerialId, s.SerialNumber,
                                ml.LineNumber, ml.Qty, ml.UomId, u.Symbol AS UomSymbol,
                                ml.UnitCost, ml.Qty*ml.UnitCost AS TotalCost,
                                ml.Reason, ml.Notes
                             FROM inv_StockMovementLines ml
                             JOIN inv_Products p ON p.Id = ml.ProductId
                             LEFT JOIN inv_ProductVariants v ON v.Id = ml.VariantId
                             LEFT JOIN inv_Batches b ON b.Id = ml.BatchId
                             LEFT JOIN inv_SerialNumbers s ON s.Id = ml.SerialId
                             LEFT JOIN inv_UOM u ON u.Id = ml.UomId
                             WHERE ml.MovementId=@MovId ORDER BY ml.LineNumber";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MovId", movementId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new StockMovementLineDto
                        {
                            Id          = r.GetGuid(r.GetOrdinal("Id")),
                            MovementId  = r.GetGuid(r.GetOrdinal("MovementId")),
                            ProductId   = r.GetGuid(r.GetOrdinal("ProductId")),
                            ProductName = r.GetString(r.GetOrdinal("ProductName")),
                            ProductSKU  = r.GetString(r.GetOrdinal("SKU")),
                            VariantId   = r["VariantId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("VariantId")),
                            VariantName = r["VariantName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("VariantName")),
                            BatchId     = r["BatchId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("BatchId")),
                            BatchNumber = r["BatchNumber"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("BatchNumber")),
                            SerialId    = r["SerialId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("SerialId")),
                            SerialNumber= r["SerialNumber"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("SerialNumber")),
                            LineNumber  = r.GetInt32(r.GetOrdinal("LineNumber")),
                            Qty         = r.GetDecimal(r.GetOrdinal("Qty")),
                            UomId       = r["UomId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("UomId")),
                            UomSymbol   = r["UomSymbol"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("UomSymbol")),
                            UnitCost    = r.GetDecimal(r.GetOrdinal("UnitCost")),
                            TotalCost   = r.GetDecimal(r.GetOrdinal("TotalCost")),
                            Reason      = r["Reason"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Reason")),
                            Notes       = r["Notes"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Notes"))
                        });
            }
            return list;
        }

        private StockMovementDto MapMovement(SqlDataReader r) => new StockMovementDto
        {
            Id                = r.GetGuid(r.GetOrdinal("Id")),
            TenantId          = r.GetGuid(r.GetOrdinal("TenantId")),
            CompanyId         = r.GetGuid(r.GetOrdinal("CompanyId")),
            CompanyName       = r.GetString(r.GetOrdinal("CompanyName")),
            MovementNumber    = r.GetString(r.GetOrdinal("MovementNumber")),
            MovementType      = r.GetString(r.GetOrdinal("MovementType")),
            MovementDate      = r.GetDateTime(r.GetOrdinal("MovementDate")),
            Reference         = r["Reference"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Reference")),
            Notes             = r["Notes"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Notes")),
            FromWarehouseId   = r["FromWarehouseId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("FromWarehouseId")),
            FromWarehouseName = r["FromWarehouseName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("FromWarehouseName")),
            ToWarehouseId     = r["ToWarehouseId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("ToWarehouseId")),
            ToWarehouseName   = r["ToWarehouseName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("ToWarehouseName")),
            Status            = r.GetString(r.GetOrdinal("Status")),
            PostedAt          = r["PostedAt"] == DBNull.Value ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("PostedAt")),
            PostedByName      = r["PostedByName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("PostedByName")),
            CreatedAt         = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            CreatedByName     = r["CreatedByName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("CreatedByName")),
            TotalLines        = r.GetInt32(r.GetOrdinal("TotalLines")),
            TotalValue        = r.GetDecimal(r.GetOrdinal("TotalValue"))
        };

        // ═══════════════════════════════════════════════════════════════════
        // BATCHES
        // ═══════════════════════════════════════════════════════════════════

        public List<BatchDto> GetBatches(Guid tenantId, Guid? productId = null)
        {
            var list = new List<BatchDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = @"SELECT b.Id, b.TenantId, b.ProductId, p.Name AS ProductName, p.SKU,
                                b.VariantId, b.BatchNumber, b.LotNumber,
                                b.ManufactureDate, b.ExpiryDate, b.ReceivedDate, b.Notes, b.IsActive, b.CreatedAt,
                                ISNULL((SELECT SUM(QtyOnHand) FROM inv_StockLevels sl WHERE sl.ProductId=b.ProductId),0) AS QtyOnHand
                            FROM inv_Batches b
                            JOIN inv_Products p ON p.Id = b.ProductId
                            WHERE b.TenantId=@TenantId AND b.IsActive=1";
                if (productId.HasValue) sql += " AND b.ProductId=@ProductId";
                sql += " ORDER BY b.ReceivedDate DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    if (productId.HasValue) cmd.Parameters.AddWithValue("@ProductId", productId.Value);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new BatchDto
                            {
                                Id              = r.GetGuid(r.GetOrdinal("Id")),
                                TenantId        = r.GetGuid(r.GetOrdinal("TenantId")),
                                ProductId       = r.GetGuid(r.GetOrdinal("ProductId")),
                                ProductName     = r.GetString(r.GetOrdinal("ProductName")),
                                ProductSKU      = r.GetString(r.GetOrdinal("SKU")),
                                VariantId       = r["VariantId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("VariantId")),
                                BatchNumber     = r.GetString(r.GetOrdinal("BatchNumber")),
                                LotNumber       = r["LotNumber"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("LotNumber")),
                                ManufactureDate = r["ManufactureDate"] == DBNull.Value ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("ManufactureDate")),
                                ExpiryDate      = r["ExpiryDate"] == DBNull.Value ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("ExpiryDate")),
                                ReceivedDate    = r.GetDateTime(r.GetOrdinal("ReceivedDate")),
                                Notes           = r["Notes"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Notes")),
                                IsActive        = r.GetBoolean(r.GetOrdinal("IsActive")),
                                CreatedAt       = r.GetDateTime(r.GetOrdinal("CreatedAt")),
                                QtyOnHand       = r.GetDecimal(r.GetOrdinal("QtyOnHand"))
                            });
                }
            }
            return list;
        }

        public Guid CreateBatch(Guid tenantId, Guid userId, CreateBatchDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"INSERT INTO inv_Batches
                    (TenantId,ProductId,VariantId,BatchNumber,LotNumber,
                     ManufactureDate,ExpiryDate,ReceivedDate,Notes,CreatedBy)
                    OUTPUT INSERTED.Id
                    VALUES(@TenantId,@ProdId,@VarId,@Batch,@Lot,@Mfg,@Exp,@Recv,@Notes,@CreatedBy)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@ProdId", dto.ProductId);
                    cmd.Parameters.AddWithValue("@VarId", (object)dto.VariantId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Batch", dto.BatchNumber ?? "");
                    cmd.Parameters.AddWithValue("@Lot", (object)dto.LotNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Mfg", (object)dto.ManufactureDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Exp", (object)dto.ExpiryDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Recv", dto.ReceivedDate.Date);
                    cmd.Parameters.AddWithValue("@Notes", (object)dto.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", userId);
                    return (Guid)cmd.ExecuteScalar();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // SERIAL NUMBERS
        // ═══════════════════════════════════════════════════════════════════

        public List<SerialNumberDto> GetSerialNumbers(Guid tenantId, Guid? productId = null,
            string status = null)
        {
            var list = new List<SerialNumberDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = @"SELECT sn.Id, sn.TenantId, sn.ProductId, p.Name AS ProductName, p.SKU,
                                sn.VariantId, sn.SerialNumber,
                                sn.WarehouseId, w.Name AS WarehouseName,
                                sn.LocationId, l.Name AS LocationName,
                                sn.Status, sn.BatchId, b.BatchNumber,
                                sn.ReceivedAt, sn.SoldAt, sn.Notes, sn.CreatedAt
                            FROM inv_SerialNumbers sn
                            JOIN inv_Products p ON p.Id = sn.ProductId
                            LEFT JOIN inv_Warehouses w ON w.Id = sn.WarehouseId
                            LEFT JOIN inv_WarehouseLocations l ON l.Id = sn.LocationId
                            LEFT JOIN inv_Batches b ON b.Id = sn.BatchId
                            WHERE sn.TenantId=@TenantId";
                if (productId.HasValue) sql += " AND sn.ProductId=@ProductId";
                if (!string.IsNullOrEmpty(status)) sql += " AND sn.Status=@Status";
                sql += " ORDER BY sn.ReceivedAt DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    if (productId.HasValue) cmd.Parameters.AddWithValue("@ProductId", productId.Value);
                    if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@Status", status);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new SerialNumberDto
                            {
                                Id            = r.GetGuid(r.GetOrdinal("Id")),
                                TenantId      = r.GetGuid(r.GetOrdinal("TenantId")),
                                ProductId     = r.GetGuid(r.GetOrdinal("ProductId")),
                                ProductName   = r.GetString(r.GetOrdinal("ProductName")),
                                ProductSKU    = r.GetString(r.GetOrdinal("SKU")),
                                VariantId     = r["VariantId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("VariantId")),
                                SerialNumber  = r.GetString(r.GetOrdinal("SerialNumber")),
                                WarehouseId   = r["WarehouseId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("WarehouseId")),
                                WarehouseName = r["WarehouseName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("WarehouseName")),
                                LocationId    = r["LocationId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("LocationId")),
                                LocationName  = r["LocationName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("LocationName")),
                                Status        = r.GetString(r.GetOrdinal("Status")),
                                BatchId       = r["BatchId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("BatchId")),
                                BatchNumber   = r["BatchNumber"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("BatchNumber")),
                                ReceivedAt    = r.GetDateTime(r.GetOrdinal("ReceivedAt")),
                                SoldAt        = r["SoldAt"] == DBNull.Value ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("SoldAt")),
                                Notes         = r["Notes"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("Notes")),
                                CreatedAt     = r.GetDateTime(r.GetOrdinal("CreatedAt"))
                            });
                }
            }
            return list;
        }

        public Guid CreateSerialNumber(Guid tenantId, Guid userId, CreateSerialNumberDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"INSERT INTO inv_SerialNumbers
                    (TenantId,ProductId,VariantId,SerialNumber,WarehouseId,LocationId,BatchId,Notes)
                    OUTPUT INSERTED.Id
                    VALUES(@TenantId,@ProdId,@VarId,@Serial,@WhId,@LocId,@BatchId,@Notes)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@ProdId", dto.ProductId);
                    cmd.Parameters.AddWithValue("@VarId", (object)dto.VariantId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Serial", dto.SerialNumber ?? "");
                    cmd.Parameters.AddWithValue("@WhId", (object)dto.WarehouseId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LocId", (object)dto.LocationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BatchId", (object)dto.BatchId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", (object)dto.Notes ?? DBNull.Value);
                    return (Guid)cmd.ExecuteScalar();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // STOCK RESERVATIONS
        // ═══════════════════════════════════════════════════════════════════

        public List<StockReservationDto> GetReservations(Guid tenantId, Guid? productId = null,
            string status = "Active")
        {
            var list = new List<StockReservationDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = @"SELECT r.Id, r.TenantId, r.ProductId, p.Name AS ProductName, p.SKU,
                                r.VariantId, v.Name AS VariantName,
                                r.WarehouseId, w.Name AS WarehouseName,
                                r.SourceType, r.SourceId, r.SourceRef,
                                r.QtyReserved, r.QtyFulfilled,
                                r.QtyReserved-r.QtyFulfilled AS QtyOutstanding,
                                r.Status, r.ExpiresAt, r.CreatedAt
                            FROM inv_StockReservations r
                            JOIN inv_Products p ON p.Id = r.ProductId
                            JOIN inv_Warehouses w ON w.Id = r.WarehouseId
                            LEFT JOIN inv_ProductVariants v ON v.Id = r.VariantId
                            WHERE r.TenantId=@TenantId";
                if (productId.HasValue) sql += " AND r.ProductId=@ProductId";
                if (!string.IsNullOrEmpty(status)) sql += " AND r.Status=@Status";
                sql += " ORDER BY r.CreatedAt DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    if (productId.HasValue) cmd.Parameters.AddWithValue("@ProductId", productId.Value);
                    if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@Status", status);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new StockReservationDto
                            {
                                Id             = r.GetGuid(r.GetOrdinal("Id")),
                                TenantId       = r.GetGuid(r.GetOrdinal("TenantId")),
                                ProductId      = r.GetGuid(r.GetOrdinal("ProductId")),
                                ProductName    = r.GetString(r.GetOrdinal("ProductName")),
                                ProductSKU     = r.GetString(r.GetOrdinal("SKU")),
                                VariantId      = r["VariantId"] == DBNull.Value ? (Guid?)null : r.GetGuid(r.GetOrdinal("VariantId")),
                                VariantName    = r["VariantName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("VariantName")),
                                WarehouseId    = r.GetGuid(r.GetOrdinal("WarehouseId")),
                                WarehouseName  = r.GetString(r.GetOrdinal("WarehouseName")),
                                SourceType     = r.GetString(r.GetOrdinal("SourceType")),
                                SourceId       = r.GetGuid(r.GetOrdinal("SourceId")),
                                SourceRef      = r["SourceRef"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("SourceRef")),
                                QtyReserved    = r.GetDecimal(r.GetOrdinal("QtyReserved")),
                                QtyFulfilled   = r.GetDecimal(r.GetOrdinal("QtyFulfilled")),
                                QtyOutstanding = r.GetDecimal(r.GetOrdinal("QtyOutstanding")),
                                Status         = r.GetString(r.GetOrdinal("Status")),
                                ExpiresAt      = r["ExpiresAt"] == DBNull.Value ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("ExpiresAt")),
                                CreatedAt      = r.GetDateTime(r.GetOrdinal("CreatedAt"))
                            });
                }
            }
            return list;
        }

        public (Guid reservationId, string error) CreateReservation(Guid tenantId, Guid userId, CreateReservationDto dto)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("sp_ReserveStock", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@ProductId", dto.ProductId);
                    cmd.Parameters.AddWithValue("@VariantId", (object)dto.VariantId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WarehouseId", dto.WarehouseId);
                    cmd.Parameters.AddWithValue("@Qty", dto.Qty);
                    cmd.Parameters.AddWithValue("@SourceType", dto.SourceType ?? "ManualHold");
                    cmd.Parameters.AddWithValue("@SourceId", dto.SourceId);
                    cmd.Parameters.AddWithValue("@SourceRef", (object)dto.SourceRef ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", userId);
                    cmd.Parameters.AddWithValue("@ExpiresAt", (object)dto.ExpiresAt ?? DBNull.Value);
                    var ridParam = cmd.Parameters.Add("@ReservationId", SqlDbType.UniqueIdentifier);
                    ridParam.Direction = ParameterDirection.Output;
                    var errParam = cmd.Parameters.Add("@Error", SqlDbType.NVarChar, 500);
                    errParam.Direction = ParameterDirection.Output;
                    cmd.ExecuteNonQuery();
                    var rid = ridParam.Value == DBNull.Value ? Guid.Empty : (Guid)ridParam.Value;
                    var err = errParam.Value == DBNull.Value ? null : errParam.Value.ToString();
                    return (rid, err);
                }
            }
        }

        public bool ReleaseReservation(Guid reservationId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("sp_ReleaseReservation", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ReservationId", reservationId);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // LOW STOCK ALERTS
        // ═══════════════════════════════════════════════════════════════════

        public List<LowStockAlertDto> GetLowStockAlerts(Guid tenantId, string status = "Active")
        {
            var list = new List<LowStockAlertDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var sql = @"SELECT a.Id, a.TenantId, a.ProductId, p.Name AS ProductName, p.SKU,
                                c.Name AS CategoryName,
                                a.WarehouseId, w.Name AS WarehouseName,
                                a.QtyOnHand, a.QtyAvailable, a.ReorderPoint, a.ReorderQty,
                                a.AlertType, a.Status,
                                a.AcknowledgedAt, u.FullName AS AcknowledgedByName,
                                a.ResolvedAt, a.CreatedAt
                            FROM inv_LowStockAlerts a
                            JOIN inv_Products p ON p.Id = a.ProductId
                            JOIN inv_Warehouses w ON w.Id = a.WarehouseId
                            LEFT JOIN inv_Categories c ON c.Id = p.CategoryId
                            LEFT JOIN sys_Users u ON u.Id = a.AcknowledgedBy
                            WHERE a.TenantId=@TenantId";
                if (!string.IsNullOrEmpty(status)) sql += " AND a.Status=@Status";
                sql += " ORDER BY a.CreatedAt DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@Status", status);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new LowStockAlertDto
                            {
                                Id                  = r.GetGuid(r.GetOrdinal("Id")),
                                TenantId            = r.GetGuid(r.GetOrdinal("TenantId")),
                                ProductId           = r.GetGuid(r.GetOrdinal("ProductId")),
                                ProductName         = r.GetString(r.GetOrdinal("ProductName")),
                                ProductSKU          = r.GetString(r.GetOrdinal("SKU")),
                                CategoryName        = r["CategoryName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("CategoryName")),
                                WarehouseId         = r.GetGuid(r.GetOrdinal("WarehouseId")),
                                WarehouseName       = r.GetString(r.GetOrdinal("WarehouseName")),
                                QtyOnHand           = r.GetDecimal(r.GetOrdinal("QtyOnHand")),
                                QtyAvailable        = r.GetDecimal(r.GetOrdinal("QtyAvailable")),
                                ReorderPoint        = r.GetDecimal(r.GetOrdinal("ReorderPoint")),
                                ReorderQty          = r.GetDecimal(r.GetOrdinal("ReorderQty")),
                                AlertType           = r.GetString(r.GetOrdinal("AlertType")),
                                Status              = r.GetString(r.GetOrdinal("Status")),
                                AcknowledgedAt      = r["AcknowledgedAt"] == DBNull.Value ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("AcknowledgedAt")),
                                AcknowledgedByName  = r["AcknowledgedByName"] == DBNull.Value ? null : r.GetString(r.GetOrdinal("AcknowledgedByName")),
                                ResolvedAt          = r["ResolvedAt"] == DBNull.Value ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("ResolvedAt")),
                                CreatedAt           = r.GetDateTime(r.GetOrdinal("CreatedAt"))
                            });
                }
            }
            return list;
        }

        public bool AcknowledgeAlert(Guid alertId, Guid userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                const string sql = @"UPDATE inv_LowStockAlerts
                    SET Status='Acknowledged', AcknowledgedAt=GETUTCDATE(), AcknowledgedBy=@UserId
                    WHERE Id=@Id AND Status='Active'";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", alertId);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // STOCK VALUATION
        // ═══════════════════════════════════════════════════════════════════

        public List<StockValuationDto> GetStockValuation(Guid tenantId, Guid? warehouseId = null)
        {
            var list = new List<StockValuationDto>();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("sp_GetStockValuation", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TenantId", tenantId);
                    cmd.Parameters.AddWithValue("@WarehouseId", (object)warehouseId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AsOf", DBNull.Value);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new StockValuationDto
                            {
                                ProductId   = r.GetGuid(r.GetOrdinal("ProductId")),
                                SKU         = r.GetString(r.GetOrdinal("SKU")),
                                ProductName = r.GetString(r.GetOrdinal("ProductName")),
                                WarehouseName = r.GetString(r.GetOrdinal("WarehouseName")),
                                QtyOnHand   = r.GetDecimal(r.GetOrdinal("QtyOnHand")),
                                UnitCost    = r.GetDecimal(r.GetOrdinal("UnitCost")),
                                TotalValue  = r.GetDecimal(r.GetOrdinal("TotalValue")),
                                Method      = r.GetString(r.GetOrdinal("Method"))
                            });
                }
            }
            return list;
        }
    }
}
