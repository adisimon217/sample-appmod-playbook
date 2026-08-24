using System;
using System.Web;
using System.Web.UI;

namespace BackOffice.Web.MasterPages
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Display current Windows user
                string userName = HttpContext.Current.User.Identity.Name;
                lblCurrentUser.Text = string.Format("Logged in as: {0}", userName);

                // Display environment badge
                string environment = HttpContext.Current.Application["Environment"] as string ?? "Unknown";
                lblEnvironment.Text = environment;
                litEnvironment.Text = environment;

                if (environment == "Production")
                {
                    lblEnvironment.CssClass = "env-badge env-prod";
                }
                else
                {
                    lblEnvironment.CssClass = "env-badge env-nonprod";
                }
            }
        }
    }
}
