using NUnit.Framework;
using System.Reflection;
using System.Threading;

namespace Rayforge.Core.Common.Tests
{
    [TestFixture]
    public class ThreadingMetaTests
    {
        #region Setup

        [SetUp]
        public void SetUp()
        {
            var method = typeof(ThreadingMeta).GetMethod("Initialize",
                BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, null);
        }

        #endregion

        #region MainThreadId Tests

        [Test]
        public void MainThreadId_IsSetToCurrentThreadId()
        {
            int currentId = Thread.CurrentThread.ManagedThreadId;
            Assert.AreEqual(currentId, ThreadingMeta.MainThreadId,
                "MainThreadId should match the thread id that initialized the class.");
        }

        #endregion

        #region IsMainThread Tests

        [Test]
        public void IsMainThread_ReturnsTrue_OnMainThread()
        {
            Assert.IsTrue(ThreadingMeta.IsMainThread,
                "IsMainThread should return true when called from the main thread.");
        }

        [Test]
        public void IsMainThread_ReturnsFalse_OnBackgroundThread()
        {
            bool isMainThread = true;
            var thread = new Thread(() =>
            {
                isMainThread = ThreadingMeta.IsMainThread;
            });

            thread.Start();
            thread.Join();

            Assert.IsFalse(isMainThread,
                "IsMainThread should return false when called from a background thread.");
        }

        #endregion
    }
}
