using Rayforge.Core.Rendering.Passes;
using UnityEngine;

namespace Rayforge.Core.Rendering.Passes
{
    /// <summary>
    /// Metadata-only container for a texture resource.
    /// Does NOT contain the actual handle, only shader IDs, name, texel size, and resolution.
    /// </summary>
    public struct TextureMeta
    {
        /// <summary>Shader property IDs for this texture.</summary>
        public TextureIds Ids;

        /// <summary>Human-readable name for profiling and debugging.</summary>
        public string Name;

        /// <summary>Texel size: x = 1/width, y = 1/height, z = width, w = height.</summary>
        public Vector4 TexelSize;

        public TextureMeta(TextureIds ids, string name, Vector4 texelSize)
        {
            Ids = ids;
            Name = name;
            TexelSize = texelSize;
        }
    }
}
