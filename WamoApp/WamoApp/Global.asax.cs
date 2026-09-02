using System;
using System.Web;
using System.Web.Security;

namespace WamoApp
{
    public class Global : HttpApplication
    {
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            SecurityHelper.ApplySecurityHeaders(Response);
            SecurityHelper.EnsureRequestCulture();
            SessionManager.TouchAnonymousCsrfCookie();
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            SessionManager.AttachPrincipalFromCookie();
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            AuditLogger.LogApplicationError(Server.GetLastError(), HttpContext.Current);
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            if (Response.StatusCode == 401 && !Request.RawUrl.EndsWith("Login.aspx", StringComparison.OrdinalIgnoreCase))
            {
                Response.ClearContent();
                Response.Redirect(FormsAuthentication.LoginUrl, false);
            }
        }
    }
}
