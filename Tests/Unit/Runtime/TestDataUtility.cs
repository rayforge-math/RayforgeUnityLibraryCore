using UnityEngine;

namespace Rayforge.Core.Tests
{
    public static class TestDataUtility
    {
        public static T[] CreateSampleItems<T>(int count)
        {
            var items = new T[count];
            var type = typeof(T);

            for (int i = 0; i < count; i++)
            {
                if (type == typeof(int))
                {
                    items[i] = (T)(object)(i + 10);
                }
                else if (type == typeof(float))
                {
                    items[i] = (T)(object)(i + 1.5f);
                }
                else if (type == typeof(string))
                {
                    items[i] = (T)(object)$"Item_{i}";
                }
                else if (type == typeof(bool))
                {
                    items[i] = (T)(object)(i % 2 == 0);
                }
                else if (type == typeof(double))
                {
                    items[i] = (T)(object)(i + 10.5);
                }
                else if (type == typeof(long))
                {
                    items[i] = (T)(object)(i + 100L);
                }
                else
                {
                    items[i] = default(T);
                }
            }
            return items;
        }
    }
}
