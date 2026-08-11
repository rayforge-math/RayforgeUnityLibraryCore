using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.Dynamic
{
    /// <summary>
    /// A specialized dynamic array base for texture-based resources.
    /// In: Generic <see cref="Texture"/> for uploads.
    /// Out: <see cref="RenderTexture"/> for usage as a render target or complex sampling.
    /// </summary>
    /// <typeparam name="TDesc">The descriptor type for the specific texture resource.</typeparam>
    /// <typeparam name="TResource">The resource managed by the allocator.</typeparam>
    public abstract class DynamicTextureArray<TDesc, TResource> : DynamicArray<Texture, RenderTexture, TDesc, TResource>, ITextureArray
        where TDesc : unmanaged, IEquatable<TDesc>, IArrayDescriptor, ITextureDescriptor
    {
        protected DynamicTextureArray(IArrayAllocator<TDesc, TResource> allocator, TDesc baseDescriptor)
            : base(allocator, baseDescriptor)
        { }

        #region ITextureArray Implementation

        /// <summary>
        /// Specialized method for updating a slice. 
        /// Maps the generic SetElement to a more descriptive name for textures.
        /// </summary>
        public abstract void SetSlice(int index, Texture source);

        /// <summary>
        /// Specialized method for extracting a slice into a RenderTexture.
        /// Maps the generic CopyElementTo to a more descriptive name.
        /// </summary>
        public abstract void CopySliceToRenderTexture(int index, RenderTexture destination);

        /// <summary>
        /// Gets the horizontal resolution of the texture resource in pixels.
        /// </summary>
        public int Width => Descriptor.Width;

        /// <summary>
        /// Gets the vertical resolution of the texture resource in pixels.
        /// </summary>
        public int Height => Descriptor.Height;

        #endregion
    }
}