using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Rayforge.Core.Common.Rendering.Helpers
{
    /// <summary>
    /// Central utility for bidirectional conversion and metadata lookup 
    /// between all native Unity texture format types (RenderTextureFormat, TextureFormat, and GraphicsFormat).
    /// </summary>
    public static class TextureFormatResolverExtension
    {
        #region RT -> Texture2D / Graphics

        /// <summary>
        /// Converts a <see cref="RenderTextureFormat"/> to its closest <see cref="TextureFormat"/> equivalent.
        /// </summary>
        /// <param name="format">The source render texture format.</param>
        /// <returns>The matching texture format.</returns>
        public static TextureFormat ToTextureFormat(this RenderTextureFormat format)
        {
            return format switch
            {
                RenderTextureFormat.R8 => TextureFormat.R8,
                RenderTextureFormat.R16 => TextureFormat.R16,
                RenderTextureFormat.RHalf => TextureFormat.RHalf,
                RenderTextureFormat.RFloat => TextureFormat.RFloat,
                RenderTextureFormat.RG16 => TextureFormat.RG16,
                RenderTextureFormat.RGHalf => TextureFormat.RGHalf,
                RenderTextureFormat.RGFloat => TextureFormat.RGFloat,
                RenderTextureFormat.ARGB32 => TextureFormat.RGBA32,
                RenderTextureFormat.ARGBHalf => TextureFormat.RGBAHalf,
                RenderTextureFormat.ARGBFloat => TextureFormat.RGBAFloat,
                RenderTextureFormat.BGRA32 => TextureFormat.BGRA32,
                RenderTextureFormat.Depth => TextureFormat.R16,
                RenderTextureFormat.Shadowmap => TextureFormat.R16,
                _ => throw new NotSupportedException($"No TextureFormat mapping for RT format: {format}")
            };
        }

        /// <summary>
        /// Converts a <see cref="RenderTextureFormat"/> to the modern <see cref="GraphicsFormat"/>.
        /// </summary>
        public static GraphicsFormat ToGraphicsFormat(this RenderTextureFormat format)
        {
            return GraphicsFormatUtility.GetGraphicsFormat(format, RenderTextureReadWrite.Linear);
        }

        #endregion

        #region Texture2D -> RT / Graphics

        /// <summary>
        /// Converts a standard <see cref="TextureFormat"/> to its <see cref="RenderTextureFormat"/> equivalent.
        /// </summary>
        public static RenderTextureFormat ToRenderTextureFormat(this TextureFormat format)
        {
            return format switch
            {
                TextureFormat.R8 => RenderTextureFormat.R8,
                TextureFormat.R16 => RenderTextureFormat.R16,
                TextureFormat.RHalf => RenderTextureFormat.RHalf,
                TextureFormat.RFloat => RenderTextureFormat.RFloat,
                TextureFormat.RG16 => RenderTextureFormat.RG16,
                TextureFormat.RGHalf => RenderTextureFormat.RGHalf,
                TextureFormat.RGFloat => RenderTextureFormat.RGFloat,
                TextureFormat.RGBA32 => RenderTextureFormat.ARGB32,
                TextureFormat.ARGB32 => RenderTextureFormat.ARGB32,
                TextureFormat.RGBAHalf => RenderTextureFormat.ARGBHalf,
                TextureFormat.RGBAFloat => RenderTextureFormat.ARGBFloat,
                TextureFormat.BGRA32 => RenderTextureFormat.BGRA32,
                TextureFormat.RGB565 => RenderTextureFormat.RGB565,
                _ => throw new NotSupportedException($"No RT mapping for Texture format: {format}")
            };
        }

        /// <summary>
        /// Converts a <see cref="TextureFormat"/> to the modern <see cref="GraphicsFormat"/>.
        /// </summary>
        public static GraphicsFormat ToGraphicsFormat(this TextureFormat format)
        {
            return GraphicsFormatUtility.GetGraphicsFormat(format, false);
        }

        #endregion

        #region Graphics -> RT / Texture2D

        /// <summary>
        /// Resolves the <see cref="RenderTextureFormat"/> from a given <see cref="GraphicsFormat"/>.
        /// </summary>
        public static RenderTextureFormat ToRenderTextureFormat(this GraphicsFormat format)
        {
            return GraphicsFormatUtility.GetRenderTextureFormat(format);
        }

        /// <summary>
        /// Resolves the <see cref="TextureFormat"/> from a given <see cref="GraphicsFormat"/>.
        /// </summary>
        public static TextureFormat ToTextureFormat(this GraphicsFormat format)
        {
            return GraphicsFormatUtility.GetTextureFormat(format);
        }

        #endregion

        #region Capabilities & Metadata

        /// <summary>
        /// Determines if the specific <see cref="RenderTextureFormat"/> supports RandomWrite access (UAV).
        /// </summary>
        public static bool SupportsRandomWrite(RenderTextureFormat format)
        {
            return format switch
            {
                RenderTextureFormat.RHalf or
                RenderTextureFormat.RFloat or
                RenderTextureFormat.RGHalf or
                RenderTextureFormat.RGFloat or
                RenderTextureFormat.ARGBHalf or
                RenderTextureFormat.ARGBFloat => true,
                _ => false
            };
        }

        /// <summary>
        /// Returns the number of individual color channels for a given <see cref="TextureFormat"/>.
        /// </summary>
        public static int GetChannelCount(TextureFormat format)
        {
            return format switch
            {
                TextureFormat.Alpha8 or TextureFormat.R8 or TextureFormat.R16 or
                TextureFormat.RHalf or TextureFormat.RFloat => 1,
                TextureFormat.RG16 or TextureFormat.RG32 or TextureFormat.RGHalf or TextureFormat.RGFloat => 2,
                TextureFormat.RGB24 or TextureFormat.RGB565 or TextureFormat.RGB9e5Float or TextureFormat.RGB48 => 3,
                _ => 4
            };
        }

        /// <summary>
        /// Calculates the total number of bits used per pixel for a given <see cref="TextureFormat"/>.
        /// </summary>
        public static int GetBitsPerPixel(TextureFormat format)
        {
            return format switch
            {
                TextureFormat.Alpha8 or TextureFormat.R8 => 8,
                TextureFormat.R16 or TextureFormat.RHalf or TextureFormat.RG16 or TextureFormat.RGB565 or TextureFormat.ARGB4444 or TextureFormat.RGBA4444 => 16,
                TextureFormat.RGB24 => 24,
                TextureFormat.RFloat or TextureFormat.RGHalf or TextureFormat.RGBA32 or TextureFormat.ARGB32 or TextureFormat.BGRA32 or TextureFormat.RG32 or TextureFormat.RGB9e5Float => 32,
                TextureFormat.RGFloat or TextureFormat.RGBAHalf or TextureFormat.RGB48 => 64,
                TextureFormat.RGBA64 or TextureFormat.RGBAFloat => 128,
                _ => 0
            };
        }

        #endregion
    }
}