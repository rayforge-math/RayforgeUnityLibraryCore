using System.Reflection;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A high-performance, generic base class for spatial world chunks.
    /// Uses a bitmask (ActiveAxes) to handle distance and positioning for 2D, 3D or custom dimensions.
    /// </summary>
    /// <typeparam name="T">The derived type for type-safe processing and registry management.</typeparam>
    [ChunkConfig(SpatialAxes.Voxel)]
    public abstract class Chunk<T> : MonoBehaviour, ISpatialEntry
        where T : Chunk<T>
    {
        #region Spatial Settings
        [Header("Spatial Settings")]
        /// <summary> The half-size of the chunk in local space. Defines the AABB bounds. </summary>
        [field: SerializeField]
        public Vector3 localExtent { get; internal set; } = new Vector3(50, 50, 50);

        public Vector3 GetWorldSize() => localExtent * 2f;

        /// <summary> If true, the chunk flags itself as dirty when the transform moves. </summary>
        public bool updateOnTransformChange = false;
        #endregion

        #region Identity & State
        [Header("Identity")]
        /// <summary> Managed by the Registry. Identifies the chunk's grid slot. </summary>
        [field: SerializeField, HideInInspector]
        public Vector3Int GridKey { get; internal set; }

        public Vector2Int GridKeyXY => new Vector2Int(GridKey.x, GridKey.y);
        public Vector2Int GridKeyXZ => new Vector2Int(GridKey.x, GridKey.z);

        /// <summary>
        /// Static cache for the axes configuration. 
        /// Initialized once per unique type T.
        /// </summary>
        public static SpatialAxes ActiveAxes { get; private set; }

        static Chunk()
        {
            var config = typeof(T).GetCustomAttribute<ChunkConfigAttribute>(true);
            ActiveAxes = config?.Axes ?? SpatialAxes.Voxel;
        }

        public bool IsXActive => (ActiveAxes & SpatialAxes.X) != 0;
        public bool IsYActive => (ActiveAxes & SpatialAxes.Y) != 0;
        public bool IsZActive => (ActiveAxes & SpatialAxes.Z) != 0;

        /// <summary> Internal dirty flag for state tracking. </summary>
        protected bool _isDirty = true;

        #endregion

        #region Lifecycle Management

        /// <summary>
        /// Forces inheriting classes to release native/GPU resources.
        /// Even if no resources are used, this must be explicitly implemented (can be empty).
        /// </summary>
        protected abstract void OnDispose();

        private bool _isDisposed = false;

        /// <summary>
        /// Public entry point to safely remove a chunk from the world.
        /// Triggers resource cleanup and destroys the GameObject.
        /// </summary>
        public void DisposeChunk()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            OnDispose();

            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        /// <summary>
        /// Safety fallback for manual deletion via the Unity Editor or Scripts.
        /// </summary>
        private void OnDestroy()
        {
            if (!_isDisposed) DisposeChunk();
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
            if (IsXActive)
            {
                if (Mathf.Abs(center.x - worldPos.x) > localExtent.x) return false;
            }

            // Check Y-Axis overlap if active
            if (IsYActive)
            {
                if (Mathf.Abs(center.y - worldPos.y) > localExtent.y) return false;
            }

            // Check Z-Axis overlap if active
            if (IsZActive)
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

            float dx = (IsXActive) ? (center.x - worldPos.x) : 0;
            float dy = (IsYActive) ? (center.y - worldPos.y) : 0;
            float dz = (IsZActive) ? (center.z - worldPos.z) : 0;

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

            if (IsXActive)
            {
                float deltaX = Mathf.Abs(center.x - worldPos.x) - localExtent.x;
                dx = Mathf.Max(0, deltaX);
            }

            if (IsYActive)
            {
                float deltaY = Mathf.Abs(center.y - worldPos.y) - localExtent.y;
                dy = Mathf.Max(0, deltaY);
            }

            if (IsZActive)
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
            Vector3 displaySize = new Vector3(
                IsXActive ? localExtent.x * 2f : 0.1f,
                IsYActive ? localExtent.y * 2f : 0.1f,
                IsZActive ? localExtent.z * 2f : 0.1f
            );

            Vector3 pos = transform.position;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(pos, displaySize);

            Gizmos.color = Color.green;
            Gizmos.DrawCube(pos, displaySize);
        }
        #endregion
    }
}
