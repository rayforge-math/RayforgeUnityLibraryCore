using UnityEngine;

namespace Rayforge.Core.Rendering.Projection
{
    /// <summary>
    /// Parameters defining the 3D volume and spatial configuration for a heightmap bake.
    /// </summary>
    public struct HeightmapBakeParams
    {
        public Vector3 WorldCenter;
        public Vector2 Extent;
        public float MinY;
        public float MaxY;
    }
}
