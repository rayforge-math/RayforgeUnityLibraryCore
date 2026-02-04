namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Interface for chunks that support Level of Detail transitions.
    /// Allows registries to trigger detail changes without knowing the specific chunk type.
    /// </summary>
    public interface ILODSpatialEntry
    {
        /// <summary> The current detail level (0 = High, 1 = Med, etc.). </summary>
        int CurrentLOD { get; }

        /// <summary>
        /// Updates the LOD state. 
        /// Implementation should check if the value changed before triggering expensive logic.
        /// </summary>
        void UpdateLOD(int newLod);
    }
}
