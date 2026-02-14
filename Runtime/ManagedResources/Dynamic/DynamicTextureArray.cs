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
    public abstract class DynamicTextureArray<TDesc, TResource> : DynamicArray<Texture, RenderTexture, TDesc, TResource>
        where TDesc : unmanaged, IEquatable<TDesc>, IArrayDescriptor
    {
        protected DynamicTextureArray(IArrayAllocator<TDesc, TResource> allocator, TDesc baseDescriptor)
            : base(allocator, baseDescriptor)
        { }
    }
}