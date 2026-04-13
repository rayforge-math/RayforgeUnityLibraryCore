namespace Rayforge.Core.Execution.Abstractions
{
    /// <summary>
    /// Defines a contract for zero-allocation logic that processes input data and returns a result.
    /// Replaces traditional Func delegates to allow for full compiler inlining.
    /// </summary>
    /// <typeparam name="TData">The type of the input/context data.</typeparam>
    /// <typeparam name="TResult">The type of the resulting object.</typeparam>
    public interface IFunctionHandler<TData, TResult>
    {
        /// <summary>
        /// Executes the logic and returns a result based on the provided data.
        /// </summary>
        /// <param name="data">The input or context data for the operation.</param>
        /// <returns>A result of type <typeparamref name="TResult"/>.</returns>
        TResult Execute(TData data);
    }
}