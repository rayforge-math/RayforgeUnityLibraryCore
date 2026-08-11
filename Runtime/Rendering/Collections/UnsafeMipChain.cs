using System;
using System.Collections.Generic;
using UnityEngine;

using Rayforge.Core.Execution.Abstractions;
using Rayforge.Core.Rendering.Collections.Helpers;

namespace Rayforge.Core.Rendering.Collections
{
    /// <summary>
    /// Represents an "unsafe" mip chain with additional flexibility for advanced scenarios.
    /// This class extends <see cref="MipChain{THandle}"/> and allows:
    /// - Creating mip levels starting at arbitrary indices in the handle array.
    /// - Optionally shrinking the handle array to exactly fit the created mip levels.
    /// - Stacking multiple mip chains into a single handle array.
    /// </summary>
    /// <typeparam name="THandle">Type of the handle (e.g., TextureHandle, RenderTexture, etc.).</typeparam>
    public abstract class UnsafeMipChain<THandle> : MipChain<THandle>
    {
        private Vector2Int[] m_MipResolutionCache;
        private static readonly Vector2Int k_EmptyCacheEntry = Vector2Int.zero;

        /// <summary>
        /// Initializes an empty unsafe mip chain.
        /// </summary>
        public UnsafeMipChain() : base()
        {
            m_MipResolutionCache = Array.Empty<Vector2Int>();
        }

        /// <summary>
        /// Retrieves the resolution of the specified mip level, using the cached value if available.
        /// </summary>
        public Vector2Int GetCachedMipResolution(int mipLevel)
        {
            if (mipLevel < m_MipResolutionCache.Length)
                return m_MipResolutionCache[mipLevel];

            return GetDefaultMipResolution(mipLevel);
        }

        /// <summary>
        /// Resets or resizes the mip resolution cache starting at a specific index.
        /// </summary>
        public void ResetResolutionCache(int startIndex, int count = 0)
        {
            if (startIndex < 0 || startIndex > m_MipResolutionCache.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            int remaining = m_MipResolutionCache.Length - startIndex;

            if (count == 0 || count >= remaining)
            {
                if (startIndex == 0)
                {
                    m_MipResolutionCache = Array.Empty<Vector2Int>();
                }
                else
                {
                    Array.Resize(ref m_MipResolutionCache, startIndex);
                }
            }
            else
            {
                for (int i = startIndex; i < startIndex + count; i++)
                    m_MipResolutionCache[i] = k_EmptyCacheEntry;
            }
        }

        /// <summary>
        /// Completely resets the mip resolution cache.
        /// </summary>
        public void ResetResolutionCache()
            => ResetResolutionCache(0, 0);

        /// <summary>
        /// Overrides Resize to automatically truncate the resolution cache when the container shrinks.
        /// </summary>
        public override void Resize(int newLength, int preserveIndex, int preserveCount)
        {
            base.Resize(newLength, preserveIndex, preserveCount);

            if (m_MipResolutionCache.Length > newLength)
            {
                Array.Resize(ref m_MipResolutionCache, newLength);
            }
        }

        /// <summary>
        /// Intercepts the creation step to automatically populate the mip resolution cache if successful.
        /// </summary>
        protected override bool CreateInternal<THandler>(int index, RenderTextureDescriptor descriptor, ref THandler handler)
        {
            bool created = base.CreateInternal(index, descriptor, ref handler);

            if (created)
            {
                if (m_MipResolutionCache.Length <= index)
                    Array.Resize(ref m_MipResolutionCache, index + 1);

                m_MipResolutionCache[index] = new Vector2Int(descriptor.width, descriptor.height);
            }

            return created;
        }

        /// <summary>
        /// Creates only the first mip level from the specified <see cref="DescriptorMipChain"/> using a struct handler.
        /// </summary>
        public void CreateFirst<THandler>(DescriptorMipChain descriptorChain, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
            => CreateUnsafe(descriptorChain, 0, 1, 0, false, ref handler);

        /// <summary>
        /// Creates a range of mip levels from the specified <see cref="DescriptorMipChain"/> using a struct handler.
        /// </summary>
        public void CreateUnsafe<THandler>(DescriptorMipChain descriptorChain, int startMip, int count, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
            => CreateUnsafe(descriptorChain, startMip, count, startMip, false, ref handler);

        /// <summary>
        /// Creates a range of mip levels from the specified <see cref="DescriptorMipChain"/> using a struct handler.
        /// </summary>
        public void CreateUnsafe<THandler>(DescriptorMipChain descriptorChain, int startMip, int count, bool shrink, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
            => CreateUnsafe(descriptorChain, startMip, count, startMip, shrink, ref handler);

        /// <summary>
        /// Creates a range of mip levels with full control over the handle array using a struct handler.
        /// </summary>
        public void CreateUnsafe<THandler>(DescriptorMipChain descriptorChain, int startMip, int count, int handleStartIndex, bool shrink, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
        {
            if (descriptorChain == null)
                throw new ArgumentNullException(nameof(descriptorChain), "DescriptorMipChain must not be null.");

            var descriptors = descriptorChain.Descriptors;
            var descCount = descriptors == null ? 0 : descriptors.Count;

            startMip = Mathf.Clamp(startMip, 0, descCount - 1);
            count = Mathf.Clamp(count, 1, descCount - startMip);

            if (m_Handles.Length < handleStartIndex + count || shrink)
                Resize(handleStartIndex + count);

            for (int i = 0; i < count; i++)
                CreateInternal(handleStartIndex + i, descriptors[startMip + i], ref handler);
        }

        public void CreateUnsafe<THandler>(RenderTextureDescriptor descriptor, int startMip, int count, bool shrink, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
            => CreateUnsafe(descriptor.width, descriptor.height, descriptor, startMip, count, startMip, shrink, ref handler);

        public void CreateUnsafe<THandler>(int width, int height, RenderTextureDescriptor descriptor, int startMip, int count, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
            => CreateUnsafe(width, height, descriptor, startMip, count, startMip, false, ref handler);

        public void CreateUnsafe<THandler>(int width, int height, RenderTextureDescriptor descriptor, int startMip, int count, bool shrink, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
            => CreateUnsafe(width, height, descriptor, startMip, count, startMip, shrink, ref handler);

        public void CreateUnsafe<THandler>(int width, int height, RenderTextureDescriptor descriptor, int startMip, int count, int handleStartIndex, bool shrink, ref THandler handler)
            where THandler : IFunctionHandler<MipCreateContext<THandle>, bool>
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Base width and height must be greater than zero.");

            Vector2Int baseRes = new Vector2Int(width, height);

            if (m_Handles.Length < handleStartIndex + count || shrink)
                Resize(handleStartIndex + count);

            for (int i = 0; i < count; i++)
            {
                var mipRes = MipChainHelpers.DefaultMipResolution(startMip + i, baseRes);
                descriptor.width = mipRes.x;
                descriptor.height = mipRes.y;

                CreateInternal(handleStartIndex + i, descriptor, ref handler);
            }
        }

        /// <summary>
        /// Sets a handle at an arbitrary index in the mip chain and resets its resolution cache entry.
        /// </summary>
        public void SetHandleUnsafe(int index, THandle handle)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            var expectedSize = index + 1;
            if (m_Handles.Length < expectedSize)
                Resize(expectedSize);

            m_Handles[index] = handle;

            if (index < m_MipResolutionCache.Length)
            {
                m_MipResolutionCache[index] = k_EmptyCacheEntry;
            }
        }

        public void CopyFromUnsafe(MipChain<THandle> other, int start, int count, int handleStartIndex)
            => CopyFromUnsafe(other.Handles, start, count, handleStartIndex);

        /// <summary>
        /// Copies a range of handles and synchronizes or resets the resolution cache accordingly.
        /// </summary>
        public void CopyFromUnsafe(IReadOnlyList<THandle> other, int start, int count, int handleStartIndex)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (handleStartIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(handleStartIndex), "Start index must be non-negative.");

            if (other.Count == 0 || count <= 0)
                return;

            start = Math.Clamp(start, 0, other.Count);
            count = Math.Clamp(count, 0, other.Count - start);

            var requiredSize = handleStartIndex + count;
            if (m_Handles.Length < requiredSize)
                Resize(requiredSize);

            for (int i = 0; i < count; i++)
                m_Handles[handleStartIndex + i] = other[start + i];

            if (other is UnsafeMipChain<THandle> unsafeOther)
            {
                for (int i = 0; i < count; i++)
                {
                    int targetIndex = handleStartIndex + i;
                    int sourceIndex = start + i;
                    Vector2Int res = unsafeOther.GetCachedMipResolution(sourceIndex);

                    if (m_MipResolutionCache.Length <= targetIndex)
                        Array.Resize(ref m_MipResolutionCache, targetIndex + 1);

                    m_MipResolutionCache[targetIndex] = res;
                }
            }
            else
            {
                ResetResolutionCache(handleStartIndex, count);
            }
        }
    }
}