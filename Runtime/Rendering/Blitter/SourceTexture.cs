namespace Rayforge.Core.Rendering.Blitter
{
    /// <summary>
    /// Identifiers for source textures used in multi-texture channel blitting.
    /// Used in <see cref="_ChannelSource"/> to select which texture provides the data for a given channel.
    /// </summary>
    public enum SourceTexture : uint
    {
        Texture0 = 0,
        Texture1 = 1,
        Texture2 = 2,
        Texture3 = 3,
        None = 4
    }
}