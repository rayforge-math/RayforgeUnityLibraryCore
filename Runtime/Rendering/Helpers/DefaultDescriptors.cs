using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Core.Rendering.Helpers
{
    public static class DefaultDescriptors
    {
        /// <summary>
        /// Returns a standard <see cref="RenderTextureDescriptor"/> for depth-only textures,
        /// suitable for Depth Pyramid / High-Z / History buffers that are written via ComputeShaders.
        /// </summary>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <param name="enableRandomWrite">Whether the texture allows random write access by a ComputeShader. Default is <c>true</c>.</param>
        /// <param name="msaaSamples">Number of MSAA samples. Default is 1 (no MSAA). High-Z buffers typically should not use MSAA.</param>
        /// <returns>A configured <see cref="RenderTextureDescriptor"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero or if <paramref name="msaaSamples"/> is less than 1.
        /// </exception>
        public static RenderTextureDescriptor DepthBuffer(int width, int height, bool enableRandomWrite = true, int msaaSamples = 1)
        {
            if (width <= 0) 
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be > 0.");
            if (height <= 0) 
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be > 0.");
            if (msaaSamples < 1)
                throw new ArgumentOutOfRangeException(nameof(msaaSamples), "MSAA samples must be at least 1.");

            return new RenderTextureDescriptor(width, height)
            {
                colorFormat = enableRandomWrite ? RenderTextureFormat.RFloat : RenderTextureFormat.Depth,
                depthBufferBits = 32,
                dimension = TextureDimension.Tex2D,
                useMipMap = false,
                autoGenerateMips = false,
                msaaSamples = msaaSamples,
                sRGB = false,
                enableRandomWrite = enableRandomWrite,
                bindMS = false,
                depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None
            };
        }

        /// <summary>
        /// Returns a depth buffer descriptor matching the current screen resolution.
        /// </summary>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero or if <paramref name="msaaSamples"/> is less than 1.
        /// </exception>
        public static RenderTextureDescriptor DepthBufferFullScreen(bool enableRandomWrite = true, int msaaSamples = 1)
            => DepthBuffer(Screen.width, Screen.height, enableRandomWrite, msaaSamples);

        /// <summary>
        /// Returns a high-precision heightmap descriptor suitable for fog, terrain, or simulation data.
        /// Uses <see cref="RenderTextureFormat.RFloat"/> (32-bit) to prevent "stepping" artifacts in height gradients.
        /// </summary>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <param name="enableRandomWrite">Whether the texture allows random write access by a ComputeShader. Default is <c>true</c>.</param>
        /// <returns>A configured <see cref="RenderTextureDescriptor"/> with 32-bit float precision.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero.</exception>
        public static RenderTextureDescriptor HeightmapPrecision(int width, int height, bool enableRandomWrite = true)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            return new RenderTextureDescriptor(width, height)
            {
                colorFormat = RenderTextureFormat.RFloat,
                depthBufferBits = 0,
                dimension = TextureDimension.Tex2D,
                volumeDepth = 1,
                useMipMap = false,
                autoGenerateMips = false,
                msaaSamples = 1,
                sRGB = false,
                enableRandomWrite = enableRandomWrite
            };
        }

        /// <summary>
        /// Returns an optimized heightmap descriptor suitable for visual effects like ground fog.
        /// Uses <see cref="RenderTextureFormat.R16"/> (16-bit) to provide a good balance between precision and VRAM usage.
        /// </summary>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <param name="enableRandomWrite">Whether the texture allows random write access by a ComputeShader. Default is <c>true</c>.</param>
        /// <returns>A configured <see cref="RenderTextureDescriptor"/> with 16-bit precision.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero.</exception>
        public static RenderTextureDescriptor HeightmapDefault(int width, int height, bool enableRandomWrite = true)
        {
            var desc = HeightmapPrecision(width, height, enableRandomWrite);
            desc.colorFormat = RenderTextureFormat.R16;
            return desc;
        }
    }
}