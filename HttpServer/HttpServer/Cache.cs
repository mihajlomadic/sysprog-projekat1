using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServer
{
    internal class Cache<K, T>
    {

        private Dictionary<K, T> cache = new Dictionary<K, T>();
        private ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

        public bool TryGetValue(K key, out T value)
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

        public void Add(K key, T value)
        {
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

    }
}
