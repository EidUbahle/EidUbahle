<%@ WebHandler Language="C#" Class="WarehousesHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Warehouses.ashx – Warehouse and Warehouse Location CRUD.
/// Also serves stock levels per warehouse.
/// </summary>
public class WarehousesHandler : IHttpHandler
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
        var entity = (ctx.Request.QueryString["entity"] ?? "warehouse").ToLowerInvariant();
        var action = (ctx.Request.QueryString["action"] ?? "").ToLowerInvariant();

        try
        {
            switch (entity)
            {
                case "warehouse": HandleWarehouse(ctx, method, action, tenantId, userId, claims); break;
                case "location":  HandleLocation(ctx, method, action, tenantId, userId, claims); break;
                case "stock":     HandleStockLevels(ctx, method, tenantId, userId, claims); break;
                default: WriteError(ctx, 400, "ERR_INVALID_ENTITY", "Unknown entity type"); break;
            }
        }
        catch (Exception ex)
        {
            WriteError(ctx, 500, "ERR_SERVER",
                ConfigHelper.IsProduction ? "Internal server error" : ex.Message);
        }
    }

    private void HandleWarehouse(HttpContext ctx, string method, string action,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var svc = ServiceLocator.InventoryService;
        switch (method)
        {
            case "GET":
                RequirePerm(ctx, claims, "inventory.warehouse.view");
                var idStr = ctx.Request.QueryString["id"];
                if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var wId))
                    Write(ctx, svc.GetWarehouse(wId));
                else
                {
                    Guid? companyId = null;
                    var cStr = ctx.Request.QueryString["companyId"];
                    if (!string.IsNullOrEmpty(cStr) && Guid.TryParse(cStr, out var cg)) companyId = cg;
                    Write(ctx, svc.GetWarehouses(tenantId, companyId));
                }
                break;

            case "POST":
                RequirePerm(ctx, claims, "inventory.warehouse.create");
                Write(ctx, svc.CreateWarehouse(tenantId, userId, Deserialize<CreateWarehouseDto>(ctx)));
                break;

            case "PUT":
                RequirePerm(ctx, claims, "inventory.warehouse.edit");
                if (Guid.TryParse(ctx.Request.QueryString["id"], out var updId))
                    Write(ctx, svc.UpdateWarehouse(updId, tenantId, userId, Deserialize<CreateWarehouseDto>(ctx)));
                else WriteError(ctx, 400, "ERR_MISSING_ID", "Warehouse ID required");
                break;

            case "DELETE":
                RequirePerm(ctx, claims, "inventory.warehouse.delete");
                if (Guid.TryParse(ctx.Request.QueryString["id"], out var delId))
                    Write(ctx, svc.DeleteWarehouse(delId, tenantId));
                else WriteError(ctx, 400, "ERR_MISSING_ID", "Warehouse ID required");
                break;

            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed");
                break;
        }
    }

    private void HandleLocation(HttpContext ctx, string method, string action,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var svc = ServiceLocator.InventoryService;
        switch (method)
        {
            case "POST":
                RequirePerm(ctx, claims, "inventory.warehouse.edit");
                Write(ctx, svc.CreateWarehouseLocation(tenantId, userId, Deserialize<CreateWarehouseLocationDto>(ctx)));
                break;

            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed");
                break;
        }
    }

    private void HandleStockLevels(HttpContext ctx, string method,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        RequirePerm(ctx, claims, "inventory.product.view");
        var svc = ServiceLocator.InventoryService;
        Guid? warehouseId = null, productId = null;
        var wStr = ctx.Request.QueryString["warehouseId"];
        var pStr = ctx.Request.QueryString["productId"];
        if (!string.IsNullOrEmpty(wStr) && Guid.TryParse(wStr, out var wg)) warehouseId = wg;
        if (!string.IsNullOrEmpty(pStr) && Guid.TryParse(pStr, out var pg)) productId = pg;
        Write(ctx, svc.GetStockLevels(tenantId, warehouseId, productId));
    }

    private static void RequirePerm(HttpContext ctx,
        System.Collections.Generic.Dictionary<string, string> claims, string perm)
    {
        var perms = claims.GetValueOrDefault("perms") ?? "";
        if (!perms.Contains(perm))
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
