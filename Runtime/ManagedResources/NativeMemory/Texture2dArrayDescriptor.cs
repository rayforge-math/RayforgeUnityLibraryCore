using Rayforge.Core.Diagnostics;
using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Descriptor for a 2D texture array, including the base texture descriptor
    /// and the number of array slices. Acts as a hashing key for texture array pooling.
    /// </summary>
    public struct Texture2dArrayDescriptor : IEquatable<Texture2dArrayDescriptor>, IArrayDescriptor, ITextureDescriptor
    {
        private Texture2dDescriptor descriptor;
        private int count;

        /// <summary>
        /// Descriptor that defines width, height, format and sampling settings
        /// for each texture in the array.
        /// </summary>
        public Texture2dDescriptor SliceDescriptor
        {
            get => descriptor;
            set
            {
                descriptor = value;
                descriptor.Validate();
            }
        }

        /// <summary>
        /// Number of texture layers in the array. Must be > 0.
        /// </summary>
        public int Count
        {
            get => count;
            set
            {
                Assertions.AtLeastOne(value, "Count must be greater than zero.");
                count = value;
            }
        }

        /// <summary>The width of the render texture. Must be > 0.</summary>
        public int Width
        {
            get => descriptor.Width;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Width must be > 0.");
                descriptor.Width = value;
            }
        }

        /// <summary>The height of the render texture. Must be > 0.</summary>
        public int Height
        {
            get => descriptor.Height;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Height must be > 0.");
                descriptor.Height = value;
            }
        }

        /// <summary>
        /// Provides a standard TextureFormat view.
        /// </summary>
        public TextureFormat Format
        {
            get => descriptor.Format;
            set => descriptor.Format = value;
        }

        /// <summary>
        /// Copies all fields from another descriptor, applying assertions.
        /// </summary>
        public void CopyFrom(Texture2dArrayDescriptor other)
        {
            SliceDescriptor = other.SliceDescriptor;
            Count = other.Count;
        }

        /// <summary>
        /// Compares both the inner descriptor and the array layer count.
        /// </summary>
        public bool Equals(Texture2dArrayDescriptor other)
            => descriptor.Equals(other.descriptor) && count == other.count;

        /// <summary>
        /// Ensures compatibility with object-based comparisons.
        /// </summary>
        public override bool Equals(object obj)
            => obj is Texture2dArrayDescriptor other && Equals(other);

        /// <summary>
        /// Computes a stable hash for dictionary / hash set usage.
        /// </summary>
        public override int GetHashCode()
            => (descriptor, count).GetHashCode();

        public static bool operator ==(Texture2dArrayDescriptor left, Texture2dArrayDescriptor right)
            => left.Equals(right);

        public static bool operator !=(Texture2dArrayDescriptor left, Texture2dArrayDescriptor right)
            => !left.Equals(right);
    }
}