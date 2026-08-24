using System.Web.Http;
using PayGate.Api.Filters;

namespace PayGate.Api
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters(HttpConfiguration config)
        {
            // Rate limiting filter - enforces max 1000 requests/minute per API key
            config.Filters.Add(new TransactionThrottleFilter());
        }
    }
}
