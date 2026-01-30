using Rayforge.Core.ShaderExtensions.Blitter;
using System;
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
        private TextureStack m_InputStack;
        private TextureStack m_DestinationStack;

        public int InputCount => m_InputStack.Count;
        public int DestinationCount => m_DestinationStack.Count;

        /// <summary>
        /// Maximum number of supported input textures.
        /// </summary>
        public const int InputCapacity = TextureStack.Capacity;

        /// <summary>
        /// Legacy support for single destination. 
        /// Acts as a wrapper for the first MRT slot (index 0).
        /// </summary>
        public TextureMeta Destination
        {
            get
            {
                return m_DestinationStack.Count > 0 ? m_DestinationStack.Peek(0) : default;
            }
            set
            {
                m_DestinationStack.Clear();
                m_DestinationStack.Push(value);
            }
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
            m_InputStack.ConsumeFrom(ref other.m_InputStack);
            m_DestinationStack.ConsumeFrom(ref other.m_DestinationStack);

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
            => m_DestinationStack.Push(new TextureMeta { handle = handle, propertyId = propertyId });

        /// <summary>
        /// Returns the last added destination and marks it as consumed (Popped).
        /// </summary>
        /// <param name="destination">The texture metadata of the last destination.</param>
        /// <returns>True if a destination was returned; false if the stack is empty.</returns>
        public bool TryPopDestination(out TextureMeta destination)
            => m_DestinationStack.TryPop(out destination);

        /// <summary>
        /// Attempts to peek at the destination texture at the specified MRT index without consuming it.
        /// </summary>
        /// <param name="index">Zero-based index of the destination slot (0-7, corresponds to SV_Target0-7).</param>
        /// <param name="destination">The texture metadata at the slot, if valid.</param>
        /// <returns>True if a valid destination exists at the specified index; otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range [0-7].</exception>
        public bool TryPeekDestination(int index, out TextureMeta destination)
        {
            destination = m_DestinationStack.Peek(index);
            return destination.handle.IsValid();
        }

        /// <summary>
        /// Adds an input texture to the next available slot.
        /// This input will be consumed by the RenderGraph when the pass is submitted.
        /// After consumption, the slot will be cleared internally.
        /// </summary>
        /// <param name="input">The texture metadata to add as input.</param>
        /// <exception cref="InvalidOperationException">Thrown if input slots are full.</exception>
        public void PushInput(TextureMeta input)
            => m_InputStack.Push(input);

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
        /// Returns the last input added and marks it as consumed.
        /// </summary>
        /// <param name="input">The texture metadata of the last input.</param>
        /// <returns>True if an input was returned; false if no inputs remain.</returns>
        public bool TryPopInput(out TextureMeta input)
            => m_InputStack.TryPop(out input);

        /// <summary>
        /// Attempts to peek at the input texture at the specified index without consuming it.
        /// </summary>
        /// <param name="index">Zero-based index of the input slot (0-7).</param>
        /// <param name="input">The texture metadata at the slot, if valid.</param>
        /// <returns>True if an input exists at the specified index; otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range [0-7].</exception>
        public bool TryPeekInput(int index, out TextureMeta input)
        {
            input = m_InputStack.Peek(index);
            return input.handle.IsValid();
        }
    }
}
