using System;
using UnityEditor;

namespace Rayforge.Core.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Encapsulates a fixed-size, stack-like group of texture slots for MRT (Multiple Render Targets) 
    /// or multi-input shader configurations. Optimized as a value type to avoid heap allocations.
    /// </summary>
    public struct TextureStack
    {
        /// <summary>
        /// Maximum number of supported texture slots in this stack.
        /// </summary>
        public const int Capacity = 8;

        private int _count;

        // Internal slots stored as explicit fields to maintain fixed memory layout without arrays.
        private TextureMeta _s0, _s1, _s2, _s3, _s4, _s5, _s6, _s7;

        /// <summary>
        /// The number of currently occupied texture slots.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Pushes a texture metadata object onto the next available slot in the stack.
        /// </summary>
        /// <param name="input">The texture metadata to store.</param>
        /// <exception cref="InvalidOperationException">Thrown if the stack has reached its capacity.</exception>
        public void Push(TextureMeta input)
        {
            if (_count >= Capacity)
                throw new InvalidOperationException($"Cannot add more than {Capacity} textures to this stack.");

            switch (_count++)
            {
                case 0: _s0 = input; break;
                case 1: _s1 = input; break;
                case 2: _s2 = input; break;
                case 3: _s3 = input; break;
                case 4: _s4 = input; break;
                case 5: _s5 = input; break;
                case 6: _s6 = input; break;
                case 7: _s7 = input; break;
            }
        }

        /// <summary>
        /// Removes and returns the top texture metadata from the stack.
        /// </summary>
        /// <param name="input">The popped texture metadata if successful; otherwise, default.</param>
        /// <returns>True if a texture was popped; false if the stack was empty.</returns>
        public bool TryPop(out TextureMeta input)
        {
            if (_count == 0) { input = default; return false; }

            _count--;
            switch (_count)
            {
                case 0: input = _s0; _s0 = default; break;
                case 1: input = _s1; _s1 = default; break;
                case 2: input = _s2; _s2 = default; break;
                case 3: input = _s3; _s3 = default; break;
                case 4: input = _s4; _s4 = default; break;
                case 5: input = _s5; _s5 = default; break;
                case 6: input = _s6; _s6 = default; break;
                case 7: input = _s7; _s7 = default; break;
                default: input = default; break;
            }
            return true;
        }

        /// <summary>
        /// Returns the texture metadata at the specified index without removing it.
        /// </summary>
        /// <param name="index">Zero-based index of the slot.</param>
        /// <returns>The texture metadata at the given index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is outside the range [0, 7].</exception>
        public TextureMeta Peek(int index)
        {
            return index switch
            {
                0 => _s0,
                1 => _s1,
                2 => _s2,
                3 => _s3,
                4 => _s4,
                5 => _s5,
                6 => _s6,
                7 => _s7,
                _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range (0-{Capacity - 1}).")
            };
        }

        /// <summary>
        /// Resets the stack count and clears all internal texture references.
        /// </summary>
        public void Clear()
        {
            _s0 = _s1 = _s2 = _s3 = _s4 = _s5 = _s6 = _s7 = default;
            _count = 0;
        }

        /// <summary>
        /// Transfers all texture slots from the source stack to this stack and clears the source.
        /// This is used to move transient pass data between recorder and executor.
        /// </summary>
        /// <param name="other">The source stack to consume from.</param>
        public void ConsumeFrom(ref TextureStack other)
        {
            this = other; // Efficient struct copy
            other.Clear();
        }
    }
}