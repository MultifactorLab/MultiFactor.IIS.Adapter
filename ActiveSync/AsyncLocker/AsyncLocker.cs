using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MultiFactor.IIS.Adapter.ActiveSync.AsyncLocker
{
    public class AsyncLocker<T>
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<T, LockEntry> _entries = new Dictionary<T, LockEntry>();

        internal int TrackedKeyCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _entries.Count;
                }
            }
        }

        public async Task<IDisposable> LockAsync(T key)
        {
            LockEntry entry;
            lock (_syncRoot)
            {
                if (!_entries.TryGetValue(key, out entry))
                {
                    entry = new LockEntry();
                    _entries.Add(key, entry);
                }

                entry.ReferenceCount++;
            }

            await entry.Semaphore.WaitAsync();
            return new DisposableAction(() => Release(key, entry));
        }

        private void Release(T key, LockEntry entry)
        {
            entry.Semaphore.Release();

            lock (_syncRoot)
            {
                entry.ReferenceCount--;
                if (entry.ReferenceCount != 0)
                {
                    return;
                }

                _entries.Remove(key);
                entry.Dispose();
            }
        }

        private sealed class LockEntry : IDisposable
        {
            public readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
            public int ReferenceCount;

            public void Dispose() => Semaphore.Dispose();
        }
    }
}