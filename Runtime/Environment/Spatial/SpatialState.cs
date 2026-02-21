using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A generic spatial wrapper that binds a specific component to its anchor-relative transform data.
    /// Decouples the spatial logic from the component type.
    /// </summary>
    /// <typeparam name="T">The type of Component being tracked.</typeparam>
    [Serializable]
    public struct SpatialState<T> : IEquatable<SpatialState<T>> where T : Component
    {
        [Header("Relative Spatial Data")]
        public Bounds anchorBounds;
        public Matrix4x4 localToAnchor;

        [Header("Component Reference")]
        public T component;

        /// <summary>
        /// A hash to detect if the underlying geometry/data has changed.
        /// </summary>
        public int dataHash;

        #region Factory Methods

        /// <summary>
        /// Specialized creator for MeshRenderers.
        /// Maps world-space bounds and matrices to the provided anchor.
        /// </summary>
        public static SpatialState<MeshRenderer> Create(Vector3 anchor, MeshRenderer renderer)
        {
            Matrix4x4 localToWorld = renderer.transform.localToWorldMatrix;
            Bounds worldBounds = renderer.bounds;

            // Calculate relative data
            Bounds anchorBounds = new Bounds(worldBounds.center - anchor, worldBounds.size);
            Matrix4x4 worldToAnchor = Matrix4x4.Translate(-anchor);
            Matrix4x4 localToAnchor = worldToAnchor * localToWorld;

            var filter = renderer.GetComponent<MeshFilter>();
            int gHash = (filter != null && filter.sharedMesh != null) ? filter.sharedMesh.GetInstanceID() : 0;

            return new SpatialState<MeshRenderer>
            {
                anchorBounds = anchorBounds,
                localToAnchor = localToAnchor,
                component = renderer,
                dataHash = gHash
            };
        }

        /// <summary>
        /// Specialized creator for Terrains.
        /// Terrains need different logic for bounds (terrainData.size).
        /// </summary>
        public static SpatialState<Terrain> Create(Vector3 anchor, Terrain terrain)
        {
            Matrix4x4 localToWorld = terrain.transform.localToWorldMatrix;
            Vector3 size = terrain.terrainData.size;
            Bounds worldBounds = new Bounds(terrain.transform.position + size * 0.5f, size);

            Bounds anchorBounds = new Bounds(worldBounds.center - anchor, worldBounds.size);
            Matrix4x4 worldToAnchor = Matrix4x4.Translate(-anchor);
            Matrix4x4 localToAnchor = worldToAnchor * localToWorld;

            return new SpatialState<Terrain>
            {
                anchorBounds = anchorBounds,
                localToAnchor = localToAnchor,
                component = terrain,
                dataHash = terrain.terrainData.GetInstanceID()
            };
        }

        #endregion

        #region Equality

        public bool Equals(SpatialState<T> other)
        {
            return dataHash == other.dataHash &&
                   ReferenceEquals(component, other.component) &&
                   localToAnchor.Equals(other.localToAnchor) &&
                   anchorBounds.Equals(other.anchorBounds);
        }

        public override bool Equals(object obj) => obj is SpatialState<T> other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (component != null ? component.GetHashCode() : 0);
                hash = hash * 31 + dataHash;
                hash = hash * 31 + localToAnchor.GetHashCode();
                hash = hash * 31 + anchorBounds.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(SpatialState<T> left, SpatialState<T> right) => left.Equals(right);
        public static bool operator !=(SpatialState<T> left, SpatialState<T> right) => !left.Equals(right);

        #endregion
    }
}
