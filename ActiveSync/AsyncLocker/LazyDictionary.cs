using System;
using System.Collections.Concurrent;

namespace MultiFactor.IIS.Adapter.ActiveSync.AsyncLocker
{
    public class LazyDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _dictionary = new ConcurrentDictionary<TKey, Lazy<TValue>>();

        public TValue GetOrAdd(TKey key, Func<TValue> valueGenerator)
        {
            return _dictionary.GetOrAdd(key, k => new Lazy<TValue>(valueGenerator)).Value;
        }
    }
}
