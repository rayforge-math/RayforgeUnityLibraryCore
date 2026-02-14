using Rayforge.Core.ManagedResources.Pooling;
using Rayforge.Core.ManagedResources.NativeMemory;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.Dynamic
{
    /// <summary>
    /// A specialized dynamic controller for <see cref="Texture2DArray"/> resources.
    /// Integrates with <see cref="DynamicArray{TElement, TDesc, TBuffer}"/> to provide
    /// automated pooling, batching, and GPU-side resizing.
    /// </summary>
    public sealed class DynamicTexture2DArray : DynamicArray<Texture, Texture2dArrayDescriptor, ManagedTexture2DArray>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicTexture2DArray"/> class
        /// linked to a specific batched texture pool.
        /// </summary>
        /// <param name="pool">The pool used for managing texture array leases.</param>
        /// <param name="baseSettings">The descriptor defining texture dimensions and format.</param>
        public DynamicTexture2DArray(
            BatchedLeasedBufferPool<Texture2dArrayDescriptor, ManagedTexture2DArray> pool,
            Texture2dArrayDescriptor baseSettings)
            : base(pool, baseSettings)
        { }

        /// <summary>
        /// Updates a specific slice in the array using a <see cref="Texture2D"/> source.
        /// Redirects to the base <see cref="SetElement"/> implementation.
        /// </summary>
        /// <param name="index">The target slice index.</param>
        /// <param name="source">The source texture to upload.</param>
        public void SetSlice(int index, Texture2D source) => SetElement(index, source);

        /// <summary>
        /// Extracts a specific slice from the current GPU array and copies it into a <see cref="RenderTexture"/>.
        /// Utilizes optimized GPU-side blitting via the underlying managed resource.
        /// </summary>
        /// <param name="index">The source slice index within the array.</param>
        /// <param name="destination">The target RenderTexture to receive the data.</param>
        /// <remarks>
        /// This operation uses Graphics.Blit internally and is more efficient than manual pixel reading.
        /// </remarks>
        public void CopyToRenderTexture(int index, RenderTexture destination)
        {
            var array = InternalArray;
            if (array != null)
            {
                array.GetSlice(index, destination);
            }
        }
    }
}