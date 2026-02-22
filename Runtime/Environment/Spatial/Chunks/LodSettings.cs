using System;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// Bundles configuration for LOD-specific spatial registries.
    /// Combines spatial metrics with LOD behavior settings.
    /// </summary>
    public ref struct LodSettings
    {
        public ReadOnlySpan<float> LodDistances;
        public bool DeactivateOnCulled;

        public LodSettings(ReadOnlySpan<float> lodDistances, bool deactivateOnCulled = true)
        {
            LodDistances = lodDistances;
            DeactivateOnCulled = deactivateOnCulled;
        }
    }
}