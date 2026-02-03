using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A high-performance base class for 3D world chunks. 
    /// Optimized for the Fog System with Origin-Shift awareness.
    /// </summary>
    /// <typeparam name="T">The derived type for type-safe processing.</typeparam>
    public abstract class Chunk3D<T> : MonoBehaviour where T : Chunk3D<T>
    {
        #region Spatial Settings
        [Header("Spatial Settings")]
        /// <summary> The half-size of the chunk in local space. </summary>
        [field: SerializeField]
        public Vector3 localExtent { get; protected set; } = new Vector3(50, 50, 50);

        /// <summary> If true, the chunk flags itself as dirty when the transform moves. </summary>
        public bool updateOnTransformChange = true;
        #endregion

        #region Identity & State
        [Header("Identity")]
        /// <summary> Managed by the Registry. Helps identify the chunk's slot. </summary>
        [field: SerializeField, HideInInspector]
        public Vector3Int currentGridKey { get; internal set; }

        /// <summary> Custom dirty flag for heightmap updates. </summary>
        protected bool _isDirty = true;
        #endregion

        #region Distance Calculations

        /// <summary> Fast squared distance to chunk center. </summary>
        public float GetSqrDistanceTo(Vector3 worldPos)
        {
            Vector3 diff = transform.position - worldPos;
            return diff.sqrMagnitude;
        }

        /// <summary> Precise squared distance to AABB edge. Returns 0 if inside. </summary>
        public float GetSqrDistanceToClosestEdge(Vector3 worldPos)
        {
            Vector3 center = transform.position;
            // Calculate the distance to the box on each axis without creating a Bounds object.
            float dx = Mathf.Max(0, (center.x - localExtent.x) - worldPos.x, worldPos.x - (center.x + localExtent.x));
            float dy = Mathf.Max(0, (center.y - localExtent.y) - worldPos.y, worldPos.y - (center.y + localExtent.y));
            float dz = Mathf.Max(0, (center.z - localExtent.z) - worldPos.z, worldPos.z - (center.z + localExtent.z));

            return dx * dx + dy * dy + dz * dz;
        }

        #endregion

        #region State Management

        public void MarkDirty() => _isDirty = true;

        /// <summary>
        /// Checks if the chunk needs a refresh. 
        /// </summary>
        public virtual bool IsDirty()
        {
            // Combined check: manual flag or unexpected transform change.
            return _isDirty || (updateOnTransformChange && transform.hasChanged);
        }

        /// <summary>
        /// Resets the dirty state. Called by the manager after successful bake/blit.
        /// </summary>
        public virtual void ClearDirty()
        {
            _isDirty = false;
            if (transform != null) transform.hasChanged = false;
        }

        /// <summary>
        /// Explicitly clears the transform.hasChanged flag to ignore World Shifts.
        /// </summary>
        public void SuppressTransformDirtyOnce()
        {
            if (transform != null) transform.hasChanged = false;
        }

        #endregion

        #region Debugging
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, localExtent * 2f);
        }
        #endregion
    }
}
