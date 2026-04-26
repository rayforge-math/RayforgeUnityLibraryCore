using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Collections.Helpers
{
    public static class IteratorExtensions
    {
        #region Base Engine

        /// <summary>
        /// The core engine that maintains the concrete struct type.
        /// Use this directly to keep the iterator on the stack and allow JIT inlining.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<T, EnumeratorState<T, TEnumerator>> ToIterator<T, TEnumerator>(this TEnumerator enumerator)
            where TEnumerator : struct, IEnumerator<T>
        {
            return new Iterator<T, EnumeratorState<T, TEnumerator>>(new EnumeratorState<T, TEnumerator>(enumerator));
        }

        #endregion

        #region Collection Overloads (List, HashSet, Queue, Stack)

        // --- Array ---
        /// <summary> Returns a high-performance struct iterator for arrays. </summary>
        public static Iterator<T, EnumeratorState<T, ArraySegment<T>.Enumerator>> ToIterator<T>(this T[] array)
            => new ArraySegment<T>(array).GetEnumerator().ToIterator<T, ArraySegment<T>.Enumerator>();

        /// <summary> Returns a boxed interface iterator for arrays. </summary>
        public static IIterator<T> ToIIterator<T>(this T[] array)
            => array.ToIterator();

        // --- List ---
        /// <summary> Returns a high-performance struct iterator for List. </summary>
        public static Iterator<T, EnumeratorState<T, List<T>.Enumerator>> ToIterator<T>(this List<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, List<T>.Enumerator>();

        /// <summary> Returns a boxed interface iterator for List. </summary>
        public static IIterator<T> ToIIterator<T>(this List<T>.Enumerator enumerator)
            => enumerator.ToIterator();

        // --- HashSet ---
        /// <summary> Returns a high-performance struct iterator for HashSet. </summary>
        public static Iterator<T, EnumeratorState<T, HashSet<T>.Enumerator>> ToIterator<T>(this HashSet<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, HashSet<T>.Enumerator>();

        /// <summary> Returns a boxed interface iterator for HashSet. </summary>
        public static IIterator<T> ToIIterator<T>(this HashSet<T>.Enumerator enumerator)
            => enumerator.ToIterator();

        // --- Queue ---
        /// <summary> Returns a high-performance struct iterator for Queue. </summary>
        public static Iterator<T, EnumeratorState<T, Queue<T>.Enumerator>> ToIterator<T>(this Queue<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, Queue<T>.Enumerator>();

        /// <summary> Returns a boxed interface iterator for Queue. </summary>
        public static IIterator<T> ToIIterator<T>(this Queue<T>.Enumerator enumerator)
            => enumerator.ToIterator();

        // --- Stack ---
        /// <summary> Returns a high-performance struct iterator for Stack. </summary>
        public static Iterator<T, EnumeratorState<T, Stack<T>.Enumerator>> ToIterator<T>(this Stack<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, Stack<T>.Enumerator>();

        /// <summary> Returns a boxed interface iterator for Stack. </summary>
        public static IIterator<T> ToIIterator<T>(this Stack<T>.Enumerator enumerator)
            => enumerator.ToIterator();

        #endregion

        #region Dictionary Overloads

        // --- Full Dictionary ---
        /// <summary> Returns a high-performance struct iterator for Dictionary (KeyValuePair). </summary>
        public static Iterator<KeyValuePair<TKey, TValue>, EnumeratorState<KeyValuePair<TKey, TValue>, Dictionary<TKey, TValue>.Enumerator>> ToIterator<TKey, TValue>(this Dictionary<TKey, TValue>.Enumerator enumerator)
            => enumerator.ToIterator<KeyValuePair<TKey, TValue>, Dictionary<TKey, TValue>.Enumerator>();

        /// <summary> Returns a boxed interface iterator for Dictionary. </summary>
        public static IIterator<KeyValuePair<TKey, TValue>> ToIIterator<TKey, TValue>(this Dictionary<TKey, TValue>.Enumerator enumerator)
            => enumerator.ToIterator();

        // --- Key Collection ---
        /// <summary> Returns a high-performance struct iterator for Dictionary Keys. </summary>
        public static Iterator<TKey, EnumeratorState<TKey, Dictionary<TKey, TValue>.KeyCollection.Enumerator>> ToIterator<TKey, TValue>(this Dictionary<TKey, TValue>.KeyCollection.Enumerator enumerator)
            => enumerator.ToIterator<TKey, Dictionary<TKey, TValue>.KeyCollection.Enumerator>();

        /// <summary> Returns a boxed interface iterator for Dictionary Keys. </summary>
        public static IIterator<TKey> ToIIterator<TKey, TValue>(this Dictionary<TKey, TValue>.KeyCollection.Enumerator enumerator)
            => enumerator.ToIterator();

        // --- Value Collection ---
        /// <summary> Returns a high-performance struct iterator for Dictionary Values. </summary>
        public static Iterator<TValue, EnumeratorState<TValue, Dictionary<TKey, TValue>.ValueCollection.Enumerator>> ToIterator<TKey, TValue>(this Dictionary<TKey, TValue>.ValueCollection.Enumerator enumerator)
            => enumerator.ToIterator<TValue, Dictionary<TKey, TValue>.ValueCollection.Enumerator>();

        /// <summary> Returns a boxed interface iterator for Dictionary Values. </summary>
        public static IIterator<TValue> ToIIterator<TKey, TValue>(this Dictionary<TKey, TValue>.ValueCollection.Enumerator enumerator)
            => enumerator.ToIterator();

        #endregion

        #region Specialized & Unity Overloads

        // --- LinkedList ---
        /// <summary> Returns a high-performance struct iterator for LinkedList. </summary>
        public static Iterator<T, EnumeratorState<T, LinkedList<T>.Enumerator>> ToIterator<T>(this LinkedList<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, LinkedList<T>.Enumerator>();

        /// <summary> Returns a boxed interface iterator for LinkedList. </summary>
        public static IIterator<T> ToIIterator<T>(this LinkedList<T>.Enumerator enumerator)
            => enumerator.ToIterator();

        // --- SortedSet ---
        /// <summary> Returns a high-performance struct iterator for SortedSet. </summary>
        public static Iterator<T, EnumeratorState<T, SortedSet<T>.Enumerator>> ToIterator<T>(this SortedSet<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, SortedSet<T>.Enumerator>();

        /// <summary> Returns a boxed interface iterator for SortedSet. </summary>
        public static IIterator<T> ToIIterator<T>(this SortedSet<T>.Enumerator enumerator)
            => enumerator.ToIterator();

        #endregion

        #region Composite & Utility

        /// <summary>
        /// Combines multiple IIterators into a single sequential stream using a high-performance struct wrapper.
        /// NOTE: While the returned Iterator is a struct, the 'params' array and the IIterator sources 
        /// within it are typically heap-allocated (Cold Path).
        /// </summary>
        public static Iterator<T, MultiCompositeState<T>> Combine<T>(params IIterator<T>[] sources)
        {
            if (sources == null || sources.Length == 0)
                return new Iterator<T, MultiCompositeState<T>>(default);

            int validCount = 0;
            int lastValidIndex = -1;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && sources[i] != IIterator<T>.Empty())
                {
                    validCount++;
                    lastValidIndex = i;
                }
            }

            if (validCount == 0) return new Iterator<T, MultiCompositeState<T>>(default);
            return new Iterator<T, MultiCompositeState<T>>(new MultiCompositeState<T>(sources));
        }

        /// <summary>
        /// Combines multiple IIterators into a single sequential stream, returned as a boxed interface.
        /// Useful for passing combined streams to methods that only accept IIterator.
        /// </summary>
        public static IIterator<T> CombineIIterator<T>(params IIterator<T>[] sources)
        {
            return Combine(sources);
        }

        #endregion
    }
}