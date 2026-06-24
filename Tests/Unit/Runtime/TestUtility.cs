namespace Rayforge.Core.TestEnv
{
    public static class TestUtility
    {
        /// <summary>
        /// Generates an array of sample items for common primitive types.
        /// </summary>
        /// <remarks>
        /// Synopsis: Acts as a lightweight data factory for unit tests. It populates 
        /// arrays with predictable, type-specific test data for int, float, string, etc., 
        /// providing a fast setup for iterator-based test scenarios.
        /// </remarks>
        public static T[] CreateSampleItems<T>(int count)
        {
            var items = new T[count];
            var type = typeof(T);

            for (int i = 0; i < count; i++)
            {
                if (type == typeof(int))
                    items[i] = (T)(object)(i + 10);
                else if (type == typeof(float))
                    items[i] = (T)(object)(i + 1.5f);
                else if (type == typeof(string))
                    items[i] = (T)(object)$"Item_{i}";
                else if (type == typeof(bool))
                    items[i] = (T)(object)(i % 2 == 0);
                else if (type == typeof(double))
                    items[i] = (T)(object)(i + 10.5);
                else if (type == typeof(long))
                    items[i] = (T)(object)(i + 100L);
                else
                    items[i] = default(T);
            }
            return items;
        }

        /// <summary>
        /// Retrieves the value of a private field using reflection.
        /// </summary>
        /// <remarks>
        /// Synopsis: Provides read access to private member state for testing purposes.
        /// 
        /// WARNING: This method should be used only in extreme edge cases or unit tests. 
        /// It relies on System.Reflection, which bypasses encapsulation, incurs significant 
        /// performance overhead, prevents JIT-inlining, and is highly brittle regarding 
        /// future refactoring or obfuscation.
        /// </remarks>
        public static object GetPrivateField<T>(T instance, string fieldName)
        {
            var field = typeof(T).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(instance);
        }
    }
}
