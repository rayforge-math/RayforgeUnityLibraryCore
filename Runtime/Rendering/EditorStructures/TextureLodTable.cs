using Rayforge.Core.Common.Rendering;
using Rayforge.Core.EditorExtensions.EditorStructures;
using System;

namespace Rayforge.Core.Rendering.EditorStructures
{
    /// <summary>
    /// Specialized LOD table for textures. 
    /// Provides helper properties for resolution-based logic.
    /// </summary>
    [Serializable]
    public class TextureLodTable : UniversalLodTable<TextureLOD>
    {
        /// <summary>
        /// Returns the resolution of the first (highest quality) LOD entry.
        /// Returns a default resolution if the table is empty.
        /// </summary>
        public PowerOfTwoResolution BaseResolution =>
            ValidEntries.Length > 0 ? ValidEntries[0].mapResolution : PowerOfTwoResolution.Resolution256;

        /// <summary>
        /// Returns the resolution of the last (lowest quality) LOD entry.
        /// </summary>
        public PowerOfTwoResolution LowestResolution =>
            ValidEntries.Length > 0 ? ValidEntries[ValidEntries.Length - 1].mapResolution : PowerOfTwoResolution.Resolution32;
    }
}