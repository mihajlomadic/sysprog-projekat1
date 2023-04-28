using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    internal abstract class AbstractCache<K, T>
    {
        protected IDictionary<K, T> cache;

        public AbstractCache(IDictionary<K, T> cache)
        {
            this.cache = cache;
        }

        public abstract bool TryGetValue(K key, out T value);

        public abstract void Add(K key, T value);

        public abstract bool Remove(K key);
    }
}
