using Rayforge.Core.Common.Cache;
using Rayforge.Core.ManagedResources.Abstractions;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// A strongly-typed wrapper for <see cref="ComputeBuffer"/> that implements <see cref="IManagedArray{T}"/>.
    /// Inherits base buffer logic while providing typed element access.
    /// </summary>
    /// <typeparam name="T">The unmanaged value type stored in the buffer.</typeparam>
    public sealed class ManagedComputeArray<T> : ManagedComputeBuffer, IManagedArray<T, T>
        where T : unmanaged
    {
        /// <summary>
        /// Gets the number of elements allocated in the underlying GPU buffer.
        /// </summary>
        /// <value>The element count, or 0 if the buffer is not created.</value>
        public int Count => m_Buffer != null ? m_Buffer.count : 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedComputeArray{T}"/> class.
        /// </summary>
        /// <param name="desc">Descriptor defining count, stride, and type. Stride must match <typeparamref name="T"/>.</param>
        /// <exception cref="ArgumentException">Thrown if the descriptor's stride does not match the size of <typeparamref name="T"/>.</exception>
        public ManagedComputeArray(ComputeBufferDescriptor desc) : base(desc)
        {
            int expectedStride = Marshal.SizeOf<T>();
            if (desc.Stride != expectedStride)
            {
                throw new ArgumentException(
                    $"Stride mismatch: The descriptor defines {desc.Stride} bytes, " +
                    $"but the type {typeof(T).Name} requires {expectedStride} bytes.");
            }
        }

        /// <summary>
        /// Uploads a single element to the GPU at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index in the buffer.</param>
        /// <param name="element">The data to upload.</param>
        /// <remarks>
        /// Uses a temporary pooled array to perform the transfer. Operation is ignored if the buffer is not created.
        /// </remarks>
        public void SetElement(int index, T element)
        {
            if (!IsCreated) return;

            var temp = StaticArrayPool<T>.Get(1);
            temp[0] = element;

            m_Buffer.SetData(temp, 0, index, 1);
        }

        /// <summary>
        /// Downloads a single element from the GPU at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index in the buffer.</param>
        /// <param name="element">The destination reference to receive the data.</param>
        /// <remarks>
        /// This is a synchronous read-back operation and may impact performance if called frequently.
        /// </remarks>
        public void CopyElementTo(int index, ref T element)
        {
            if (!IsCreated) return;

            var temp = StaticArrayPool<T>.Get(1);

            m_Buffer.GetData(temp, 0, index, 1);
            element = temp[0];
        }

        /// <summary>
        /// Static factory method to create and allocate a typed compute array.
        /// </summary>
        /// <param name="count">Number of elements to allocate.</param>
        /// <param name="type">The <see cref="ComputeBufferType"/> for the allocation.</param>
        /// <returns>A fully initialized and allocated <see cref="ManagedComputeArray{T}"/>.</returns>
        public static ManagedComputeArray<T> CreateTyped(int count, ComputeBufferType type = ComputeBufferType.Structured)
        {
            var desc = new ComputeBufferDescriptor
            {
                Count = count,
                Stride = Marshal.SizeOf<T>(),
                Type = type
            };

            var array = new ManagedComputeArray<T>(desc);
            array.Create();
            return array;
        }
    }
}