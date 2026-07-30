using System;
using System.Collections.Generic;
using System.Web.UI;

public partial class Admin_Companies : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        var claims = Context.Items["JwtClaims"] as Dictionary<string, string>;
        if (claims == null) { Response.Redirect("~/Pages/Login.aspx", false); return; }

        var isAdmin = claims.GetValueOrDefault("adm") == "True" ||
                      claims.GetValueOrDefault("sad") == "True";
        if (!isAdmin) { Response.Redirect("~/Pages/Dashboard.aspx", false); return; }
    }
}
