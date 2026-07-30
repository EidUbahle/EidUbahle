using System;
using System.Web.UI;
using System.Collections.Generic;
using EidUbahle.CrossCutting;
using EidUbahle.Infrastructure.Security;
using EidUbahle.Domain.DTOs;
using System.Web.Script.Serialization;

public partial class MasterPages_Classic : MasterPage
{
    protected UserClaimsDto CurrentUser { get; private set; }

    protected void Page_Load(object sender, EventArgs e)
    {
        // Claims were placed by JwtAuthModule
        var claims = Context.Items["JwtClaims"] as Dictionary<string, string>;
        if (claims == null)
        {
            Response.Redirect("~/Pages/Login.aspx", false);
            return;
        }

        CurrentUser = new UserClaimsDto
        {
            UserId = Guid.TryParse(claims.GetValueOrDefault("sub"), out var uid) ? uid : Guid.Empty,
            TenantId = Guid.TryParse(claims.GetValueOrDefault("tid"), out var tid) ? tid : Guid.Empty,
            Username = claims.GetValueOrDefault("usr"),
            FullName = claims.GetValueOrDefault("nam"),
            Email = claims.GetValueOrDefault("eml"),
            LanguageCode = claims.GetValueOrDefault("lng") ?? "en",
            IsTenantAdmin = claims.GetValueOrDefault("adm") == "True",
            IsSuperAdmin = claims.GetValueOrDefault("sad") == "True",
        };

        // Inject user context as JS variable for client-side use
        var ser = new JavaScriptSerializer();
        string userJson = ser.Serialize(new
        {
            userId = CurrentUser.UserId,
            tenantId = CurrentUser.TenantId,
            username = CurrentUser.Username,
            fullName = CurrentUser.FullName,
            email = CurrentUser.Email,
            languageCode = CurrentUser.LanguageCode,
            isTenantAdmin = CurrentUser.IsTenantAdmin,
            isSuperAdmin = CurrentUser.IsSuperAdmin,
        });

        var script = $"window.__EID_USER__ = {userJson};";
        Page.ClientScript.RegisterStartupScript(GetType(), "eid_user", script, true);
    }
}
