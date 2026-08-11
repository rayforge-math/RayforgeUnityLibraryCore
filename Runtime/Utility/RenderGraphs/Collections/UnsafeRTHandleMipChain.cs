using Rayforge.Core.Common;
using Rayforge.Core.Rendering.Collections;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Core.Utility.RenderGraphs.Collections
{
    /// <summary>
    /// Represents an "unsafe" variant of <see cref="RTHandleMipChain"/>.
    /// 
    /// This class inherits from <see cref="UnsafeMipChain{THandle}"/> and exposes 
    /// advanced functionality not available in the safe <see cref="RTHandleMipChain"/>:
    /// - Checking ranges of mip handles for validity.
    /// - Copying subsets of chains or stacking multiple chains into one array.
    /// - Explicit control over handle array resizing and layout.
    ///
    /// Use this class only when you need these low-level capabilities and accept responsibility 
    /// for maintaining consistency. For most scenarios, prefer the safe 
    /// <see cref="RTHandleMipChain"/> which provides the same basic functionality 
    /// without exposing unsafe operations.
    ///
    /// Redundant `IsValid` methods are provided for API consistency with the safe variant.
    /// </summary>
    public sealed class UnsafeRTHandleMipChain : UnsafeMipChain<RTHandle>, IDisposable
    {
        /// <summary>
        /// Initializes a mip chain with a texture creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        /// <param name="releaseFunc">Function to release a given mip level.</param>
        public UnsafeRTHandleMipChain(CreateFunction createFunc, ReleaseFunction releaseFunc)
            : base(createFunc, releaseFunc)
        { }

        /// <summary>
        /// Releases all allocated RTHandles and clears the internal collection.
        /// Should be called when the owner (e.g., RenderPass or Feature) is disposed to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            if (m_Handles == null) return;

            foreach (var handle in m_Handles)
            {
                if (handle != null)
                {
                    handle.Release();
                }
            }

            Resize(0);
        }
    }
}