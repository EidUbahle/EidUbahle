using System;
using System.Web.UI;
using System.Collections.Generic;
using EidUbahle.Domain.DTOs;
using System.Web.Script.Serialization;

public partial class MasterPages_Compact : MasterPage
{
    protected UserClaimsDto CurrentUser { get; private set; }

    protected void Page_Load(object sender, EventArgs e)
    {
        var claims = Context.Items["JwtClaims"] as Dictionary<string, string>;
        if (claims == null) { Response.Redirect("~/Pages/Login.aspx", false); return; }

        CurrentUser = new UserClaimsDto
        {
            UserId = Guid.TryParse(claims.GetValueOrDefault("sub"), out var uid) ? uid : Guid.Empty,
            FullName = claims.GetValueOrDefault("nam"),
            LanguageCode = claims.GetValueOrDefault("lng") ?? "en",
        };

        var ser = new JavaScriptSerializer();
        var script = $"window.__EID_USER__ = {ser.Serialize(new { userId = CurrentUser.UserId, fullName = CurrentUser.FullName, languageCode = CurrentUser.LanguageCode })};";
        Page.ClientScript.RegisterStartupScript(GetType(), "eid_user", script, true);
    }
}
