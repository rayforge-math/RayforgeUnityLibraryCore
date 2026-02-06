namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Interface for chunks that support Level of Detail transitions.
    /// Allows registries to trigger detail changes without knowing the specific chunk type.
    /// </summary>
    public interface ISpatialLOD
    {
        /// <summary> The current detail level (0 = High, 1 = Med, etc.). </summary>
        int CurrentLOD { get; }

        /// <summary>
        /// Updates the Level of Detail (LOD) state of the entry.
        /// </summary>
        /// <param name="newLod">The new LOD index (0 for highest detail, -1 for culled/inactive).</param>
        /// <returns>
        /// True if the LOD level actually changed; otherwise, false. 
        /// Use this return value to avoid redundant updates or expensive re-baking logic.
        /// </returns>
        bool UpdateLOD(int newLod);
    }
}
