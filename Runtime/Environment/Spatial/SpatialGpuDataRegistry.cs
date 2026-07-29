using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Buffering;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Execution.Abstractions;
using Rayforge.Core.Environment.Abstractions;
using System;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Generic bridge for registries that participate in a spatial culling and rendering pipeline.
    /// This class enforces the presence of spatial (culling) and visual (rendering) data stores.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for the entities (e.g., Vector3Int for Chunks).</typeparam>
    /// <typeparam name="TCulling">The struct type used for GPU culling (e.g., SphereSpatialData).</typeparam>
    /// <typeparam name="TRender">The struct type used for GPU rendering (e.g., MatrixSpatialData).</typeparam>
    public class SpatialGpuDataRegistry<TKey, TCulling, TRender>
        : SyncedGpuDataRegistry<TKey, TCulling, TRender>, IIterable<SyncedArrayIteratorMeta<TCulling, TRender>>, ISpatialMetadataProvider<TKey, TCulling, TRender>, IGpuDataProvider<TKey>
        where TKey : struct, IEquatable<TKey>
        where TCulling : unmanaged, IGpuData<TCulling>
        where TRender : unmanaged, IGpuData<TRender>
    {
        #region Properties

        private MetadataStore<TCulling> CullingStore => m_StoreA;
        private MetadataStore<TRender> RenderStore => m_StoreB;

        /// <inheritdoc />
        public Array CullingUntypedBuffer => CullingStore.UntypedBuffer;
        /// <inheritdoc />
        public TCulling[] CullingTypedBuffer => CullingStore.TypedBuffer;
        /// <inheritdoc />
        public ReadOnlySpan<TCulling> CullingAsSpan() => CullingStore.AsSpan();

        /// <inheritdoc />
        public Array RenderUntypedBuffer => RenderStore.UntypedBuffer;
        /// <inheritdoc />
        public TRender[] RenderTypedBuffer => RenderStore.TypedBuffer;
        /// <inheritdoc />
        public ReadOnlySpan<TRender> RenderAsSpan() => RenderStore.AsSpan();

        /// <inheritdoc />
        public int CullingStride => CullingStore.Stride;

        /// <inheritdoc />
        public int RenderStride => RenderStore.Stride;

        #endregion

        #region Lifecycle & Configuration

        /// <summary>
        /// Initializes a new spatial registry and automatically registers the mandatory spatial and visual stores.
        /// </summary>
        /// <param name="capacity">Maximum number of slots available in the registry.</param>
        /// <param name="batchSize">Size of a single dirty-tracking batch for optimized GPU uploads.</param>
        public SpatialGpuDataRegistry(int capacity, int batchSize) : base(capacity, batchSize)
        { }

        #endregion

        #region Data Access (Setters)

        /// <inheritdoc />
        public int SetMetadata(TKey key, TCulling culling, TRender render)
        {
            int idx = GetOrAllocateIndex(key);
            CullingStore.Set(idx, culling);
            RenderStore.Set(idx, render);
            return idx;
        }

        /// <inheritdoc />
        public int SetCulling(TKey key, TCulling culling)
        {
            int idx = GetOrAllocateIndex(key);
            CullingStore.Set(idx, culling);
            return idx;
        }

        /// <inheritdoc />
        public int SetRender(TKey key, TRender render)
        {
            int idx = GetOrAllocateIndex(key);
            RenderStore.Set(idx, render);
            return idx;
        }

        /// <inheritdoc />
        public int ReleaseAndKill(TKey key)
        {
            if (Release(key, out int index))
            {
                CullingStore.Set(index, default(TCulling).InvalidData());
                RenderStore.Set(index, default(TRender).InvalidData());
                return index;
            }
            return -1;
        }

        #endregion

        #region Data Access (Getters & State)

        /// <inheritdoc />
        public bool TryGetMetadata(TKey key, out TCulling culling, out TRender render)
        {
            if (TryGetIndex(key, out int index))
            {
                culling = CullingStore.Get(index);
                render = RenderStore.Get(index);
                return true;
            }
            culling = default;
            render = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetCulling(TKey key, out TCulling spatial)
        {
            if (TryGetIndex(key, out int index))
            {
                spatial = CullingStore.Get(index);
                return true;
            }
            spatial = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetRender(TKey key, out TRender visual)
        {
            if (TryGetIndex(key, out int index))
            {
                visual = RenderStore.Get(index);
                return true;
            }
            visual = default;
            return false;
        }

        /// <inheritdoc />
        public bool Contains(TKey key) => TryGetIndex(key, out _);

        #endregion

        #region High-Performance Sync (Zero-Allocation)

        /// <inheritdoc />
        public void ForEachCullingDirty<TAction>(ref TAction action, bool merge = true)
            where TAction : struct, IExecutionHandler<BufferSegmentMeta<TCulling>>
        {
            CullingStore.ForEachDirtySegment(ref action, merge);
        }

        /// <inheritdoc />
        public void ForEachRenderDirty<TAction>(ref TAction action, bool merge = true)
            where TAction : struct, IExecutionHandler<BufferSegmentMeta<TRender>>
        {
            RenderStore.ForEachDirtySegment(ref action, merge);
        }

        /// <inheritdoc />
        public void ForEachSyncedDirty<TAction>(ref TAction action, int batchesPerWindow = 1)
            where TAction : struct, IExecutionHandler<SyncedSegmentMeta<TCulling, TRender>>
        {
            if (batchesPerWindow < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(batchesPerWindow),
                    "The number of batches per window must be at least 1.");
            }

            var syncState = new SyncedDirtySegmentState<TCulling, TRender>(
                CullingStore.TypedBuffer,
                RenderStore.TypedBuffer,
                CullingStore.DirtyBits,
                RenderStore.DirtyBits,
                0,
                CullingStore.Capacity,
                CullingStore.BatchSize,
                batchesPerWindow
            );

            var it = new Iterator<SyncedSegmentMeta<TCulling, TRender>, SyncedDirtySegmentState<TCulling, TRender>>(syncState);
            while (it.MoveNext())
            {
                action.Execute(it.Current);
            }
        }

        #endregion

        #region Flexible Dirty Iteration (Boxing)

        /// <inheritdoc />
        public IIterator<BufferSegmentMeta<TCulling>> GetCullingDirtyIterator(bool merge = true)
            => CullingStore.GetDirtySegmentIterator(merge);

        /// <inheritdoc />
        public IIterator<BufferSegmentMeta<TRender>> GetRenderDirtyIterator(bool merge = true)
            => RenderStore.GetDirtySegmentIterator(merge);

        /// <inheritdoc />
        public IIterator<SyncedSegmentMeta<TCulling, TRender>> GetSyncedDirtyIterator(int batchesPerWindow = 1)
        {
            if (batchesPerWindow < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(batchesPerWindow),
                    "The number of batches per window must be at least 1.");
            }

            var syncState = new SyncedDirtySegmentState<TCulling, TRender>(
                CullingStore.TypedBuffer,
                RenderStore.TypedBuffer,
                CullingStore.DirtyBits,
                RenderStore.DirtyBits,
                0,
                CullingStore.Capacity,
                CullingStore.BatchSize,
                batchesPerWindow
            );

            return new Iterator<SyncedSegmentMeta<TCulling, TRender>, SyncedDirtySegmentState<TCulling, TRender>>(syncState);
        }

        #endregion

        #region IIterable<SyncedArrayIteratorMeta<TCulling, TRender>> Implementation

        /// <inheritdoc />
        public new IIterator<SyncedArrayIteratorMeta<TCulling, TRender>> GetIterator()
        {
            var state = new SyncedArrayIteratorState<TCulling, TRender>(
                CullingStore.TypedBuffer,
                RenderStore.TypedBuffer,
                0,
                HighestIndex + 1
            );

            var iter = new Iterator<SyncedArrayIteratorMeta<TCulling, TRender>, SyncedArrayIteratorState<TCulling, TRender>>(state);

            return iter;
        }

        /// <inheritdoc />
        public new void ForEach<TAction>(ref TAction action) 
            where TAction : struct, IExecutionHandler<SyncedArrayIteratorMeta<TCulling, TRender>>
        {
            var state = new SyncedArrayIteratorState<TCulling, TRender>(
                CullingStore.TypedBuffer,
                RenderStore.TypedBuffer,
                0,
                HighestIndex + 1
            );

            var iter = new Iterator<SyncedArrayIteratorMeta<TCulling, TRender>, SyncedArrayIteratorState<TCulling, TRender>>(state);

            foreach (var meta in iter)
            {
                action.Execute(meta);
            }
        }

        #endregion

        #region Dirty State Management

        /// <inheritdoc />
        public void MarkAllDirty()
        {
            CullingStore.MarkAllDirty();
            RenderStore.MarkAllDirty();
        }

        /// <inheritdoc />
        public void ClearCullingDirty() => CullingStore.ClearDirty();

        /// <inheritdoc />
        public void ClearRenderDirty() => RenderStore.ClearDirty();

        /// <inheritdoc />
        public void ClearAllDirty()
        {
            CullingStore.ClearDirty();
            RenderStore.ClearDirty();
        }

        #endregion
    }
}