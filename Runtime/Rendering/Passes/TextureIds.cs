namespace Rayforge.Core.Rendering.Passes
{
    /// <summary>
    /// Holds shader property IDs for a texture and an optional texel size vector.
    /// Can be used for depth pyramids, color pyramids, or other textures with mip chain.
    /// </summary>
    public struct TextureIds
    {
        /// <summary>Property ID of the texture.</summary>
        public int texture;

        /// <summary>Property ID of the texel size vector (optional, can be 0 if not needed).</summary>
        public int texelSize;

        public TextureIds(int textureId, int texelSizeId = 0)
        {
            texture = textureId;
            texelSize = texelSizeId;
        }

        public override string ToString() => $"TextureIds(TextureID={texture}, TexelSizeID={texelSize})";
    }
}