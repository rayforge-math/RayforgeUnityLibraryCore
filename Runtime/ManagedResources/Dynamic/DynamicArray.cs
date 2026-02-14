using Rayforge.Core.ManagedResources.Abstractions;
using System;

namespace Rayforge.Core.ManagedResources.Dynamic
{
    /// <summary>
    /// A generic wrapper providing dynamic resizing capabilities. 
    /// Bridges <see cref="IDynamicArray"/> with an <see cref="IArrayAllocator{TDesc, TResource}"/>.
    /// Distinguishes between input data (TIn) and output data (TOut).
    /// </summary>
    /// <typeparam name="TIn">The type used for setting/uploading elements.</typeparam>
    /// <typeparam name="TOut">The type used for getting/downloading elements.</typeparam>
    /// <typeparam name="TDesc">The descriptor type for the resource.</typeparam>
    /// <typeparam name="TResource">The managed resource type (e.g., TBuffer or List of Slices).</typeparam>
    public abstract class DynamicArray<TIn, TOut, TDesc, TResource> : IDynamicArray, IManagedArray<TIn, TOut>, IDisposable
        where TDesc : unmanaged, IArrayDescriptor
    {
        private readonly IArrayAllocator<TDesc, TResource> m_Allocator;
        private TDesc m_BaseDescriptor;

        /// <summary>
        /// Direct access to the internal resource managed by the allocator.
        /// </summary>
        public TResource InternalArray => m_Allocator.InternalArray;

        /// <summary>
        /// Logical element count. Must be implemented by the storage strategy.
        /// </summary>
        public abstract int Count { get; }

        protected DynamicArray(IArrayAllocator<TDesc, TResource> allocator, TDesc baseDescriptor)
        {
            m_Allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
            m_BaseDescriptor = baseDescriptor;
        }

        /// <summary>
        /// Handles allocation and capacity management. 
        /// Data preservation is handled externally if needed.
        /// </summary>
        public void Create(int count)
        {
            if (count <= 0)
            {
                Release();
                return;
            }

            if (InternalArray == null)
            {
                m_Allocator.Rent(m_BaseDescriptor, count);
            }
            else if (!m_Allocator.EnsureSize(count))
            {
                m_Allocator.Resize(count);
            }
        }

        #region Abstract Accessors

        /// <summary>
        /// Sets data at a specific index using the TIn type.
        /// </summary>
        public abstract void SetElement(int index, TIn element);

        /// <summary>
        /// Copies data from a specific index into a TOut reference.
        /// </summary>
        public abstract void CopyElementTo(int index, ref TOut element);

        #endregion

        public void Release() => m_Allocator.Release();

        public void Dispose()
        {
            Release();
            m_Allocator.Dispose();
        }
    }
}