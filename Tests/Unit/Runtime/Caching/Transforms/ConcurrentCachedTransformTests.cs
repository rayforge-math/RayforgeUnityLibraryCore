using NUnit.Framework;
using Rayforge.Core.Caching.Abstractions;
using Rayforge.Core.Caching.Abstractions.Tests;
using System.Collections.Generic;
using System.Linq;
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

        [Test, Timeout(15000)]
        public void Concurrency_MixedReadWrite_HeavyLoad_Robust()
        {
            var parent = CallCreateFactory("ChaosParent");
            var child = CallCreateFactory("ChaosChild");

            // Use barrier for synchronized start
            var startSignal = new ManualResetEventSlim(false);
            bool isRunning = true;
            int inconsistencies = 0;
            int totalReads = 0;

            var tasks = Enumerable.Range(0, ThreadCount).Select(_ => Task.Run(() => {
                startSignal.Wait(); // Wait for green light
                while (Volatile.Read(ref isRunning))
                {
                    // Capture state
                    var p = child.Position;
                    var r = child.Rotation;
                    var s = child.Scale;

                    // Logic: All components of a vector should be identical 
                    // if they were written in the same 'chaos' frame.
                    bool posTorn = !Mathf.Approximately(p.x, p.y) || !Mathf.Approximately(p.y, p.z);
                    bool scaleTorn = !Mathf.Approximately(s.x, s.y) || !Mathf.Approximately(s.y, s.z);

                    if (posTorn || scaleTorn)
                        Interlocked.Increment(ref inconsistencies);

                    Interlocked.Increment(ref totalReads);
                }
            })).ToArray();

            // Trigger start
            startSignal.Set();

            for (int j = 0; j < IterationsPerThread; j++)
            {
                float val = j;
                Vector3 chaosVec = new Vector3(val, val, val);

                // Add small gaps between writes to maximize tearing windows
                child.Position = chaosVec;
                Thread.SpinWait(10); // Crucial: Increases race window
                child.Rotation = Quaternion.Euler(val, val, val);
                Thread.SpinWait(10);
                child.Scale = chaosVec;

                if (j % 5 == 0) child.Parent = (j % 10 == 0) ? null : parent;
            }

            Volatile.Write(ref isRunning, false);
            Task.WaitAll(tasks);

            Assert.AreEqual(0, inconsistencies, $"Detected {inconsistencies} torn reads!");

            // Cleanup
            Object.DestroyImmediate(child.Self.gameObject);
            Object.DestroyImmediate(parent.Self.gameObject);
        }

        #endregion
    }
}