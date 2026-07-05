using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.TestEnv;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture]
    public class GpuDataRegistryTests : IGpuDataProviderTests
    {
        #region Create Test Env

        protected override IGpuDataProvider<Vector2> CreateProvider(Dictionary<Type, Array> expected, Vector2[] keys, int length, int batchSize)
        {
            var registry = new GpuDataRegistry<Vector2>(length, batchSize);

            registry.AddStore<int>();
            registry.AddStore<float>();
            registry.AddStore<long>();
            registry.AddStore<double>();

            Vector2 startKey = new Vector2(0, 0);

            var intArr = (int[])expected[typeof(int)];
            var floatArr = (float[])expected[typeof(float)];
            var longArr = (long[])expected[typeof(long)];
            var doubleArr = (double[])expected[typeof(double)];

            for (int i = 0; i < length; i++)
            {
                Vector2 key = keys[i];

                registry.Set(key, intArr[i]);
                registry.Set(key, floatArr[i]);
                registry.Set(key, longArr[i]);
                registry.Set(key, doubleArr[i]);
            }

            return registry;
        }

        #endregion


    }
}
