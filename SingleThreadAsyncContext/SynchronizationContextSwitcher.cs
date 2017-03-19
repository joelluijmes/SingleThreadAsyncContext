using System;
using System.Threading;

namespace SingleThreadAsyncContext
{
    public sealed class SynchronizationContextSwitcher : IDisposable
    {
        private readonly SynchronizationContext _oldContext;

        public SynchronizationContextSwitcher(SynchronizationContext newContext)
        {
            _oldContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(newContext);
        }

        public void Dispose()
        {
            SynchronizationContext.SetSynchronizationContext(_oldContext);
        }
    }
}
