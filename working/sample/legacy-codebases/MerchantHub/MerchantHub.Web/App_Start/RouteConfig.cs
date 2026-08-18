using System.Web.Mvc;
using System.Web.Routing;

namespace MerchantHub.Web
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("hangfire/{*pathInfo}");

            routes.MapRoute(
                name: "MerchantDashboard",
                url: "merchant/{merchantId}/dashboard",
                defaults: new { controller = "Merchant", action = "Dashboard" }
            );

            routes.MapRoute(
                name: "MerchantTransactions",
                url: "merchant/{merchantId}/transactions/{action}/{id}",
                defaults: new { controller = "Transaction", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Reports",
                url: "reports/{action}/{id}",
                defaults: new { controller = "Report", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Disputes",
                url: "disputes/{action}/{id}",
                defaults: new { controller = "Dispute", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Admin",
                url: "admin/{action}/{id}",
                defaults: new { controller = "Admin", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
