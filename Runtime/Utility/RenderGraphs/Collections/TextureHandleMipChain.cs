using System;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using Rayforge.Core.Rendering.Collections;

namespace Rayforge.Core.Utility.RenderGraphs.Collections
{
    /// <summary>
    /// Represents a chain of <see cref="TextureHandle"/>s corresponding to mip levels of a texture
    /// specifically for use in RenderGraph passes. 
    /// 
    /// Unity's standard RenderTexture MipChain can be cumbersome in RenderGraph because:
    /// - Each mip level needs its own <see cref="TextureHandle"/> allocation.
    /// - Copying or generating mips between levels requires explicit pass setup.
    /// - Automatic mip generation via standard RenderTexture is not directly supported in RenderGraph.
    /// 
    /// This structure simplifies the process by:
    /// - Creating all mip levels via a zero-allocation struct handler.
    /// - Allowing optional mip map generation between handles in a RenderGraph-friendly way.
    /// - Providing easy access to individual mip handles and read-only spans for pass binding.
    /// </summary>
    public sealed class TextureHandleMipChain : MipChain<TextureHandle>
    {
        /// <summary>
        /// Initializes an empty texture handle mip chain.
        /// </summary>
        public TextureHandleMipChain() : base()
        {
        }

        /// <summary>
        /// Resets or clears an individual texture handle when the chain is resized or destroyed.
        /// </summary>
        protected override void DestroyHandle(ref TextureHandle handle)
        {
            handle = default;
        }

        /// <summary>
        /// Returns true if all mip handles in the chain are valid.
        /// </summary>
        public bool IsValid()
        {
            foreach (var handle in Handles)
            {
                if (!handle.IsValid())
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns true if the specified mip handle is valid.
        /// </summary>
        /// <param name="mip">Index of the mip level to check.</param>
        /// <returns>True if the handle at the given mip level is valid; otherwise false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mip"/> is negative or greater than the highest mip index.</exception>
        public bool IsValid(int mip)
        {
            if (mip < 0 || mip >= MipCount)
                throw new ArgumentOutOfRangeException(nameof(mip), $"Mip index must be between 0 and {MipCount - 1}.");

            return Handles[mip].IsValid();
        }
    }
}