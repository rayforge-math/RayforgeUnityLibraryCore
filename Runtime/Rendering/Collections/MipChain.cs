using Rayforge.Core.Execution.Abstractions;
using Rayforge.Core.Rendering.Abstractions;
using Rayforge.Core.Rendering.Collections.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Rendering.Collections
{
    /// <summary>
    /// Context data passed to the creation handler for a specific mip level.
    /// </summary>
    /// <typeparam name="THandle">Type of the handle.</typeparam>
    public struct MipCreateContext<THandle>
    {
        /// <summary>Descriptor describing the texture to create for this mip level.</summary>
        public RenderTextureDescriptor Descriptor;

        /// <summary>Index of the mip level being created.</summary>
        public int MipLevel;

        internal THandle[] Handles;
        internal int Index;

        /// <summary>
        /// Reference to the actual handle slot stored internally in the mip chain.
        /// </summary>
        public ref THandle Handle => ref Handles[Index];
    }

    /// <summary>
    /// Represents a chain of handles corresponding to mip levels of a texture.
    /// Provides creation, resizing, copying, and optional generation of successive mip levels
    /// using zero-allocation struct function handlers for creation and an abstract method for handle destruction.
    /// </summary>
    /// <typeparam name="THandle">Type of the handle (e.g., TextureHandle, RenderTexture, etc.).</typeparam>
    public abstract class MipChain<THandle> : IRenderingCollection<THandle>
    {
        protected THandle[] m_Handles;

        private Vector2Int m_BaseResolution = new Vector2Int(-1, -1);
        private static readonly Func<int, Vector2Int, Vector2Int> m_CalculateMipResFunc = MipChainHelpers.DefaultMipResolution;

        /// <summary>Read-only access to the handles.</summary>
        public IReadOnlyList<THandle> Handles => m_Handles ?? Array.Empty<THandle>();

        /// <summary>Access a specific mip level handle by index.</summary>
        /// <param name="index">The mip level index.</param>
        public THandle this[int index] => m_Handles[index];

        /// <summary>Total number of mip levels.</summary>
        public int MipCount => m_Handles?.Length ?? 0;

        /// <summary>
        /// Initializes an empty mip chain.
        /// </summary>
        public MipChain()
        {
            m_Handles = Array.Empty<THandle>();
        }

        /// <summary>
        /// Destroys or releases an individual handle to prevent memory or resource leaks.
        /// Must be implemented by derived classes depending on the handle type semantics.
        /// </summary>
        /// <param name="handle">Reference to the handle being destroyed.</param>
        protected abstract void DestroyHandle(ref THandle handle);

        /// <summary>
        /// Computes the theoretical resolution of the specified mip level, based on the base resolution.
        /// </summary>
        /// <param name="mipLevel">
        /// Index of the mip level to compute (0 = base level, 1 = first mip, etc.).
        /// </param>
        /// <returns>
        /// A <see cref="Vector2Int"/> representing the width and height of the mip level
        /// as defined by the configured mip resolution calculation function (default / theoretical).
        /// </returns>
        public Vector2Int GetDefaultMipResolution(int mipLevel)
            => m_CalculateMipResFunc(mipLevel, m_BaseResolution);

        /// <summary>
        /// Creates all mip levels from the specified <see cref="DescriptorMipChain"/> using a struct-based function handler.
        /// Handles are stored at indices starting from 0 in the handle array.
        /// The handle array is resized to exactly match the number of mip levels in the chain.
        /// </summary>
        /// <typeparam name="THandler">Type of the creation function handler.</typeparam>
        /// <param name="descriptorChain">The descriptor chain providing descriptors for each mip level.</param>
        /// <param name="handler">Reference to the creation handler struct.</param>
        /// <returns><c>true</c> if at least one new handle was created; <c>false</c> if all handles were reused.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="descriptorChain"/> is null.</exception>
        public bool Create<THandler>(DescriptorMipChain descriptorChain, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
        {
            if (descriptorChain == null)
                throw new ArgumentNullException(nameof(descriptorChain), "DescriptorMipChain must not be null.");

            var descriptors = descriptorChain.Descriptors;
            var count = descriptors == null ? 0 : descriptors.Count;
            Resize(count);

            if (count > 0)
            {
                var first = descriptors[0];
                m_BaseResolution = new Vector2Int(first.width, first.height);
            }

            bool anyCreated = false;
            for (int i = 0; i < count; i++)
                anyCreated |= CreateInternal(i, descriptors[i], ref handler);

            return anyCreated;
        }

        /// <summary>
        /// Creates all mip levels based on a single <see cref="RenderTextureDescriptor"/> as the base descriptor.
        /// </summary>
        public bool Create<THandler>(RenderTextureDescriptor descriptor, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
            => Create(descriptor.width, descriptor.height, descriptor, 1, ref handler);

        /// <summary>
        /// Creates all mip levels based on a single <see cref="RenderTextureDescriptor"/> as the base descriptor.
        /// </summary>
        public bool Create<THandler>(RenderTextureDescriptor descriptor, int mipCount, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
            => Create(descriptor.width, descriptor.height, descriptor, mipCount, ref handler);

        /// <summary>
        /// Creates all mip levels based on a single <see cref="RenderTextureDescriptor"/> as the base descriptor.
        /// Handles are stored at indices starting from 0 in the handle array.
        /// The handle array is resized to exactly match the number of mip levels being created. 
        /// If it was previously larger or smaller, it will be resized to <paramref name="mipCount"/>.
        /// </summary>
        /// <param name="width">Width of the base mip level.</param>
        /// <param name="height">Height of the base mip level.</param>
        /// <param name="descriptor">Base descriptor for mip creation; will be resized for each mip level.</param>
        /// <param name="mipCount">Total number of mip levels to create.</param>
        /// <param name="handler">Reference to the creation handler struct.</param>
        /// <returns><c>true</c> if at least one new handle was created; <c>false</c> if all handles were reused.</returns>
        /// <exception cref="ArgumentException">Thrown if the descriptor width or height is not positive.</exception>
        public bool Create<THandler>(int width, int height, RenderTextureDescriptor descriptor, int mipCount, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Base width and height must be greater than zero.");

            m_BaseResolution = new Vector2Int(width, height);
            Resize(mipCount);

            bool anyCreated = false;
            for (int i = 0; i < mipCount; i++)
            {
                var mipRes = GetDefaultMipResolution(i);
                descriptor.width = mipRes.x;
                descriptor.height = mipRes.y;
                anyCreated |= CreateInternal(i, descriptor, ref handler);
            }

            return anyCreated;
        }

        /// <summary>
        /// Internal method that invokes the handler for a single mip level.
        /// </summary>
        protected virtual bool CreateInternal<THandler>(int index, RenderTextureDescriptor descriptor, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
        {
            var context = new MipCreateContext<THandle>
            {
                Descriptor = descriptor,
                MipLevel = index,
                Handles = m_Handles,
                Index = index
            };

            return handler.Execute(context);
        }

        /// <summary>
        /// Resizes the internal array to <paramref name="newLength"/>.
        /// Unpreserved handles are destroyed using <see cref="DestroyHandle(ref THandle)"/>.
        /// </summary>
        /// <param name="newLength">New array length.</param>
        public virtual void Resize(int newLength)
            => Resize(newLength, 0, MipCount);

        /// <summary>
        /// Resizes the array and optionally preserves a subset of existing elements, destroying unpreserved ones using <see cref="DestroyHandle(ref THandle)"/>.
        /// </summary>
        /// <param name="newLength">New array length.</param>
        /// <param name="preserveIndex">Start index in the old array to preserve.</param>
        /// <param name="preserveCount">Number of elements to preserve.</param>
        public virtual void Resize(int newLength, int preserveIndex, int preserveCount)
        {
            if (newLength < 0) newLength = 0;
            if (MipCount == newLength) return;

            if (m_Handles != null && m_Handles.Length > 0)
            {
                preserveIndex = Math.Clamp(preserveIndex, 0, m_Handles.Length - 1);
                preserveCount = Math.Min(preserveCount, m_Handles.Length - preserveIndex);
                preserveCount = Math.Min(preserveCount, newLength);

                for (int i = 0; i < m_Handles.Length; i++)
                {
                    bool isPreserved = i >= preserveIndex && i < (preserveIndex + preserveCount);

                    if (!isPreserved && m_Handles[i] != null)
                    {
                        DestroyHandle(ref m_Handles[i]);
                    }
                }
            }
            else
            {
                preserveCount = 0;
            }

            if (newLength == 0)
            {
                m_Handles = Array.Empty<THandle>();
                return;
            }

            var newHandles = new THandle[newLength];

            if (m_Handles != null && preserveCount > 0)
            {
                Array.Copy(m_Handles, preserveIndex, newHandles, 0, preserveCount);
            }

            m_Handles = newHandles;
        }

        /// <summary>
        /// Returns a read-only span of handles.
        /// </summary>
        public ReadOnlySpan<THandle> AsSpan()
            => m_Handles == null
            ? ReadOnlySpan<THandle>.Empty
            : m_Handles.AsSpan(0, MipCount);

        /// <summary>
        /// Returns a read-only span of handles.
        /// </summary>
        /// <param name="start">Start index of the span.</param>
        /// <param name="length">Number of elements in the span.</param>
        public ReadOnlySpan<THandle> AsSpan(int start, int length)
        {
            if (m_Handles == null)
                return ReadOnlySpan<THandle>.Empty;

            start = Math.Clamp(start, 0, MipCount);
            length = Math.Clamp(length, 0, MipCount - start);
            return m_Handles.AsSpan(start, length);
        }

        /// <summary>
        /// Copies all handles from another mip chain.
        /// </summary>
        /// <param name="other">Source mip chain.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="other"/> is null.</exception>
        public void CopyFrom(MipChain<THandle> other)
            => CopyFrom(other, 0, other.MipCount);

        /// <summary>
        /// Copies a range of handles from another mip chain.
        /// <para>
        /// This method can bypass the usual safety guarantees of a mip chain
        /// (for example, contiguous layout or complete mip coverage) and is
        /// intended for advanced usage where such constraints are managed manually.
        /// </para>
        /// </summary>
        /// <param name="other">Source mip chain.</param>
        /// <param name="start">Start index in the source chain.</param>
        /// <param name="count">Number of handles to copy.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="other"/> is null.</exception>
        public void CopyFrom(MipChain<THandle> other, int start, int count)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            start = Math.Clamp(start, 0, other.MipCount);
            count = Math.Clamp(count, 0, other.MipCount - start);

            Resize(count);
            for (int i = 0; i < count; i++)
                m_Handles[i] = other[start + i];
        }

        /// <summary>
        /// Creates a MipChain from a single handle. The chain will have length 1.
        /// Useful when no actual mip levels are needed and a single texture/handle represents the entire chain.
        /// </summary>
        /// <param name="handle">The single handle representing the chain.</param>
        public void CopyFrom(THandle handle)
        {
            Resize(1);
            m_Handles[0] = handle;
        }

        /// <summary>
        /// Enumerates all consecutive mip transitions in the chain.
        /// Each iteration yields a pair where the source mip (i-1) is used
        /// to generate the destination mip (i).
        /// </summary>
        /// <remarks>
        /// The first yielded element always represents the transition
        /// from mip level 0 (source) to mip level 1 (destination).
        /// </remarks>
        /// <returns>
        /// An enumerable sequence of <see cref="MipPair{THandle}"/> describing
        /// all mip generation steps in ascending order.
        /// </returns>
        public IEnumerable<MipPair<THandle>> EnumerateMipPairs()
        {
            // Mip 0 has no source; generation starts at mip 1
            for (int mip = 1; mip < MipCount; ++mip)
            {
                yield return new MipPair<THandle>(
                    m_Handles[mip - 1],
                    m_Handles[mip],
                    mip
                );
            }
        }
    }
}