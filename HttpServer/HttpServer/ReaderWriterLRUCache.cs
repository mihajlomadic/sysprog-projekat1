using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServer
{
    internal class ReaderWriterLRUCache<K, T> : AbstractCache<K, T>
    {

        private ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        private LinkedList<K> lruList = new LinkedList<K>();
        private int maxCapacity;

        public ReaderWriterLRUCache(int _maxCapacity = 10) : base(new Dictionary<K, T>())
        {
            maxCapacity = _maxCapacity;
        }

        public override bool TryGetValue(K key, out T value)
        {
            bool cacheHit = false;
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                cacheHit = cache.TryGetValue(key, out value);
                if (cacheHit)
                {
                    cacheLock.EnterWriteLock();
                    try
                    {
                        lruList.Remove(key);
                        lruList.AddFirst(key);
                    }
                    finally
                    {
                        cacheLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
            return cacheHit;
        }

        public override void Add(K key, T value)
        {
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (cache.ContainsKey(key))
                {
                    lruList.Remove(key);
                    lruList.AddFirst(key);
                    return;
                }
                cacheLock.EnterWriteLock();
                try
                {
                    if (cache.Count >= maxCapacity)
                    {
                        K lruKey = lruList.Last();
                        lruList.RemoveLast();
                        cache.Remove(lruKey);
                    }
                    cache.Add(key, value);
                    lruList.AddFirst(key);
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

    }
}
