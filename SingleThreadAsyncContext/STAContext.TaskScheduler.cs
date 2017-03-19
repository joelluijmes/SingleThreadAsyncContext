using System.Collections.Generic;
using System.Threading.Tasks;

namespace SingleThreadAsyncContext
{
    public sealed partial class STAContext
    {
        private sealed class STATaskScheduler : TaskScheduler
        {
            private readonly STAContext _context;

            public STATaskScheduler(STAContext context)
            {
                _context = context;
            }

            public void DoTryExecuteTask(Task task)
            {
                TryExecuteTask(task);
            }

            protected override void QueueTask(Task task)
            {
                _context.EnqueueTask(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
                => STAContext.Current == _context && TryExecuteTask(task);

            protected override IEnumerable<Task> GetScheduledTasks()
                => _context._queue;
        }
    }
}
