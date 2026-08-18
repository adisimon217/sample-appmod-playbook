using System;
using System.Web;
using System.Web.UI;
using Telerik.Web.UI;

namespace BackOffice.Web.UserControls
{
    public partial class NavigationMenu : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Show admin menu only for users in the BackOffice-Admins group
                if (HttpContext.Current.User.IsInRole(@"DOMAIN\BackOffice-Admins"))
                {
                    pnlAdmin.Visible = true;
                }
            }
        }

        protected void RadPanelBar1_ItemClick(object sender, RadPanelBarEventArgs e)
        {
            // Navigation is handled via NavigateUrl, but log for audit
            string userName = HttpContext.Current.User.Identity.Name;
            string page = e.Item.NavigateUrl ?? e.Item.Text;

            System.Diagnostics.Trace.WriteLine(
                string.Format("[NAV] User: {0}, Page: {1}, Time: {2}",
                    userName, page, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        }
    }
}
