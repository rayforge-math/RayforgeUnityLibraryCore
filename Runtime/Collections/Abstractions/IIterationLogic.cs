namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Defines the stepping logic for an iterator.
    /// Implementing this as a struct allows the compiler to avoid boxing and heap allocations.
    /// </summary>
    /// <typeparam name="TType">The type of the objects being iterated.</typeparam>
    /// <typeparam name="TState">The custom state struct required to track progress.</typeparam>
    public interface IIterationLogic<TType, TState>
        where TState : struct
    {
        /// <summary>
        /// Checks if a next element is available.
        /// Implementation Note: This method is allowed to advance internal pointers 
        /// to the next valid element to optimize the subsequent MoveNext call.
        /// </summary>
        /// <param name="state">A reference to the tracking state.</param>
        /// <returns>True if more elements are pending; false otherwise.</returns>
        bool HasNext(ref TState state);

        /// <summary>
        /// Returns the next element without consuming it or clearing the eager-fetch cache.
        /// Essential for "Lockstep" synchronization between multiple iterators.
        /// </summary>
        /// <param name="state">A reference to the tracking state.</param>
        /// <param name="result">The next element if found.</param>
        /// <returns>True if a next element exists.</returns>
        bool TryPeekNext(ref TState state, out TType result);

        /// <summary>
        /// Advances the iteration logic using a reference to the provided state.
        /// </summary>
        /// <param name="state">A reference to the tracking state.</param>
        /// <param name="result">The found element if successful.</param>
        /// <returns>True if an element was found; false if the iteration is complete.</returns>
        bool MoveNext(ref TState state, out TType result);
    }
}