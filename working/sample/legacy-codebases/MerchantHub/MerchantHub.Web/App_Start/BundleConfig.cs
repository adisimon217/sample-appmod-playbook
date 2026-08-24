using System.Web.Optimization;

namespace MerchantHub.Web
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                "~/Scripts/bootstrap.bundle.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/datatables").Include(
                "~/Scripts/DataTables/jquery.dataTables.min.js",
                "~/Scripts/DataTables/dataTables.bootstrap4.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/merchanthub").Include(
                "~/Scripts/app/merchant-hub.js",
                "~/Scripts/app/dashboard.js",
                "~/Scripts/app/transactions.js",
                "~/Scripts/app/reports.js",
                "~/Scripts/app/disputes.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/Content/bootstrap.min.css",
                "~/Content/DataTables/css/dataTables.bootstrap4.min.css",
                "~/Content/site.css",
                "~/Content/merchant-hub.css"));

            // Set EnableOptimizations for production
            BundleTable.EnableOptimizations = true;
        }
    }
}
