using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServer
{
    internal class ReaderWriterCache<K, T> : AbstractCache<K, T>
    {

        private ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

        public ReaderWriterCache() : base(new Dictionary<K, T>()) { }

        public override bool TryGetValue(K key, out T value)
        {
            bool cacheHit = false;
            cacheLock.EnterReadLock();
            try
            {
                cacheHit = cache.TryGetValue(key, out value);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
            return cacheHit;
        }

        public override void Add(K key, T value)
        {
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (cache.ContainsKey(key))
                    return;
                cacheLock.EnterWriteLock();
                try
                {
                    cache.Add(key, value);
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
        }

        public override bool Remove(K key)
        {
            cacheLock.EnterWriteLock();
            try
            {
                return cache.Remove(key);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

    }
}
