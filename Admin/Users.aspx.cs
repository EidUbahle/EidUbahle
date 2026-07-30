using System;
using System.Collections.Generic;
using System.Web.UI;
using EidUbahle.CrossCutting;
using EidUbahle.Domain.DTOs;

public partial class Admin_Users : Page
{
    protected UserClaimsDto CurrentUser { get; private set; }

    protected void Page_Load(object sender, EventArgs e)
    {
        var claims = Context.Items["JwtClaims"] as Dictionary<string, string>;
        if (claims == null) { Response.Redirect("~/Pages/Login.aspx", false); return; }

        var isAdmin = claims.GetValueOrDefault("adm") == "True" ||
                      claims.GetValueOrDefault("sad") == "True";
        if (!isAdmin) { Response.Redirect("~/Pages/Dashboard.aspx", false); return; }
    }
}
