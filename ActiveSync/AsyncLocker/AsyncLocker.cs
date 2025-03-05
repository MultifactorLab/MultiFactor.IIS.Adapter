using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultiFactor.IIS.Adapter.ActiveSync.AsyncLocker
{
    public class AsyncLocker<T>
    {
        private readonly LazyDictionary<T, SemaphoreSlim> _semaphoreDictionary = new LazyDictionary<T, SemaphoreSlim>();

        public async Task<IDisposable> LockAsync(T key)
        {
            SemaphoreSlim semaphore = _semaphoreDictionary.GetOrAdd(key, () => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            return new DisposableAction(() => semaphore.Release());
        }
    }
}
