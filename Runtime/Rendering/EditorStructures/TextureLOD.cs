using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.EditorExtensions.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Rendering.EditorStructures
{
    /// <summary>
    /// Represents a specific Level of Detail configuration for textures, 
    /// defining the distance and the corresponding resolution.
    /// </summary>
    [Serializable]
    public struct TextureLOD : ILodEntry<TextureLOD>, IEquatable<TextureLOD>
    {
        [Tooltip("Distance threshold for this level.")]
        public float distanceThreshold;

        [Tooltip("Edge resolution for the texture.")]
        public PowerOfTwoResolution mapResolution;

        /// <summary>
        /// Interface implementation for the distance threshold.
        /// </summary>
        public float DistanceThreshold
        {
            get => distanceThreshold;
            set => distanceThreshold = value;
        }

        #region Equality Implementation

        /// <summary>
        /// Checks if two TextureLOD entries are identical.
        /// English: Using Mathf.Approximately for distances to handle float precision issues.
        /// </summary>
        public bool Equals(TextureLOD other)
        {
            return Mathf.Approximately(distanceThreshold, other.distanceThreshold) &&
                   mapResolution == other.mapResolution;
        }

        /// <summary>
        /// Fallback Equals to fulfill C# guidelines for structs.
        /// </summary>
        public override bool Equals(object obj) => obj is TextureLOD other && Equals(other);

        /// <summary>
        /// Gets a hash code based on the data values.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + distanceThreshold.GetHashCode();
                hash = hash * 23 + mapResolution.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(TextureLOD left, TextureLOD right) => left.Equals(right);
        public static bool operator !=(TextureLOD left, TextureLOD right) => !left.Equals(right);

        #endregion

        #region Logical Validation

        /// <summary>
        /// Logic to check if this LOD entry is valid compared to a predecessor.
        /// A valid successor must have a lower resolution.
        /// </summary>
        public bool IsLogicalSuccessor(TextureLOD predecessor)
        {
            if (mapResolution == PowerOfTwoResolution.None ||
                predecessor.mapResolution == PowerOfTwoResolution.None)
            {
                return false;
            }

            return mapResolution.IsLowerThan(predecessor.mapResolution);
        }

        /// <summary>
        /// Forces this entry to have a lower resolution than the predecessor.
        /// </summary>
        public void MakeValidSuccessor(TextureLOD predecessor)
        {
            bool firstElement = predecessor.mapResolution == PowerOfTwoResolution.None;

            if (firstElement)
            {
                if (mapResolution == PowerOfTwoResolution.None)
                {
                    mapResolution = PowerOfTwoResolution.Resolution256;
                }
            }
            else
            {
                if (!mapResolution.IsLowerThan(predecessor.mapResolution) || mapResolution == PowerOfTwoResolution.None)
                {
                    mapResolution = predecessor.mapResolution.Downscale();
                }
            }
        }

        #endregion
    }
}