using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Abstractions.Tests;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Tests
{
    public struct CullingTestData : IGpuData<CullingTestData>
    {
        public int data;

        public bool IsValid => data != 0;
        public CullingTestData InvalidData()
        {
            return new CullingTestData { data = 0 };
        }
    }

    public struct RenderTestData : IGpuData<RenderTestData>
    {
        public int data;

        public bool IsValid => data != 0;
        public RenderTestData InvalidData()
        {
            return new RenderTestData { data = 0 };
        }
    }

    [TestFixture]
    public class SpatialGpuDataRegistryTests : ISpatialMetadataProviderTests<Vector2Int, CullingTestData, RenderTestData, SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>>
    {
        #region TestEnv
        protected override SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData> CreateProvider(int capacity, int batchSize)
        {
            return new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(capacity, batchSize);
        }

        protected override CullingTestData GetDefaultCulling()
        {
            return new CullingTestData { data = 1 };
        }

        protected override RenderTestData GetDefaultRender()
        {
            return new RenderTestData { data = 1 };
        }

        #endregion

        #region Constructor Test

        [Test]
        public void Constructor_ValidParameters_Succeeds()
        {
            var registry = new SpatialGpuDataRegistry<Vector3Int, CullingTestData, RenderTestData>(16, 4);

            Assert.That(registry.Capacity, Is.EqualTo(16));
            Assert.That(registry.BatchSize, Is.EqualTo(4));
        }

        [Test]
        public void Constructor_ZeroOrNegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialGpuDataRegistry<Vector3Int, CullingTestData, RenderTestData>(0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialGpuDataRegistry<Vector3Int, CullingTestData, RenderTestData>(-16, 4));
        }

        [Test]
        public void Constructor_ZeroOrNegativeBatchSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialGpuDataRegistry<Vector3Int, CullingTestData, RenderTestData>(16, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpatialGpuDataRegistry<Vector3Int, CullingTestData, RenderTestData>(16, -4));
        }

        [Test]
        public void Constructor_CapacityNotMultipleOfBatchSize_ThrowsArgumentException()
        {
            // 10 is not a multiple of 4
            Assert.Throws<ArgumentException>(() => new SpatialGpuDataRegistry<Vector3Int, CullingTestData, RenderTestData>(10, 4));
        }

        #endregion
    }
}
