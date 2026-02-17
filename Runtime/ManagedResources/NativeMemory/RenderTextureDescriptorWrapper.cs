using Rayforge.Core.Diagnostics;
using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Wrapper around Unity's <see cref="RenderTextureDescriptor"/> to provide
    /// value-based comparison and hashing for use in dictionaries and pools.
    /// Includes sampling settings like <see cref="FilterMode"/> and <see cref="TextureWrapMode"/>.
    /// </summary>
    public struct RenderTextureDescriptorWrapper : IEquatable<RenderTextureDescriptorWrapper>, ITextureDescriptor
    {
        /// <summary>The underlying Unity descriptor.</summary>
        private RenderTextureDescriptor descriptor;

        /// <summary>Filter mode for sampling (Point, Bilinear, Trilinear).</summary>
        public FilterMode FilterMode;

        /// <summary>Wrap mode for texture coordinates (Clamp, Repeat, etc.).</summary>
        public TextureWrapMode WrapMode;

        /// <summary>Anisotropic filtering level.</summary>
        public int AnisoLevel;

        /// <summary>
        /// Gets or sets the underlying <see cref="RenderTextureDescriptor"/>.
        /// Setting this will trigger validation of dimensions and samples.
        /// </summary>
        public RenderTextureDescriptor Descriptor
        {
            get => descriptor;
            set
            {
                descriptor = value;
                Validate();
            }
        }

        /// <summary>The width of the render texture. Must be > 0.</summary>
        public int Width
        {
            get => descriptor.width;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Width must be > 0.");
                descriptor.width = value;
            }
        }

        /// <summary>The height of the render texture. Must be > 0.</summary>
        public int Height
        {
            get => descriptor.height;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Height must be > 0.");
                descriptor.height = value;
            }
        }

        /// <summary>
        /// Provides a standard TextureFormat view, though GraphicsFormat is preferred internally.
        /// </summary>
        public TextureFormat Format
        {
            get => (TextureFormat)descriptor.colorFormat;
            set => descriptor.colorFormat = (RenderTextureFormat)value;
        }

        public RenderTextureFormat ColorFormat { get => descriptor.colorFormat; set => descriptor.colorFormat = value; }
        public GraphicsFormat GraphicsFormat { get => descriptor.graphicsFormat; set => descriptor.graphicsFormat = value; }

        /// <summary>Depth buffer bits (0, 16, 24, 32).</summary>
        public int DepthBufferBits
        {
            get => descriptor.depthBufferBits;
            set
            {
                if (value != 0 && value != 16 && value != 24 && value != 32)
                    throw new ArgumentException("DepthBufferBits must be 0, 16, 24, or 32.");
                descriptor.depthBufferBits = value;
            }
        }

        public TextureDimension Dimension { get => descriptor.dimension; set => descriptor.dimension = value; }
        public int VolumeDepth { get => descriptor.volumeDepth; set => descriptor.volumeDepth = Mathf.Max(1, value); }
        public int MSAASamples { get => descriptor.msaaSamples; set => descriptor.msaaSamples = Mathf.Clamp(value, 1, 8); }

        public bool UseMipMap { get => descriptor.useMipMap; set => descriptor.useMipMap = value; }
        public bool AutoGenerateMips { get => descriptor.autoGenerateMips; set => descriptor.autoGenerateMips = value; }
        public bool EnableRandomWrite { get => descriptor.enableRandomWrite; set => descriptor.enableRandomWrite = value; }
        public bool UseDynamicScale { get => descriptor.useDynamicScale; set => descriptor.useDynamicScale = value; }
        public bool SRGB { get => descriptor.sRGB; set => descriptor.sRGB = value; }
        public bool BindMS { get => descriptor.bindMS; set => descriptor.bindMS = value; }

        /// <summary>
        /// Compares this wrapper with another for equality, including sampling settings.
        /// </summary>
        public bool Equals(RenderTextureDescriptorWrapper other)
        {
            return Equals(other.descriptor) &&
                   FilterMode == other.FilterMode &&
                   WrapMode == other.WrapMode &&
                   AnisoLevel == other.AnisoLevel;
        }

        /// <summary>
        /// Compares this wrapper's descriptor with a raw <see cref="RenderTextureDescriptor"/>.
        /// Sampling settings are ignored in this specific overload.
        /// </summary>
        public bool Equals(RenderTextureDescriptor other)
        {
            return
                other.width == descriptor.width &&
                other.height == descriptor.height &&
                other.colorFormat == descriptor.colorFormat &&
                other.graphicsFormat == descriptor.graphicsFormat &&
                other.depthBufferBits == descriptor.depthBufferBits &&
                other.dimension == descriptor.dimension &&
                other.volumeDepth == descriptor.volumeDepth &&
                other.msaaSamples == descriptor.msaaSamples &&
                other.useMipMap == descriptor.useMipMap &&
                other.autoGenerateMips == descriptor.autoGenerateMips &&
                other.enableRandomWrite == descriptor.enableRandomWrite &&
                other.useDynamicScale == descriptor.useDynamicScale &&
                other.sRGB == descriptor.sRGB &&
                other.bindMS == descriptor.bindMS;
        }

        public override bool Equals(object obj) => obj is RenderTextureDescriptorWrapper other && Equals(other);

        /// <summary>
        /// Creates a stable hash code based on all descriptor properties and sampling modes.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                GetRawDescriptorHash(),
                (int)FilterMode,
                (int)WrapMode,
                AnisoLevel
            );
        }

        /// <summary>
        /// Generates a hash for the fields within the Unity <see cref="RenderTextureDescriptor"/>.
        /// </summary>
        private int GetRawDescriptorHash()
        {
            var hash = new HashCode();
            hash.Add(descriptor.width);
            hash.Add(descriptor.height);
            hash.Add(descriptor.colorFormat);
            hash.Add(descriptor.depthBufferBits);
            hash.Add(descriptor.dimension);
            hash.Add(descriptor.volumeDepth);
            hash.Add(descriptor.msaaSamples);
            hash.Add(descriptor.useMipMap);
            hash.Add(descriptor.enableRandomWrite);
            hash.Add(descriptor.sRGB);
            return hash.ToHashCode();
        }

        public static bool operator ==(RenderTextureDescriptorWrapper left, RenderTextureDescriptorWrapper right) => left.Equals(right);
        public static bool operator !=(RenderTextureDescriptorWrapper left, RenderTextureDescriptorWrapper right) => !left.Equals(right);

        /// <summary>
        /// Ensures that the underlying Unity descriptor has valid default values.
        /// </summary>
        private void Validate()
        {
            if (descriptor.volumeDepth <= 0) descriptor.volumeDepth = 1;
            if (descriptor.msaaSamples <= 0) descriptor.msaaSamples = 1;
        }
    }
}