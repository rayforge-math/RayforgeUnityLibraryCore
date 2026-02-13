using Rayforge.Core.ManagedResources.NativeMemory;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.Pooling
{
    /// <summary>
    /// Managed pool for <see cref="ManagedRenderTexture"/> objects.
    /// Provides default create/release functions.
    /// </summary>
    public sealed class ManagedRenderTexturePool : LeasedBufferPool<RenderTextureDescriptorWrapper, ManagedRenderTexture>
    {
        private static readonly BufferCreateFunc<RenderTextureDescriptorWrapper, ManagedRenderTexture> k_DefaultCreate =
            desc =>
            {
                desc.FilterMode = FilterMode.Bilinear;
                desc.WrapMode = TextureWrapMode.Clamp;
                return ManagedRenderTexture.Create(desc);
            };

        private static readonly BufferReleaseFunc<ManagedRenderTexture> k_DefaultRelease =
            buffer => buffer.Release();

        /// <summary>
        /// Default constructor using standard factory methods for managed render textures.
        /// </summary>
        public ManagedRenderTexturePool()
            : base(
                createFunc: k_DefaultCreate,
                releaseFunc: k_DefaultRelease)
        { }

        /// <summary>
        /// Constructor allowing custom create/release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        public ManagedRenderTexturePool(
            BufferCreateFunc<RenderTextureDescriptorWrapper, ManagedRenderTexture> createFunc,
            BufferReleaseFunc<ManagedRenderTexture> releaseFunc)
            : base(createFunc, releaseFunc)
        { }

        /// <summary>
        /// Constructor allowing custom create/release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer.</param>
        public ManagedRenderTexturePool(BufferCreateFunc<RenderTextureDescriptorWrapper, ManagedRenderTexture> createFunc)
            : base(createFunc, k_DefaultRelease)
        { }
    }
}