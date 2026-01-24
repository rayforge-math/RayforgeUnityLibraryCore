using UnityEngine;

namespace Rayforge.Core.Rendering.Passes
{
    /// <summary>
    /// Combines <see cref="TextureMeta"/> with a concrete texture handle.
    /// </summary>
    /// <typeparam name="THandle">The type of texture handle (RTHandle, TextureHandle, etc.).</typeparam>
    public struct TextureHandleMeta<THandle>
    {
        public TextureMeta Meta;
        public THandle Handle;

        /// <summary>
        /// Initializes a new instance with a prebuilt <see cref="TextureMeta"/> and handle.
        /// </summary>
        /// <param name="meta">The metadata for this texture.</param>
        /// <param name="handle">The texture handle.</param>
        public TextureHandleMeta(TextureMeta meta, THandle handle)
        {
            Meta = meta;
            Handle = handle;
        }

        /// <summary>
        /// Initializes a new instance with individual parameters for metadata and a handle.
        /// </summary>
        /// <param name="ids">Shader property IDs for binding.</param>
        /// <param name="name">Debug name for the resource.</param>
        /// <param name="resolution">Width and height of the texture.</param>
        /// <param name="handle">The actual texture handle.</param>
        public TextureHandleMeta(TextureIds ids, string name, Vector2Int resolution, THandle handle)
        {
            Meta = new TextureMeta(ids, name, new Vector4(1.0f / resolution.x, 1.0f / resolution.y, resolution.x, resolution.y));
            Handle = handle;
        }

        /// <summary>
        /// Initializes a new instance with individual parameters for metadata and a handle.
        /// </summary>
        /// <param name="ids">Shader property IDs for binding.</param>
        /// <param name="name">Debug name for the resource.</param>
        /// <param name="texelSize">Calculated texel size vector (x=1/width, y=1/height, z=width, w=height).</param>
        /// <param name="handle">The actual texture handle.</param>
        public TextureHandleMeta(TextureIds ids, string name, Vector4 texelSize, THandle handle)
        {
            Meta = new TextureMeta(ids, name, texelSize);
            Handle = handle;
        }
    }
}