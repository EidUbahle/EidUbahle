using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace EidUbahle.Infrastructure.Caching
{
    public interface IAppCache
    {
        T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? duration = null) where T : class;
        void Remove(string key);
        void RemoveByPrefix(string prefix);
        bool Contains(string key);
        void Set<T>(string key, T value, TimeSpan? duration = null) where T : class;
        T Get<T>(string key) where T : class;
    }

    /// <summary>
    /// Thread-safe in-memory cache backed by System.Runtime.Caching.MemoryCache.
    /// Used for translations, permissions, exchange rates, settings.
    /// </summary>
    public class AppMemoryCache : IAppCache
    {
        private static readonly MemoryCache _cache = MemoryCache.Default;
        private static readonly object _lock = new object();
        private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(15);

        public T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? duration = null) where T : class
        {
            var cached = _cache.Get(key) as T;
            if (cached != null) return cached;

            lock (_lock)
            {
                cached = _cache.Get(key) as T;
                if (cached != null) return cached;

                var value = factory();
                if (value != null)
                    _cache.Set(key, value, new CacheItemPolicy
                    {
                        AbsoluteExpiration = DateTimeOffset.UtcNow.Add(duration ?? DefaultDuration)
                    });
                return value;
            }
        }

        public void Set<T>(string key, T value, TimeSpan? duration = null) where T : class
        {
            if (value == null) { _cache.Remove(key); return; }
            _cache.Set(key, value, new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(duration ?? DefaultDuration)
            });
        }

        public T Get<T>(string key) where T : class => _cache.Get(key) as T;

        public void Remove(string key) => _cache.Remove(key);

        public bool Contains(string key) => _cache.Contains(key);

        public void RemoveByPrefix(string prefix)
        {
            // MemoryCache doesn't support prefix removal natively; iterate
            var keys = new List<string>();
            foreach (var kv in _cache)
                if (kv.Key.StartsWith(prefix))
                    keys.Add(kv.Key);
            foreach (var k in keys) _cache.Remove(k);
        }
    }
}
