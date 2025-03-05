using System;

namespace MultiFactor.IIS.Adapter.ActiveSync.AsyncLocker
{
    public sealed class DisposableAction : IDisposable
    {
        private readonly Action _action;

        public DisposableAction(Action action) => _action = action;

        public void Dispose()
        {
            _action?.Invoke();
        }
    }
}
