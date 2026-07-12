using System;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Interface for chunks that support Level of Detail (LOD) transitions.
    /// Extends the base chunk with logic for detail management and notification.
    /// </summary>
    public interface ILODState
    {
        /// <summary> 
        /// The current detail level (0 = High detail, increasing values = lower detail, -1 = Culled). 
        /// </summary>
        int CurrentLOD { get; }

        /// <summary>
        /// The maximum possible LOD index defined for this chunk.
        /// Used to normalize LOD-dependent logic (e.g., shaders or debug colors).
        /// </summary>
        int MaxLOD { get; }

        /// <summary>
        /// Indicates if the chunk is currently active and visible in the world.
        /// Typically false if CurrentLOD is -1.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Event fired whenever the LOD level is successfully updated.
        /// Parameters: (ILODState sender, int oldLod, int newLod)
        /// </summary>
        event Action<ILODState, int, int> OnLODChanged;

        /// <summary>
        /// Event fired whenever the visibility state changes.
        /// Parameters: (ILODState sender, bool isVisible)
        /// </summary>
        event Action<ILODState, bool> OnVisibilityChanged;
    }
}
