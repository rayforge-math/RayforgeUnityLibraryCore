using System.Runtime.InteropServices;
using Rayforge.Core.Environment.Abstractions;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Lightweight sphere for fast distance-based culling.
    /// Memory: 16 Bytes (1x float4)
    /// Use case: Terrain chunks, particles, simple LOD triggers.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SphereSpatialData : ISpatialData
    {
        public Vector3 Position;    // 12 Bytes
        public float Radius;        // 4 Bytes
    }

    /// <summary>
    /// Axis-Aligned Bounding Box for precise frustum culling.
    /// Memory: 32 Bytes (2x float4)
    /// Use case: Static world geometry, large terrain sectors.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct AabbSpatialData : ISpatialData
    {
        public Vector3 MinBounds;   // 12 Bytes
        public float LayerMask;     // 4 Bytes
        public Vector3 MaxBounds;   // 12 Bytes
        public float ActiveFlag;    // 4 Bytes
    }

    /// <summary>
    /// Full transformation data.
    /// Memory: 64 Bytes (4x float4)
    /// Use case: Anything that is rotated or scaled.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MatrixSpatialData : ISpatialData
    {
        public Matrix4x4 LocalToWorld;  // 64 Bytes
    }
}
