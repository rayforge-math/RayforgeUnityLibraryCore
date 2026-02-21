using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Extension of the base iterable logic for spatial queries.
    /// </summary>
    public interface ISpatialIterable<TType, TState> : IIterable<TType, TState>
        where TState : struct
    {
        /// <summary>
        /// Factory method for a specialized spatial iterator.
        /// </summary>
        bool TryGetIterator<TKey, TType>(TKey key, out Iterator<TType, TState> iter);
    }
}