using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A high-performance, generic base class for spatial world chunks.
    /// Uses a bitmask (ActiveAxes) to handle distance and positioning for 2D, 3D or custom dimensions.
    /// </summary>
    /// <typeparam name="T">The derived type for type-safe processing and registry management.</typeparam>
    public abstract class Chunk<T> : MonoBehaviour, ISpatialEntry
        where T : Chunk<T>
    {
        #region Spatial Settings
        [Header("Spatial Settings")]
        /// <summary> The half-size of the chunk in local space. Defines the AABB bounds. </summary>
        [field: SerializeField]
        public Vector3 localExtent { get; internal set; } = new Vector3(50, 50, 50);

        /// <summary> If true, the chunk flags itself as dirty when the transform moves. </summary>
        public bool updateOnTransformChange = false;
        #endregion

        #region Identity & State
        [Header("Identity")]
        /// <summary> Managed by the Registry. Identifies the chunk's grid slot. </summary>
        [field: SerializeField, HideInInspector]
        public Vector3Int GridKey { get; internal set; }

        private static SpatialAxes _axes = SpatialAxes.Voxel;

        /// <summary>
        /// Defines which axes are active. Drives distance calculations and gizmos.
        /// Overriding this in derived classes (e.g. Chunk2D) flattens the math automatically.
        /// </summary>
        public static SpatialAxes ActiveAxes
        {
            get => _axes;
            internal set => _axes = value;
        }

        /// <summary> Internal dirty flag for state tracking. </summary>
        protected bool _isDirty = true;

        #endregion

        #region Lifecycle Management

        /// <summary>
        /// Forces inheriting classes to release native/GPU resources.
        /// Even if no resources are used, this must be explicitly implemented (can be empty).
        /// </summary>
        protected abstract void OnDispose();

        /// <summary>
        /// Public entry point to safely remove a chunk from the world.
        /// Triggers resource cleanup and destroys the GameObject.
        /// </summary>
        public void DisposeChunk()
        {
            OnDispose();

            if (this != null && gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Safety fallback for manual deletion via the Unity Editor or Scripts.
        /// </summary>
        private void OnDestroy()
        {
            OnDispose();
        }

        #endregion

        #region Universal Distance Calculations

        /// <summary>
        /// Checks if a world position is within the chunk's AABB.
        /// Automatically respects ActiveAxes (e.g., ignores height if Y is inactive).
        /// </summary>
        public bool Contains(Vector3 worldPos)
        {
            Vector3 center = transform.position;

            // Check X-Axis overlap if active
            if ((ActiveAxes & SpatialAxes.X) != 0)
            {
                if (Mathf.Abs(center.x - worldPos.x) > localExtent.x) return false;
            }

            // Check Y-Axis overlap if active
            if ((ActiveAxes & SpatialAxes.Y) != 0)
            {
                if (Mathf.Abs(center.y - worldPos.y) > localExtent.y) return false;
            }

            // Check Z-Axis overlap if active
            if ((ActiveAxes & SpatialAxes.Z) != 0)
            {
                if (Mathf.Abs(center.z - worldPos.z) > localExtent.z) return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates the squared distance to a world position, respecting ActiveAxes.
        /// </summary>
        public virtual float GetSqrDistanceTo(Vector3 worldPos)
        {
            Vector3 center = transform.position;

            float dx = ((ActiveAxes & SpatialAxes.X) != 0) ? (center.x - worldPos.x) : 0;
            float dy = ((ActiveAxes & SpatialAxes.Y) != 0) ? (center.y - worldPos.y) : 0;
            float dz = ((ActiveAxes & SpatialAxes.Z) != 0) ? (center.z - worldPos.z) : 0;

            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// Precise squared distance to the AABB edge, respecting ActiveAxes.
        /// Inactive axes contribute 0 to the distance result.
        /// </summary>
        public virtual float GetSqrDistanceToClosestEdge(Vector3 worldPos)
        {
            Vector3 center = transform.position;
            float dx = 0, dy = 0, dz = 0;

            if ((ActiveAxes & SpatialAxes.X) != 0)
            {
                float deltaX = Mathf.Abs(center.x - worldPos.x) - localExtent.x;
                dx = Mathf.Max(0, deltaX);
            }

            if ((ActiveAxes & SpatialAxes.Y) != 0)
            {
                float deltaY = Mathf.Abs(center.y - worldPos.y) - localExtent.y;
                dy = Mathf.Max(0, deltaY);
            }

            if ((ActiveAxes & SpatialAxes.Z) != 0)
            {
                float deltaZ = Mathf.Abs(center.z - worldPos.z) - localExtent.z;
                dz = Mathf.Max(0, deltaZ);
            }

            return dx * dx + dy * dy + dz * dz;
        }

        #endregion

        #region State Management

        /// <summary> Flags the chunk as dirty. </summary>
        public void MarkDirty() => _isDirty = true;

        /// <summary> Checks if the chunk needs a refresh. </summary>
        public virtual bool IsDirty => _isDirty || (updateOnTransformChange && transform.hasChanged);

        /// <summary> Resets the dirty state and the transform flag. </summary>
        public virtual void ClearDirty()
        {
            _isDirty = false;
            if (transform != null) transform.hasChanged = false;
        }

        /// <summary> Resets the transform flag to ignore World Shifts in the dirty state. </summary>
        public void SuppressTransformDirtyOnce()
        {
            if (transform != null) transform.hasChanged = false;
        }

        #endregion

        #region Debugging
        protected virtual void OnDrawGizmosSelected()
        {
            bool isFull3D = (ActiveAxes & SpatialAxes.Y) != 0;
            Gizmos.color = isFull3D ? Color.cyan : Color.green;

            Vector3 pos = transform.position;
            Gizmos.DrawWireCube(pos, localExtent * 2f);

            if (!isFull3D)
            {
                Gizmos.color = new Color(0, 1, 0, 0.2f);
                Gizmos.DrawCube(pos, new Vector3(localExtent.x * 2f, 0.05f, localExtent.z * 2f));
            }
        }
        #endregion
    }
}
