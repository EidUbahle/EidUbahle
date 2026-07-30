using System;
using System.Web;
using EidUbahle.CrossCutting;
using EidUbahle.Infrastructure.Security;

namespace EidUbahle
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Initialize the service locator with the DB connection string
            ServiceLocator.Initialize(ConfigHelper.DbConnectionString);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            if (ex == null) return;

            // Log the error (extend with Serilog/NLog in production)
            System.Diagnostics.Trace.TraceError("Unhandled exception: {0}", ex);

            // Don't expose stack traces in production
            if (ConfigHelper.IsProduction)
            {
                Server.ClearError();
                Response.Redirect("~/Pages/Error.aspx?code=500", false);
            }
        }

        protected void Session_Start(object sender, EventArgs e) { }
        protected void Session_End(object sender, EventArgs e) { }
    }
}
