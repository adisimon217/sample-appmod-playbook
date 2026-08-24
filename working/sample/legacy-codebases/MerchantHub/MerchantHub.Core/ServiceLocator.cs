using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using log4net;
using MerchantHub.Data;
using MerchantHub.Data.Repositories;
using MerchantHub.Core.Services;
using MerchantHub.Reports;

namespace MerchantHub.Core
{
    /// <summary>
    /// Central service locator for resolving all application dependencies.
    /// TODO: We should really move to a proper DI container (Autofac or Unity)
    ///       but this has been working for 5 years and nobody wants to touch it.
    /// NOTE: Thread-safety issues have been reported in production under heavy load.
    ///       See incident INC-2023-0847 for details.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ServiceLocator));
        private static readonly Dictionary<Type, Func<object>> _registrations = new Dictionary<Type, Func<object>>();
        private static readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();
        private static readonly object _lock = new object();
        private static bool _initialized = false;

        /// <summary>
        /// Initialize all service registrations. Called from Global.asax Application_Start.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                _log.Warn("ServiceLocator.Initialize() called multiple times - ignoring");
                return;
            }

            lock (_lock)
            {
                if (_initialized) return;

                _log.Info("Initializing ServiceLocator...");

                // Register data contexts
                Register<MerchantHubContext>(() => new MerchantHubContext());
                Register<PayGateReadContext>(() => new PayGateReadContext());

                // Register repositories
                Register<IMerchantRepository>(() => new MerchantRepository(Resolve<MerchantHubContext>()));
                Register<ITransactionRepository>(() => new TransactionRepository(
                    Resolve<MerchantHubContext>(),
                    ConfigurationManager.ConnectionStrings["MerchantHubDB"].ConnectionString));
                Register<IDisputeRepository>(() => new DisputeRepository(Resolve<MerchantHubContext>()));
                Register<IMonthlyStatementRepository>(() => new MonthlyStatementRepository(
                    ConfigurationManager.ConnectionStrings["MerchantHubDB"].ConnectionString));

                // Register services
                Register<IMerchantService>(() => new MerchantService(
                    Resolve<IMerchantRepository>(),
                    Resolve<ITransactionRepository>()));
                Register<ITransactionService>(() => new TransactionService(
                    Resolve<ITransactionRepository>(),
                    Resolve<PayGateReadContext>()));
                Register<IDisputeService>(() => new DisputeService(
                    Resolve<IDisputeRepository>(),
                    Resolve<ITransactionRepository>()));
                Register<IReportService>(() => new ReportService(
                    Resolve<IMonthlyStatementRepository>(),
                    Resolve<IMerchantRepository>()));

                // Register report generator (singleton due to COM interop cost)
                RegisterSingleton<IReportGenerator>(() => new CrystalReportsWrapper(
                    ConfigurationManager.AppSettings["MerchantHub.ReportOutputPath"],
                    ConfigurationManager.AppSettings["MerchantHub.CrystalReportsLicenseKey"]));

                _initialized = true;
                _log.Info("ServiceLocator initialized successfully with " + _registrations.Count + " registrations");
            }
        }

        /// <summary>
        /// Register a factory function for a given type.
        /// </summary>
        public static void Register<T>(Func<object> factory)
        {
            var type = typeof(T);
            if (_registrations.ContainsKey(type))
            {
                _log.WarnFormat("Overwriting existing registration for {0}", type.Name);
            }
            _registrations[type] = factory;
        }

        /// <summary>
        /// Register a singleton instance (created on first resolve).
        /// </summary>
        public static void RegisterSingleton<T>(Func<object> factory)
        {
            var type = typeof(T);
            _registrations[type] = () =>
            {
                if (!_singletons.ContainsKey(type))
                {
                    lock (_lock)
                    {
                        if (!_singletons.ContainsKey(type))
                        {
                            _singletons[type] = factory();
                        }
                    }
                }
                return _singletons[type];
            };
        }

        /// <summary>
        /// Resolve an instance of the requested type.
        /// WARNING: This creates new instances on every call for non-singletons.
        ///          Known to cause connection pool exhaustion under load.
        /// </summary>
        public static T Resolve<T>()
        {
            var type = typeof(T);
            if (!_registrations.ContainsKey(type))
            {
                var msg = string.Format("No registration found for type {0}. Ensure ServiceLocator.Initialize() was called.", type.FullName);
                _log.Error(msg);
                throw new InvalidOperationException(msg);
            }

            try
            {
                return (T)_registrations[type]();
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Failed to resolve type {0}: {1}", type.Name, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get the current merchant ID from session. Used throughout the app.
        /// NOTE: This couples the service layer to HttpContext which makes testing painful.
        /// </summary>
        public static int GetCurrentMerchantId()
        {
            if (HttpContext.Current == null || HttpContext.Current.Session == null)
            {
                _log.Warn("GetCurrentMerchantId called outside of HTTP context");
                return 0;
            }

            var merchantId = HttpContext.Current.Session["CurrentMerchantId"];
            if (merchantId == null)
            {
                _log.Warn("CurrentMerchantId not found in session");
                return 0;
            }

            return (int)merchantId;
        }

        /// <summary>
        /// Reset all registrations (used in unit tests).
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _registrations.Clear();
                _singletons.Clear();
                _initialized = false;
            }
        }
    }
}
