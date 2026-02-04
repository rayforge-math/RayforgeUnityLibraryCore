namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// The Master-Enum that consolidates all available grid sizes.
    /// Mapping to sub-enums (Decimal/Binary) ensures that values are synchronized 
    /// across the entire engine and prevents "magic numbers".
    /// </summary>
    public enum ChunkSize : int
    {
        #region Decimal Options
        // English: 10m - High precision, small areas.
        DecimalTiny = ChunkSizeDecimal.Tiny,

        // English: 50m - Good for dense environments.
        DecimalSmall = ChunkSizeDecimal.Small,

        // English: 100m - The metric standard for open worlds.
        DecimalMedium = ChunkSizeDecimal.Medium,

        // English: 200m - Balanced for performance and visibility.
        DecimalLarge = ChunkSizeDecimal.Large,

        // English: 500m - Large scale for background elements.
        DecimalHuge = ChunkSizeDecimal.Huge,

        // English: 1000m - Horizon scale.
        DecimalEpic = ChunkSizeDecimal.Epic,
        #endregion

        #region Binary Options
        // English: 16m - Power-of-two equivalent to Tiny.
        BinaryTiny = ChunkSizeBinary.Tiny,

        // English: 64m - Common tile size for GPU systems.
        BinarySmall = ChunkSizeBinary.Small,

        // English: 128m - Perfect 1:1 mapping for standard render targets.
        BinaryMedium = ChunkSizeBinary.Medium,

        // English: 256m - Large tile, efficient for GPU batching.
        BinaryLarge = ChunkSizeBinary.Large,

        // English: 512m - Huge area, ideal for 270p/512p maps.
        BinaryHuge = ChunkSizeBinary.Huge,

        // English: 1024m - Epic scale, matches 1K texture resolution.
        BinaryEpic = ChunkSizeBinary.Epic
        #endregion
    }

    /// <summary>
    /// Standardized chunk sizes based on the Metric/Decimal system.
    /// Best used when gameplay distances (e.g., "300m fog distance") are the priority.
    /// </summary>
    public enum ChunkSizeDecimal : int
    {
        // 10m - High precision, small areas.
        Tiny = 10,

        // 50m - Good for dense environments.
        Small = 50,

        // 100m - The metric standard for open worlds.
        Medium = 100,

        // 200m - Balanced for performance and visibility.
        Large = 200,

        // 500m - Large scale for background elements.
        Huge = 500,

        // 1000m - Horizon scale, very low update frequency.
        Epic = 1000
    }

    /// <summary>
    /// Standardized chunk sizes based on Power-of-Two (Binary) values.
    /// Best used for Volumetrics/Shaders where 1 meter should map perfectly to 1 texel 
    /// (e.g., a 128x128 texture on a 128m chunk).
    /// </summary>
    public enum ChunkSizeBinary : int
    {
        // 16m - Equivalent to 16x16 texels.
        Tiny = 16,

        // 64m - Common tile size for many engines.
        Small = 64,

        // 128m - Perfect 1:1 mapping for standard render targets.
        Medium = 128,

        // 256m - Large tile, efficient for GPU batching.
        Large = 256,

        // 512m - Huge area, minimizes draw calls for distant fog.
        Huge = 512,

        // 1024m - Epic scale, matches 1K texture resolution.
        Epic = 1024
    }
}
