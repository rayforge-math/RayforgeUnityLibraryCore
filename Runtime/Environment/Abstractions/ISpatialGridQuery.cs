using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides optimized spatial queries (Bounds, Radius) using zero-allocation handlers and boxed iterators.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells.</typeparam>
    public interface ISpatialGridQuery<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        #region High-Performance Iteration (Zero-Allocation)

        /// <summary> Executes a specialized action for all grid keys that intersect the given world-space bounds. </summary>
        void ForEachKeyInBounds<TAction>(Bounds worldBounds, ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>;

        /// <summary> Executes a specialized action for all grid keys that intersect the given relative bounds. </summary>
        void ForEachKeyInRelativeBounds<TAction>(Bounds relativeBounds, ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>;

        /// <summary> Executes a specialized action for all grid keys within a world-space radius. </summary>
        void ForEachKeyInRadius<TAction>(Vector3 worldCenter, float radius, ref TAction action, bool useEdgeDistance = true)
            where TAction : struct, IExecutionHandler<TKey>;

        /// <summary> Executes a specialized action for all grid keys within a radius relative to the Anchor. </summary>
        void ForEachKeyInRelativeRadius<TAction>(Vector3 relativeCenter, float radius, ref TAction action, bool useEdgeDistance = true)
            where TAction : struct, IExecutionHandler<TKey>;

        #endregion

        #region Flexible Iteration (Boxing)

        /// <summary> Returns an iterator for all grid keys touched by world-space bounds. </summary>
        IIterator<TKey> GetKeysInBounds(Bounds worldBounds);

        /// <summary> Returns an iterator for all grid keys touched by bounds relative to the Anchor. </summary>
        IIterator<TKey> GetKeysInRelativeBounds(Bounds relativeBounds);

        /// <summary> Returns an iterator for all grid keys within a world-space radius. </summary>
        IIterator<TKey> GetKeysInRadius(Vector3 worldCenter, float radius, bool useEdgeDistance = true);

        /// <summary> Returns an iterator for all grid keys within a radius relative to the Anchor. </summary>
        IIterator<TKey> GetKeysInRelativeRadius(Vector3 relativeCenter, float radius, bool useEdgeDistance = true);

        #endregion
    }
}
