namespace Rayforge.Core.Collections.Abstractions.Tests
{
    /// <summary>
    /// A data container used to hold test scenarios for <see cref="IIterationLogic{T, TLogic}"/>.
    /// Bundles the logic state with the expected output values.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TLogic">The specific iteration logic struct.</typeparam>
    public struct IterationTestData<T, TLogic>
        where TLogic : struct, IIterationLogic<T, TLogic>
    {
        #region Fields

        /// <summary>
        /// The instance of the iteration logic state.
        /// </summary>
        public TLogic logic;

        /// <summary>
        /// The array of expected values that the iterator should produce.
        /// </summary>
        public T[] expected;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="IterationTestData{T, TLogic}"/> struct.
        /// </summary>
        /// <param name="logic">The logic state.</param>
        /// <param name="expected">The expected output collection.</param>
        public IterationTestData(TLogic logic, T[] expected)
        {
            this.logic = logic;
            this.expected = expected;
        }

        #endregion
    }
}
