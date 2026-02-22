using Rayforge.Core.Collections.Abstractions;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    public interface ISpatialRegistry<TKey, TValue> : ISpatialCollection<TKey>
        where TValue : Component
    {
        /// <summary>
        /// Retrieves all components of type T within a specific grid cell.
        /// </summary>
        /// <param name="key">The grid coordinate.</param>
        /// <returns>True if there is an iterable collection for the given key. </returns>
        bool TryGetIterator(TKey key, out IIterator<TValue> iter);
    }
}