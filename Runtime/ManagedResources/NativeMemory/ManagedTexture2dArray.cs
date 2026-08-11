using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around Unity's <see cref="Texture2DArray"/>.
    /// Provides creation, validation, and controlled release using the managed buffer pattern.
    /// </summary>
    public sealed class ManagedTexture2DArray : ManagedBuffer<Texture2dArrayDescriptor, Texture2DArray>, IManagedArray<Texture, RenderTexture>
    {
        /// <summary>
        /// Gets the number of slices in the texture array.
        /// </summary>
        public int Count => m_Buffer != null ? m_Buffer.depth : 0;

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
            var d = desc.SliceDescriptor;

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
                d.Format,
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
        /// Copies a source texture into a specific slice of the array. 
        /// Supports <see cref="Texture2D"/> and <see cref="RenderTexture"/>.
        /// </summary>
        /// <param name="index">The target slice index within the array.</param>
        /// <param name="source">The source texture to copy data from.</param>
        /// <exception cref="NullReferenceException">Thrown if the internal buffer is not yet created.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the source texture is null.</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is outside the bounds of the array.</exception>
        /// <exception cref="ArgumentException">Thrown if the source dimensions do not match the array settings.</exception>
        public void SetSlice(int index, Texture source)
        {
            if (m_Buffer == null)
                throw new NullReferenceException("GPU buffer is not allocated.");
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (index < 0 || index >= m_Descriptor.Count)
                throw new IndexOutOfRangeException($"Slice index {index} is out of bounds.");

            var d = m_Descriptor.SliceDescriptor;

            if (source.width != d.Width || source.height != d.Height)
            {
                throw new ArgumentException(
                    $"Dimension mismatch! Expected {d.Width}x{d.Height}, but got {source.width}x{source.height}.");
            }

            int sourceMips = 1;
            if (source is Texture2D t2d) sourceMips = t2d.mipmapCount;
            else if (source is RenderTexture rt) sourceMips = rt.useMipMap ? rt.mipmapCount : 1;

            int mipsToCopy = Mathf.Min(d.MipCount, sourceMips);

            for (int m = 0; m < mipsToCopy; m++)
            {
                Graphics.CopyTexture(source, 0, m, m_Buffer, index, m);
            }
        }

        /// <summary>
        /// Extracts a specific slice from the array into a provided <see cref="RenderTexture"/>.
        /// </summary>
        /// <param name="index">The slice index to read from.</param>
        /// <param name="destination">The target RenderTexture that will receive the slice data.</param>
        /// <exception cref="NullReferenceException">Thrown if the internal buffer is not yet created.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the destination is null.</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is outside the bounds of the array.</exception>
        public void GetSlice(int index, RenderTexture destination)
        {
            if (m_Buffer == null)
                throw new NullReferenceException("GPU buffer is not allocated.");
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (index < 0 || index >= m_Descriptor.Count)
                throw new IndexOutOfRangeException($"Index {index} is out of bounds.");

            Graphics.Blit(m_Buffer, destination, index, 0);
        }

        /// <summary>
        /// Performs a bulk upload of an entire array of textures into the GPU resource.
        /// </summary>
        /// <param name="textures">An array of <see cref="Texture2D"/> to be uploaded into the slices.</param>
        /// <returns>True if the upload operation was completed successfully.</returns>
        /// <remarks>
        /// This method will call <see cref="Texture2DArray.Apply"/> after all slices have been copied.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if the textures array is null.</exception>
        public bool SetTextures(Texture2D[] textures)
        {
            if (textures == null)
                throw new ArgumentNullException(nameof(textures));
            if (m_Buffer == null)
                return false;

            int count = Mathf.Min(textures.Length, m_Descriptor.Count);
            for (int i = 0; i < count; i++)
            {
                if (textures[i] != null)
                    SetSlice(i, textures[i]);
            }

            m_Buffer.Apply(false);
            return true;
        }

        /// <summary>
        /// Implementation of <see cref="IManagedArray{T}.SetElement"/>.
        /// Redirects to <see cref="SetSlice(int, Texture)"/>.
        /// </summary>
        /// <param name="index">Target slice index.</param>
        /// <param name="element">Source texture (Texture2D or RenderTexture).</param>
        public void SetElement(int index, Texture element) => SetSlice(index, element);

        /// <summary>
        /// Implementation of <see cref="IManagedArray{T}.CopyElementTo"/>.
        /// Extracts a slice into a <see cref="RenderTexture"/>.
        /// </summary>
        /// <param name="index">Source slice index.</param>
        /// <param name="element">Target RenderTexture reference.</param>
        /// <remarks>
        /// Note: The 'ref' element must be an existing, allocated RenderTexture.
        /// </remarks>
        public void CopyElementTo(int index, ref RenderTexture element) => GetSlice(index, element);

        public override bool Equals(ManagedBuffer<Texture2dArrayDescriptor, Texture2DArray> other)
            => ReferenceEquals(this, other);
    }
}