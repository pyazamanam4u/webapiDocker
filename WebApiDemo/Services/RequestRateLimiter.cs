using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace WebApiDemo.Services
{
    public class RequestRateLimiter
    {
        private readonly IMemoryCache _cache;
        private readonly object _lock = new object();

        public RequestRateLimiter(IMemoryCache cache)
        {
            _cache = cache;
        }

        // Register an access for a particular cache key (used to decide long caching)
        public void RegisterAccessForKey(string key)
        {
            var storeKey = $"hits:{key}";
            lock (_lock)
            {
                var list = _cache.GetOrCreate(storeKey, entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromHours(1);
                    return new List<DateTime>();
                });

                list.Add(DateTime.UtcNow);

                // Remove old entries older than 1 hour
                list.RemoveAll(dt => dt < DateTime.UtcNow.AddHours(-1));

                // Count hits in last 5 minutes
                var recent = list.FindAll(dt => dt >= DateTime.UtcNow.AddMinutes(-5)).Count;
                if (recent >= 3)
                {
                    // mark long cache for this key for 1 day
                    _cache.Set($"cache:long:{key}", true, DateTimeOffset.UtcNow.AddDays(1));
                }
            }
        }

        public int GetRecentAccessCountForKey(string key)
        {
            var storeKey = $"hits:{key}";
            var list = _cache.Get<List<DateTime>>(storeKey);
            if (list == null) return 0;
            list.RemoveAll(dt => dt < DateTime.UtcNow.AddMinutes(-5));
            return list.Count;
        }

        public bool IsLongCached(string key)
        {
            return _cache.TryGetValue($"cache:long:{key}", out bool v) && v;
        }

        // Register a request from an IP and set ban if threshold exceeded
        public void RegisterRequestFromIp(string ip)
        {
            var storeKey = $"ipreq:{ip}";
            lock (_lock)
            {
                var list = _cache.GetOrCreate(storeKey, entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromHours(2);
                    return new List<DateTime>();
                });

                list.Add(DateTime.UtcNow);
                list.RemoveAll(dt => dt < DateTime.UtcNow.AddHours(-1));

                if (list.Count >= 3)
                {
                    // ban for 1 hour
                    _cache.Set($"ban:{ip}", DateTime.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(1));
                }
            }
        }

        public int GetRequestsInLastHourForIp(string ip)
        {
            var storeKey = $"ipreq:{ip}";
            var list = _cache.Get<List<DateTime>>(storeKey);
            if (list == null) return 0;
            list.RemoveAll(dt => dt < DateTime.UtcNow.AddHours(-1));
            return list.Count;
        }

        public bool IsBanned(string ip, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            if (_cache.TryGetValue($"ban:{ip}", out DateTime bannedUntil))
            {
                if (bannedUntil > DateTime.UtcNow)
                {
                    remaining = bannedUntil - DateTime.UtcNow;
                    return true;
                }
            }
            return false;
        }
    }
}
