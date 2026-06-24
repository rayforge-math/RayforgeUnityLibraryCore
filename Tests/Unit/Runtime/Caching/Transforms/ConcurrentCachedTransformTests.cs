using NUnit.Framework;
using Rayforge.Core.Caching.Abstractions;
using Rayforge.Core.Caching.Abstractions.Tests;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Rayforge.Core.Caching.Transforms.Tests
{
    [TestFixture]
    public class ConcurrentCachedTransformTests : CachedTransformContractTests<ConcurrentCachedTransform>
    {
        protected override ConcurrentCachedTransform CallCreateFactory(string name)
        {
            return ConcurrentCachedTransform.Create(name);
        }

        protected override ConcurrentCachedTransform CallTemplateCreateFactory(string name, ICachedTransform parent = null)
        {
            return ConcurrentCachedTransform.Create(name, parent);
        }

        protected override ConcurrentCachedTransform CreateInstance(GameObject go)
        {
            return new ConcurrentCachedTransform(go);
        }

        private const int ThreadCount = 8;
        private const int IterationsPerThread = 10000;

        #region Concurrency Stress Tests

        [Test, Timeout(5000)]
        public void Concurrency_PureRead_HeavyLoad()
        {
            var ct = CallCreateFactory("ReadStress");
            var tasks = new List<Task>();

            for (int i = 0; i < ThreadCount; i++)
            {
                tasks.Add(Task.Run(() => {
                    for (int j = 0; j < IterationsPerThread; j++)
                    {
                        var p = ct.Position;
                        var r = ct.Rotation;
                        var s = ct.Scale;
                        var par = ct.Parent;
                    }
                }));
            }

            bool completed = Task.WaitAll(tasks.ToArray(), millisecondsTimeout: 5000);

            // Rethrow any worker exceptions
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                    throw task.Exception!.Flatten().InnerException!;
            }

            Assert.IsTrue(completed, "Test timed out — threads did not finish.");
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test, Timeout(10000)]
        public void Concurrency_MixedReadWrite_HeavyLoad()
        {
            var parent = CallCreateFactory("ChaosParent");
            var child = CallCreateFactory("ChaosChild");
            var tasks = new List<Task>();
            bool isRunning = true;
            int totalReads = 0;
            int inconsistencies = 0;

            // Worker threads — read constantly
            for (int i = 0; i < ThreadCount; i++)
            {
                tasks.Add(Task.Run(() => {
                    while (isRunning)
                    {
                        var p = child.Position;
                        var r = child.Rotation;
                        var s = child.Scale;

                        bool posTorn = Mathf.Abs(p.x - p.y) > 0.001f ||
                                       Mathf.Abs(p.y - p.z) > 0.001f ||
                                       Mathf.Abs(p.x - p.z) > 0.001f;
                        bool scaleTorn = Mathf.Abs(s.x - s.y) > 0.001f ||
                                         Mathf.Abs(s.y - s.z) > 0.001f ||
                                         Mathf.Abs(s.x - s.z) > 0.001f;
                        float magSq = r.x * r.x + r.y * r.y + r.z * r.z + r.w * r.w;
                        bool rotTorn = Mathf.Abs(magSq - 1.0f) > 0.001f;

                        if (posTorn || scaleTorn || rotTorn)
                            Interlocked.Increment(ref inconsistencies);

                        Interlocked.Increment(ref totalReads);
                        Thread.Yield();
                    }
                }));
            }

            // Chaos writer — runs synchronously on test thread
            for (int j = 0; j < IterationsPerThread; j++)
            {
                float val = j;
                Vector3 chaosVec = new Vector3(val, val, val);
                child.Position = chaosVec;
                child.Rotation = Quaternion.Euler(val, val, val);
                child.Scale = chaosVec;
                if (j % 5 == 0) child.Parent = child.Parent == null ? parent : null;
                if (j % 10 == 0) child.Refresh();
            }

            // Signal workers to stop and wait
            isRunning = false;
            bool completed = Task.WaitAll(tasks.ToArray(), millisecondsTimeout: 10000);

            // Rethrow any worker exceptions
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                    throw task.Exception!.Flatten().InnerException!;
            }

            Assert.IsTrue(completed, "Test timed out — threads did not finish.");
            Assert.AreEqual(0, inconsistencies,
                $"Race condition detected after {totalReads} reads!");
            Assert.Greater(totalReads, 0,
                "No reads were performed by worker threads.");

            Object.DestroyImmediate(child.Self.gameObject);
            Object.DestroyImmediate(parent.Self.gameObject);
        }

        #endregion
    }
}