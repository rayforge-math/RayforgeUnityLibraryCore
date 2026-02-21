using Rayforge.Core.Collections.Abstractions;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides read-only spatial access to tracked objects.
    /// Allows querying objects based on grid cells and tracking modification states.
    /// </summary>
    public interface ISpatialCollection
    {
        /// <summary>
        /// Checks if the collection is initialized and ready for spatial operations.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Retrieves all components of type T within a specific grid cell.
        /// </summary>
        /// <typeparam name="T">The type of Component to look for.</typeparam>
        /// <param name="key">The grid coordinate.</param>
        bool TryGetIterator<T>(Vector3Int key, out IIterator<T> iter) where T : Component;

        /// <summary>
        /// Checks if a specific cell contains any registered entries.
        /// </summary>
        /// <param name="key">The grid coordinate to check.</param>
        /// <returns>True if the cell contains at least one object.</returns>
        bool HasEntriesInCell(Vector3Int key);

        /// <summary>
        /// Provides an enumerable of all grid cells that have been modified (objects added/removed).
        /// </summary>
        /// <returns>An enumerable of dirty cell keys.</returns>
        IIterator<Vector3Int> GetDirtyCells();
    }
}