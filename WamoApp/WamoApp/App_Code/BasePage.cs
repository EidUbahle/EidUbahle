using System;
using System.Web.UI;

namespace WamoApp
{
    public class BasePage : Page
    {
        protected virtual bool RequiresAuthentication => false;
        protected virtual string RequiredPagePath => null;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (RequiresAuthentication) SessionManager.RequireAuthenticated();
            if (!string.IsNullOrWhiteSpace(RequiredPagePath)) PermissionManager.DemandPageAccess(RequiredPagePath);
        }
    }

    public class ProtectedPage : BasePage { protected override bool RequiresAuthentication => true; }
    public class AdminPage : ProtectedPage { protected override string RequiredPagePath => Request.AppRelativeCurrentExecutionFilePath.TrimStart('~', '/'); }
    public class AccessDeniedPage : BasePage { }
    public class LogoutPage : BasePage
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SessionManager.LogoutCurrentUser();
            Response.Redirect("~/Login.aspx", false);
        }
    }
}
