using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around <see cref="RenderTexture"/> that ensures proper creation,
    /// configuration, and disposal. Inherits from <see cref="ManagedBuffer{TDesc, TBuffer}"/>.
    /// </summary>
    public sealed class ManagedRenderTexture : ManagedBuffer<RenderTextureDescriptorWrapper, RenderTexture>
    {
        /// <summary>
        /// Returns true if the RenderTexture object exists and the native GPU resource is allocated.
        /// </summary>
        public override bool IsCreated => m_Buffer != null && m_Buffer.IsCreated();

        /// <summary>
        /// Public constructor to initialize the managed render texture.
        /// Use the <see cref="Create"/> factory method instead of calling directly.
        /// </summary>
        /// <param name="descriptor">Descriptor describing texture properties.</param>
        public ManagedRenderTexture(RenderTextureDescriptorWrapper descriptor)
            : base(descriptor)
        { }

        /// <summary>
        /// Implementation of the abstract allocation method. 
        /// Uses the internal <see cref="m_Descriptor"/> to instantiate the <see cref="RenderTexture"/>.
        /// </summary>
        /// <returns>The newly created and initialized <see cref="RenderTexture"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if width or height is invalid.</exception>
        protected override RenderTexture Allocate()
        {
            var d = m_Descriptor;

            if (d.Width <= 0)
                throw new ArgumentOutOfRangeException(nameof(d.Width), "RenderTexture width must be greater than zero.");
            if (d.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(d.Height), "RenderTexture height must be greater than zero.");

            var texture = new RenderTexture(d.InternalDescriptor)
            {
                filterMode = d.FilterMode,
                wrapMode = d.WrapMode,
                anisoLevel = d.AnisoLevel,
                name = "ManagedRenderTexture"
            };

            texture.Create();
            return texture;
        }

        /// <summary>
        /// Creates and optionally initializes a managed render texture wrapper.
        /// Sampling settings are now part of the <see cref="RenderTextureDescriptorWrapper"/>.
        /// </summary>
        /// <param name="desc">Descriptor defining resolution, format, and sampling properties.</param>
        /// <param name="init">If true, the GPU resource is allocated immediately.</param>
        /// <returns>A new <see cref="ManagedRenderTexture"/> instance.</returns>
        public static ManagedRenderTexture Create(RenderTextureDescriptorWrapper desc)
        {
            var wrapper = new ManagedRenderTexture(desc);
            wrapper.Create();
            return wrapper;
        }

        /// <summary>
        /// Releases the underlying GPU render texture and clears internal references.
        /// After this call, the texture is no longer valid.
        /// </summary>
        public override void Release()
        {
            if (m_Buffer != null)
            {
                if (m_Buffer.IsCreated())
                {
                    m_Buffer.Release();
                }

                if (Application.isPlaying) UnityEngine.Object.Destroy(m_Buffer);
                else UnityEngine.Object.DestroyImmediate(m_Buffer);

                m_Buffer = null;
            }
        }

        /// <summary>
        /// Compares managed render textures by reference. Useful for pooling or resource tracking.
        /// </summary>
        public override bool Equals(ManagedBuffer<RenderTextureDescriptorWrapper, RenderTexture> other)
            => ReferenceEquals(this, other);
    }
}