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
    public abstract class DynamicTextureArray<TDesc, TResource> : DynamicArray<Texture, RenderTexture, TDesc, TResource>, IDynamicTextureArray
        where TDesc : unmanaged, IEquatable<TDesc>, IArrayDescriptor
    {
        protected DynamicTextureArray(IArrayAllocator<TDesc, TResource> allocator, TDesc baseDescriptor)
            : base(allocator, baseDescriptor)
        { }

        #region IDynamicTextureArray Implementation

        /// <summary>
        /// Specialized method for updating a slice. 
        /// Maps the generic SetElement to a more descriptive name for textures.
        /// </summary>
        public virtual void SetSlice(int index, Texture source) => SetElement(index, source);

        /// <summary>
        /// Specialized method for extracting a slice into a RenderTexture.
        /// Maps the generic CopyElementTo to a more descriptive name.
        /// </summary>
        public virtual void CopySliceToRenderTexture(int index, RenderTexture destination)
            => CopyElementTo(index, ref destination);

        /// <summary>
        /// Must be implemented by the concrete class to return the 
        /// actual GPU texture (e.g., Texture2DArray) for shader binding.
        /// </summary>
        public abstract Texture GetBaseResource();

        #endregion
    }
}