using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A universal snapshot of an object's state within the spatial grid.
    /// Used for heightmaps, visibility, and other world-data pipelines.
    /// </summary>
    [Serializable]
    public struct SpatialObjectState : IEquatable<SpatialObjectState>
    {
        /// <summary>
        /// The axis-aligned bounding box of the object in world space.
        /// </summary>
        [Header("Spatial Data")]
        public Bounds worldBounds;

        /// <summary>
        /// The transformation matrix converting local object space to world space.
        /// </summary>
        public Matrix4x4 localToWorld;

        /// <summary>
        /// Reference to the mesh asset used for rendering or baking.
        /// </summary>
        [Header("Geometry Data")]
        public Mesh mesh;

        /// <summary>
        /// The index of the sub-mesh if the renderer uses multiple materials.
        /// </summary>
        public int subMeshIndex;

        /// <summary>
        /// A hash representing the internal state of the geometry to detect modifications.
        /// </summary>
        public int geometryHash;

        /// <summary>
        /// Compares this state with another to determine if a spatial or geometric update is required.
        /// </summary>
        /// <param name="other">The other state to compare against.</param>
        /// <returns>True if all spatial and geometric properties are identical.</returns>
        public bool Equals(SpatialObjectState other)
        {
            return geometryHash == other.geometryHash &&
                   subMeshIndex == other.subMeshIndex &&
                   mesh == other.mesh &&
                   localToWorld.Equals(other.localToWorld) &&
                   worldBounds.Equals(other.worldBounds);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current state.
        /// </summary>
        public override bool Equals(object obj) => obj is SpatialObjectState other && Equals(other);

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current state.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (mesh != null ? mesh.GetHashCode() : 0);
                hash = hash * 31 + subMeshIndex;
                hash = hash * 31 + geometryHash;
                hash = hash * 31 + localToWorld.GetHashCode();
                hash = hash * 31 + worldBounds.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(SpatialObjectState left, SpatialObjectState right) => left.Equals(right);

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(SpatialObjectState left, SpatialObjectState right) => !left.Equals(right);
    }
}
