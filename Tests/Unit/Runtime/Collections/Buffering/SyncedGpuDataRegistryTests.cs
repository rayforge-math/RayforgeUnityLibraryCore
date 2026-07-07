

using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture]
    public class SyncedGpuDataRegistryTests
    {
        #region Test Structs

        public struct PositionData : IGpuData<PositionData>
        {
            public Vector3 Value;

            public bool IsValid => Value != new Vector3(float.MinValue, 0, 0);

            public PositionData InvalidData() => new PositionData { Value = new Vector3(float.MinValue, 0, 0) };
        }

        public struct RotationData : IGpuData<RotationData>
        {
            public float Angle;

            public bool IsValid => Angle != float.MinValue;

            public RotationData InvalidData() => new RotationData { Angle = float.MinValue };
        }

        #endregion

        #region Constructor

        [Test]
        public void Constructor_ValidParameters_InitializesStoresAndCapacity()
        {
            // Arrange
            int capacity = 128;
            int batchSize = 16;

            // Act
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(capacity, batchSize);

            // Assert
            Assert.AreEqual(capacity, registry.Capacity, "Capacity should be set correctly.");
            Assert.AreEqual(batchSize, registry.BatchSize, "BatchSize should be set correctly.");

            Assert.IsNotNull(registry.StoreARawBuffer, "StoreA should be initialized.");
            Assert.IsNotNull(registry.StoreBRawBuffer, "StoreB should be initialized.");
        }

        #endregion
    }
}
