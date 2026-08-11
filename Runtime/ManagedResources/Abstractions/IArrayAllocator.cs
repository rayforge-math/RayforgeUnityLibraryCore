using System;

namespace Rayforge.Core.ManagedResources.Abstractions
{
    public interface IArrayAllocator<TDesc, TBuffer> : IDisposable
        where TDesc : unmanaged
    {
        /// <summary>
        /// Provides access to the currently active buffer.
        /// Whether this is a new object or the same one as before is hidden.
        /// </summary>
        TBuffer InternalArray { get; }

        /// <summary>
        /// Returns true if the current storage can accommodate 'count' elements.
        /// PooledArray: Checks the BatchedLease size.
        /// PooledElement: Checks if the current container has enough slots.
        /// </summary>
        bool EnsureSize(int count);

        /// <summary>
        /// Requests a buffer setup for 'count'. 
        /// If 'preserve' was true in DynamicArray, this might be a new instance 
        /// to allow data migration.
        /// </summary>
        void Rent(TDesc descriptor, int count);

        /// <summary>
        /// Resizes the current allocation.
        /// </summary>
        void Resize(int count);

        /// <summary>
        /// Full cleanup of all held resources (leases or pooled elements).
        /// </summary>
        void Release();
    }
}