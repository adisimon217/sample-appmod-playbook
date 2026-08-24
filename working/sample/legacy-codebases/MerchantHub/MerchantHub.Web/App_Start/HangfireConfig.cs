using System;
using System.Configuration;
using Hangfire;
using Hangfire.SqlServer;
using log4net;

namespace MerchantHub.Web.App_Start
{
    public static class HangfireConfig
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(HangfireConfig));

        public static void Configure()
        {
            _log.Info("Configuring Hangfire...");

            var connectionString = ConfigurationManager.ConnectionStrings["HangfireConnection"].ConnectionString;

            GlobalConfiguration.Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true,
                    SchemaName = "MerchantHub_Hangfire"
                });

            // Configure job activator to use our ServiceLocator
            GlobalConfiguration.Configuration.UseActivator(new ServiceLocatorJobActivator());

            _log.Info("Hangfire configured successfully");
        }
    }

    /// <summary>
    /// Custom job activator that resolves dependencies through our ServiceLocator.
    /// This is a workaround since we don't have proper DI.
    /// </summary>
    public class ServiceLocatorJobActivator : JobActivator
    {
        public override object ActivateJob(Type jobType)
        {
            // Try to resolve from ServiceLocator first
            try
            {
                var method = typeof(MerchantHub.Core.ServiceLocator)
                    .GetMethod("Resolve")
                    .MakeGenericMethod(jobType);
                return method.Invoke(null, null);
            }
            catch
            {
                // Fall back to Activator.CreateInstance
                return Activator.CreateInstance(jobType);
            }
        }
    }
}
