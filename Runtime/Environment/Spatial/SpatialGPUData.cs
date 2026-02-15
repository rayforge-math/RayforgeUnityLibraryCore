using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Lightweight sphere for fast distance-based culling.
    /// Memory: 16 Bytes (1x float4)
    /// Use case: Terrain chunks, particles, simple LOD triggers.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SphereSpatialData
    {
        public Vector3 Position;    // 12 Bytes
        public float Radius;        // 4 Bytes

        public static SphereSpatialData Inactive => new SphereSpatialData { Radius = -1f };
    }

    /// <summary>
    /// Axis-Aligned Bounding Box for precise frustum culling.
    /// Memory: 32 Bytes (2x float4)
    /// Use case: Static world geometry, large terrain sectors.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct AABBSpatialData
    {
        public Vector3 MinBounds;   // 12 Bytes
        public float LayerMask;     // 4 Bytes
        public Vector3 MaxBounds;   // 12 Bytes
        public float IsActive;      // 4 Bytes
    }

    /// <summary>
    /// Full transformation data.
    /// Memory: 64 Bytes (4x float4)
    /// Use case: Anything that is rotated or scaled.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MatrixSpatialData
    {
        public Matrix4x4 LocalToWorld;  // 64 Bytes
    }
}
