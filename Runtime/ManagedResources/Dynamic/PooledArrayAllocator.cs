using Rayforge.Core.ManagedResources.Abstractions;
using Rayforge.Core.ManagedResources.Pooling;
using System;

namespace Rayforge.Core.ManagedResources.Dynamic
{
    /// <summary>
    /// Implementation of IArrayAllocator that leases entire array containers 
    /// from a BatchedLeasedBufferPool.
    /// </summary>
    public sealed class PooledArrayAllocator<TDesc, TBuffer> : IArrayAllocator<TDesc, TBuffer>
        where TBuffer : class, IPooledBuffer<TDesc>
        where TDesc : unmanaged, IEquatable<TDesc>, IArrayDescriptor
    {
        private readonly BatchedLeasedBufferPool<TDesc, TBuffer> m_Pool;
        private BatchedLeasedBuffer<TBuffer> m_Lease;

        /// <summary>
        /// Current buffer managed by the active lease.
        /// </summary>
        public TBuffer InternalArray => m_Lease?.BufferHandle;

        public PooledArrayAllocator(BatchedLeasedBufferPool<TDesc, TBuffer> pool)
        {
            m_Pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        /// <summary>
        /// Checks if the current lease can still accommodate the requested count.
        /// Maps directly to the batching logic of the pool.
        /// </summary>
        public bool EnsureSize(int count)
        {
            return m_Lease != null && m_Lease.EnsureBatchSize(count);
        }

        /// <summary>
        /// Acquires a new lease from the pool. 
        /// If an old lease existed, it is returned to the pool before the new one is stored.
        /// </summary>
        public void Rent(TDesc descriptor, int count)
        {
            descriptor.Count = count;

            m_Lease?.Return();
            m_Lease = m_Pool.Rent(descriptor);
        }

        /// <summary>
        /// Triggers a resize on the current lease (no new object allocation).
        /// </summary>
        public void Resize(int count)
        {
            m_Lease?.Resize(count);
        }

        /// <summary>
        /// Returns the lease to the pool and clears the state.
        /// </summary>
        public void Release()
        {
            m_Lease?.Return();
            m_Lease = null;
        }

        public void Dispose()
        {
            Release();
        }
    }
}