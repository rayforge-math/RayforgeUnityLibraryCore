using Rayforge.Core.ManagedResources.Abstractions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using Rayforge.Core.Common.Cache;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around Unity's <see cref="ComputeBuffer"/> that handles allocation, data upload, and cleanup.
    /// Inherits from <see cref="ManagedBuffer{TDesc,TBuffer}"/> for generic GPU resource management.
    /// </summary>
    public sealed class ManagedComputeBuffer : ManagedBuffer<ComputeBufferDescriptor, ComputeBuffer>
    {
        /// <summary>
        /// Returns true if the ComputeBuffer is allocated and valid on the GPU.
        /// </summary>
        public override bool IsCreated => m_Buffer != null && m_Buffer.IsValid();

        /// <summary>
        /// Public constructor: initializes the managed buffer with an existing ComputeBuffer and descriptor.
        /// Use the <see cref="Create"/> methods to allocate buffers safely.
        /// </summary>
        /// <param name="desc">Descriptor describing buffer properties.</param>
        public ManagedComputeBuffer(ComputeBufferDescriptor desc)
            : base(desc)
        { }

        /// <summary>
        /// Implementation of the abstract allocation method from <see cref="ManagedBuffer{TDesc, TInternal}"/>.
        /// Performs the actual Unity <see cref="ComputeBuffer"/> instantiation.
        /// </summary>
        /// <param name="desc">The descriptor to use for allocation.</param>
        /// <returns>The newly allocated <see cref="ComputeBuffer"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count or stride is less than or equal to 0.</exception>
        protected override ComputeBuffer Allocate()
        {
            if (Descriptor.Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(Descriptor.Count), "ComputeBuffer count must be > 0.");
            if (Descriptor.Stride <= 0)
                throw new ArgumentOutOfRangeException(nameof(Descriptor.Stride), "ComputeBuffer stride must be > 0.");

            return new ComputeBuffer(Descriptor.Count, Descriptor.Stride, Descriptor.Type);
        }

        /// <summary>
        /// Creates a compute buffer from a descriptor.
        /// Validates count and stride before allocation.
        /// </summary>
        /// <param name="desc">Descriptor defining the buffer size, stride, and type.</param>
        /// <returns>A new <see cref="ManagedComputeBuffer"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <see cref="ComputeBufferDescriptor.Count"/> is <= 0
        /// or <see cref="ComputeBufferDescriptor.Stride"/> is <= 0.
        /// </exception>
        public static ManagedComputeBuffer Create(ComputeBufferDescriptor desc)
        {
            var buffer = new ManagedComputeBuffer(desc);
            buffer.Create();
            return buffer;
        }

        /// <summary>
        /// Creates a compute buffer from a raw count and stride.
        /// </summary>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="stride">Stride in bytes per element.</param>
        /// <param name="type">Optional buffer type. Defaults to <see cref="ComputeBufferType.Structured"/>.</param>
        /// <returns>A new <see cref="ManagedComputeBuffer"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="count"/> or <paramref name="stride"/> are <= 0.
        /// </exception>
        public static ManagedComputeBuffer Create(int count, int stride, ComputeBufferType type = ComputeBufferType.Structured)
        {
            var desc = new ComputeBufferDescriptor { Count = count, Stride = stride, Type = type };
            return Create(desc);
        }

        /// <summary>
        /// Creates a compute buffer for a strongly-typed element array.
        /// Automatically calculates the stride from the type size.
        /// </summary>
        /// <typeparam name="TType">The unmanaged element type stored in the buffer.</typeparam>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="type">Optional buffer type. Defaults to <see cref="ComputeBufferType.Structured"/>.</param>
        /// <returns>A new <see cref="ManagedComputeBuffer"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="count"/> is <= 0 or if stride (calculated from <typeparamref name="TType"/>) is 0.
        /// </exception>
        public static ManagedComputeBuffer Create<TType>(int count, ComputeBufferType type = ComputeBufferType.Structured)
            where TType : unmanaged
        {
            int stride = Marshal.SizeOf<TType>();
            return Create(count, stride, type);
        }

        /// <summary>
        /// Uploads raw array data to the GPU buffer.
        /// </summary>
        public void SetData(Array data)
            => m_Buffer.SetData(data);

        /// <summary>
        /// Uploads a strongly-typed list to the GPU buffer.
        /// </summary>
        public void SetData<T>(List<T> data) where T : struct
            => m_Buffer.SetData(data);

        /// <summary>
        /// Uploads a native array (e.g., NativeArray) to the GPU buffer.
        /// </summary>
        public void SetData<T>(NativeArray<T> data) where T : struct
            => m_Buffer.SetData(data);

        /// <summary>
        /// Reads back data from the GPU buffer into a CPU array.
        /// </summary>
        public void GetData(Array data)
            => m_Buffer.GetData(data);

        /// <summary>
        /// Sets the internal counter value for Append/Consume buffers.
        /// </summary>
        public void SetCounterValue(uint counterValue)
            => m_Buffer.SetCounterValue(counterValue);

        /// <summary>
        /// Uploads data from a <see cref="IComputeData{T}"/> container to the buffer.
        /// Useful for e.g. constant buffers.
        /// </summary>
        public void SetData<T>(IComputeData<T> data)
            where T : unmanaged
        {
            if (data == null) return;
            var uploadCache = StaticArrayPool<T>.Get(1);
            uploadCache[0] = data.RawData;
            SetData(uploadCache);
        }

        /// <summary>
        /// Uploads data from a <see cref="IComputeDataArray{T}"/> container to the buffer.
        /// Useful for uniform-style data structures.
        /// </summary>
        public void SetData<T>(IComputeDataArray<T> data)
            where T : unmanaged
        {
            if (data == null) return;
            SetData(data.RawData);
        }

        /// <summary>
        /// Releases the GPU buffer.
        /// After calling this, the buffer is no longer valid and internal references are cleared.
        /// </summary>
        public override void Release()
        {
            if (m_Buffer != null)
            {
                m_Buffer.Release();
                m_Buffer = null;
            }
        }

        /// <summary>
        /// Compares buffer instances by reference.
        /// For pooled or managed buffers, reference equality is usually sufficient.
        /// </summary>
        public override bool Equals(ManagedBuffer<ComputeBufferDescriptor, ComputeBuffer> other)
            => ReferenceEquals(this, other);
    }
}