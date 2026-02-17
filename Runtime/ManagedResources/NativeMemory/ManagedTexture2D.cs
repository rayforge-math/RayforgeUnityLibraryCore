using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around Unity's <see cref="Texture2D"/>.
    /// Provides creation, configuration, and controlled release for pooling or resource tracking.
    /// Inherits from <see cref="ManagedBuffer{TDesc, TInternal}"/>.
    /// </summary>
    public sealed class ManagedTexture2D : ManagedBuffer<Texture2dDescriptor, Texture2D>
    {
        /// <summary>
        /// Returns true if the internal Texture2D object is allocated and valid.
        /// Implementation of the abstract property in ManagedBuffer.
        /// </summary>
        public override bool IsCreated => m_Buffer != null;

        /// <summary>Width of the texture in pixels.</summary>
        public int Width => m_Descriptor.Width;

        /// <summary>Height of the texture in pixels.</summary>
        public int Height => m_Descriptor.Height;

        /// <summary>
        /// Public constructor used internally to initialize the wrapper with a descriptor.
        /// Use <see cref="Create"/> to instantiate.
        /// </summary>
        /// <param name="descriptor">Descriptor defining the texture properties.</param>
        public ManagedTexture2D(Texture2dDescriptor descriptor)
            : base(descriptor)
        { }

        /// <summary>
        /// Internal implementation of the allocation logic.
        /// Uses <see cref="m_Descriptor"/> (including AnisoLevel) to instantiate the <see cref="Texture2D"/>.
        /// </summary>
        /// <returns>A new <see cref="Texture2D"/> instance.</returns>
        protected override Texture2D Allocate()
        {
            var d = m_Descriptor;

            if (d.Width <= 0)
                throw new ArgumentOutOfRangeException(nameof(d.Width), "Texture width must be > 0.");
            if (d.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(d.Height), "Texture height must be > 0.");

            var texture = new Texture2D(
                d.Width,
                d.Height,
                d.Format,
                d.MipCount,
                d.Linear)
            {
                filterMode = d.FilterMode,
                wrapMode = d.WrapMode,
                anisoLevel = d.AnisoLevel,
                name = "ManagedTexture2D"
            };

            texture.Apply(false);
            return texture;
        }

        /// <summary>
        /// Factory method to create a managed Texture2D wrapper.
        /// </summary>
        /// <param name="desc">Descriptor describing resolution, format, mipmap settings, and filtering.</param>
        /// <returns>A new <see cref="ManagedTexture2D"/> instance.</returns>
        public static ManagedTexture2D Create(Texture2dDescriptor desc)
        {
            var wrapper = new ManagedTexture2D(desc);
            wrapper.Create();
            return wrapper;
        }

        /// <summary>
        /// Releases the underlying texture and destroys the Unity object reference.
        /// </summary>
        public override void Release()
        {
            if (m_Buffer != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(m_Buffer);
                else UnityEngine.Object.DestroyImmediate(m_Buffer);

                m_Buffer = null;
            }
        }

        /// <summary>
        /// Compares managed textures by reference.
        /// </summary>
        public override bool Equals(ManagedBuffer<Texture2dDescriptor, Texture2D> other)
            => ReferenceEquals(this, other);
    }
}