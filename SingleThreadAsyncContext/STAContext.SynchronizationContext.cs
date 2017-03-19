using System.Threading;

namespace SingleThreadAsyncContext
{
    public sealed partial class STAContext
    {
        private sealed class STASynchronizationContext : SynchronizationContext
        {
            public STASynchronizationContext(STAContext context)
            {
                Context = context;
            }

            public STAContext Context { get; }

            public override void Send(SendOrPostCallback d, object state)
            {
                if (STAContext.Current != Context)
                {
                    var task = Context.StartTask(() => d(state));
                    task.GetAwaiter().GetResult();
                }
                else
                    d(state);
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                var task = Context.StartTask(() => d(state));
                Context.EnqueueTask(task);
            }

            public override void OperationStarted()
            {
                Context.OperationStarted();
            }

            public override void OperationCompleted()
            {
                Context.OperationCompleted();
            }

            public override SynchronizationContext CreateCopy()
            {
                return new STASynchronizationContext(Context);
            }

            private bool Equals(STASynchronizationContext other)
            {
                return Equals(Context, other.Context);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;

                var context = obj as STASynchronizationContext;
                return context != null && Equals(context);
            }

            public override int GetHashCode()
            {
                return Context?.GetHashCode() ?? 0;
            }

            public static bool operator ==(STASynchronizationContext left, STASynchronizationContext right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(STASynchronizationContext left, STASynchronizationContext right)
            {
                return !Equals(left, right);
            }
        }
    }
}
