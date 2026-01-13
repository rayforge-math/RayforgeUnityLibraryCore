using UnityEngine;

namespace Rayforge.Core.ShaderExtensions.Blitter
{
    /// <summary>
    /// Provides shader property identifiers for common blit parameters used when drawing
    /// full-screen procedural triangles inside RenderGraph passes.
    /// </summary>
    public static class BlitParameters
    {
        static class BlitShaderIDs
        {
            public const string BlitTextureName = "_BlitTexture";
            public const string BlitDestinationName = "_BlitDestination";
            public const string BlitScaleBiasName = "_BlitScaleBias";

            public static readonly int BlitTextureId = Shader.PropertyToID(BlitTextureName);
            public static readonly int BlitDestinationId = Shader.PropertyToID(BlitDestinationName);
            public static readonly int BlitScaleBiasId = Shader.PropertyToID(BlitScaleBiasName);
        }
        
        /// <summary>Shader property name used to bind the source texture for a blit pass.</summary>
        public static string BlitTexture => BlitShaderIDs.BlitTextureName;
        
        /// <summary>Shader property name used to bind the destination texture for a blit pass.</summary>
        public static string BlitDestination => BlitShaderIDs.BlitDestinationName;
        
        /// <summary>Shader property name used to provide scale and bias for sampling operations.</summary>
        public static string BlitScaleBias => BlitShaderIDs.BlitScaleBiasName;

        /// <summary>Property ID for the blit texture.</summary>
        public static int BlitTextureId => BlitShaderIDs.BlitTextureId;

        /// <summary>Property ID for the blit destination texture.</summary>
        public static int BlitDestinationId => BlitShaderIDs.BlitDestinationId;

        /// <summary>Property ID for the blit scale/bias vector.</summary>
        public static int BlitScaleBiasId => BlitShaderIDs.BlitScaleBiasId;
    }
}