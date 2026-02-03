using Rayforge.Core.Environment.Spatial.Helpers;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    using UnityEngine;

    /// <summary>
    /// A high-performance, barebones base class for 3D world chunks. 
    /// Focuses on spatial data and state. Registration logic is handled externally.
    /// </summary>
    /// <typeparam name="T">The derived type for type-safe processing.</typeparam>
    public abstract class WorldChunk3D<T> : MonoBehaviour
        where T : WorldChunk3D<T>
    {
        #region Spatial Settings
        [Header("Spatial Settings")]
        /// <summary> 
        /// The half-size of the chunk in local space. 
        /// Protected setter allows derived classes (like WorldChunk2D) to sync their area settings.
        /// </summary>
        [field: SerializeField]
        public Vector3 localExtent { get; protected set; } = new Vector3(50, 50, 50);

        /// <summary> If true, the chunk flags itself as dirty when the transform moves. </summary>
        [Tooltip("Automatically detect transform changes via transform.hasChanged.")]
        public bool updateOnTransformChange = true;
        #endregion

        #region Identity & GPU Data
        [Header("Identity & GPU")]
        /// <summary> 
        /// Generic ID for GPU buffers, managed by an external registry or pool. 
        /// Internal setter: Only the Registry or the Chunk Manager should assign this ID.
        /// </summary>
        [field: SerializeField, HideInInspector]
        public int gpuIdentifier { get; internal set; } = -1;

        /// <summary> 
        /// Stores the last known grid key. 
        /// Essential for the Registry to identify this chunk's slot in the dictionary.
        /// Internal setter: Managed by the Registry during the (Un)Register process.
        /// </summary>
        [field: SerializeField, HideInInspector]
        public Vector3Int currentGridKey { get; internal set; }

        /// <summary> Custom dirty flag for internal data changes (e.g., heightmap updates). </summary>
        protected bool _isDirty = true;
        #endregion

        #region Grid Accessors
        // These methods utilize SpatialUtils. 
        // They require an anchor to ensure stable keys during Floating Origin shifts.

        /// <summary> Returns the 3D Grid Key using the global SpatialUtils and the registry anchor. </summary>
        public Vector3Int GetGridKey3D(float gridSize, Vector3 anchor)
            => SpatialUtils.PositionToKey3D(transform.position, gridSize, anchor);

        /// <summary> Returns the 2D Grid Key (XZ) using the global SpatialUtils and the registry anchor. </summary>
        public Vector2Int GetGridKey2D(float gridSize, Vector3 anchor)
            => SpatialUtils.PositionToKey2D(transform.position, gridSize, anchor);

        #endregion

        #region Spatial Logic

        /// <summary> Calculates the AABB in World Space based on current position and local extent. </summary>
        public virtual Bounds GetWorldBounds()
        {
            return new Bounds(transform.position, localExtent * 2f);
        }

        /// <summary> Calculates a bounding radius for fast distance-based culling. </summary>
        public float GetBoundingRadius() => localExtent.magnitude;

        #endregion

        #region Distance Calculations

        /// <summary> 
        /// Calculates the squared distance to the chunk's center. 
        /// Fast and ideal for sorting or basic LOD checks.
        /// </summary>
        /// <param name="worldPos">The reference position (e.g., Camera).</param>
        public float GetSqrDistanceTo(Vector3 worldPos)
        {
            Vector3 diff = transform.position - worldPos;
            return diff.sqrMagnitude;
        }

        /// <summary> 
        /// Calculates the actual distance to the chunk's center. 
        /// Note: Uses Mathf.Sqrt, use GetSqrDistanceTo where possible for performance.
        /// </summary>
        public float GetDistanceTo(Vector3 worldPos)
        {
            return Vector3.Distance(transform.position, worldPos);
        }

        /// <summary>
        /// Calculates the closest squared distance from a point to the chunk's AABB.
        /// Returns 0 if the point is inside the chunk.
        /// This is the most precise method for culling and baking prioritization.
        /// </summary>
        /// <param name="worldPos">The reference position.</param>
        public float GetSqrDistanceToClosestEdge(Vector3 worldPos)
        {
            Bounds b = GetWorldBounds();

            // Calculate the distance to the box on each axis.
            float dx = Mathf.Max(0, b.min.x - worldPos.x, worldPos.x - b.max.x);
            float dy = Mathf.Max(0, b.min.y - worldPos.y, worldPos.y - b.max.y);
            float dz = Mathf.Max(0, b.min.z - worldPos.z, worldPos.z - b.max.z);

            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// Calculates the closest actual distance to the chunk's AABB.
        /// </summary>
        public float GetDistanceToClosestEdge(Vector3 worldPos)
        {
            return Mathf.Sqrt(GetSqrDistanceToClosestEdge(worldPos));
        }

        #endregion

        #region Dirty State Management

        /// <summary> Manually marks the chunk as dirty (e.g. after data modifications). </summary>
        public void MarkDirty() => _isDirty = true;

        /// <summary>
        /// Checks if the chunk needs a refresh. 
        /// Combines the manual dirty flag with Unity's transform tracking.
        /// </summary>
        public virtual bool IsDirty()
        {
            // If gpuIdentifier is -1, the chunk is considered dirty because it hasn't synced yet.
            return _isDirty || gpuIdentifier == -1 || (updateOnTransformChange && transform.hasChanged);
        }

        /// <summary>
        /// Resets the dirty state. Should be called by the manager after successful sync.
        /// </summary>
        public virtual void ClearDirty()
        {
            _isDirty = false;
            // Crucial to reset hasChanged to prevent infinite dirty-loops after a move/shift.
            if (transform != null) transform.hasChanged = false;
        }

        #endregion

        #region Debugging & Gizmos

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Bounds b = GetWorldBounds();
            Gizmos.DrawWireCube(b.center, b.size);
        }

        #endregion
    }
}
