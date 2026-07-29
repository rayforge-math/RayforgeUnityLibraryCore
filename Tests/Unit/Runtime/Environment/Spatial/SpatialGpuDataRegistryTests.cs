using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Abstractions.Tests;
using Rayforge.Core.Execution.Abstractions;
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

    public struct TestExecutionHandler : IExecutionHandler<SyncedArrayIteratorMeta<CullingTestData, RenderTestData>>
    {
        public int Count;

        public void Execute(SyncedArrayIteratorMeta<CullingTestData, RenderTestData> value)
        {
            Count++;
        }
    }

    public struct CullingDirtyExecutionHandler : IExecutionHandler<BufferSegmentMeta<CullingTestData>>
    {
        public int Count;

        public void Execute(BufferSegmentMeta<CullingTestData> value)
        {
            Count++;
        }
    }

    public struct RenderDirtyExecutionHandler : IExecutionHandler<BufferSegmentMeta<RenderTestData>>
    {
        public int Count;

        public void Execute(BufferSegmentMeta<RenderTestData> value)
        {
            Count++;
        }
    }

    public struct SyncedDirtyExecutionHandler : IExecutionHandler<SyncedSegmentMeta<CullingTestData, RenderTestData>>
    {
        public int Count;

        public void Execute(SyncedSegmentMeta<CullingTestData, RenderTestData> value)
        {
            Count++;
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

        #region MarkAllDirty Tests

        [Test]
        public void MarkAllDirty_MarksBothStoresAsDirty()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Act
            provider.MarkAllDirty();

            // Assert
            int count = 0;
            using (var iterator = provider.GetSyncedDirtyIterator())
            {
                while (iterator.MoveNext())
                {
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(4));
        }

        #endregion

        #region ClearDirty Tests

        [Test]
        public void ClearDirty_ClearsStoreDirtyState()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);
            provider.MarkAllDirty();

            // Act
            provider.ClearCullingDirty();
            provider.ClearRenderDirty();

            // Assert
            int count = 0;
            using (var iterator = provider.GetSyncedDirtyIterator())
            {
                while (iterator.MoveNext())
                {
                    count++;
                }
            }

            // Since culling dirty is cleared, the synced dirty iterator should yield no segments
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void ClearAllDirty_ClearsBothStoresDirtyState()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);
            provider.MarkAllDirty();

            // Act
            provider.ClearAllDirty();

            // Assert
            int count = 0;
            using (var iterator = provider.GetSyncedDirtyIterator())
            {
                while (iterator.MoveNext())
                {
                    count++;
                }
            }

            // Since all dirty states are cleared, the synced dirty iterator should yield no segments
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void MarkCullingDirty_MarksOnlyCullingStoreAsDirty()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Act
            provider.MarkCullingDirty();

            // Assert
            int cullingCount = 0;
            using (var cullingIterator = provider.GetCullingDirtyIterator(false))
            {
                while (cullingIterator.MoveNext())
                {
                    cullingCount++;
                }
            }

            int renderCount = 0;
            using (var renderIterator = provider.GetRenderDirtyIterator(false))
            {
                while (renderIterator.MoveNext())
                {
                    renderCount++;
                }
            }

            Assert.That(cullingCount, Is.EqualTo(4));
            Assert.That(renderCount, Is.EqualTo(0));
        }

        [Test]
        public void MarkRenderDirty_MarksOnlyRenderStoreAsDirty()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Act
            provider.MarkRenderDirty();

            // Assert
            int cullingCount = 0;
            using (var cullingIterator = provider.GetCullingDirtyIterator(false))
            {
                while (cullingIterator.MoveNext())
                {
                    cullingCount++;
                }
            }

            int renderCount = 0;
            using (var renderIterator = provider.GetRenderDirtyIterator(false))
            {
                while (renderIterator.MoveNext())
                {
                    renderCount++;
                }
            }

            Assert.That(cullingCount, Is.EqualTo(0));
            Assert.That(renderCount, Is.EqualTo(4));
        }

        #endregion

        #region Complete Iteration Tests

        [Test]
        public void GetIterator_IteratesUpToHighestIndex()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Add some test data so HighestIndex moves up (e.g., indices 0 and 2)
            provider.SetMetadata(new Vector2Int(0, 0), GetDefaultCulling(), GetDefaultRender());
            provider.SetMetadata(new Vector2Int(1, 1), GetDefaultCulling(), GetDefaultRender());
            provider.ReleaseAndKill(new Vector2Int(1, 1)); // Leaves HighestIndex at 1 (length = HighestIndex + 1 = 2)

            // Act
            int count = 0;
            using (var iterator = provider.GetIterator())
            {
                while (iterator.MoveNext())
                {
                    count++;
                }
            }

            // Assert
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void ForEach_ExecutesHandlerUpToHighestIndex()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            provider.SetMetadata(new Vector2Int(0, 0), GetDefaultCulling(), GetDefaultRender());
            provider.SetMetadata(new Vector2Int(1, 1), GetDefaultCulling(), GetDefaultRender());
            provider.ReleaseAndKill(new Vector2Int(1, 1)); // Leaves HighestIndex at 1 (length = HighestIndex + 1 = 2)

            var handler = new TestExecutionHandler();

            // Act
            provider.ForEach(ref handler);

            // Assert
            Assert.That(handler.Count, Is.EqualTo(2));
        }

        #endregion

        #region Separate Dirty Iteration Tests

        [Test]
        public void ForEachCullingDirty_ReturnsOnlyCullingDirtySegments()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            provider.SetCulling(new Vector2Int(0, 0), GetDefaultCulling());

            var handler = new CullingDirtyExecutionHandler();

            // Act
            provider.ForEachCullingDirty(ref handler);

            // Assert
            Assert.That(handler.Count, Is.EqualTo(1));
        }

        [Test]
        public void ForEachCullingDirty_IgnoresRenderStoreDirtyState()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Mark only the render store as dirty
            provider.MarkRenderDirty();

            var handler = new CullingDirtyExecutionHandler();

            // Act
            provider.ForEachCullingDirty(ref handler);

            // Assert
            Assert.That(handler.Count, Is.EqualTo(0));
        }

        [Test]
        public void ForEachRenderDirty_ReturnsOnlyRenderDirtySegments()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Set a value so the segment containing it is marked dirty
            provider.SetRender(new Vector2Int(0, 0), GetDefaultRender());

            var handler = new RenderDirtyExecutionHandler();

            // Act
            provider.ForEachRenderDirty(ref handler);

            // Assert
            Assert.That(handler.Count, Is.EqualTo(1));
        }

        [Test]
        public void ForEachRenderDirty_IgnoresCullingStoreDirtyState()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Mark only the culling store as dirty
            provider.MarkCullingDirty();

            var handler = new RenderDirtyExecutionHandler();

            // Act
            provider.ForEachRenderDirty(ref handler);

            // Assert
            Assert.That(handler.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetCullingDirtyIterator_ReturnsOnlyCullingDirtySegments()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Mark only culling dirty
            provider.MarkCullingDirty();

            int count = 0;
            using (var iterator = provider.GetCullingDirtyIterator(false))
            {
                while (iterator.MoveNext())
                {
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(4));
        }

        [Test]
        public void GetCullingDirtyIterator_IgnoresRenderStoreDirtyState()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Mark only render dirty, leaving culling clean
            provider.MarkRenderDirty();

            int count = 0;
            using (var iterator = provider.GetCullingDirtyIterator())
            {
                while (iterator.MoveNext())
                {
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void GetRenderDirtyIterator_ReturnsOnlyRenderDirtySegments()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Mark only render dirty
            provider.MarkRenderDirty();

            int count = 0;
            using (var iterator = provider.GetRenderDirtyIterator(false))
            {
                while (iterator.MoveNext())
                {
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(4));
        }

        [Test]
        public void GetRenderDirtyIterator_IgnoresCullingStoreDirtyState()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Mark only culling dirty, leaving render clean
            provider.MarkCullingDirty();

            int count = 0;
            using (var iterator = provider.GetRenderDirtyIterator())
            {
                while (iterator.MoveNext())
                {
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(0));
        }

        #endregion

        #region Synced Dirty Iteration Tests

        [Test]
        public void GetSyncedDirtyIterator_ThrowsWhenBatchesPerWindowIsLessThanOne()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetSyncedDirtyIterator(0));
        }

        [Test]
        public void GetSyncedDirtyIterator_IteratesOverSyncedDirtySegments()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Mark both stores dirty
            provider.MarkAllDirty();

            int count = 0;
            using (var iterator = provider.GetSyncedDirtyIterator(batchesPerWindow: 1))
            {
                while (iterator.MoveNext())
                {
                    count++;
                }
            }

            // 16 capacity with a batch size of 4 means 4 segments total
            Assert.That(count, Is.EqualTo(4));
        }

        [Test]
        public void ForEachSyncedDirty_ThrowsWhenBatchesPerWindowIsLessThanOne()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);
            var handler = new SyncedDirtyExecutionHandler();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.ForEachSyncedDirty(ref handler, 0));
        }

        [Test]
        public void ForEachSyncedDirty_ExecutesHandlerOnSyncedDirtySegments()
        {
            var provider = new SpatialGpuDataRegistry<Vector2Int, CullingTestData, RenderTestData>(16, 4);

            // Mark both stores dirty
            provider.MarkAllDirty();

            var handler = new SyncedDirtyExecutionHandler();

            // Act
            provider.ForEachSyncedDirty(ref handler, batchesPerWindow: 1);

            // Assert
            // 16 capacity with a batch size of 4 means 4 segments total
            Assert.That(handler.Count, Is.EqualTo(4));
        }

        #endregion
    }
}
