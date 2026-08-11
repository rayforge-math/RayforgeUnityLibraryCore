using System;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Bitmask defining which spatial dimensions are relevant for an entry.
    /// Used for positioning, distance calculations (LOD), and spatial queries.
    /// </summary>
    [Flags]
    public enum SpatialAxes
    {
        None = 0,
        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2,
        W = 1 << 3,

        XY = X | Y,
        XZ = X | Z,
        YZ = Y | Z,

        /// <summary> Horizontal plane only (X and Z). Ideal for Terrain/Heightmaps. </summary>
        Surface = XZ,

        /// <summary> Full volumetric space (X, Y, and Z). Ideal for Voxel/3D worlds. </summary>
        Voxel = X | Y | Z,

        /// <summary> All dimensions including logic/time/layer axis. </summary>
        Full = X | Y | Z | W
    }
}
