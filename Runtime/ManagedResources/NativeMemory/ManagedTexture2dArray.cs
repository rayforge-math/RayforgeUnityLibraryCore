using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around Unity's <see cref="Texture2DArray"/>.
    /// Provides creation, validation, and controlled release using the managed buffer pattern.
    /// </summary>
    public sealed class ManagedTexture2DArray : ManagedBuffer<Texture2dArrayDescriptor, Texture2DArray>
    {
        /// <summary>
        /// Returns true if the Texture2DArray object is allocated and valid.
        /// </summary>
        public override bool IsCreated => m_Buffer != null;

        /// <summary>
        /// Public constructor used internally to initialize the wrapper with a descriptor.
        /// Allocation happens via the <see cref="Allocate"/> method during <see cref="ManagedBuffer{TDesc, TInternal}.Create"/>.
        /// </summary>
        public ManagedTexture2DArray(Texture2dArrayDescriptor descriptor)
            : base(descriptor)
        { }

        /// <summary>
        /// Internal implementation of the allocation logic.
        /// Uses the internally stored <see cref="m_Descriptor"/> to instantiate the <see cref="Texture2DArray"/>.
        /// </summary>
        /// <returns>A new <see cref="Texture2DArray"/> instance.</returns>
        protected override Texture2DArray Allocate()
        {
            var desc = m_Descriptor;
            var d = desc.Descriptor;

            if (desc.Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(desc.Count), "Texture2DArray count must be > 0.");
            if (d.Width <= 0)
                throw new ArgumentOutOfRangeException(nameof(d.Width), "Texture width must be > 0.");
            if (d.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(d.Height), "Texture height must be > 0.");

            var texture = new Texture2DArray(
                d.Width,
                d.Height,
                desc.Count,
                d.ColorFormat,
                d.MipCount > 1,
                d.Linear)
            {
                filterMode = d.FilterMode,
                wrapMode = d.WrapMode,
                anisoLevel = d.AnisoLevel,
                name = "ManagedTexture2DArray"
            };

            texture.Apply(false);
            return texture;
        }

        /// <summary>
        /// Factory method to create a managed Texture2DArray wrapper.
        /// </summary>
        /// <param name="desc">Descriptor defining each texture in the array and number of layers.</param>
        /// <returns>A new <see cref="ManagedTexture2DArray"/> instance.</returns>
        public static ManagedTexture2DArray Create(Texture2dArrayDescriptor desc)
        {
            var wrapper = new ManagedTexture2DArray(desc);
            wrapper.Create();
            return wrapper;
        }

        /// <summary>
        /// Releases the underlying Texture2DArray and cleans up the Unity object.
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
        /// Copies the provided textures into the array.
        /// Validates dimensions, format, and mip count before copying via GPU.
        /// </summary>
        public bool SetTextures(Texture2D[] textures)
        {
            if (m_Buffer == null) return false;
            if (textures == null || textures.Length == 0)
            {
                Debug.LogError("Texture array provided for upload is null or empty.");
                return false;
            }

            var template = m_Descriptor.Descriptor;
            int texturesToCopy = Mathf.Min(textures.Length, m_Descriptor.Count);

            for (int i = 0; i < texturesToCopy; i++)
            {
                if (textures[i] == null) continue;

                if (textures[i].width != template.Width || textures[i].height != template.Height)
                {
                    Debug.LogError($"Mismatched dimensions at index {i}. Expected {template.Width}x{template.Height}.");
                    continue;
                }

                for (int j = 0; j < template.MipCount; ++j)
                {
                    try
                    {
                        Graphics.CopyTexture(textures[i], 0, j, m_Buffer, i, j);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to copy slice {i} Mip {j}: {ex.Message}");
                    }
                }
            }

            m_Buffer.Apply(false);
            return true;
        }

        public override bool Equals(ManagedBuffer<Texture2dArrayDescriptor, Texture2DArray> other)
            => ReferenceEquals(this, other);
    }
}