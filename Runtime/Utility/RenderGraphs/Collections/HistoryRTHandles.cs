using Rayforge.Core.Common;
using Rayforge.Core.Execution.Abstractions;
using Rayforge.Core.Rendering.Collections;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Core.Utility.RenderGraphs.Collections
{
    /// <summary>
    /// Manages a pair of persistent render targets (history handles) for frame-over-frame operations.
    /// One handle represents the current target (write), the other holds the previous frame's data (read).
    /// Suitable for temporal effects like reprojection, motion blur, or any frame-history dependent process.
    /// </summary>
    public sealed class HistoryRTHandles : HistoryBuffer<RTHandle>, IDisposable
    {
        /// <summary>
        /// Container for data required to allocate or reallocate an RTHandle.
        /// </summary>
        public struct RTAllocData
        {
            public RenderTextureDescriptor descriptor;
            public string name;

            internal RTHandle[] sourceArray;
            internal int index;

            public ref RTHandle Handle => ref sourceArray[index];
        }

        private string[] m_HandleNames;
        private const string k_DefaultHandleName = "HistoryHandle";

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRTHandles"/>.
        /// </summary>
        /// <param name="initial0">Initial first handle (current).</param>
        /// <param name="initial1">Initial second handle (history).</param>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="reAllocFunc"/> is <c>null</c>.</exception>
        public HistoryRTHandles(RTHandle initial0, RTHandle initial1, string handleName = null)
            : base(initial0, initial1)
        {
            m_HandleNames = new string[2];
            for (int i = 0; i < 2; ++i)
            {
                m_HandleNames[i] = string.IsNullOrEmpty(handleName) ? $"{k_DefaultHandleName}_{i}" : $"{handleName}_{i}";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRTHandles"/>.
        /// The handles are initially null; allocation is expected to be done later via <see cref="ReAllocateHandlesIfNeeded"/>.
        /// </summary>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="reAllocFunc"/> is <c>null</c>.</exception>
        public HistoryRTHandles(string handleName)
            : this(null, null, handleName)
        { }

        /// <summary>
        /// Releases all allocated RTHandles and clears the internal collection.
        /// Should be called when the owner (e.g., RenderPass or Feature) is disposed to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            if (m_Resources == null) return;

            for (int i = 0; i < m_Resources.Length; i++)
            {
                ReleaseAtIndex(i);
            }
        }

        /// <summary>
        /// Releases a specific handle by its index and sets the slot to null.
        /// </summary>
        /// <param name="index">The internal index to release.</param>
        private void ReleaseAtIndex(int index)
        {
            if (m_Resources[index] != null)
            {
                m_Resources[index].Release();
                m_Resources[index] = null;
            }
        }

        /// <summary>
        /// Releases the current Target handle and sets its slot to null.
        /// </summary>
        public void DisposeTarget() => ReleaseAtIndex(TargetIndex);

        /// <summary>
        /// Releases the History handle and sets its slot to null.
        /// </summary>
        public void DisposeHistory() => ReleaseAtIndex(HistoryIndex);

        /// <summary>
        /// Internal helper to reallocate a specific slot by its index.
        /// </summary>
        /// <typeparam name="THandler">The struct handler type providing allocation logic.</typeparam>
        /// <param name="index">The internal array index to check (0 or 1).</param>
        /// <param name="descriptor">The render texture descriptor for the allocation check.</param>
        /// <param name="allocator">A reference to the struct handler.</param>
        /// <returns><c>true</c> if the handle was reallocated; otherwise, <c>false</c>.</returns>
        private bool ReAllocateAtIndex<THandler>(int index, RenderTextureDescriptor descriptor, ref THandler allocator)
            where THandler : struct, IFunctionHandler<RTAllocData, bool>
        {
            var allocData = new RTAllocData
            {
                descriptor = descriptor,
                name = m_HandleNames[index],
                sourceArray = m_Resources,
                index = index
            };

            return allocator.Execute(allocData);
        }

        /// <summary>
        /// Reallocates only the current Target handle if needed based on the provided descriptor.
        /// </summary>
        /// <param name="descriptor">The render texture descriptor for the target.</param>
        /// <param name="data">Optional user-defined context for the allocation logic.</param>
        /// <returns><c>true</c> if the target handle was reallocated; otherwise, <c>false</c>.</returns>
        public bool ReAllocateTargetIfNeeded<THandler>(RenderTextureDescriptor descriptor, ref THandler allocator)
            where THandler : struct, IFunctionHandler<RTAllocData, bool>
            => ReAllocateAtIndex(TargetIndex, descriptor, ref allocator);

        /// <summary>
        /// Reallocates only the History handle if needed based on the provided descriptor.
        /// </summary>
        /// <param name="descriptor">The render texture descriptor for the history.</param>
        /// <param name="data">Optional user-defined context for the allocation logic.</param>
        /// <returns><c>true</c> if the history handle was reallocated; otherwise, <c>false</c>.</returns>
        public bool ReAllocateHistoryIfNeeded<THandler>(RenderTextureDescriptor descriptor, ref THandler allocator)
            where THandler : struct, IFunctionHandler<RTAllocData, bool>
            => ReAllocateAtIndex(HistoryIndex, descriptor, ref allocator);

        /// <summary>
        /// Orchestrates the reallocation of both handles and optionally swaps their roles.
        /// </summary>
        /// <param name="descriptor">The render texture descriptor applied to both handles.</param>
        /// <param name="swap">If <c>true</c>, calls <see cref="PingPongBuffer{T}.Swap"/> after checking allocations.</param>
        /// <param name="data">Optional user-defined context passed to the allocation function.</param>
        /// <returns><c>true</c> if at least one of the handles was reallocated; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if descriptor has non-positive dimensions.</exception>
        public bool ReAllocateHandlesIfNeeded<THandler>(RenderTextureDescriptor descriptor, ref THandler allocator, bool swap = false)
            where THandler : struct, IFunctionHandler<RTAllocData, bool>
        {
            if (descriptor.width <= 0 || descriptor.height <= 0)
                throw new ArgumentException("Descriptor must have positive dimensions.", nameof(descriptor));

            bool changed = ReAllocateTargetIfNeeded(descriptor, ref allocator);
            changed |= ReAllocateHistoryIfNeeded(descriptor, ref allocator);

            if (swap) Swap();
            return changed;
        }
    }
}