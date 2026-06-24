namespace Rayforge.Core.Collections.Abstractions.Tests
{
    /// <summary>
    /// A mock implementation of <see cref="IIterationLogic{T, TState}"/> used for unit testing.
    /// Simulates sequential iteration over an array.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public struct MockLogic<T> : IIterationLogic<T, MockLogic<T>>
    {
        #region Fields

        /// <summary>
        /// The collection of items to iterate over.
        /// </summary>
        public T[] Items;

        /// <summary>
        /// The current iteration index.
        /// </summary>
        public int Index;

        #endregion

        #region IIterationLogic Implementation

        /// <inheritdoc />
        public bool HasNext(ref MockLogic<T> state)
            => state.Items != null && state.Index < state.Items.Length;

        /// <inheritdoc />
        public bool MoveNext(ref MockLogic<T> state, out T result)
        {
            if (state.Items != null && state.Index < state.Items.Length)
            {
                result = state.Items[state.Index];
                state.Index++;
                return true;
            }

            result = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryPeekNext(ref MockLogic<T> state, out T result)
        {
            if (state.Items != null && state.Index < state.Items.Length)
            {
                result = state.Items[state.Index];
                return true;
            }

            result = default;
            return false;
        }

        #endregion
    }
}
