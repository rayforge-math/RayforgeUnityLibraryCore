namespace Rayforge.Core.Execution.Abstractions
{
    /// <summary>
    /// Defines a universal contract for zero-allocation execution during iterations.
    /// Designed to be implemented as a <see langword="struct"/> to allow the compiler 
    /// to inline the execution logic and eliminate delegate overhead.
    /// </summary>
    /// <typeparam name="TData">The type of the element being processed.</typeparam>
    public interface IExecutionHandler<TData>
    {
        /// <summary>
        /// Processes a single element with the provided context data.
        /// </summary>
        /// <param name="value">The element to process.</param>
        void Execute(TData value);
    }
}