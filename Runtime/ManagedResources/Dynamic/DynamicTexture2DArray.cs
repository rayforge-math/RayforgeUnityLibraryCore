using Rayforge.Core.ManagedResources.NativeMemory;
using Rayforge.Core.ManagedResources.Pooling;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.Dynamic
{
    /// <summary>
    /// A specialized implementation of <see cref="DynamicTextureArray{TDesc, TResource}"/> 
    /// managing a single contiguous <see cref="Texture2DArray"/>.
    /// Optimized for hardware supporting texture arrays, providing efficient shader access.
    /// In: <see cref="Texture"/> (Upload) | Out: <see cref="RenderTexture"/> (Slice Copy/View).
    /// </summary>
    public sealed class DynamicTexture2DArray : DynamicTextureArray<Texture2dArrayDescriptor, ManagedTexture2DArray>
    {
        /// <summary>
        /// Gets the total number of slices currently allocated in the texture array.
        /// </summary>
        public override int Count => InternalArray?.Count ?? 0;

        /// <summary>
        /// Initializes the dynamic texture array with a pooled allocation strategy.
        /// Uses a contiguous buffer approach via <see cref="PooledArrayAllocator{TDesc, TBuffer}"/>.
        /// </summary>
        /// <param name="pool">The pool used for renting the managed texture array.</param>
        /// <param name="baseDescriptor">Template descriptor for texture properties (size, format).</param>
        public DynamicTexture2DArray(
            BatchedLeasedBufferPool<Texture2dArrayDescriptor, ManagedTexture2DArray> pool,
            Texture2dArrayDescriptor baseDescriptor)
            : base(new PooledArrayAllocator<Texture2dArrayDescriptor, ManagedTexture2DArray>(pool), baseDescriptor)
        { }

        #region IDynamicTextureArray Implementation

        /// <summary>
        /// Updates a specific layer (slice) using a source texture.
        /// This typically triggers a GPU upload via the underlying managed buffer.
        /// </summary>
        public override void SetSlice(int index, Texture source)
        {
            if (InternalArray == null || source == null) return;
            InternalArray.SetSlice(index, source);
        }

        /// <summary>
        /// Extracts the content of a specific slice into a destination RenderTexture.
        /// </summary>
        public override void CopySliceToRenderTexture(int index, RenderTexture destination)
        {
            if (InternalArray == null || destination == null) return;
            InternalArray.GetSlice(index, destination);
        }

        #endregion

        #region DynamicArray Implementation

        /// <summary>
        /// Standard implementation of the element setter.
        /// </summary>
        public override void Set(int index, Texture element) => SetSlice(index, element);

        /// <summary>
        /// Implementation of the generic element getter. 
        /// Copies the specified array slice into the provided RenderTexture reference.
        /// </summary>
        /// <param name="index">The source slice index.</param>
        /// <param name="element">The destination RenderTexture.</param>
        public override void Get(int index, ref RenderTexture element)
            => CopySliceToRenderTexture(index, element);

        #endregion
    }
}