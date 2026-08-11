using Rayforge.Core.ManagedResources.Abstractions;
using System;

namespace Rayforge.Core.ManagedResources.Dynamic
{
    /// <summary>
    /// A generic wrapper providing dynamic resizing capabilities. 
    /// Bridges <see cref="IDynamicArray{TIn, TOut}"/> with an <see cref="IArrayAllocator{TDesc, TResource}"/>.
    /// Distinguishes between input data (TIn) and output data (TOut).
    /// </summary>
    /// <typeparam name="TIn">The type used for setting/uploading elements.</typeparam>
    /// <typeparam name="TOut">The type used for getting/downloading elements.</typeparam>
    /// <typeparam name="TDesc">The descriptor type for the resource (e.g., format, resolution).</typeparam>
    /// <typeparam name="TResource">The managed resource type (e.g., ComputeBuffer or Texture2DArray).</typeparam>
    public abstract class DynamicArray<TIn, TOut, TDesc, TResource> : IDynamicArray<TIn, TOut>
        where TDesc : unmanaged, IArrayDescriptor
    {
        #region Private Fields

        private readonly IArrayAllocator<TDesc, TResource> m_Allocator;
        private TDesc m_BaseDescriptor;

        #endregion

        #region Properties

        /// <summary>
        /// Direct access to the internal resource managed by the allocator.
        /// </summary>
        public TResource InternalArray => m_Allocator.InternalArray;

        /// <summary>
        /// Returns true if the underlying resource has been allocated by the allocator.
        /// </summary>
        public bool IsCreated => InternalArray != null;

        /// <summary>
        /// Descriptor used for array allocation.
        /// </summary>
        public TDesc Descriptor => m_BaseDescriptor;

        /// <summary>
        /// Logical element count. Must be implemented by the specific storage strategy.
        /// </summary>
        public abstract int Count { get; }

        #endregion

        #region Constructor

        protected DynamicArray(IArrayAllocator<TDesc, TResource> allocator, TDesc baseDescriptor)
        {
            m_Allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
            m_BaseDescriptor = baseDescriptor;
        }

        #endregion

        #region Lifecycle (IDynamicArray)

        /// <summary>
        /// Handles allocation and capacity management via the linked allocator. 
        /// If count is 0, the resource is released.
        /// </summary>
        /// <param name="count">The target number of elements.</param>
        public void Reallocate(int count)
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

        /// <summary>
        /// Explicitly returns the resource handle to the allocator (e.g., returning to a pool).
        /// </summary>
        public void Release() => m_Allocator.Release();

        /// <summary>
        /// Disposes the allocator and releases any held resources.
        /// </summary>
        public virtual void Dispose()
        {
            Release();
            m_Allocator.Dispose();
        }

        #endregion

        #region Abstract Accessors (IArray)

        /// <summary>
        /// Maps the <see cref="IArray{TIn, TOut}.Set"/> call to the internal storage logic.
        /// </summary>
        public abstract void Set(int index, TIn data);

        /// <summary>
        /// Maps the <see cref="IArray{TIn, TOut}.Get"/> call to the internal storage logic.
        /// </summary>
        public abstract void Get(int index, ref TOut result);

        #endregion
    }
}