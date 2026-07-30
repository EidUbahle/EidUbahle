using System;
using System.Web.UI;

public partial class Pages_Reports_Index : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Redirect("~/Pages/Accounting/Reports.aspx", true);
    }
}
