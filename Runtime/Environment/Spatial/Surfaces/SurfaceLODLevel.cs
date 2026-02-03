using UnityEngine;

using Rayforge.Core.Common.Rendering;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    [System.Serializable]
    public struct SurfaceLODLevel
    {
        [Tooltip("Distance threshold for this level. Objects beyond this (but within viewDistance) use the next LOD.")]
        public float distanceThreshold;

        [Tooltip("Edge resolution for the heightmap.")]
        public PowerOfTwoResolution mapResolution;
    }
}
