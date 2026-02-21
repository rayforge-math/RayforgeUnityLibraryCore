using Rayforge.Core.Collections.Iterator;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// A universal provider for high-performance iteration.
    /// Decouples the data structure from the iteration logic. 
    /// Any class implementing this can be iterated without heap allocations.
    /// </summary>
    /// <typeparam name="TType">The type of elements to provide.</typeparam>
    /// <typeparam name="TState">The internal state struct used for traversal.</typeparam>
    public interface IIterable<TType, TState>
        where TState : struct
    {
        /// <summary>
        /// The core logic required to advance through the collection.
        /// This method is usually passed as a delegate to the Iterator.
        /// </summary>
        bool TryGetNext(ref TState state, out TType result);
    }
}