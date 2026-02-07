using System;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Interface for chunks that support Level of Detail (LOD) transitions.
    /// Extends the base chunk with logic for detail management and notification.
    /// </summary>
    public interface ILODState : IChunk
    {
        /// <summary> 
        /// The current detail level (0 = High detail, increasing values = lower detail, -1 = Culled). 
        /// </summary>
        int CurrentLOD { get; }

        /// <summary>
        /// Event fired whenever the LOD level is successfully updated.
        /// Parameters: (ILODState sender, int oldLod, int newLod)
        /// </summary>
        event Action<ILODState, int, int> OnLODChanged;
    }
}
