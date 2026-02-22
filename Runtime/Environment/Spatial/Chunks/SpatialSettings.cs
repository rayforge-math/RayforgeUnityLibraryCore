using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// Bundles the spatial configuration to ensure consistency across the system.
    /// Helps avoid "Parameter Fatigue" in constructors.
    /// </summary>
    public ref struct SpatialSettings
    {
        public GridSize GridSize;
        public Vector3 Anchor;

        public SpatialSettings(GridSize gridSize, Vector3 anchor)
        {
            GridSize = gridSize;
            Anchor = anchor;
        }
    }
}