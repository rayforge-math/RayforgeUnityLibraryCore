using System;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Defines the write-access interface for managing a chunk's LOD state.
    /// Used by the registry or coordinator to push updates and configuration to the chunk.
    /// </summary>
    public interface ILODReceiver
    {
        /// <summary>
        /// Initializes or updates the LOD range metadata for this chunk.
        /// This should be called before the first LOD update to ensure correct scaling and visualization.
        /// </summary>
        /// <param name="maxLod">The highest possible LOD index (e.g., 4 for a system with 5 LOD levels).</param>
        void ConfigureLODRange(int maxLod);

        /// <summary>
        /// Updates the current LOD level of the chunk.
        /// </summary>
        /// <param name="newLod">The target LOD index. Use -1 for culling.</param>
        /// <param name="useHardDeactivation">If true, the GameObject will be disabled when culled.</param>
        /// <returns>True if the LOD level actually changed.</returns>
        bool UpdateLOD(int newLod, bool useHardDeactivation);

        /// <summary>
        /// Directly controls the visibility state of the chunk.
        /// </summary>
        /// <param name="visible">True if the chunk should be rendered.</param>
        /// <param name="useHardDeactivation">If true, uses GameObject.SetActive for culling.</param>
        void SetVisibility(bool visible, bool useHardDeactivation);
    }
}
