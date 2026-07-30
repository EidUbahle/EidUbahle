using System;
using System.Web.UI;

public partial class Pages_Dashboard : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Auth is enforced by JwtAuthModule
        // Server-side page is a thin adapter – all data loaded via AJAX
    }
}
