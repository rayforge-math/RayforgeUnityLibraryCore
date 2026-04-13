using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// A spatial registry that bridges grid coordinates with component data.
    /// Optimized for high-frequency access to entities stored within specific spatial cells 
    /// and tracking modification states.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells.</typeparam>
    /// <typeparam name="TValue">The component type, constrained to Unity's Component base class.</typeparam>
    public interface ISpatialRegistry<TKey, TValue> : ISpatialCollection<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        #region High-Performance Access (Zero-Allocation)

        /// <summary>
        /// Attempts to execute a specialized action for every component stored in the specified grid cell.
        /// This is the most efficient way to process cell content without heap allocation.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for the component type.</typeparam>
        /// <param name="key">The grid coordinate key to query.</param>
        /// <param name="action">The action to execute for each component found. Passed by reference.</param>
        /// <returns>True if the cell exists and was processed (even if empty); false if the cell is untracked.</returns>
        bool TryForEachInCell<TAction>(TKey key, ref TAction action)
            where TAction : struct, IExecutionHandler<TValue>;

        #endregion

        #region Flexible Access (Boxing)

        /// <summary>
        /// Retrieves a flexible iterator for all components within a specific grid cell.
        /// CAUTION: This implementation boxes the internal state. Use TryForEachInCell for performance-critical logic.
        /// </summary>
        /// <param name="key">The grid coordinate key.</param>
        /// <param name="iterator">The resulting boxed IIterator instance.</param>
        /// <returns>True if the cell was found and an iterator was created; false otherwise.</returns>
        bool TryGetEntryIterator(TKey key, out IIterator<TValue> iterator);

        #endregion
    }
}