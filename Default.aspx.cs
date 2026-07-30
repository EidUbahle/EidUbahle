using System;
using System.Web.UI;
using System.Collections.Generic;

public partial class _Default : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        var claims = Context.Items["JwtClaims"] as Dictionary<string, string>;
        if (claims != null)
            Response.Redirect("~/Pages/Dashboard.aspx", false);
        else
            Response.Redirect("~/Pages/Login.aspx", false);
    }
}
