using UnityEngine;

namespace Rayforge.Core.Rendering.Passes
{
    /// <summary>
    /// Represents a unified metadata container for a texture resource.
    /// This generic structure pairs metadata (IDs, size, name) with a specific handle type,
    /// allowing seamless transitions between persistent <see cref="UnityEngine.Rendering.RTHandle"/>s 
    /// and transient <see cref="UnityEngine.Rendering.RenderGraphModule.TextureHandle"/>s.
    /// </summary>
    /// <typeparam name="THandle">The type of texture handle (e.g., RTHandle or TextureHandle).</typeparam>
    public struct TextureHandleMeta<THandle>
    {
        /// <summary> 
        /// The shader property IDs associated with this resource.
        /// Facilitates consistent binding to shader variables across different render passes.
        /// </summary>
        public TextureIds Ids;

        /// <summary> 
        /// Human-readable identifier for this resource. 
        /// Used for profiling, Frame Debugger visualization, and internal logging.
        /// </summary>
        public string Name;

        /// <summary> 
        /// Texel size data: x = 1/width, y = 1/height, z = width, w = height.
        /// Essential for shaders to perform accurate texture sampling and coordinate calculations.
        /// </summary>
        public Vector4 TexelSize;

        /// <summary> 
        /// The underlying texture resource. 
        /// For a depth pyramid, Index 0 typically represents the full-resolution source, 
        /// while subsequent indices hold downsampled versions.
        /// </summary>
        public THandle Handle;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureHandleMeta{THandle}"/> struct.
        /// </summary>
        /// <param name="ids">Shader property IDs for binding.</param>
        /// <param name="name">Debug name for the resource.</param>
        /// <param name="texelSize">Calculated texel size vector.</param>
        /// <param name="handle">The actual texture handle (RTHandle or TextureHandle).</param>
        public TextureHandleMeta(TextureIds ids, string name, Vector4 texelSize, THandle handle)
        {
            Ids = ids;
            Name = name;
            TexelSize = texelSize;
            Handle = handle;
        }
    }
}