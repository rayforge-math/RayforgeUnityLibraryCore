using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// Configuration data for surface detection. 
    /// Defined as a serializable struct to allow easy editing in the Unity Inspector.
    /// </summary>
    [Serializable]
    public struct SurfaceTrackerSettings
    {
        [Tooltip("If enabled, the tracker automatically scans all children of the provided root Transform.")]
        public bool scanHierarchy;

        [Tooltip("If not empty, only objects containing this string in their name are considered.")]
        public string nameFilter;

        [Space(5)]
        [Tooltip("If enabled, objects must have a minimum physical size to be accepted.")]
        public bool enableAreaCheck;

        [Tooltip("Minimum XZ-Area in square meters (e.g., 1.0 for a 1x1m area).")]
        public float minAreaThreshold;

        /// <summary>
        /// Provides a default configuration for the tracker.
        /// </summary>
        public static SurfaceTrackerSettings Default => new SurfaceTrackerSettings
        {
            scanHierarchy = true,
            nameFilter = "",
            enableAreaCheck = true,
            minAreaThreshold = 1.0f
        };
    }
}