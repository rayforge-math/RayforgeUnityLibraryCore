using Rayforge.Core.Execution.Abstractions;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Provides read-only access to a centralized metadata registry.
    /// This interface allows external systems (like renderers or UI) 
    /// to query registry state and dispatch GPU updates without being able to 
    /// modify the underlying mapping or data stores.
    /// </summary>
    public interface IMetadataRegistry
    {
        /// <summary>
        /// Gets the total capacity allocated for all metadata stores.
        /// Represents the maximum number of unique keys that can be registered.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets the size of the blocks used for dirty-tracking and GPU uploads.
        /// Metadata is synchronized in chunks of this size to optimize bus bandwidth.
        /// </summary>
        int BatchSize { get; }

        /// <summary>
        /// Gets the current number of active keys tracked by the registry.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the highest slot index currently in use.
        /// This is vital for optimizing GPU compute dispatches 
        /// (e.g., dispatching only enough thread groups to cover active data).
        /// </summary>
        int HighestIndex { get; }

        /// <summary>
        /// Executes a specialized action for each dirty segment range of a specific metadata type.
        /// <para>
        /// RECOMMENDED: This is the fastest way to iterate. By using a struct constraint and ref parameter,
        /// the JIT compiler can inline the action, resulting in zero-allocation, stack-only execution.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The unmanaged metadata type (e.g., SpatialData).</typeparam>
        /// <typeparam name="TAction">A struct implementing the <see cref="IExecutionHandler{BufferSegmentMeta}"/> contract.</typeparam>
        /// <param name="action">The action to execute for each dirty segment.</param>
        /// <param name="mergeContiguous">If true, contiguous dirty batches are merged into a single segment.</param>
        void ForEachDirtySegment<T, TAction>(ref TAction action, bool mergeContiguous = true)
            where T : unmanaged
            where TAction : struct, IExecutionHandler<BufferSegmentMeta>;

        /// <summary>
        /// Executes a specialized action for each dirty batch index of a specific metadata type.
        /// <para>
        /// RECOMMENDED: Optimized for high-frequency calls. Avoids boxing and ensures the iteration
        /// state remains on the stack.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The unmanaged metadata type.</typeparam>
        /// <typeparam name="TAction">A struct implementing the <see cref="IExecutionHandler{int}"/> contract.</typeparam>
        /// <param name="action">The action to execute for each dirty index.</param>
        void ForEachDirtyIndex<T, TAction>(ref TAction action)
            where T : unmanaged
            where TAction : struct, IExecutionHandler<int>;

        /// <summary>
        /// Provides a specialized iterator over contiguous dirty element ranges for a specific metadata type.
        /// <para>
        /// CAUTION: Returning the iterator as an interface type causes BOXING of the internal struct state.
        /// Use <see cref="ForEachDirtySegment{T, TAction}"/> for performance-critical synchronization loops.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The unmanaged metadata type.</typeparam>
        /// <param name="mergeContiguous">If true, contiguous dirty batches are merged into a single segment.</param>
        /// <returns>A boxed iterator instance.</returns>
        IIterator<BufferSegmentMeta> GetDirtySegmentIterator<T>(bool mergeContiguous = true)
            where T : unmanaged;

        /// <summary>
        /// Returns an iterator over the indices of all segments marked as modified for a specific metadata type.
        /// <para>
        /// CAUTION: This method causes the internal iterator struct to be BOXED onto the heap.
        /// Use <see cref="ForEachDirtyIndex{T, TAction}"/> to keep the operation on the stack.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The unmanaged metadata type.</typeparam>
        /// <returns>A boxed iterator instance.</returns>
        IIterator<int> GetDirtySegmentIndices<T>()
            where T : unmanaged;
    }
}