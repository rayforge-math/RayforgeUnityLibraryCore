using Rayforge.Core.ManagedResources.Abstractions;
using Rayforge.Core.ManagedResources.Pooling;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.Dynamic
{
    /// <summary>
    /// A generic wrapper that provides dynamic resizing capabilities by
    /// bridging <see cref="IDynamicArray{TElement}"/> with a <see cref="BatchedLeasedBufferPool{TDesc, TBuffer}"/>.
    /// </summary>
    /// <typeparam name="TElement">The type of elements (e.g., float, MyStruct).</typeparam>
    /// <typeparam name="TDesc">The descriptor type for the buffer.</typeparam>
    /// <typeparam name="TBuffer">The managed buffer type (must implement IManagedArray).</typeparam>
    public class DynamicArray<TElement, TDesc, TBuffer> : IDynamicArray<TElement>, IManagedArray<TElement>, IDisposable
        where TBuffer : IPooledBuffer<TDesc>, IManagedArray<TElement>
        where TDesc : unmanaged, IEquatable<TDesc>, IArrayDescriptor
    {
        private readonly BatchedLeasedBufferPool<TDesc, TBuffer> m_Pool;
        private BatchedLeasedBuffer<TBuffer> m_Lease;
        private TDesc m_BaseDescriptor;

        /// <summary>
        /// Exposes the internal resource.
        /// </summary>
        public TBuffer InternalArray
        {
            get
            {
                if (m_Lease != null)
                {
                    return m_Lease.BufferHandle;
                }
                return default;
            }
        }

        /// <summary>
        /// Gets the current number of elements in the buffer.
        /// </summary>
        public int Count => m_Lease != null ? m_Lease.BufferHandle.Count : 0;

        /// <summary>
        /// Initializes a new dynamic array linked to a specific batched pool.
        /// </summary>
        /// <param name="pool">The pool used for resizing and batching.</param>
        /// <param name="baseDescriptor">The template descriptor (Count will be modified during Resize).</param>
        public DynamicArray(BatchedLeasedBufferPool<TDesc, TBuffer> pool, TDesc baseDescriptor)
        {
            m_Pool = pool ?? throw new ArgumentNullException(nameof(pool));
            m_BaseDescriptor = baseDescriptor;
        }

        /// <summary>
        /// Implementation of IDynamicArray. Resizes the resource via the pool.
        /// </summary>
        /// <param name="count">Desired number of elements.</param>
        /// <param name="preserve">If true, copies data from the old buffer to the new one.</param>
        public void Create(int count, bool preserve = false)
        {
            if (count <= 0)
            {
                Dispose();
                return;
            }

            if (m_Lease == null)
            {
                m_BaseDescriptor.Count = count;
                m_Lease = m_Pool.Rent(m_BaseDescriptor);
                return;
            }

            if (!m_Lease.EnsureBatchSize(count))
            {
                if (preserve)
                {
                    TDesc nextDesc = m_BaseDescriptor;
                    nextDesc.Count = count;
                    var nextLease = m_Pool.Rent(nextDesc);

                    int copyCount = Math.Min(Count, count);
                    for (int i = 0; i < copyCount; i++)
                    {
                        TElement val = default;
                        m_Lease.BufferHandle.CopyElementTo(i, ref val);
                        nextLease.BufferHandle.SetElement(i, val);
                    }

                    m_Lease.Return();
                    m_Lease = nextLease;
                }
                else
                {
                    m_Lease.Resize(count);
                }
            }
        }

        #region IManagedArray Forwarding

        public void SetElement(int index, TElement element) => m_Lease?.BufferHandle.SetElement(index, element);

        public void CopyElementTo(int index, ref TElement element) => m_Lease?.BufferHandle.CopyElementTo(index, ref element);

        public void Release()
        {
            m_Lease?.Return();
            m_Lease = null;
        }

        #endregion

        public void Dispose() => Release();
    }
}