using Rayforge.Core.ManagedResources.Abstractions;
using System;
using Unity.Collections;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around a <see cref="NativeArray{TType}"/>.
    /// Provides automatic creation, release, and pooling support.
    /// </summary>
    /// <typeparam name="TType">The struct type stored in the array.</typeparam>
    public sealed class ManagedSystemBuffer<TType> : ManagedBuffer<SystemBufferDescriptor, NativeArray<TType>>
        where TType : struct
    {
        /// <summary>
        /// Returns true if the underlying NativeArray is allocated and has not been disposed.
        /// Implementation of the abstract property in ManagedBuffer.
        /// </summary>
        public override bool IsCreated => m_Buffer.IsCreated;

        /// <summary>
        /// Public constructor to initialize the managed system buffer.
        /// Use <see cref="Create"/> instead.
        /// </summary>
        /// <param name="descriptor">Descriptor describing buffer properties.</param>
        public ManagedSystemBuffer(SystemBufferDescriptor descriptor)
            : base(descriptor)
        { }

        /// <summary>
        /// Implementation of the allocation using the internal descriptor field.
        /// </summary>
        /// <returns>A new <see cref="NativeArray{TType}"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <see cref="m_Descriptor"/>.Count is less than or equal to 0.
        /// </exception>
        protected override NativeArray<TType> Allocate()
        {
            if (m_Descriptor.Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(m_Descriptor.Count), "System buffer count must be greater than zero.");

            return new NativeArray<TType>(m_Descriptor.Count, m_Descriptor.Allocator);
        }

        /// <summary>
        /// Creates a managed system buffer with the specified descriptor.
        /// </summary>
        /// <param name="desc">Descriptor defining the element count and allocator.</param>
        /// <returns>A new <see cref="ManagedSystemBuffer{TType}"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <see cref="SystemBufferDescriptor.Count"/> is less than 1.
        /// </exception>
        public static ManagedSystemBuffer<TType> Create(SystemBufferDescriptor desc)
        {
            if (desc.Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(desc.Count), "System buffer count must be greater than zero.");

            var buffer = new ManagedSystemBuffer<TType>(desc);
            buffer.Create();
            return buffer;
        }

        /// <summary>
        /// Compares managed system buffers by reference.
        /// </summary>
        public override bool Equals(ManagedBuffer<SystemBufferDescriptor, NativeArray<TType>> other)
            => ReferenceEquals(this, other);

        /// <summary>
        /// Releases the underlying NativeArray memory.
        /// </summary>
        public override void Release()
        {
            if (m_Buffer.IsCreated)
            {
                m_Buffer.Dispose();
            }
        }
    }
}