using System;
using UnityEngine;
using UnityEngine.Rendering;
using Rayforge.Core.Rendering.Collections;

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
    /// Redundant <see cref="IsValid()"/> methods are provided for API consistency with the safe variant.
    /// </summary>
    public sealed class UnsafeRTHandleMipChain : UnsafeMipChain<RTHandle>, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnsafeRTHandleMipChain"/>.
        /// </summary>
        public UnsafeRTHandleMipChain() : base()
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
        /// Checks whether all mip handles in the chain are valid.
        /// </summary>
        /// <returns><c>true</c> if all mip handles are valid; otherwise, <c>false</c>.</returns>
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
        /// Checks whether the mip handle at the specified index is valid.
        /// </summary>
        /// <param name="mip">Zero-based index of the mip level to check.</param>
        /// <returns><c>true</c> if the mip handle is valid; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mip"/> is out of range.</exception>
        public bool IsValid(int mip)
        {
            if (mip < 0 || mip >= MipCount)
                throw new ArgumentOutOfRangeException(nameof(mip), $"Mip index must be between 0 and {MipCount - 1}.");
            return Handles[mip] != null;
        }

        /// <summary>
        /// Checks whether all mip handles in the specified range are valid.
        /// </summary>
        /// <param name="startMip">Zero-based index of the first mip level to check.</param>
        /// <param name="count">Number of consecutive mip levels to check starting from <paramref name="startMip"/>.</param>
        /// <returns><c>true</c> if all mip handles in the range are valid; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified range is out of bounds.</exception>
        public bool IsValid(int startMip, int count)
        {
            if (startMip < 0 || startMip >= MipCount)
                throw new ArgumentOutOfRangeException(nameof(startMip), $"Start mip index must be between 0 and {MipCount - 1}.");
            if (count <= 0 || startMip + count > MipCount)
                throw new ArgumentOutOfRangeException(nameof(count), $"Count must be positive and within the range of available handles.");

            for (int i = startMip; i < startMip + count; i++)
            {
                if (!IsValid(i))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Releases all allocated RTHandles and clears the internal collection.
        /// Should be called when the owner (e.g., RenderPass or Feature) is disposed to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            Resize(0);
        }
    }
}