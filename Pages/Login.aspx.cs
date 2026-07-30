using System;
using System.Web.UI;

public partial class Pages_Login : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // If already authenticated, redirect to dashboard
        var claims = Context.Items["JwtClaims"];
        if (claims != null)
        {
            Response.Redirect("~/Pages/Dashboard.aspx", false);
        }
    }
}
