using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;
using PayGate.Api.Filters;

namespace PayGate.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Enable CORS for merchant portal
            var cors = new EnableCorsAttribute("https://merchant.paygate.com", "*", "GET,POST,PUT,DELETE");
            config.EnableCors(cors);

            // Message handler for API key authentication (applied globally to non-admin routes)
            config.MessageHandlers.Add(new ApiKeyAuthHandler());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "TransactionApi",
                routeTemplate: "api/transactions/{action}/{id}",
                defaults: new { controller = "Transaction", id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "SettlementApi",
                routeTemplate: "api/settlement/{action}/{id}",
                defaults: new { controller = "Settlement", id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "MerchantApi",
                routeTemplate: "api/merchants/{action}/{id}",
                defaults: new { controller = "Merchant", id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "AdminApi",
                routeTemplate: "api/admin/{action}/{id}",
                defaults: new { controller = "Admin", id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // JSON by default
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(
                new MediaTypeHeaderValue("text/html"));

            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling =
                Newtonsoft.Json.ReferenceLoopHandling.Ignore;

            config.Formatters.JsonFormatter.SerializerSettings.DateFormatHandling =
                Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
        }
    }
}
