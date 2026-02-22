using Rayforge.Core.Collections.Abstractions;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides read-only spatial access to tracked objects.
    /// Allows querying objects based on grid cells and tracking modification states.
    /// </summary>
    public interface ISpatialCollection<TKey>
    {
        /// <summary>
        /// Checks if the collection is initialized and ready for spatial operations.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Checks if a specific cell contains any registered entries.
        /// </summary>
        /// <param name="key">The grid coordinate to check.</param>
        /// <returns>True if the cell contains at least one object.</returns>
        bool HasEntriesInCell(TKey key);

        /// <summary>
        /// Provides an enumerable of all grid cells that have been modified (objects added/removed).
        /// </summary>
        /// <returns>An enumerable of dirty cell keys.</returns>
        IIterator<TKey> GetDirtyCells();
    }
}