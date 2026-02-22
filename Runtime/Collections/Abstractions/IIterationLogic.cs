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
        /// Advances the iteration logic using a reference to the provided state.
        /// </summary>
        /// <param name="state">A reference to the tracking state.</param>
        /// <param name="result">The found element if successful.</param>
        /// <returns>True if an element was found; false if the iteration is complete.</returns>
        bool MoveNext(ref TState state, out TType result);
    }
}