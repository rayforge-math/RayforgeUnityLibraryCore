using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.EditorExtensions.Abstractions;
using System;
using UnityEngine;
using static Codice.CM.Common.CmCallContext;

namespace Rayforge.Core.Rendering.EditorStructures
{
    /// <summary>
    /// Represents a specific Level of Detail configuration for textures, 
    /// defining the distance and the corresponding resolution.
    /// </summary>
    [Serializable]
    public struct TextureLOD : ILodEntry<TextureLOD>
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
    }
}