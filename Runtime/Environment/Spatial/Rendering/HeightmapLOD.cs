using Rayforge.Core.Common.Rendering;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    [System.Serializable]
    public struct TextureLOD
    {
        [Tooltip("Distance threshold for this level.")]
        public float distanceThreshold;
        [Tooltip("Edge resolution for the texture.")]
        public PowerOfTwoResolution mapResolution;
    }
}
