using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.Diagnostics;
using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Descriptor for a 2D texture, containing resolution, pixel format,
    /// mipmap configuration, and sampling/filtering settings.
    /// Used as the configuration key for texture pooling.
    /// </summary>
    public struct Texture2dDescriptor : IEquatable<Texture2dDescriptor>, ITextureDescriptor
    {
        private int width;
        private int height;
        private TextureFormat colorFormat;
        private int mipCount;
        private bool linear;
        private FilterMode filterMode;
        private TextureWrapMode wrapMode;
        private int anisoLevel;

        /// <summary>Texture width in pixels. Must be > 0.</summary>
        public int Width
        {
            get => width;
            set
            {
                Assertions.AtLeastOne(value, "Width must be greater than zero.");
                width = value;
            }
        }

        /// <summary>Texture height in pixels. Must be > 0.</summary>
        public int Height
        {
            get => height;
            set
            {
                Assertions.AtLeastOne(value, "Height must be greater than zero.");
                height = value;
            }
        }

        /// <summary>
        /// Provides a standard TextureFormat view.
        /// </summary>
        public TextureFormat Format
        {
            get => colorFormat;
            set => colorFormat = value;
        }

        /// <summary>Number of mip levels. Must be >= 1.</summary>
        public int MipCount
        {
            get => mipCount;
            set
            {
                Assertions.AtLeastOne(value, "MipCount must be at least 1.");
                mipCount = value;
            }
        }

        /// <summary>Linear color space flag.</summary>
        public bool Linear { get => linear; set => linear = value; }

        /// <summary>Filtering mode for texture sampling (Point, Bilinear, Trilinear).</summary>
        public FilterMode FilterMode { get => filterMode; set => filterMode = value; }

        /// <summary>Wrap mode for texture addressing (Clamp, Repeat, etc.).</summary>
        public TextureWrapMode WrapMode { get => wrapMode; set => wrapMode = value; }

        /// <summary>Anisotropic filtering level (0 to 16).</summary>
        public int AnisoLevel
        {
            get => anisoLevel;
            set => anisoLevel = Mathf.Clamp(value, 0, 16);
        }

        /// <summary>
        /// Basic constructor for manual initialization.
        /// </summary>
        public Texture2dDescriptor(int width, int height, TextureFormat format, int mipCount = 1, bool linear = true)
        {
            this.width = width;
            this.height = height;
            this.colorFormat = format;
            this.mipCount = Mathf.Max(1, mipCount);
            this.linear = linear;
            this.filterMode = FilterMode.Bilinear;
            this.wrapMode = TextureWrapMode.Clamp;
            this.anisoLevel = 1;

            Validate();
        }

        /// <summary>
        /// Creates a descriptor from a Unity RenderTextureDescriptor.
        /// Useful when you want to create a regular Texture2D that matches a RenderTexture's specs.
        /// </summary>
        /// <param name="rtDesc">The source RenderTexture descriptor.</param>
        public Texture2dDescriptor(RenderTextureDescriptor rtDesc)
        {
            this.width = rtDesc.width;
            this.height = rtDesc.height;
            this.mipCount = Mathf.Max(1, rtDesc.useMipMap ? 1 : 0);
            this.linear = !rtDesc.sRGB;

            this.colorFormat = rtDesc.colorFormat.ToTextureFormat();

            this.filterMode = FilterMode.Bilinear;
            this.wrapMode = TextureWrapMode.Clamp;
            this.anisoLevel = 1;

            Validate();
        }

        /// <summary>
        /// Validates the texture properties to ensure they are compatible with Unity's Texture2D requirements.
        /// </summary>
        public void Validate()
        {
            if (mipCount <= 0) mipCount = 1;
            anisoLevel = Mathf.Clamp(anisoLevel, 0, 16);
        }

        /// <summary>
        /// Compares all descriptor fields for equality, including sampling and aniso settings.
        /// </summary>
        public bool Equals(Texture2dDescriptor other)
            => width == other.width
            && height == other.height
            && colorFormat == other.colorFormat
            && mipCount == other.mipCount
            && linear == other.linear
            && filterMode == other.filterMode
            && wrapMode == other.wrapMode
            && anisoLevel == other.anisoLevel;

        public override bool Equals(object obj)
            => obj is Texture2dDescriptor other && Equals(other);

        /// <summary>
        /// Creates a stable hash code for dictionary lookups (e.g., in a texture pool).
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                (width, height, colorFormat).GetHashCode(),
                (mipCount, linear, filterMode).GetHashCode(),
                (wrapMode, anisoLevel).GetHashCode()
            );
        }

        public static bool operator ==(Texture2dDescriptor left, Texture2dDescriptor right)
            => left.Equals(right);

        public static bool operator !=(Texture2dDescriptor left, Texture2dDescriptor right)
            => !left.Equals(right);
    }
}