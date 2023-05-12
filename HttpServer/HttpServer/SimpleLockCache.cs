using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServer
{
    internal class SimpleLockCache<K, T> : AbstractCache<K, T>
    {
        private object _cacheLock = new();

        public SimpleLockCache() : base(new Dictionary<K, T>()) { }

        public override bool TryGetValue(K key, out T value)
        {
            bool cacheHit = false;
            lock (_cacheLock)
            {
                cacheHit = cache.TryGetValue(key, out value);
            }
            return cacheHit;
        }

        public override void Add(K key, T value)
        {
            lock (_cacheLock)
            {
                if (cache.ContainsKey(key))
                    return;
                cache.Add(key, value);
            }
        }
    }
}
