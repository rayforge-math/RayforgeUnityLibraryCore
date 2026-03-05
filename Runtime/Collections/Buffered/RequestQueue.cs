using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using System;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Buffered
{
    /// <summary>
    /// A generic queue that manages pending Add/Update and Remove requests using custom Iterators.
    /// Automatically resolves conflicts if a key is added and removed in the same cycle.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type (equatable struct).</typeparam>
    /// <typeparam name="TValue">The data payload for updates (preferably a struct).</typeparam>
    public class RequestQueue<TKey, TValue> where TKey : struct, IEquatable<TKey>
    {
        private readonly HashSet<TKey> m_PendingRemovals = new();
        private readonly Dictionary<TKey, TValue> m_PendingUpdates = new();

        /// <summary>
        /// Returns true if there are any queued changes.
        /// </summary>
        public bool HasRequests => m_PendingRemovals.Count > 0 || m_PendingUpdates.Count > 0;

        /// <summary>
        /// Provides an iterator over the keys marked for removal.
        /// </summary>
        public IIterator<TKey> GetRemovalIterator()
            => m_PendingRemovals.GetEnumerator().ToIterator();

        /// <summary>
        /// Provides an iterator over the actual update payloads.
        /// </summary>
        public IIterator<KeyValuePair<TKey, TValue>> GetUpdateIterator()
            => m_PendingUpdates.GetEnumerator().ToIterator();

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

        /// <summary>
        /// The number of pending removal requests.
        /// </summary>
        public int RemovalCount => m_PendingRemovals.Count;

        /// <summary>
        /// The number of pending update/addition requests.
        /// </summary>
        public int UpdateCount => m_PendingUpdates.Count;
    }
}