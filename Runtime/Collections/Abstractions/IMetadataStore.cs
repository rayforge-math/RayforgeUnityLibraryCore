using Rayforge.Core.Execution.Abstractions;
using System;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Defines a non-generic contract for metadata stores to allow centralized management.
    /// Enables the Registry to perform mass operations (Reset, GPU Sync) without knowing the specific TValue type.
    /// </summary>
    public interface IMetadataStore
    {
        #region General Properties

        /// <summary>
        /// Gets the total number of elements the store can hold.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets the byte size of a single element in the underlying data store.
        /// Directly used as the 'stride' parameter when creating a ComputeBuffer.
        /// </summary>
        int Stride { get; }

        /// <summary>
        /// Gets the number of elements per dirty-tracking segment.
        /// </summary>
        public int BatchSize {  get; }

        /// <summary>
        /// Gets the total number of batches in the buffer.
        /// </summary>
        public int TotalBatchCount { get; }

        /// <summary>
        /// Gets a value indicating whether any data segments have been modified and require synchronization.
        /// </summary>
        bool AnyDirty { get; }

        /// <summary>
        /// Gets the underlying data as a raw Array.
        /// Use this for untyped operations like ComputeBuffer.SetData.
        /// </summary>
        Array RawData { get; }

        #endregion

        #region State Management

        /// <summary>
        /// Resets the store to its initial state, clearing all data and dirty flags.
        /// Essential for full scene reloads or clearing the registry.
        /// </summary>
        void Clear();

        /// <summary>
        /// Clears all dirty segment markers. 
        /// Typically called automatically after a successful GPU synchronization.
        /// </summary>
        void ClearDirty();

        /// <summary>
        /// Marks all segments as dirty, forcing a full synchronization of the entire data set.
        /// Useful for recovering from a lost graphics context or initial buffer filling.
        /// </summary>
        void MarkAllDirty();

        #endregion

        #region Dirty Iteration (Optimized)

        /// <summary>
        /// Executes a specialized action for each dirty segment range.
        /// <para>
        /// RECOMMENDED: This is the fastest way to iterate. By using a struct constraint and ref parameter,
        /// the JIT compiler can inline the action, resulting in zero-allocation, stack-only execution.
        /// </para>
        /// </summary>
        /// <typeparam name="TAction">A struct implementing the <see cref="IExecutionHandler{T}"/> contract.</typeparam>
        /// <param name="action">The action to execute for each dirty segment.</param>
        /// <param name="mergeContiguous">If true, contiguous dirty batches are merged into a single segment.</param>
        void ForEachDirtySegment<TAction>(ref TAction action, bool mergeContiguous = true)
            where TAction : struct, IExecutionHandler<BufferSegmentMeta>;//, allows ref struct;

        /// <summary>
        /// Executes a specialized action for each dirty batch index.
        /// <para>
        /// RECOMMENDED: Optimized for high-frequency calls. Avoids boxing and ensures the iteration
        /// state remains on the stack.
        /// </para>
        /// </summary>
        /// <typeparam name="TAction">A struct implementing the <see cref="IExecutionHandler{T}"/> contract.</typeparam>
        /// <param name="action">The action to execute for each dirty index.</param>
        void ForEachDirtyIndex<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<int>;//, allows ref struct;

        /// <summary>
        /// Provides a specialized iterator over contiguous dirty element ranges.
        /// <para>
        /// CAUTION: Returning the iterator as an interface type causes BOXING of the internal struct state.
        /// Use <see cref="ForEachDirtySegment{TAction}"/> for performance-critical synchronization loops.
        /// </para>
        /// </summary>
        /// <param name="mergeContiguous">If true, contiguous dirty batches are merged into a single segment.</param>
        /// <returns>A boxed iterator instance.</returns>
        IIterator<BufferSegmentMeta> GetDirtySegmentIterator(bool mergeContiguous = true);

        /// <summary>
        /// Returns an iterator over the indices of all segments marked as modified.
        /// <para>
        /// CAUTION: This method causes the internal iterator struct to be BOXED onto the heap.
        /// Use <see cref="ForEachDirtyIndex{TAction}"/> to keep the operation on the stack.
        /// </para>
        /// </summary>
        /// <returns>A boxed iterator instance.</returns>
        IIterator<int> GetDirtySegmentIndices();

        #endregion

        #region Full Iteration (Ignores Dirty State)

        /// <summary>
        /// Executes a specialized action for every batch segment in the store, regardless of its dirty state.
        /// <para>
        /// PERFORMANCE: Zero-allocation, stack-only execution via struct inlining. 
        /// Ideal for full-buffer uploads or validation.
        /// </para>
        /// </summary>
        /// <typeparam name="TAction">A struct implementing <see cref="IExecutionHandler{BufferSegmentMeta}"/>.</typeparam>
        /// <param name="action">The action to execute for each batch segment.</param>
        void ForEachSegment<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<BufferSegmentMeta>;

        /// <summary>
        /// Provides a specialized iterator over all batch segments, ignoring dirty flags.
        /// <para>CAUTION: Causes BOXING of the internal struct state.</para>
        /// </summary>
        /// <returns>A boxed iterator instance.</returns>
        IIterator<BufferSegmentMeta> GetSegmentIterator();

        #endregion
    }
}