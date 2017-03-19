using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SingleThreadAsyncContext.Tests
{
    [TestFixture]
    public class STAContextTests
    {
        [Test]
        public void CurrentContext_Is_Null_Without_Context()
        {
            Assert.Null(STAContext.Current);
        }

        [Test]
        public void CurrentContext_IsSTAContext()
        {
            STAContext context = null;

            STAContext.Run(() => { context = STAContext.Current; });

            Assert.IsNotNull(context);
        }

        [Test]
        public void Run_AsyncTask_Blocks_Until_Completed()
        {
            var blocked = false;

            STAContext.Run(async () =>
            {
                await Task.Delay(5);
                blocked = true;
            });

            Assert.IsTrue(blocked);
        }

        [Test]
        public void Run_AsyncTask_Throws_On_Null()
        {
            Assert.Throws<ArgumentNullException>(() => STAContext.Run((Func<Task>) null));
        }

        [Test]
        public void Run_AsyncTaskWithResult_Blocks_Until_Completed()
        {
            var blocked = false;

            var result = STAContext.Run(async () =>
            {
                await Task.Delay(5);
                blocked = true;

                return 42;
            });

            Assert.IsTrue(blocked);
            Assert.AreEqual(42, result);
        }

        [Test]
        public void Run_AsyncVoid_Blocks_Until_Completed()
        {
            var blocked = false;

            STAContext.Run((Action) (async () =>
            {
                await Task.Delay(5);
                blocked = true;
            }));

            Assert.IsTrue(blocked);
        }

        [Test]
        public void Stay_On_The_Same_Thread()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var contextId = STAContext.Run(() => Thread.CurrentThread.ManagedThreadId);

            Assert.AreEqual(threadId, contextId);
        }
    }
}
