using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Minimum requirements for an object to be managed by a SpatialRegistry.
    /// Defines lifecycle hooks, state tracking, and dimensional relevance.
    /// </summary>
    public interface ISpatialEntry
    {
        /// <summary> 
        /// True if the internal state (e.g., data, mesh, or transform) is out of sync.
        /// </summary>
        bool IsDirty { get; }

        /// <summary> 
        /// Resets the dirty state after a successful update or bake.
        /// </summary>
        void ClearDirty();

        /// <summary> 
        /// The Unity GameObject associated with this entry for scene management.
        /// </summary>
        GameObject gameObject { get; }

        /// <summary>
        /// Defines which axes are "active" for this entry.
        /// This determines if a dimension is used for centering within a grid cell 
        /// or included in distance-based calculations (LOD).
        /// </summary>
        SpatialAxes ActiveAxes { get; }
    }
}