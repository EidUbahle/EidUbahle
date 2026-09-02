using System;
using System.Configuration;
using System.Web.UI;

namespace WamoApp
{
    public partial class SiteMaster : MasterPage
    {
        protected string PageTitle { get; private set; }
        protected string CurrentLanguage { get; private set; }
        protected string CsrfToken { get; private set; }
        protected string AdminMenuHtml { get; private set; }
        protected bool ShowAdminShell { get; private set; }
        protected bool IsAuthenticated { get; private set; }
        protected bool IsRtl { get; private set; }
        protected string CurrentUserName { get; private set; }
        protected string ApplicationUrl => ConfigurationManager.AppSettings["applicationUrl"] ?? string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            PageTitle = string.IsNullOrWhiteSpace(Page.Title) ? "WAMO Waste Management" : Page.Title;
            CurrentLanguage = LocalizationHelper.GetCurrentLanguage();
            IsRtl = LocalizationHelper.IsRightToLeft(CurrentLanguage);
            CsrfToken = SecurityHelper.GetOrCreateCsrfToken();
            IsAuthenticated = SessionManager.IsAuthenticated();
            CurrentUserName = SessionManager.GetCurrentUserName();
            ShowAdminShell = Request.AppRelativeCurrentExecutionFilePath.StartsWith("~/Admin/", StringComparison.OrdinalIgnoreCase);
            AdminMenuHtml = ShowAdminShell ? PermissionManager.BuildAdminMenuHtml() : string.Empty;
        }
    }
}
