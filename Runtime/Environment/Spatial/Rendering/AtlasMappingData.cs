using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AtlasMappingData
    {
        // The slice index in the Texture2DArray
        public float SliceIndex;
        // Scaling factor for UVs (1.0 / slotsPerDim)
        public float RelativeScale;
        // Offset within the slice for shared slices
        public Vector2 RelativeOffset;

        public static AtlasMappingData Inactive => new AtlasMappingData { SliceIndex = -1f };
    }
}