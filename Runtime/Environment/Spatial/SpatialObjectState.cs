using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    [Serializable]
    public struct SpatialObjectState : IEquatable<SpatialObjectState>
    {
        /// <summary>
        /// The bounding box of the object relative to the Registry's Anchor.
        /// English: Using relative coordinates prevents re-bakes after an Origin Shift.
        /// </summary>
        [Header("Relative Spatial Data")]
        public Bounds anchorBounds;

        /// <summary>
        /// The matrix converting local space to Anchor-relative space.
        /// English: This matrix is immune to world-space fluctuations.
        /// </summary>
        public Matrix4x4 localToAnchor;

        [Header("Geometry Data")]
        public Mesh mesh;
        public int subMeshIndex;

        /// <summary>
        /// A hash to detect if the mesh itself has changed (e.g. LOD change).
        /// </summary>
        public int geometryHash;

        /// <summary>
        /// Creates a relative spatial state from world-space data.
        /// English: Neutralizes world coordinates by shifting them into anchor-space.
        /// </summary>
        /// <param name="worldBounds">The current bounds in Unity world space.</param>
        /// <param name="localToWorld">The current localToWorld matrix of the GameObject.</param>
        /// <param name="anchor">The current reference anchor of the registry.</param>
        /// <param name="mesh">The mesh reference for geometry tracking.</param>
        /// <returns>A stable SpatialObjectState relative to the anchor.</returns>
        public static SpatialObjectState Create(
            Bounds worldBounds,
            Matrix4x4 localToWorld,
            Vector3 anchor,
            Mesh mesh)
        {
            Vector3 relativeCenter = worldBounds.center - anchor;
            Bounds anchorBounds = new Bounds(relativeCenter, worldBounds.size);

            // create delta for translation
            Matrix4x4 worldToAnchor = Matrix4x4.Translate(-anchor);
            Matrix4x4 localToAnchor = worldToAnchor * localToWorld;

            return new SpatialObjectState
            {
                anchorBounds = anchorBounds,
                localToAnchor = localToAnchor,
                mesh = mesh,
                subMeshIndex = 0,
                geometryHash = (mesh != null) ? mesh.GetInstanceID() : 0
            };
        }

        /// <summary>
        /// Checks if the object has effectively moved OR changed its geometry.
        /// English: This remains stable even if Unity's world origin shifts.
        /// </summary>
        public bool Equals(SpatialObjectState other)
        {
            return geometryHash == other.geometryHash &&
                   subMeshIndex == other.subMeshIndex &&
                   mesh == other.mesh &&
                   localToAnchor.Equals(other.localToAnchor) &&
                   anchorBounds.Equals(other.anchorBounds);
        }

        public override bool Equals(object obj) => obj is SpatialObjectState other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (mesh != null ? mesh.GetHashCode() : 0);
                hash = hash * 31 + subMeshIndex;
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
