using NUnit.Framework;
using Rayforge.Core.Caching.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.TestTools;

namespace Rayforge.Core.Tests.Caching.Concurrent
{
    [TestFixture]
    public class ConcurrentCachedTransformTests
    {
        private GameObject _go;
        private ConcurrentCachedTransform _concurrentTransform;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject("TestObject");
            _concurrentTransform = new ConcurrentCachedTransform(_go);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void Position_SetAndGet_ReturnsCorrectValue()
        {
            Vector3 targetPos = new Vector3(10, 20, 30);
            _concurrentTransform.Position = targetPos;

            Assert.AreEqual(targetPos, _concurrentTransform.Position);
        }

        [Test]
        public void MultiThreaded_PositionAccess_DoesNotCrash()
        {
            bool hasError = false;
            int iterations = 1000;
            List<Thread> threads = new List<Thread>();

            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(() =>
                {
                    try
                    {
                        for (int j = 0; j < iterations; j++)
                        {
                            _concurrentTransform.Position = new Vector3(j, j, j);
                            Vector3 read = _concurrentTransform.Position;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Thread Error: {ex.Message}");
                        hasError = true;
                    }
                });
                threads.Add(t);
            }

            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            Assert.IsFalse(hasError, "Concurrent access caused an exception or race condition.");
        }
    }
}
