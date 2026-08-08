using NUnit.Framework;
using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Rendering.Abstractions;
using System;
using UnityEngine;
using static Rayforge.Core.Environment.Spatial.Rendering.Tests.LodAtlasMapperTests;

namespace Rayforge.Core.Environment.Spatial.Rendering.Tests
{
    [TestFixture]
    public class LodAtlasMapperTests
    {
        #region Test Env

        public class MockAtlasMapper<TKey> : LodAtlasMapper<TKey, AabbSpatialData, AabbGpuDataRegistry<TKey, TextureMappingData>>
            where TKey : struct, IEquatable<TKey>
        {
            protected override AabbGpuDataRegistry<TKey, TextureMappingData> CreateRegistry(int totalCapacity, int batchSize)
            {
                return new AabbGpuDataRegistry<TKey, TextureMappingData>(totalCapacity, batchSize);
            }

            protected override AabbSpatialData CreateSpatialEntry(Vector3 worldPos, float extent)
            {
                float halfExtent = extent * 0.5f;
                var minBounds = worldPos - new Vector3(halfExtent, halfExtent, halfExtent);
                var maxBounds = worldPos + new Vector3(halfExtent, halfExtent, halfExtent);
                return new AabbSpatialData { MinBounds = minBounds, MaxBounds = maxBounds };
            }
        }
        public struct MockLodGridProvider<TKey> : ILODGridConfiguration<TKey>, ILODGridMetrics<TKey>
            where TKey : struct, IEquatable<TKey>
        {
            private readonly int[] _capacities;
            private readonly float[] _distances;
            private readonly float[] _sqrDistances;

            // ILODGridConfiguration Properties
            public Vector3 ViewerPos { get; set; }
            public int LodCount => _capacities?.Length ?? 0;
            public int ActiveCellCount { get; set; }
            public ReadOnlySpan<float> LodSqrDistances => _sqrDistances;
            public ReadOnlySpan<float> LodDistances => _distances;

            // ISpatialGridConfiguration Properties
            public GridSize GridSize { get; set; }
            public Vector3 Anchor { get; set; }
            public SpatialAxes ActiveAxes { get; set; }
            public bool IsInitialized { get; set; }
            public int Count { get; set; }

            // Events
            public event Action<ILODGridConfiguration<TKey>> OnLODSettingsChanged;
            public event Action<ISpatialGridConfiguration<TKey>> OnGridStructureChanged;
            public event Action<ISpatialGridConfiguration<TKey>, Vector3> OnAnchorChanged;

            public MockLodGridProvider(params int[] capacities)
            {
                _capacities = capacities;
                _distances = new float[capacities.Length];
                _sqrDistances = new float[capacities.Length];

                ViewerPos = Vector3.zero;
                ActiveCellCount = 0;
                GridSize = default;
                Anchor = Vector3.zero;
                ActiveAxes = default;
                IsInitialized = true;
                Count = 0;

                OnLODSettingsChanged = null;
                OnGridStructureChanged = null;
                OnAnchorChanged = null;
            }

            public int GetKeyCountInLODLevel(int lodIndex, Vector3 center) => _capacities[lodIndex];

            public int GetKeyCountInFullRange(Vector3 center)
            {
                int total = 0;
                foreach (var cap in _capacities) total += cap;
                return total;
            }

            public int GetMaxCapacityForLODLevel(int lodIndex) => _capacities[lodIndex];
        }

        #endregion
        /*
        #region Initialize Tests

        [Test]
        public void Initialize_ValidParameters_SetsUpCorrectly()
        {
            var mapper = new MockAtlasMapper<int>();
            var provider = new MockLodGridProvider<int>(10, 20);
            var resolutions = new PowerOfTwoResolution[] { PowerOfTwoResolution.Res64, PowerOfTwoResolution.Res32 };

            mapper.Initialize(provider, resolutions, batchSize: 4);

            Assert.IsTrue(mapper.IsInitialized);
            Assert.AreEqual(2, provider.LodCount);
        }

        [Test]
        public void Initialize_NullProvider_ThrowsArgumentNullException()
        {
            var mapper = new MockAtlasMapper<int>();
            var resolutions = new PowerOfTwoResolution[] { PowerOfTwoResolution.Res64 };

            // Casting null to the specific Mock provider type to satisfy generic constraint
            Assert.Throws<ArgumentNullException>(() => mapper.Initialize<MockLodGridProvider<int>>(default, resolutions, batchSize: 4));
        }

        [Test]
        public void Initialize_EmptyResolutions_ThrowsArgumentException()
        {
            var mapper = new MockAtlasMapper<int>();
            var provider = new MockLodGridProvider<int>(10);
            var resolutions = Array.Empty<PowerOfTwoResolution>();

            Assert.Throws<ArgumentException>(() => mapper.Initialize(provider, resolutions, batchSize: 4));
        }

        [Test]
        public void Initialize_LodCountMismatch_ThrowsInvalidOperationException()
        {
            var mapper = new MockAtlasMapper<int>();
            var provider = new MockLodGridProvider<int>(10, 20); // 2 LODs
            var resolutions = new PowerOfTwoResolution[] { PowerOfTwoResolution.Res64 }; // 1 provided

            Assert.Throws<InvalidOperationException>(() => mapper.Initialize(provider, resolutions, batchSize: 4));
        }

        [Test]
        public void Initialize_InvalidBatchSize_ThrowsArgumentOutOfRangeException()
        {
            var mapper = new MockAtlasMapper<int>();
            var provider = new MockLodGridProvider<int>(10);
            var resolutions = new PowerOfTwoResolution[] { PowerOfTwoResolution.Res64 };

            Assert.Throws<ArgumentOutOfRangeException>(() => mapper.Initialize(provider, resolutions, batchSize: 0));
        }

        #endregion
        */
    }
}
