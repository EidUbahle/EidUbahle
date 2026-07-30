using System;
using System.Web;
using EidUbahle.Infrastructure.Caching;
using EidUbahle.Infrastructure.Localization;
using EidUbahle.Infrastructure.Security;
using EidUbahle.Services;

namespace EidUbahle.CrossCutting
{
    /// <summary>
    /// Simple service locator for WebForms DI.
    /// All services are singletons (stateless) or scoped per-request.
    /// Replace with a real DI container (Autofac, SimpleInjector) if desired.
    /// </summary>
    public static class ServiceLocator
    {
        private static IAppCache _cache;
        private static string _connectionString;
        private static bool _initialized;
        private static readonly object _initLock = new object();

        public static void Initialize(string connectionString)
        {
            if (_initialized) return;
            lock (_initLock)
            {
                if (_initialized) return;
                _connectionString = connectionString;
                _cache = new AppMemoryCache();
                _initialized = true;
            }
        }

        public static IAppCache Cache =>
            _cache ?? throw new InvalidOperationException("ServiceLocator not initialized");

        public static AuthService AuthService =>
            new AuthService(_connectionString, _cache);

        public static TranslationService TranslationService =>
            new TranslationService(_connectionString, _cache);

        public static string ConnectionString =>
            _connectionString ?? throw new InvalidOperationException("ServiceLocator not initialized");
    }
}
