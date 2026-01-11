using Rayforge.Core.ShaderExtensions.Blitter;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Core.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Base class for RenderGraph pass input/output configuration and pass metadata.
    /// Supports up to 8 input textures directly.
    /// </summary>
    /// <remarks>
    /// Input textures are treated as transient, one-shot commands.
    /// They are consumed by the RenderGraph recorder when the pass is submitted,
    /// allowing a single pass data instance to be reused safely without explicit resets.
    /// </remarks>
    /// <typeparam name="TDerived">Type of the derived class (CRTP).</typeparam>
    /// <typeparam name="TMeta">Type of the pass metadata (e.g., compute or raster meta).</typeparam>
    public abstract class PassDataBase<TDerived, TMeta> : IDisposable
        where TDerived : PassDataBase<TDerived, TMeta>
        where TMeta : struct
    {
        // --- Fixed input slots ---
        private TextureMeta _input0;
        private TextureMeta _input1;
        private TextureMeta _input2;
        private TextureMeta _input3;
        private TextureMeta _input4;
        private TextureMeta _input5;
        private TextureMeta _input6;
        private TextureMeta _input7;

        private int _inputCount = 0;

        /// <summary>
        /// The number of currently pushed input textures in this pass.
        /// <para>
        /// This value is automatically updated when pushing or consuming inputs.
        /// </para>
        /// </summary>
        public int InputCount => _inputCount;

        /// <summary>
        /// Maximum number of supported input textures.
        /// </summary>
        public const int InputCapacity = 8;

        private TextureMeta m_Destination;
        /// <summary>
        /// Gets or sets the destination texture that this pass writes into.
        /// </summary>
        public TextureMeta Destination
        {
            get => m_Destination;
            set => m_Destination = value;
        }

        private TMeta m_PassMeta;
        /// <summary>
        /// Gets or sets the metadata describing this pass (e.g., shader, kernel, material).
        /// </summary>
        public TMeta PassMeta
        {
            get => m_PassMeta;
            set => m_PassMeta = value;
        }

        /// <summary>
        /// Releases any allocated resources. Override in derived types if needed.
        /// </summary>
        public virtual void Dispose() { }

        /// <summary>
        /// Fetches configuration values from another pass data instance.
        /// <para>
        /// This consumes all input textures from <paramref name="other"/> and resets them in the source.
        /// The destination texture is also transferred and cleared in the source.
        /// Pass metadata is copied but not reset, as it is usually constant per pass type.
        /// </para>
        /// </summary>
        /// <param name="other">The pass data instance to fetch from.</param>
        public void FetchFrom(PassDataBase<TDerived, TMeta> other)
        {
            ConsumeInputsFrom(other);

            m_Destination = other.m_Destination;
            other.m_Destination = default;

            m_PassMeta = other.m_PassMeta;
        }

        /// <summary>
        /// Override this in a derived pass data type to copy any additional fields.
        /// If not overridden, these fields will not be present during render pass dispatch.
        /// </summary>
        /// <remarks>
        /// The precise derived type <see cref="TDerived"/> is known at compile time,
        /// so the compiler can resolve this call directly without using a vtable.
        /// </remarks>
        public abstract void CopyUserData(TDerived other);

        /// <summary>
        /// Sets the destination texture using a RenderGraph handle and optional shader property ID.
        /// This slot will be consumed by the RenderGraph when the pass is submitted.
        /// After consumption, the destination will be cleared internally.
        /// </summary>
        /// <param name="handle">The texture handle to write into.</param>
        /// <param name="propertyId">Optional shader property ID to bind the texture to.</param>
        public void PushDestination(TextureHandle handle, int propertyId = 0)
            => Destination = new TextureMeta { handle = handle, propertyId = propertyId };

        /// <summary>
        /// Adds an input texture to the next available slot.
        /// This input will be consumed by the RenderGraph when the pass is submitted.
        /// After consumption, the slot will be cleared internally.
        /// </summary>
        /// <param name="input">The texture metadata to add as input.</param>
        /// <exception cref="InvalidOperationException">Thrown if input slots are full.</exception>
        public void PushInput(TextureMeta input)
        {
            if (_inputCount >= InputCapacity)
                throw new InvalidOperationException($"Cannot add more than {InputCapacity} inputs.");

            switch (_inputCount++)
            {
                case 0: _input0 = input; break;
                case 1: _input1 = input; break;
                case 2: _input2 = input; break;
                case 3: _input3 = input; break;
                case 4: _input4 = input; break;
                case 5: _input5 = input; break;
                case 6: _input6 = input; break;
                case 7: _input7 = input; break;
            }
        }

        /// <summary>
        /// Sets an input texture at the first available slot using a shader property ID and texture handle.
        /// This input will be consumed by the RenderGraph when the pass is submitted.
        /// After consumption, the slot will be cleared internally.
        /// </summary>
        /// <param name="handle">RenderGraph texture handle.</param>
        /// <param name="propertyId">Shader property ID to bind the texture to.</param>
        public void PushInput(TextureHandle handle, int propertyId)
            => PushInput(new TextureMeta { propertyId = propertyId, handle = handle });

        /// <summary>
        /// Sets the first input texture (index 0) using the default blit property ID.
        /// This input will be consumed by the RenderGraph when the pass is submitted.
        /// After consumption, the slot will be cleared internally.
        /// </summary>
        /// <param name="handle">RenderGraph texture handle.</param>
        public void PushInput(TextureHandle handle)
            => PushInput(new TextureMeta { propertyId = BlitParameters.BlitTextureId, handle = handle });

        /// <summary>
        /// Consumes all input textures from the given source pass data.
        /// Each input slot is copied to this instance and then cleared in the source.
        /// </summary>
        /// <param name="source">The source pass data instance to consume inputs from.</param>
        private void ConsumeInputsFrom(PassDataBase<TDerived, TMeta> source)
        {
            _input0 = source._input0; source._input0 = default;
            _input1 = source._input1; source._input1 = default;
            _input2 = source._input2; source._input2 = default;
            _input3 = source._input3; source._input3 = default;
            _input4 = source._input4; source._input4 = default;
            _input5 = source._input5; source._input5 = default;
            _input6 = source._input6; source._input6 = default;
            _input7 = source._input7; source._input7 = default;

            _inputCount = source._inputCount;
            source._inputCount = 0;
        }

        /// <summary>
        /// Returns the last input added and marks it as consumed.
        /// </summary>
        /// <param name="input">The texture metadata of the last input.</param>
        /// <returns>True if an input was returned; false if no inputs remain.</returns>
        public bool TryPopInput(out TextureMeta input)
        {
            if (_inputCount == 0)
            {
                input = default;
                return false;
            }

            _inputCount--;
            switch (_inputCount)
            {
                case 0: input = _input0; _input0 = default; break;
                case 1: input = _input1; _input1 = default; break;
                case 2: input = _input2; _input2 = default; break;
                case 3: input = _input3; _input3 = default; break;
                case 4: input = _input4; _input4 = default; break;
                case 5: input = _input5; _input5 = default; break;
                case 6: input = _input6; _input6 = default; break;
                case 7: input = _input7; _input7 = default; break;
                default: input = default; break; // safety
            }
            return true;
        }

        /// <summary>
        /// Attempts to peek at the input texture at the specified index without consuming it.
        /// </summary>
        /// <param name="index">Zero-based index of the input slot (0-7).</param>
        /// <param name="input">The texture metadata at the slot, if valid.</param>
        /// <returns>True if an input exists at the specified index; otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range [0-7].</exception>
        public bool TryPeekInput(int index, out TextureMeta input)
        {
            input = index switch
            {
                0 => _input0,
                1 => _input1,
                2 => _input2,
                3 => _input3,
                4 => _input4,
                5 => _input5,
                6 => _input6,
                7 => _input7,
                _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range (0-{InputCapacity - 1}).")
            };

            return input.handle.IsValid();
        }
    }
}
