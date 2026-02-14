using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ChunkGPUData
    {
        public Vector3 minBounds;
        public float lodLevel;
        public Vector3 maxBounds;
        public int identifier;
        public Vector3 worldCenter;
        public float radius;
    }
}
