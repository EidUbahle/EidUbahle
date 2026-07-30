<%@ WebHandler Language="C#" Class="StockMovementsHandler" %>

using System;
using System.Web;
using System.Web.Script.Serialization;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;
using EidUbahle.Infrastructure.Security;

/// <summary>
/// /Handlers/StockMovements.ashx – Opening Stock, Adjustments, Transfers, Receipts, Issues.
/// Also handles Batches and Serial Numbers.
/// </summary>
public class StockMovementsHandler : IHttpHandler
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
        var entity = (ctx.Request.QueryString["entity"] ?? "movement").ToLowerInvariant();
        var action = (ctx.Request.QueryString["action"] ?? "").ToLowerInvariant();

        try
        {
            switch (entity)
            {
                case "movement": HandleMovement(ctx, method, action, tenantId, userId, claims); break;
                case "batch":    HandleBatch(ctx, method, action, tenantId, userId, claims); break;
                case "serial":   HandleSerial(ctx, method, action, tenantId, userId, claims); break;
                default: WriteError(ctx, 400, "ERR_INVALID_ENTITY", "Unknown entity type"); break;
            }
        }
        catch (Exception ex)
        {
            WriteError(ctx, 500, "ERR_SERVER",
                ConfigHelper.IsProduction ? "Internal server error" : ex.Message);
        }
    }

    private void HandleMovement(HttpContext ctx, string method, string action,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var svc = ServiceLocator.InventoryService;
        switch (method)
        {
            case "GET":
                RequirePerm(ctx, claims, "inventory.movement.view");
                var idStr = ctx.Request.QueryString["id"];
                if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var mId))
                {
                    Write(ctx, svc.GetMovement(mId));
                }
                else
                {
                    Guid? companyId = null;
                    var cStr = ctx.Request.QueryString["companyId"];
                    if (!string.IsNullOrEmpty(cStr) && Guid.TryParse(cStr, out var cg)) companyId = cg;
                    var typeStr   = ctx.Request.QueryString["type"];
                    var statusStr = ctx.Request.QueryString["status"];
                    int.TryParse(ctx.Request.QueryString["page"] ?? "1", out var page);
                    int.TryParse(ctx.Request.QueryString["pageSize"] ?? "50", out var pageSize);
                    Write(ctx, svc.GetMovements(tenantId, companyId, typeStr, statusStr, page, pageSize));
                }
                break;

            case "POST":
                switch (action)
                {
                    case "post":
                        RequirePerm(ctx, claims, "inventory.movement.post");
                        if (Guid.TryParse(ctx.Request.QueryString["id"], out var postId))
                            Write(ctx, svc.PostMovement(postId, tenantId, userId));
                        else WriteError(ctx, 400, "ERR_MISSING_ID", "Movement ID required");
                        break;
                    default:
                        RequirePerm(ctx, claims, "inventory.movement.create");
                        Write(ctx, svc.CreateMovement(tenantId, userId, Deserialize<CreateStockMovementDto>(ctx)));
                        break;
                }
                break;

            case "DELETE":
                RequirePerm(ctx, claims, "inventory.movement.delete");
                if (Guid.TryParse(ctx.Request.QueryString["id"], out var delId))
                    Write(ctx, svc.DeleteMovement(delId, tenantId, userId));
                else WriteError(ctx, 400, "ERR_MISSING_ID", "Movement ID required");
                break;

            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed");
                break;
        }
    }

    private void HandleBatch(HttpContext ctx, string method, string action,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var svc = ServiceLocator.InventoryService;
        switch (method)
        {
            case "GET":
                RequirePerm(ctx, claims, "inventory.batch.view");
                Guid? prodId = null;
                var pStr = ctx.Request.QueryString["productId"];
                if (!string.IsNullOrEmpty(pStr) && Guid.TryParse(pStr, out var pg)) prodId = pg;
                Write(ctx, svc.GetBatches(tenantId, prodId));
                break;

            case "POST":
                RequirePerm(ctx, claims, "inventory.batch.create");
                Write(ctx, svc.CreateBatch(tenantId, userId, Deserialize<CreateBatchDto>(ctx)));
                break;

            default:
                WriteError(ctx, 405, "ERR_METHOD", "Method not allowed");
                break;
        }
    }

    private void HandleSerial(HttpContext ctx, string method, string action,
        Guid tenantId, Guid userId, System.Collections.Generic.Dictionary<string, string> claims)
    {
        var svc = ServiceLocator.InventoryService;
        switch (method)
        {
            case "GET":
                RequirePerm(ctx, claims, "inventory.batch.view");
                Guid? prodId = null;
                var pStr    = ctx.Request.QueryString["productId"];
                var status  = ctx.Request.QueryString["status"];
                if (!string.IsNullOrEmpty(pStr) && Guid.TryParse(pStr, out var pg)) prodId = pg;
                Write(ctx, svc.GetSerialNumbers(tenantId, prodId, status));
                break;

            case "POST":
                RequirePerm(ctx, claims, "inventory.batch.create");
                Write(ctx, svc.CreateSerialNumber(tenantId, userId, Deserialize<CreateSerialNumberDto>(ctx)));
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
