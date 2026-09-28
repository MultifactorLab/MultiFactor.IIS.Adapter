using System;
using System.Threading;

namespace MultiFactor.IIS.Adapter.ActiveSync.AsyncLocker
{
    public sealed class DisposableAction : IDisposable
    {
        private Action _action;

        public DisposableAction(Action action) => _action = action;

        public void Dispose()
        {
            Interlocked.Exchange(ref _action, null)?.Invoke();
        }
    }
}