namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// The Master-Enum that consolidates all available grid sizes.
    /// Mapping to sub-enums (Decimal/Binary) ensures that values are synchronized 
    /// across the entire engine and prevents "magic numbers".
    /// </summary>
    public enum GridSize : int
    {
        #region Decimal Options
        // English: 10m - High precision, small areas.
        Size10 = GridSizeDecimal.Size10,

        // English: 50m - Good for dense environments.
        Size50 = GridSizeDecimal.Size50,

        // English: 100m - The metric standard for open worlds.
        Size100 = GridSizeDecimal.Size100,

        // English: 200m - Balanced for performance and visibility.
        Size200 = GridSizeDecimal.Size200,

        // English: 500m - Large scale for background elements.
        Size500 = GridSizeDecimal.Size500,

        // English: 1000m - Horizon scale.
        Size1000 = GridSizeDecimal.Size1000,
        #endregion

        #region Binary Options
        // English: 16m - Power-of-two equivalent.
        Size16 = GridSizeBinary.Size16,

        // English: 32m - Power-of-two tile size.
        Size32 = GridSizeBinary.Size32,

        // English: 64m - Common tile size for GPU systems.
        Size64 = GridSizeBinary.Size64,

        // English: 128m - Perfect 1:1 mapping for standard render targets.
        Size128 = GridSizeBinary.Size128,

        // English: 256m - Large tile, efficient for GPU batching.
        Size256 = GridSizeBinary.Size256,

        // English: 512m - Huge area, ideal for maps.
        Size512 = GridSizeBinary.Size512,

        // English: 1024m - Epic scale, matches 1K texture resolution.
        Size1024 = GridSizeBinary.Size1024
        #endregion
    }

    /// <summary>
    /// Standardized chunk sizes based on the Metric/Decimal system.
    /// Best used when gameplay distances (e.g., "300m fog distance") are the priority.
    /// </summary>
    public enum GridSizeDecimal : int
    {
        // 10m - High precision, small areas.
        Size10 = 10,

        // 50m - Good for dense environments.
        Size50 = 50,

        // 100m - The metric standard for open worlds.
        Size100 = 100,

        // 200m - Balanced for performance and visibility.
        Size200 = 200,

        // 500m - Large scale for background elements.
        Size500 = 500,

        // 1000m - Horizon scale, very low update frequency.
        Size1000 = 1000
    }

    /// <summary>
    /// Standardized chunk sizes based on Power-of-Two (Binary) values.
    /// Best used for Volumetrics/Shaders where 1 meter should map perfectly to 1 texel 
    /// (e.g., a 128x128 texture on a 128m chunk).
    /// </summary>
    public enum GridSizeBinary : int
    {
        // 16m - Equivalent to 16x16 texels.
        Size16 = 16,

        // 32m - Power-of-two size.
        Size32 = 32,

        // 64m - Common tile size for many engines.
        Size64 = 64,

        // 128m - Perfect 1:1 mapping for standard render targets.
        Size128 = 128,

        // 256m - Large tile, efficient for GPU batching.
        Size256 = 256,

        // 512m - Huge area, minimizes draw calls for distant fog.
        Size512 = 512,

        // 1024m - Epic scale, matches 1K texture resolution.
        Size1024 = 1024
    }
}
