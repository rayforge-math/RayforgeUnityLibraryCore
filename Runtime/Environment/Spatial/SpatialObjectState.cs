using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    [Serializable]
    public struct SpatialObjectState : IEquatable<SpatialObjectState>
    {
        /// <summary>
        /// The bounding box of the object relative to the Registry's Anchor.
        /// Using relative coordinates prevents re-bakes after an Origin Shift.
        /// </summary>
        [Header("Relative Spatial Data")]
        public Bounds anchorBounds;

        /// <summary>
        /// The matrix converting local space to Anchor-relative space.
        /// This matrix is immune to world-space fluctuations.
        /// </summary>
        public Matrix4x4 localToAnchor;

        [Header("Geometry Data")]
        public Renderer renderer;
        public Terrain terrain;

        /// <summary>
        /// Helps the Baker decide whether to use DrawMesh or a Terrain-specific pass.
        /// </summary>
        public bool IsTerrain => terrain != null;

        /// <summary>
        /// A hash to detect if the mesh itself has changed (e.g. LOD change).
        /// </summary>
        public int geometryHash;

        /// <summary>
        /// Creates a relative spatial state from world-space data.
        /// Neutralizes world coordinates by shifting them into anchor-space.
        /// </summary>
        /// <param name="worldBounds">The current bounds in Unity world space.</param>
        /// <param name="localToWorld">The current localToWorld matrix of the GameObject.</param>
        /// <param name="anchor">The current reference anchor of the registry.</param>
        /// <param name="renderer">The mesh reference for geometry tracking.</param>
        /// <returns>A stable SpatialObjectState relative to the anchor.</returns>
        public static SpatialObjectState Create(
            Bounds worldBounds,
            Matrix4x4 localToWorld,
            Vector3 anchor,
            Renderer renderer,
            Terrain terrain = null)
        {
            Vector3 relativeCenter = worldBounds.center - anchor;
            Bounds anchorBounds = new Bounds(relativeCenter, worldBounds.size);

            Matrix4x4 worldToAnchor = Matrix4x4.Translate(-anchor);
            Matrix4x4 localToAnchor = worldToAnchor * localToWorld;

            int gHash = 0;
            if (terrain != null) gHash = terrain.terrainData.GetInstanceID();
            else if (renderer != null) gHash = renderer.gameObject.GetInstanceID();

            return new SpatialObjectState
            {
                anchorBounds = anchorBounds,
                localToAnchor = localToAnchor,
                renderer = renderer,
                terrain = terrain,
                geometryHash = gHash
            };
        }

        /// <summary>
        /// Checks if the object has effectively moved OR changed its geometry.
        /// This remains stable even if Unity's world origin shifts.
        /// </summary>
        public bool Equals(SpatialObjectState other)
        {
            return geometryHash == other.geometryHash &&
                   renderer == other.renderer &&
                   terrain == other.terrain &&
                   localToAnchor.Equals(other.localToAnchor) &&
                   anchorBounds.Equals(other.anchorBounds);
        }

        public override bool Equals(object obj) => obj is SpatialObjectState other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (renderer != null ? renderer.GetHashCode() : 0);
                hash = hash * 31 + (terrain != null ? terrain.GetHashCode() : 0);
                hash = hash * 31 + geometryHash;
                hash = hash * 31 + localToAnchor.GetHashCode();
                hash = hash * 31 + anchorBounds.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(SpatialObjectState left, SpatialObjectState right) => left.Equals(right);
        public static bool operator !=(SpatialObjectState left, SpatialObjectState right) => !left.Equals(right);
    }
}
