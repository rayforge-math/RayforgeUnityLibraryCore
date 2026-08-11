using Rayforge.Core.Execution.Abstractions;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Defines a contract for collections that support both boxed iteration and high-performance,
    /// stack-only execution via execution handlers.
    /// </summary>
    /// <typeparam name="T">The data type being iterated.</typeparam>
    public interface IIterable<T>
    {
        /// <summary>
        /// Provides an iterator over the elements of the collection.
        /// <para>
        /// CAUTION: This method typically causes boxing of the internal iterator struct 
        /// if the returned type is an interface. Use <see cref="ForEach{TAction}"/> 
        /// for performance-critical loops to keep execution on the stack.
        /// </para>
        /// </summary>
        /// <returns>A boxed iterator instance.</returns>
        IIterator<T> GetIterator();

        /// <summary>
        /// Executes a specialized action for every element in the collection.
        /// <para>
        /// PERFORMANCE: Zero-allocation, stack-only execution via struct inlining. 
        /// The JIT compiler can inline the <paramref name="action"/>, making this the 
        /// preferred method for performance-sensitive synchronization or processing tasks.
        /// </para>
        /// </summary>
        /// <typeparam name="TAction">A struct implementing the <see cref="IExecutionHandler{T}"/> contract.</typeparam>
        /// <param name="action">The action to execute for each element.</param>
        void ForEach<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<T>;
    }
}
