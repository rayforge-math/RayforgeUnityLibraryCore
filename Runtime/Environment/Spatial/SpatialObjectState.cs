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
        public MeshRenderer renderer;
        public Terrain terrain;

        /// <summary>
        /// Helps the Baker decide whether to use DrawMesh or a Terrain-specific pass.
        /// </summary>
        public bool IsTerrain => terrain != null;

        /// <summary>
        /// A hash to detect if the mesh itself has changed (e.g. LOD change).
        /// </summary>
        public int geometryHash;

        public static SpatialObjectState Create(Vector3 anchor, Terrain terrain)
        {
            Matrix4x4 localToWorld = terrain.transform.localToWorldMatrix;
            Vector3 size = terrain.terrainData.size;
            Bounds worldBounds = new Bounds(terrain.transform.position + size * 0.5f, size);

            Bounds anchorBounds = new Bounds(worldBounds.center - anchor, worldBounds.size);
            Matrix4x4 worldToAnchor = Matrix4x4.Translate(-anchor);
            Matrix4x4 localToAnchor = worldToAnchor * localToWorld;

            return new SpatialObjectState
            {
                anchorBounds = anchorBounds,
                localToAnchor = localToAnchor,
                renderer = null,
                terrain = terrain,
                geometryHash = terrain.terrainData.GetInstanceID()
            };
        }

        public static SpatialObjectState Create(Vector3 anchor, MeshRenderer renderer)
        {
            Matrix4x4 localToWorld = renderer.transform.localToWorldMatrix;
            Bounds worldBounds = renderer.bounds;

            Bounds anchorBounds = new Bounds(worldBounds.center - anchor, worldBounds.size);
            Matrix4x4 worldToAnchor = Matrix4x4.Translate(-anchor);
            Matrix4x4 localToAnchor = worldToAnchor * localToWorld;

            var filter = renderer.GetComponent<MeshFilter>();
            int gHash = (filter != null && filter.sharedMesh != null) ? filter.sharedMesh.GetInstanceID() : 0;

            return new SpatialObjectState
            {
                anchorBounds = anchorBounds,
                localToAnchor = localToAnchor,
                renderer = renderer,
                terrain = null,
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
