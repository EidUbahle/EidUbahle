using System;
using System.Web.UI;

public partial class Pages_Banking_BankAccounts : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Authentication handled by JwtAuthModule.
        // Data fetching is done client-side via AJAX to /Handlers/Banks.ashx.
    }
}
