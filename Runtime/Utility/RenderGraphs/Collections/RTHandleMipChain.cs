using System;
using UnityEngine;
using UnityEngine.Rendering;
using Rayforge.Core.Rendering.Collections;

namespace Rayforge.Core.Utility.RenderGraphs.Collections
{
    /// <summary>
    /// Represents a chain of <see cref="RTHandle"/>s corresponding to mip levels of a texture
    /// specifically for use in rendering passes and resource management. 
    /// 
    /// Unity's standard RenderTexture MipChain can be cumbersome because:
    /// - Each mip level needs its own <see cref="RTHandle"/> allocation.
    /// - Copying or generating mips between levels requires explicit pass setup.
    /// 
    /// This structure simplifies the process by:
    /// - Creating all mip levels via a zero-allocation struct handler.
    /// - Automatically handling resource release and disposal of RTHandles.
    /// - Providing easy access to individual mip handles and read-only spans.
    /// </summary>
    public sealed class RTHandleMipChain : MipChain<RTHandle>, IDisposable
    {
        /// <summary>
        /// Initializes an empty RTHandle mip chain.
        /// </summary>
        public RTHandleMipChain() : base()
        {
        }

        /// <summary>
        /// Releases or destroys an individual RTHandle safely.
        /// </summary>
        protected override void DestroyHandle(ref RTHandle handle)
        {
            if (handle != null)
            {
                handle.Release();
                handle = null;
            }
        }

        /// <summary>
        /// Returns true if all mip handles in the chain are valid and allocated.
        /// </summary>
        public bool IsValid()
        {
            foreach (var handle in Handles)
            {
                if (handle == null)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns true if the specified mip handle is valid.
        /// </summary>
        /// <param name="mip">Index of the mip level to check.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mip"/> is out of bounds.</exception>
        public bool IsValid(int mip)
        {
            if (mip < 0 || mip >= MipCount)
                throw new ArgumentOutOfRangeException(nameof(mip), $"Mip index must be between 0 and {MipCount - 1}.");

            return Handles[mip] != null;
        }

        /// <summary>
        /// Releases all allocated RTHandles and clears the internal collection.
        /// Should be called when the owner (e.g., RenderPass or Feature) is disposed to prevent memory leaks.
        /// </summary>
        public void Dispose()
            => Resize(0);
    }
}