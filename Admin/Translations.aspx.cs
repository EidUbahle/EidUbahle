using System;
using System.Web.UI;
using System.Collections.Generic;

public partial class Admin_Translations : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Permission check: requires admin.translations.view
        var claims = Context.Items["JwtClaims"] as Dictionary<string, string>;
        if (claims == null)
        {
            Response.Redirect("~/Pages/Login.aspx", false);
            return;
        }
        // Additional permission validation can be done here
        // In production, call PermissionService.HasPermission(userId, "admin.translations.view")
    }
}
