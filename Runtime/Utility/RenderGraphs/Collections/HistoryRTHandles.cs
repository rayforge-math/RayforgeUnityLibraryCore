using Rayforge.Core.Common;
using Rayforge.Core.Diagnostics;
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
    /// <typeparam name="TData">
    /// Optional user-defined context passed to the allocation function. Useful for providing external resources,
    /// a render graph context, or any other data required during allocation without capturing from the surrounding scope.
    /// </typeparam>
    public class HistoryRTHandles<TData> : HistoryHandles<RTHandle>, IDisposable
    {
        /// <summary>
        /// Function signature for creating or reallocating a texture handle.
        /// </summary>
        /// <param name="handle">The handle to create or reallocate.</param>
        /// <param name="descriptor">The render texture descriptor used for allocation.</param>
        /// <param name="name">Optional name for debugging/profiling.</param>
        /// <param name="data">Optional user-provided context for allocation logic.</param>
        /// <returns><c>true</c> if a handle was allocated/reallocated, <c>false</c> otherwise.</returns>
        public delegate bool TextureReAllocFunction(ref RTHandle handle, RenderTextureDescriptor descriptor, string name, TData data = default);

        private string[] m_HandleNames;
        private TextureReAllocFunction m_ReAllocFunc;

        private const string k_DefaultHandleName = "HistoryHandle";

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRTHandles{TData}"/>.
        /// </summary>
        /// <param name="reAllocFunc">Delegate used to create or reallocate handles.</param>
        /// <param name="initial0">Initial first handle (current).</param>
        /// <param name="initial1">Initial second handle (history).</param>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="reAllocFunc"/> is <c>null</c>.</exception>
        public HistoryRTHandles(TextureReAllocFunction reAllocFunc, RTHandle initial0, RTHandle initial1, string handleName = null)
            : base(initial0, initial1)
        {
            if (reAllocFunc == null)
                throw new ArgumentNullException(nameof(reAllocFunc));

            m_ReAllocFunc = reAllocFunc;

            m_HandleNames = new string[2];
            for (int i = 0; i < 2; ++i)
            {
                m_HandleNames[i] = string.IsNullOrEmpty(handleName) ? $"{k_DefaultHandleName}_{i}" : $"{handleName}_{i}";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRTHandles{TData}"/>.
        /// The handles are initially null; allocation is expected to be done later via <see cref="ReAllocateHandlesIfNeeded"/>.
        /// </summary>
        /// <param name="reAllocFunc">Delegate used to create or reallocate handles.</param>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="reAllocFunc"/> is <c>null</c>.</exception>
        public HistoryRTHandles(TextureReAllocFunction reAllocFunc, string handleName)
            : this(reAllocFunc, null, null, handleName)
        { }

        /// <summary>
        /// Releases all allocated RTHandles and clears the internal collection.
        /// Should be called when the owner (e.g., RenderPass or Feature) is disposed to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            if (m_Handles == null) return;

            for (int i = 0; i < m_Handles.Length; i++)
            {
                ReleaseAtIndex(i);
            }
        }

        /// <summary>
        /// Releases a specific handle by its index and sets the slot to null.
        /// Provides English comments as requested.
        /// </summary>
        /// <param name="index">The internal index to release.</param>
        private void ReleaseAtIndex(int index)
        {
            if (m_Handles[index] != null)
            {
                m_Handles[index].Release();
                m_Handles[index] = null;
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
        /// <param name="index">The internal array index to check (0 or 1).</param>
        /// <param name="descriptor">The render texture descriptor used for the allocation check.</param>
        /// <param name="data">Optional user-defined context passed to the allocation function.</param>
        /// <returns><c>true</c> if the handle at the specified index was reallocated; otherwise, <c>false</c>.</returns>
        private bool ReAllocateAtIndex(int index, RenderTextureDescriptor descriptor, TData data)
        {
            // Capture the current reference from the internal collection
            RTHandle handle = m_Handles[index];

            // Invoke the allocation delegate. 'ref' allows the delegate to replace the handle instance.
            if (m_ReAllocFunc.Invoke(ref handle, descriptor, m_HandleNames[index], data))
            {
                m_Handles[index] = handle;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reallocates only the current Target handle if needed based on the provided descriptor.
        /// </summary>
        /// <param name="descriptor">The render texture descriptor for the target.</param>
        /// <param name="data">Optional user-defined context for the allocation logic.</param>
        /// <returns><c>true</c> if the target handle was reallocated; otherwise, <c>false</c>.</returns>
        public bool ReAllocateTargetIfNeeded(RenderTextureDescriptor descriptor, TData data = default)
            => ReAllocateAtIndex(TargetIndex, descriptor, data);

        /// <summary>
        /// Reallocates only the History handle if needed based on the provided descriptor.
        /// </summary>
        /// <param name="descriptor">The render texture descriptor for the history.</param>
        /// <param name="data">Optional user-defined context for the allocation logic.</param>
        /// <returns><c>true</c> if the history handle was reallocated; otherwise, <c>false</c>.</returns>
        public bool ReAllocateHistoryIfNeeded(RenderTextureDescriptor descriptor, TData data = default)
            => ReAllocateAtIndex(HistoryIndex, descriptor, data);

        /// <summary>
        /// Orchestrates the reallocation of both handles and optionally swaps their roles.
        /// </summary>
        /// <param name="descriptor">The render texture descriptor applied to both handles.</param>
        /// <param name="swap">If <c>true</c>, calls <see cref="PingPongBuffer{T}.Swap"/> after checking allocations.</param>
        /// <param name="data">Optional user-defined context passed to the allocation function.</param>
        /// <returns><c>true</c> if at least one of the handles was reallocated; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if descriptor has non-positive dimensions.</exception>
        public bool ReAllocateHandlesIfNeeded(RenderTextureDescriptor descriptor, bool swap = false, TData data = default)
        {
            if (descriptor.width <= 0 || descriptor.height <= 0)
                throw new ArgumentException("Descriptor must have positive dimensions.", nameof(descriptor));

            bool changed = ReAllocateTargetIfNeeded(descriptor, data);
            changed |= ReAllocateHistoryIfNeeded(descriptor, data);

            if (swap) Swap();
            return changed;
        }
    }

    /// <summary>
    /// Manages a pair of persistent render targets (history handles) for frame-over-frame operations.
    /// One handle represents the current target (write), the other holds the previous frame's data (read).
    /// Suitable for temporal effects like reprojection, motion blur, or any frame-history dependent process.
    /// </summary>
    /// <typeparam name="TData">
    /// Optional user-defined context passed to the allocation function. Useful for providing external resources,
    /// a render graph context, or any other data required during allocation without capturing from the surrounding scope.
    /// </typeparam>
    public class HistoryRTHandles : HistoryRTHandles<NoData>
    {
        /// <summary>
        /// Function signature for creating or reallocating a texture handle.
        /// </summary>
        /// <param name="handle">The handle to create or reallocate.</param>
        /// <param name="descriptor">The render texture descriptor used for allocation.</param>
        /// <param name="name">Optional name for debugging/profiling.</param>
        /// <returns><c>true</c> if a handle was allocated/reallocated, <c>false</c> otherwise.</returns>
        public delegate bool TextureReAllocFunctionNoParam(ref RTHandle handle, RenderTextureDescriptor descriptor, string name);

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRTHandles"/>.
        /// </summary>
        /// <param name="reAllocFunc">Delegate used to create or reallocate handles.</param>
        /// <param name="initial0">Initial first handle (current).</param>
        /// <param name="initial1">Initial second handle (history).</param>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        public HistoryRTHandles(TextureReAllocFunctionNoParam reAllocFunc, RTHandle initial0, RTHandle initial1, string handleName = null)
            : base((ref RTHandle handle, RenderTextureDescriptor descriptor, string name, NoData _) =>
            {
                return reAllocFunc.Invoke(ref handle, descriptor, name);
            },
            initial0, 
            initial1, 
            handleName)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRTHandles"/>.
        /// The handles are initially null; allocation is expected to be done later via <see cref="ReAllocateHandlesIfNeeded"/>.
        /// </summary>
        /// <param name="reAllocFunc">Delegate used to create or reallocate handles.</param>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        public HistoryRTHandles(TextureReAllocFunctionNoParam reAllocFunc, string handleName)
            : this(reAllocFunc, null, null, handleName)
        { }
    }
}