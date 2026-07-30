using System;
using System.Web.UI;

public partial class Pages_Accounting_ChartOfAccounts : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Authentication is handled by JwtAuthModule.
        // Page code-behind is intentionally minimal – all data fetching
        // is done client-side via AJAX to /Handlers/Accounts.ashx.
    }
}
