using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SingleThreadAsyncContext
{
    public sealed partial class STAContext : IDisposable
    {
        private readonly BlockingCollection<Task> _queue;
        private readonly STASynchronizationContext _synchronizationContext;
        private readonly STATaskScheduler _taskScheduler;

        private int _outstandingTaskCount;

        public STAContext()
        {
            _queue = new BlockingCollection<Task>();
            _synchronizationContext = new STASynchronizationContext(this);
            _taskScheduler = new STATaskScheduler(this);
            TaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.HideScheduler, TaskContinuationOptions.HideScheduler, _taskScheduler);
        }

        public TaskFactory TaskFactory { get; }
        public SynchronizationContext SynchronizationContext => _synchronizationContext;
        public TaskScheduler TaskScheduler => _taskScheduler;
        public static STAContext Current => (SynchronizationContext.Current as STASynchronizationContext)?.Context;

        public void Dispose()
        {
            _queue?.Dispose();
        }

        public void Execute()
        {
            using (new SynchronizationContextSwitcher(_synchronizationContext))
            {
                foreach (var task in _queue.GetConsumingEnumerable())
                    _taskScheduler.DoTryExecuteTask(task);
            }
        }

        public static void Run(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (var context = new STAContext())
            {
                var task = context.StartTask(action);
                context.Execute();

                task.GetAwaiter().GetResult();
            }
        }

        public static TResult Run<TResult>(Func<TResult> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            using (var context = new STAContext())
            {
                var task = context.StartTask(func);
                context.Execute();

                return task.GetAwaiter().GetResult();
            }
        }

        public static void Run(Func<Task> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            using (var context = new STAContext())
            {
                context.OperationStarted();
                var task = context.StartTask(func).Unwrap().ContinueWith(t =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    context.OperationCompleted();
                    t.GetAwaiter().GetResult();
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, context._taskScheduler);

                context.Execute();
                task.GetAwaiter().GetResult();
            }
        }

        public static TResult Run<TResult>(Func<Task<TResult>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (var context = new STAContext())
            {
                context.OperationStarted();
                var task = context.StartTask(action).Unwrap().ContinueWith(t =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    context.OperationCompleted();
                    return t.GetAwaiter().GetResult();
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, context._taskScheduler);

                context.Execute();
                return task.GetAwaiter().GetResult();
            }
        }

        private void EnqueueTask(Task task)
        {
            OperationStarted();
            task.ContinueWith(t => OperationCompleted(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, _taskScheduler);
            _queue.TryAdd(task);
        }

        private void OperationStarted()
        {
            Interlocked.Increment(ref _outstandingTaskCount);
        }

        private void OperationCompleted()
        {
            if (Interlocked.Decrement(ref _outstandingTaskCount) == 0)
                _queue.CompleteAdding();
        }

        private Task StartTask(Action action) =>
            TaskFactory.StartNew(action, TaskFactory.CancellationToken, TaskFactory.CreationOptions | TaskCreationOptions.DenyChildAttach, TaskFactory.Scheduler ?? TaskScheduler.Default);

        private Task<TResult> StartTask<TResult>(Func<TResult> func) =>
            TaskFactory.StartNew(func, TaskFactory.CancellationToken, TaskFactory.CreationOptions | TaskCreationOptions.DenyChildAttach, TaskFactory.Scheduler ?? TaskScheduler.Default);
    }
}
