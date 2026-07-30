<%@ WebHandler Language="C#" Class="InventoryHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/Inventory.ashx – Stock Levels, Valuation, Low Stock Alerts, Reservations.
/// Query string: entity=valuation|alert|reservation
/// </summary>
public class InventoryHandler : IHttpHandler
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
        var entity = (ctx.Request.QueryString["entity"] ?? "stock").ToLowerInvariant();
        var action = (ctx.Request.QueryString["action"] ?? "").ToLowerInvariant();

        try
        {
            switch (entity)
            {
                case "stock":       HandleStock(ctx, method, tenantId, userId, claims); break;
                case "valuation":   HandleValuation(ctx, method, tenantId, userId, claims); break;
                case "alert":       HandleAlert(ctx, method, action, tenantId, userId, claims); break;
                case "reservation": HandleReservation(ctx, method, action, tenantId, userId, claims); break;
                default: WriteError(ctx, 400, "ERR_INVALID_ENTITY", "Unknown entity type"); break;
            }
        }
        catch (Exception ex)
        {
            WriteError(ctx, 500, "ERR_SERVER",
                ConfigHelper.IsProduction ? "Internal server error" : ex.Message);
        }
    }

    private void HandleStock(HttpContext ctx, string method,
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

    private void HandleValuation(HttpContext ctx, string method,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        RequirePerm(ctx, claims, "inventory.valuation.view");
        var svc = ServiceLocator.InventoryService;
        Guid? warehouseId = null;
        var wStr = ctx.Request.QueryString["warehouseId"];
        if (!string.IsNullOrEmpty(wStr) && Guid.TryParse(wStr, out var wg)) warehouseId = wg;
        Write(ctx, svc.GetStockValuation(tenantId, warehouseId));
    }

    private void HandleAlert(HttpContext ctx, string method, string action,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var svc = ServiceLocator.InventoryService;
        switch (method)
        {
            case "GET":
                RequirePerm(ctx, claims, "inventory.alert.view");
                var status = ctx.Request.QueryString["status"] ?? "Active";
                Write(ctx, svc.GetLowStockAlerts(tenantId, status));
                break;

            case "POST":
                if (action == "acknowledge")
                {
                    RequirePerm(ctx, claims, "inventory.alert.acknowledge");
                    if (Guid.TryParse(ctx.Request.QueryString["id"], out var aId))
                        Write(ctx, svc.AcknowledgeAlert(aId, tenantId, userId));
                    else WriteError(ctx, 400, "ERR_MISSING_ID", "Alert ID required");
                }
                else WriteError(ctx, 400, "ERR_INVALID_ACTION", "Unknown action");
                break;

            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed");
                break;
        }
    }

    private void HandleReservation(HttpContext ctx, string method, string action,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var svc = ServiceLocator.InventoryService;
        switch (method)
        {
            case "GET":
                RequirePerm(ctx, claims, "inventory.reservation.view");
                Guid? productId = null;
                var pStr   = ctx.Request.QueryString["productId"];
                var status = ctx.Request.QueryString["status"] ?? "Active";
                if (!string.IsNullOrEmpty(pStr) && Guid.TryParse(pStr, out var pg)) productId = pg;
                Write(ctx, svc.GetReservations(tenantId, productId, status));
                break;

            case "POST":
                if (action == "release")
                {
                    RequirePerm(ctx, claims, "inventory.reservation.release");
                    if (Guid.TryParse(ctx.Request.QueryString["id"], out var rId))
                        Write(ctx, svc.ReleaseReservation(rId, tenantId));
                    else WriteError(ctx, 400, "ERR_MISSING_ID", "Reservation ID required");
                }
                else
                {
                    RequirePerm(ctx, claims, "inventory.reservation.create");
                    Write(ctx, svc.ReserveStock(tenantId, userId, Deserialize<CreateReservationDto>(ctx)));
                }
                break;

            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed");
                break;
        }
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
