using System;
using System.Collections.Generic;

using Rayforge.Core.Rendering.Abstractions;

namespace Rayforge.Core.Rendering.Collections
{
    /// <summary>
    /// Generic ping-pong buffer that manages two slots of type <typeparamref name="TResource"/>.
    /// Provides a minimal mechanism to alternate between two logical roles (First/Second), 
    /// allowing for temporal reprojection or double-buffering.
    /// The roles are swapped each time <see cref="Swap"/> is called.
    /// </summary>
    /// <typeparam name="TResource">The type of resource managed (e.g., MipChains, RTHandles, or Buffers).</typeparam>
    public class PingPongBuffer<TResource> : IRenderingCollection<TResource>
    {
        protected readonly TResource[] m_Resources;
        private int m_CurrentIndex;

        /// <summary>
        /// The internal index currently acting as the "First" slot.
        /// </summary>
        public int FirstIndex => m_CurrentIndex;

        /// <summary>
        /// The internal index currently acting as the "Second" slot.
        /// </summary>
        public int SecondIndex => NextIndex(m_CurrentIndex);

        /// <summary>
        /// Gets the resource currently considered the "first" slot (e.g., Current Frame).
        /// After calling <see cref="Swap"/>, this will return the alternate resource.
        /// </summary>
        public TResource First => m_Resources[m_CurrentIndex];

        /// <summary>
        /// Gets the resource currently considered the "second" slot (e.g., History/Previous Frame).
        /// After calling <see cref="Swap"/>, this will return the alternate resource.
        /// </summary>
        public TResource Second => m_Resources[NextIndex(m_CurrentIndex)];

        /// <summary>
        /// Returns a read-only list of the managed resources.
        /// </summary>
        public IReadOnlyList<TResource> Handles => m_Resources;

        /// <summary>
        /// Initializes the <see cref="PingPongBuffer{TResource}"/> with two external resources.
        /// </summary>
        /// <param name="resource0">Initial resource for slot 0.</param>
        /// <param name="resource1">Initial resource for slot 1.</param>
        public PingPongBuffer(TResource resource0, TResource resource1)
        {
            m_Resources = new TResource[2];
            m_Resources[0] = resource0;
            m_Resources[1] = resource1;
            m_CurrentIndex = 0;
        }

        /// <summary>
        /// Replaces the resource currently considered the "first" slot.
        /// </summary>
        /// <param name="resource">New resource to set.</param>
        public void SetFirst(TResource resource) => m_Resources[m_CurrentIndex] = resource;

        /// <summary>
        /// Replaces the resource currently considered the "second" slot.
        /// </summary>
        /// <param name="resource">New resource to set.</param>
        public void SetSecond(TResource resource) => m_Resources[NextIndex(m_CurrentIndex)] = resource;

        /// <summary>
        /// Swaps the logical roles of the two slots.
        /// The resource previously returned by <see cref="First"/> becomes <see cref="Second"/>, and vice versa.
        /// </summary>
        public void Swap() => m_CurrentIndex = NextIndex(m_CurrentIndex);

        /// <summary>
        /// Calculates the alternating index for a two-slot buffer using XOR.
        /// </summary>
        private static int NextIndex(int index) => index ^ 1;

        /// <summary>
        /// Returns the resources as a span for high-performance access.
        /// </summary>
        public ReadOnlySpan<TResource> AsSpan() => m_Resources.AsSpan();

        /// <summary>
        /// Returns a specific range of the managed resources as a span.
        /// </summary>
        /// <param name="index">Starting index (clamped to buffer range).</param>
        /// <param name="count">Number of elements (clamped to buffer range).</param>
        public ReadOnlySpan<TResource> AsSpan(int index, int count)
        {
            index = Math.Clamp(index, 0, 1);
            count = Math.Clamp(count, 1, 2 - index);
            return m_Resources.AsSpan(index, count);
        }
    }
}