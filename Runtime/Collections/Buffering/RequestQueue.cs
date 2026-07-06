using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Collections.Buffering
{
    /// <summary>
    /// A generic queue that manages pending Add/Update and Remove requests using custom Iterators.
    /// Automatically resolves conflicts if a key is added and removed in the same cycle.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type (equatable struct).</typeparam>
    /// <typeparam name="TValue">The data payload for updates (preferably a struct).</typeparam>
    public class RequestQueue<TKey, TValue> where TKey : struct, IEquatable<TKey>
    {
        #region Properties

        private readonly HashSet<TKey> m_PendingRemovals = new();
        private readonly Dictionary<TKey, TValue> m_PendingUpdates = new();

        /// <summary>
        /// Returns true if there are any queued changes.
        /// </summary>
        public bool HasRequests => m_PendingRemovals.Count > 0 || m_PendingUpdates.Count > 0;

        /// <summary>
        /// The number of pending removal requests.
        /// </summary>
        public int RemovalCount => m_PendingRemovals.Count;

        /// <summary>
        /// The number of pending update/addition requests.
        /// </summary>
        public int UpdateCount => m_PendingUpdates.Count;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestQueue{TKey, TValue}"/> class.
        /// </summary>
        public RequestQueue() : this(0) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestQueue{TKey, TValue}"/> class 
        /// with an initial capacity for the internal collections.
        /// </summary>
        /// <param name="initialCapacity">The expected number of pending requests.</param>
        public RequestQueue(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Capacity must be non-negative.");

            m_PendingRemovals = new HashSet<TKey>(initialCapacity);
            m_PendingUpdates = new Dictionary<TKey, TValue>(initialCapacity);
        }

        #endregion

        #region Queue Logic

        /// <summary>
        /// Queues an update or addition. If the key was marked for removal, that removal is cancelled.
        /// </summary>
        public void EnqueueUpdate(TKey key, TValue value)
        {
            m_PendingRemovals.Remove(key);
            m_PendingUpdates[key] = value;
        }

        /// <summary>
        /// Queues a removal. If the key had a pending update, that update is cancelled.
        /// </summary>
        public void EnqueueRemoval(TKey key)
        {
            m_PendingUpdates.Remove(key);
            m_PendingRemovals.Add(key);
        }

        /// <summary>
        /// Clears all pending requests.
        /// </summary>
        public void Clear()
        {
            m_PendingRemovals.Clear();
            m_PendingUpdates.Clear();
        }

        #endregion

        #region Iteration

        /// <summary>
        /// Executes an action for each pending removal. 
        /// Using a struct action avoids boxing of the underlying HashSet enumerator.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for keys.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEachRemoval<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>
        {
            foreach (var key in m_PendingRemovals)
            {
                action.Execute(key);
            }
        }

        /// <summary>
        /// Executes an action for each pending update.
        /// Using a struct action avoids boxing of the underlying Dictionary enumerator.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for KeyValuePairs.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEachUpdate<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<KeyValuePair<TKey, TValue>>
        {
            foreach (var kvp in m_PendingUpdates)
            {
                action.Execute(kvp);
            }
        }

        /// <summary>
        /// Provides an iterator over the keys marked for removal.
        /// CAUTION: This causes boxing of the internal enumerator. Use ForEachRemoval for performance.
        /// </summary>
        public IIterator<TKey> GetRemovalIterator()
            => m_PendingRemovals.GetEnumerator().ToIterator();

        /// <summary>
        /// Provides an iterator over the actual update payloads.
        /// CAUTION: This causes boxing of the internal enumerator. Use ForEachUpdate for performance.
        /// </summary>
        public IIterator<KeyValuePair<TKey, TValue>> GetUpdateIterator()
            => m_PendingUpdates.GetEnumerator().ToIterator();

        #endregion
    }
}