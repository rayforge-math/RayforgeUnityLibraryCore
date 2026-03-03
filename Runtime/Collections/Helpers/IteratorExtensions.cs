using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Helpers
{
    public static class IteratorExtensions
    {
        #region Base Engine

        /// <summary>
        /// The core engine that maintains the concrete struct type.
        /// Use this directly if you want to avoid boxing entirely.
        /// </summary>
        public static Iterator<T, EnumeratorState<T, TEnumerator>> ToIterator<T, TEnumerator>(this TEnumerator enumerator)
            where TEnumerator : struct, IEnumerator<T>
        {
            return new Iterator<T, EnumeratorState<T, TEnumerator>>(new EnumeratorState<T, TEnumerator>(enumerator));
        }

        #endregion

        #region Collection Overloads (Boxing to IIterator)

        /// <summary> Overload for List Enumerators. </summary>
        public static IIterator<T> ToIterator<T>(this List<T>.Enumerator enumerator)
        {
            return enumerator.ToIterator<T, List<T>.Enumerator>();
        }

        /// <summary> Overload for HashSet Enumerators. </summary>
        public static IIterator<T> ToIterator<T>(this HashSet<T>.Enumerator enumerator)
        {
            return enumerator.ToIterator<T, HashSet<T>.Enumerator>();
        }

        /// <summary> Overload for Queue Enumerators. </summary>
        public static IIterator<T> ToIterator<T>(this Queue<T>.Enumerator enumerator)
        {
            return enumerator.ToIterator<T, Queue<T>.Enumerator>();
        }

        /// <summary> Overload for Stack Enumerators. </summary>
        public static IIterator<T> ToIterator<T>(this Stack<T>.Enumerator enumerator)
        {
            return enumerator.ToIterator<T, Stack<T>.Enumerator>();
        }

        #endregion

        #region Dictionary Overloads

        /// <summary> Overload for full Dictionary Enumerators (KeyValuePair). </summary>
        public static IIterator<KeyValuePair<TKey, TValue>> ToIterator<TKey, TValue>(this Dictionary<TKey, TValue>.Enumerator enumerator)
        {
            return enumerator.ToIterator<KeyValuePair<TKey, TValue>, Dictionary<TKey, TValue>.Enumerator>();
        }

        /// <summary> Overload for Dictionary Key Enumerators. </summary>
        public static IIterator<TKey> ToIterator<TKey, TValue>(this Dictionary<TKey, TValue>.KeyCollection.Enumerator enumerator)
        {
            return enumerator.ToIterator<TKey, Dictionary<TKey, TValue>.KeyCollection.Enumerator>();
        }

        /// <summary> Overload for Dictionary Value Enumerators. </summary>
        public static IIterator<TValue> ToIterator<TKey, TValue>(this Dictionary<TKey, TValue>.ValueCollection.Enumerator enumerator)
        {
            return enumerator.ToIterator<TValue, Dictionary<TKey, TValue>.ValueCollection.Enumerator>();
        }

        #endregion

        #region Specialized & Unity Overloads

        /// <summary> 
        /// Overload for LinkedList Enumerators. 
        /// Useful for pools or queues where items are frequently added/removed.
        /// </summary>
        public static IIterator<T> ToIterator<T>(this LinkedList<T>.Enumerator enumerator)
        {
            return enumerator.ToIterator<T, LinkedList<T>.Enumerator>();
        }

        /// <summary>
        /// Overload for SortedSet Enumerators.
        /// </summary>
        public static IIterator<T> ToIterator<T>(this SortedSet<T>.Enumerator enumerator)
        {
            return enumerator.ToIterator<T, SortedSet<T>.Enumerator>();
        }

        #endregion

        #region Composite & Utility

        /// <summary>
        /// Combines multiple IIterators into a single sequential stream.
        /// This allows merging data from different registries (e.g., Mesh + Terrain) 
        /// without allocating temporary lists.
        /// </summary>
        public static IIterator<T> Combine<T>(params IIterator<T>[] sources)
        {
            if (sources == null || sources.Length == 0)
                return IIterator<T>.Empty;

            int validCount = 0;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && sources[i] != IIterator<T>.Empty())
                    validCount++;
            }

            if (validCount == 0) return IIterator<T>.Empty();
            if (validCount == 1)
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != null && sources[i] != IIterator<T>.Empty()) 
                        return sources[i];
                }
            }

            return new Iterator<T, MultiCompositeState<T>>(new MultiCompositeState<T>(sources));
        }

        #endregion
    }
}