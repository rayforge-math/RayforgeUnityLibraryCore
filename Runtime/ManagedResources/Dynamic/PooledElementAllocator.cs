using Rayforge.Core.ManagedResources.Abstractions;
using Rayforge.Core.ManagedResources.Pooling;
using System;
using System.Collections.Generic;

namespace Rayforge.Core.ManagedResources.Dynamic
{
    /// <summary>
    /// An allocator that pools individual elements (e.g., single textures) 
    /// and manages them in a list to represent a virtual array.
    /// This strategy ensures consistency by validating that all elements in the list 
    /// share the same descriptor.
    /// </summary>
    /// <typeparam name="TDesc">The descriptor type defining the buffer properties.</typeparam>
    /// <typeparam name="TBuffer">The managed buffer type (e.g., Texture2D).</typeparam>
    public sealed class PooledElementAllocator<TDesc, TBuffer> : IArrayAllocator<TDesc, IReadOnlyList<TBuffer>>
        where TBuffer : class, IPooledBuffer<TDesc>
        where TDesc : unmanaged, IEquatable<TDesc>, IArrayDescriptor
    {
        private readonly LeasedBufferPool<TDesc, TBuffer> m_Pool;

        /// <summary>
        /// Internal storage for the active leases rented from the pool.
        /// </summary>
        private readonly List<LeasedBuffer<TBuffer>> m_ElementLeases;

        /// <summary>
        /// Internal cache to expose buffer handles as a read-only list for external access.
        /// </summary>
        private readonly List<TBuffer> m_BufferHandleView;

        /// <summary>
        /// Provides access to the currently active list of buffer handles.
        /// </summary>
        public IReadOnlyList<TBuffer> InternalArray => m_BufferHandleView;

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledElementAllocator{TElement, TDesc, TBuffer}"/> class.
        /// </summary>
        /// <param name="pool">The pool used to rent individual elements.</param>
        public PooledElementAllocator(LeasedBufferPool<TDesc, TBuffer> pool)
        {
            m_Pool = pool ?? throw new ArgumentNullException(nameof(pool));
            m_ElementLeases = new List<LeasedBuffer<TBuffer>>();
            m_BufferHandleView = new List<TBuffer>();
        }

        /// <summary>
        /// Checks if the number of currently rented elements is at least <paramref name="count"/>.
        /// </summary>
        /// <param name="count">The required number of elements.</param>
        /// <returns>True if the current count is sufficient; otherwise, false.</returns>
        public bool EnsureSize(int count) => m_ElementLeases.Count >= count;

        /// <summary>
        /// Synchronizes the internal list of leases with the requested count.
        /// If the provided descriptor differs from existing elements, the entire list is cleared and re-allocated.
        /// </summary>
        /// <param name="descriptor">The descriptor template (Count is forced to 1 for each element).</param>
        /// <param name="count">The target number of elements in the list.</param>
        public void Rent(TDesc descriptor, int count)
        {
            descriptor.Count = 1;

            if (m_ElementLeases.Count > 0)
            {
                if (!m_ElementLeases[0].BufferHandle.Descriptor.Equals(descriptor))
                {
                    Release();
                }
            }

            ApplySize(descriptor, count);
        }

        /// <summary>
        /// Adjusts the list size using the descriptor from the first existing element.
        /// </summary>
        /// <param name="count">The new target element count.</param>
        public void Resize(int count)
        {
            if (m_ElementLeases.Count == 0) return;

            TDesc currentDesc = m_ElementLeases[0].BufferHandle.Descriptor;
            ApplySize(currentDesc, count);
        }

        /// <summary>
        /// Core logic to rent or return leases until the list matches the target count.
        /// </summary>
        private void ApplySize(TDesc descriptor, int count)
        {
            while (m_ElementLeases.Count < count)
            {
                var lease = m_Pool.Rent(descriptor);
                m_ElementLeases.Add(lease);
                m_BufferHandleView.Add(lease.BufferHandle);
            }

            while (m_ElementLeases.Count > count)
            {
                int lastIdx = m_ElementLeases.Count - 1;
                m_ElementLeases[lastIdx].Return();
                m_ElementLeases.RemoveAt(lastIdx);
                m_BufferHandleView.RemoveAt(lastIdx);
            }
        }

        /// <summary>
        /// Returns all rented elements to the pool and clears internal state.
        /// </summary>
        public void Release()
        {
            for (int i = 0; i < m_ElementLeases.Count; i++)
            {
                m_ElementLeases[i].Return();
            }
            m_ElementLeases.Clear();
            m_BufferHandleView.Clear();
        }

        /// <summary>
        /// Disposes of the allocator by releasing all active leases.
        /// </summary>
        public void Dispose() => Release();
    }
}