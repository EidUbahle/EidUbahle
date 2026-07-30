<%@ WebHandler Language="C#" Class="ProductsHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Products.ashx – Products, Categories, Brands, Units of Measure CRUD.
/// Query string: entity=product|category|brand|uom
/// </summary>
public class ProductsHandler : IHttpHandler
{
    private static readonly JavaScriptSerializer _json =
        new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

    public bool IsReusable => false;

    public void ProcessRequest(HttpContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.Cache.SetNoStore();

        if (!IsXhr(ctx.Request)) { WriteError(ctx, 400, "ERR_INVALID_REQUEST", "Invalid request"); return; }

        var claims = ctx.Items["JwtClaims"] as System.Collections.Generic.Dictionary<string, string>;
        if (claims == null) { WriteError(ctx, 401, "ERR_UNAUTHORIZED", "Authentication required"); return; }

        if (!Guid.TryParse(claims.GetValueOrDefault("tid"), out var tenantId) ||
            !Guid.TryParse(claims.GetValueOrDefault("sub"), out var userId))
        { WriteError(ctx, 401, "ERR_INVALID_TOKEN", "Invalid token claims"); return; }

        var method = ctx.Request.HttpMethod.ToUpper();
        var entity = (ctx.Request.QueryString["entity"] ?? "product").ToLowerInvariant();
        var action = (ctx.Request.QueryString["action"] ?? "").ToLowerInvariant();

        try
        {
            switch (entity)
            {
                case "product":  HandleProduct(ctx, method, action, tenantId, userId, claims); break;
                case "category": HandleCategory(ctx, method, action, tenantId, userId, claims); break;
                case "brand":    HandleBrand(ctx, method, action, tenantId, userId, claims); break;
                case "uom":      HandleUom(ctx, method, action, tenantId, userId, claims); break;
                default: WriteError(ctx, 400, "ERR_INVALID_ENTITY", "Unknown entity type"); break;
            }
        }
        catch (Exception ex)
        {
            WriteError(ctx, 500, "ERR_SERVER",
                ConfigHelper.IsProduction ? "Internal server error" : ex.Message);
        }
    }

    // ── Products ─────────────────────────────────────────────────────────────

    private void HandleProduct(HttpContext ctx, string method, string action,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var svc = ServiceLocator.InventoryService;
        switch (method)
        {
            case "GET":
                RequirePerm(ctx, claims, "inventory.product.view");
                var idStr = ctx.Request.QueryString["id"];
                if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var pId))
                {
                    Write(ctx, svc.GetProduct(pId));
                }
                else
                {
                    var search   = ctx.Request.QueryString["search"];
                    var catStr   = ctx.Request.QueryString["categoryId"];
                    var brandStr = ctx.Request.QueryString["brandId"];
                    var typeStr  = ctx.Request.QueryString["type"];
                    Guid? catId  = !string.IsNullOrEmpty(catStr) && Guid.TryParse(catStr, out var cg) ? cg : (Guid?)null;
                    Guid? brId   = !string.IsNullOrEmpty(brandStr) && Guid.TryParse(brandStr, out var bg) ? bg : (Guid?)null;
                    int.TryParse(ctx.Request.QueryString["page"] ?? "1", out var page);
                    int.TryParse(ctx.Request.QueryString["pageSize"] ?? "50", out var pageSize);
                    Write(ctx, svc.GetProducts(tenantId, search, catId, brId, typeStr, true, page, pageSize));
                }
                break;

            case "POST":
                if (action == "")
                {
                    RequirePerm(ctx, claims, "inventory.product.create");
                    var dto = Deserialize<CreateProductDto>(ctx);
                    Write(ctx, svc.CreateProduct(tenantId, userId, dto));
                }
                break;

            case "PUT":
                RequirePerm(ctx, claims, "inventory.product.edit");
                if (Guid.TryParse(ctx.Request.QueryString["id"], out var updateId))
                {
                    var dto = Deserialize<UpdateProductDto>(ctx);
                    Write(ctx, svc.UpdateProduct(updateId, tenantId, userId, dto));
                }
                else WriteError(ctx, 400, "ERR_MISSING_ID", "Product ID required");
                break;

            case "DELETE":
                RequirePerm(ctx, claims, "inventory.product.delete");
                if (Guid.TryParse(ctx.Request.QueryString["id"], out var deleteId))
                    Write(ctx, svc.DeleteProduct(deleteId, tenantId, userId));
                else WriteError(ctx, 400, "ERR_MISSING_ID", "Product ID required");
                break;

            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed");
                break;
        }
    }

    // ── Categories ───────────────────────────────────────────────────────────

    private void HandleCategory(HttpContext ctx, string method, string action,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var svc = ServiceLocator.InventoryService;
        switch (method)
        {
            case "GET":
                RequirePerm(ctx, claims, "inventory.category.view");
                var idStr = ctx.Request.QueryString["id"];
                if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var cId))
                    Write(ctx, svc.GetCategory(cId));
                else if (ctx.Request.QueryString["flat"] == "1")
                    Write(ctx, svc.GetCategoriesFlat(tenantId));
                else
                    Write(ctx, svc.GetCategories(tenantId));
                break;

            case "POST":
                RequirePerm(ctx, claims, "inventory.category.create");
                Write(ctx, svc.CreateCategory(tenantId, userId, Deserialize<CreateCategoryDto>(ctx)));
                break;

            case "PUT":
                RequirePerm(ctx, claims, "inventory.category.edit");
                if (Guid.TryParse(ctx.Request.QueryString["id"], out var updId))
                    Write(ctx, svc.UpdateCategory(updId, tenantId, userId, Deserialize<CreateCategoryDto>(ctx)));
                else WriteError(ctx, 400, "ERR_MISSING_ID", "Category ID required");
                break;

            case "DELETE":
                RequirePerm(ctx, claims, "inventory.category.delete");
                if (Guid.TryParse(ctx.Request.QueryString["id"], out var delId))
                    Write(ctx, svc.DeleteCategory(delId, tenantId));
                else WriteError(ctx, 400, "ERR_MISSING_ID", "Category ID required");
                break;

            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed");
                break;
        }
    }

    // ── Brands ───────────────────────────────────────────────────────────────

    private void HandleBrand(HttpContext ctx, string method, string action,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var svc = ServiceLocator.InventoryService;
        switch (method)
        {
            case "GET":
                RequirePerm(ctx, claims, "inventory.brand.view");
                var idStr = ctx.Request.QueryString["id"];
                if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var bId))
                    Write(ctx, svc.GetBrand(bId));
                else
                    Write(ctx, svc.GetBrands(tenantId));
                break;

            case "POST":
                RequirePerm(ctx, claims, "inventory.brand.create");
                Write(ctx, svc.CreateBrand(tenantId, userId, Deserialize<CreateBrandDto>(ctx)));
                break;

            case "PUT":
                RequirePerm(ctx, claims, "inventory.brand.edit");
                if (Guid.TryParse(ctx.Request.QueryString["id"], out var updId))
                    Write(ctx, svc.UpdateBrand(updId, tenantId, Deserialize<CreateBrandDto>(ctx)));
                else WriteError(ctx, 400, "ERR_MISSING_ID", "Brand ID required");
                break;

            case "DELETE":
                RequirePerm(ctx, claims, "inventory.brand.delete");
                if (Guid.TryParse(ctx.Request.QueryString["id"], out var delId))
                    Write(ctx, svc.DeleteBrand(delId, tenantId));
                else WriteError(ctx, 400, "ERR_MISSING_ID", "Brand ID required");
                break;

            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed");
                break;
        }
    }

    // ── Units of Measure ─────────────────────────────────────────────────────

    private void HandleUom(HttpContext ctx, string method, string action,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var svc = ServiceLocator.InventoryService;
        switch (method)
        {
            case "GET":
                RequirePerm(ctx, claims, "inventory.uom.view");
                Write(ctx, svc.GetUoms(tenantId));
                break;

            case "POST":
                if (action == "seed")
                {
                    RequirePerm(ctx, claims, "inventory.uom.create");
                    Write(ctx, svc.SeedDefaultUoms(tenantId, userId));
                }
                else
                {
                    RequirePerm(ctx, claims, "inventory.uom.create");
                    Write(ctx, svc.CreateUom(tenantId, userId, Deserialize<CreateUomDto>(ctx)));
                }
                break;

            case "PUT":
                RequirePerm(ctx, claims, "inventory.uom.edit");
                if (Guid.TryParse(ctx.Request.QueryString["id"], out var updId))
                    Write(ctx, svc.UpdateUom(updId, tenantId, Deserialize<CreateUomDto>(ctx)));
                else WriteError(ctx, 400, "ERR_MISSING_ID", "UOM ID required");
                break;

            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed");
                break;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void RequirePerm(HttpContext ctx,
        System.Collections.Generic.Dictionary<string, string> claims, string perm)
    {
        var perms = claims.GetValueOrDefault("perms") ?? "";
        if (!perms.Contains(perm) && !claims.GetValueOrDefault("role")?.Contains("SuperAdmin") == true)
        {
            WriteError(ctx, 403, "ERR_FORBIDDEN", $"Permission '{perm}' required");
            ctx.Response.End();
        }
    }

    private static T Deserialize<T>(HttpContext ctx)
    {
        var body = new System.IO.StreamReader(ctx.Request.InputStream).ReadToEnd();
        return _json.Deserialize<T>(body);
    }

    private static void Write(HttpContext ctx, object result)
    {
        ctx.Response.Write(_json.Serialize(result));
    }

    private static void WriteError(HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.Write(_json.Serialize(new { success = false, errorCode = code, message }));
    }

    private static bool IsXhr(HttpRequest req) =>
        string.Equals(req.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
        || req.ContentType?.Contains("application/json") == true
        || req.Path.EndsWith(".ashx", StringComparison.OrdinalIgnoreCase);
}
