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
        /// <remarks>
        /// Note: While this generic method catch-all handles any struct-based IEnumerator, 
        /// the explicit overloads below are provided primarily for:
        /// 1. Better IDE IntelliSense discovery (showing exact return types).
        /// 2. Guiding the compiler to the most efficient specific implementation (e.g. ArraySegments).
        /// 3. Serving as a 'safety net' for users who aren't aware of the generic constraints.
        /// </remarks>
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<T, EnumeratorState<T, ArraySegment<T>.Enumerator>> ToIterator<T>(this T[] array)
            => new ArraySegment<T>(array).GetEnumerator().ToIterator<T, ArraySegment<T>.Enumerator>();

        // --- List ---
        /// <summary> Returns a high-performance struct iterator for List. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<T, EnumeratorState<T, List<T>.Enumerator>> ToIterator<T>(this List<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, List<T>.Enumerator>();

        // --- HashSet ---
        /// <summary> Returns a high-performance struct iterator for HashSet. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<T, EnumeratorState<T, HashSet<T>.Enumerator>> ToIterator<T>(this HashSet<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, HashSet<T>.Enumerator>();

        // --- Queue ---
        /// <summary> Returns a high-performance struct iterator for Queue. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<T, EnumeratorState<T, Queue<T>.Enumerator>> ToIterator<T>(this Queue<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, Queue<T>.Enumerator>();

        // --- Stack ---
        /// <summary> Returns a high-performance struct iterator for Stack. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<T, EnumeratorState<T, Stack<T>.Enumerator>> ToIterator<T>(this Stack<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, Stack<T>.Enumerator>();

        #endregion

        #region Dictionary Overloads

        // --- Full Dictionary ---
        /// <summary> Returns a high-performance struct iterator for Dictionary (KeyValuePair). </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<KeyValuePair<TKey, TValue>, EnumeratorState<KeyValuePair<TKey, TValue>, Dictionary<TKey, TValue>.Enumerator>> ToIterator<TKey, TValue>(this Dictionary<TKey, TValue>.Enumerator enumerator)
            => enumerator.ToIterator<KeyValuePair<TKey, TValue>, Dictionary<TKey, TValue>.Enumerator>();

        // --- Key Collection ---
        /// <summary> Returns a high-performance struct iterator for Dictionary Keys. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<TKey, EnumeratorState<TKey, Dictionary<TKey, TValue>.KeyCollection.Enumerator>> ToIterator<TKey, TValue>(this Dictionary<TKey, TValue>.KeyCollection.Enumerator enumerator)
            => enumerator.ToIterator<TKey, Dictionary<TKey, TValue>.KeyCollection.Enumerator>();

        // --- Value Collection ---
        /// <summary> Returns a high-performance struct iterator for Dictionary Values. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<TValue, EnumeratorState<TValue, Dictionary<TKey, TValue>.ValueCollection.Enumerator>> ToIterator<TKey, TValue>(this Dictionary<TKey, TValue>.ValueCollection.Enumerator enumerator)
            => enumerator.ToIterator<TValue, Dictionary<TKey, TValue>.ValueCollection.Enumerator>();

        #endregion

        #region Specialized & Unity Overloads

        // --- LinkedList ---
        /// <summary> Returns a high-performance struct iterator for LinkedList. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<T, EnumeratorState<T, LinkedList<T>.Enumerator>> ToIterator<T>(this LinkedList<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, LinkedList<T>.Enumerator>();

        // --- SortedSet ---
        /// <summary> Returns a high-performance struct iterator for SortedSet. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<T, EnumeratorState<T, SortedSet<T>.Enumerator>> ToIterator<T>(this SortedSet<T>.Enumerator enumerator)
            => enumerator.ToIterator<T, SortedSet<T>.Enumerator>();

        #endregion

        #region Composite & Utility

        /// <summary>
        /// Combines multiple IIterators into a single sequential stream using a high-performance struct wrapper.
        /// NOTE: While the returned Iterator is a struct, the 'params' array and the IIterator sources 
        /// within it are typically heap-allocated (Cold Path).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Iterator<T, MultiCompositeState<T>> Combine<T>(params IIterator<T>[] sources)
            => new Iterator<T, MultiCompositeState<T>>(new MultiCompositeState<T>(sources));

        #endregion
    }
}