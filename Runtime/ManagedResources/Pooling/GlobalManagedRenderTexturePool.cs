using UnityEngine;
using Rayforge.Core.ManagedResources.NativeMemory;

namespace Rayforge.Core.ManagedResources.Pooling
{
    /// <summary>
    /// Global static access to a pool of managed render textures.
    /// Provides simple Rent() for default use.
    /// </summary>
    public sealed class GlobalManagedRenderTexturePool : GlobalManagedPoolBase<RenderTextureDescriptorWrapper, ManagedRenderTexture>
    {
        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static GlobalManagedRenderTexturePool()
        {
            m_Pool = new LeasedBufferPool<RenderTextureDescriptorWrapper, ManagedRenderTexture>(
                createFunc: desc =>
                {
                    desc.FilterMode = FilterMode.Bilinear;
                    desc.WrapMode = TextureWrapMode.Clamp;
                    return ManagedRenderTexture.Create(desc);
                },
                releaseFunc: buffer => buffer.Release()
            );
        }
    }
}