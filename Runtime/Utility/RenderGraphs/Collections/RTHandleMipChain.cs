using Rayforge.Core.Rendering.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Rayforge.Core.Common;
using System;

namespace Rayforge.Core.Utility.RenderGraphs.Collections
{
    /// <summary>
    /// Represents a chain of <see cref="RTHandle"/>s corresponding to mip levels of a texture
    /// specifically for use in RenderGraph passes. 
    /// 
    /// Unity's standard RenderTexture MipChain can be cumbersome in RenderGraph because:
    /// - Each mip level needs its own <see cref="RTHandle"/> allocation.
    /// - Copying or generating mips between levels requires explicit pass setup.
    /// - Automatic mip generation via standard RenderTexture is not directly supported in RenderGraph.
    /// 
    /// This structure simplifies the process by:
    /// - Creating all mip levels via a user-provided function.
    /// - Allowing optional mip map generation between handles in a RenderGraph-friendly way.
    /// - Providing easy access to individual mip handles and read-only spans for pass binding.
    /// </summary>
    public sealed class RTHandleMipChain : MipChain<RTHandle>, IDisposable
    {
        /// <summary>
        /// Initializes a mip chain with a texture creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        /// <param name="releaseFunc">Function to release a given mip level.</param>
        public RTHandleMipChain(CreateFunction createFunc, ReleaseFunction releaseFunc)
            : base(createFunc, releaseFunc)
        { }

        /// <summary>
        /// Releases all allocated RTHandles and clears the internal collection.
        /// Should be called when the owner (e.g., RenderPass or Feature) is disposed to prevent memory leaks.
        /// </summary>
        public void Dispose()
            => Resize(0);
    }
}