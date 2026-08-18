using System;
using System.Web.Mvc;
using log4net;
using MerchantHub.Core;

namespace MerchantHub.Web.Controllers
{
    public class HomeController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(HomeController));

        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var merchantId = ServiceLocator.GetCurrentMerchantId();
                if (merchantId > 0)
                {
                    return RedirectToAction("Dashboard", "Merchant", new { merchantId = merchantId });
                }
            }
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "MerchantHub - Payment Processing Portal";
            ViewBag.Version = System.Configuration.ConfigurationManager.AppSettings["MerchantHub.Version"];
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Contact Support";
            return View();
        }

        [AllowAnonymous]
        public ActionResult HealthCheck()
        {
            // Basic health check endpoint for load balancer
            try
            {
                var repo = ServiceLocator.Resolve<Data.Repositories.IMerchantRepository>();
                // Quick DB connectivity check
                var count = repo.GetActiveCount();
                return Content("OK - " + count + " active merchants", "text/plain");
            }
            catch (Exception ex)
            {
                _log.Error("Health check failed", ex);
                Response.StatusCode = 503;
                return Content("UNHEALTHY: " + ex.Message, "text/plain");
            }
        }
    }
}
