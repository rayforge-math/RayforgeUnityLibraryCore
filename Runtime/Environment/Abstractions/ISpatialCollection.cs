using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides read-only spatial access to tracked objects.
    /// Allows querying objects based on grid cells.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells (must be an equatable struct).</typeparam>
    public interface ISpatialCollection<TKey> where TKey : struct, IEquatable<TKey>
    {
        #region State Management

        /// <summary>
        /// Checks if a specific cell is currently active.
        /// </summary>
        /// <param name="key">The grid coordinate to check.</param>
        /// <returns>True if the cell contains at least one object.</returns>
        bool IsCellActive(TKey key);

        /// <summary>
        /// Resets the dirty state for all cells in the registry.
        /// </summary>
        void ClearDirtyCells();

        #endregion

        #region High-Performance Access (Zero-Allocation)

        /// <summary>
        /// Iterates over all active/occupied cell keys in the collection using a high-performance struct action.
        /// Avoids heap allocation and boxing.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for the key type.</typeparam>
        /// <param name="action">The action to execute for each active cell key. Passed by reference.</param>
        void ForEachCell<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>;

        /// <summary>
        /// Iterates over all cells marked as dirty (modified) since the last clear using a high-performance struct action.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for the key type.</typeparam>
        /// <param name="action">The action to execute for each dirty cell key. Passed by reference.</param>
        void ForEachDirtyCell<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>;

        #endregion

        #region Flexible Access (Boxing)

        /// <summary>
        /// Provides an iterator of all currently active/occupied grid cells.
        /// CAUTION: This involves boxing of the iterator state. Use ForEachCell for performance-critical loops.
        /// </summary>
        /// <returns>A boxed iterator of all active cell keys.</returns>
        IIterator<TKey> GetCellIterator();

        /// <summary>
        /// Provides an iterator of all grid cells that have been modified (objects added/removed).
        /// CAUTION: This involves boxing. Use ForEachDirtyCell for performance-critical loops.
        /// </summary>
        /// <returns>A boxed iterator of dirty cell keys.</returns>
        IIterator<TKey> GetDirtyCellIterator();

        #endregion
    }
}