using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Execution.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Components
{
    /// <summary>
    /// A spatial registry that bridges grid coordinates with component data.
    /// Optimized for high-frequency access to entities stored within specific spatial cells 
    /// and tracking modification states.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells.</typeparam>
    /// <typeparam name="TValue">The component type, constrained to Unity's Component base class.</typeparam>
    public interface ISpatialRegistry<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : Component
    {
        #region Lookup & Validation

        /// <summary>
        /// Checks if an object with the specified instance ID is registered.
        /// </summary>
        /// <param name="id">The unique InstanceID.</param>
        /// <returns>True if registered; otherwise false.</returns>
        bool Contains(int id);

        /// <summary>
        /// Attempts to retrieve the spatial state for a registered object.
        /// </summary>
        /// <param name="id">The unique InstanceID.</param>
        /// <param name="state">The resulting component state.</param>
        /// <returns>True if found; otherwise false.</returns>
        bool TryGetState(int id, out ComponentState<TValue> state);

        #endregion

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

        /// <summary> Iterates over all registered instance IDs using an execution handler. </summary>
        void ForEachId<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<int>;

        /// <summary> Iterates over all active spatial cell keys using an execution handler. </summary>
        void ForEachKey<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>;

        /// <summary> Iterates over all instance IDs belonging to a specific cell key using an execution handler. </summary>
        bool TryForEachCellId<TAction>(TKey key, ref TAction action)
            where TAction : struct, IExecutionHandler<int>;

        /// <summary> Iterates over all registered component states using an execution handler. </summary>
        void ForEachState<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<ComponentState<TValue>>;

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
        /// Retrieves a flexible iterator for all components within a specific grid cell.
        /// CAUTION: This implementation boxes the internal state. Use TryForEachInCell for performance-critical logic.
        /// </summary>
        /// <param name="key">The grid coordinate key.</param>
        /// <param name="iterator">The resulting boxed IIterator instance.</param>
        /// <returns>True if the cell was found and an iterator was created; false otherwise.</returns>
        bool TryGetEntryIterator(TKey key, out IIterator<TValue> iterator);

        /// <summary> Gets all registered instance IDs. </summary>
        IIterator<int> AllIds { get; }

        /// <summary> Gets all active spatial cell keys. </summary>
        IIterator<TKey> AllKeys { get; }

        /// <summary> Gets all registered component states. </summary>
        IIterator<ComponentState<TValue>> AllStates { get; }

        /// <summary> Gets an iterator for all instance IDs inside a specific cell. </summary>
        IIterator<int> CellIds(TKey key);

        /// <summary>
        /// Provides an iterator of all grid cells that have been modified (objects added/removed).
        /// CAUTION: This involves boxing. Use ForEachDirtyCell for performance-critical loops.
        /// </summary>
        /// <returns>A boxed iterator of dirty cell keys.</returns>
        IIterator<TKey> GetDirtyCellIterator();

        #endregion
    }
}