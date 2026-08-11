using Rayforge.Core.Collections.Abstractions;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// Lightweight sphere for fast distance-based culling.
    /// Memory: 16 Bytes (1x float4)
    /// Use case: Terrain chunks, particles, simple LOD triggers.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SphereSpatialData : IGpuData<SphereSpatialData>
    {
        public Vector3 Position;    // 12 Bytes
        public float Radius;        // 4 Bytes

        public bool IsValid => Radius > 0f;

        public SphereSpatialData InvalidData()
        {
            return new SphereSpatialData
            {
                Position = Vector3.zero,
                Radius = 0f
            };
        }
    }

    /// <summary>
    /// Axis-Aligned Bounding Box for precise frustum culling.
    /// Memory: 32 Bytes (2x float4)
    /// Use case: Static world geometry, large terrain sectors.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct AabbSpatialData : IGpuData<AabbSpatialData>
    {
        public Vector3 MinBounds;   // 12 Bytes
        public float LayerMask;     // 4 Bytes
        public Vector3 MaxBounds;   // 12 Bytes
        public float ActiveFlag;    // 4 Bytes

        public bool IsValid => BitConverter.SingleToInt32Bits(ActiveFlag) != 0x0;

        public AabbSpatialData InvalidData()
        {
            return new AabbSpatialData
            {
                MinBounds = Vector3.zero,
                MaxBounds = Vector3.zero,
                LayerMask = BitConverter.Int32BitsToSingle(0x0),
                ActiveFlag = BitConverter.Int32BitsToSingle(0x0)
            };
        }
    }

    /// <summary>
    /// Full transformation data.
    /// Memory: 64 Bytes (4x float4)
    /// Use case: Anything that is rotated or scaled.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MatrixSpatialData : IGpuData<MatrixSpatialData>
    {
        public Matrix4x4 LocalToWorld;  // 64 Bytes

        public bool IsValid => LocalToWorld != Matrix4x4.zero;

        public MatrixSpatialData InvalidData()
        {
            return new MatrixSpatialData
            {
                LocalToWorld = Matrix4x4.zero
            };
        }
    }
}
