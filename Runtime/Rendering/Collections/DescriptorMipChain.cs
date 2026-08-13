using Rayforge.Core.Rendering.Collections.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Rendering.Collections
{
    /// <summary>
    /// Manages a chain of <see cref="RenderTextureDescriptor"/> instances for multiple mip levels.
    /// The resolution calculation is delegated to <see cref="MipChainLayout"/>, decoupling this class from RenderGraph.
    /// Supports dynamic resolution, mip count, and format changes.
    /// </summary>
    public sealed class DescriptorMipChain
    {
        #region Fields and Properties

        private MipChainLayout m_Layout;
        private RenderTextureDescriptor[] m_Descriptors;
        private RenderTextureFormat m_Format;

        /// <summary>Read-only access to the mip level descriptors.</summary>
        public IReadOnlyList<RenderTextureDescriptor> Descriptors => m_Descriptors;

        /// <summary>Access a specific mip level descriptor by index.</summary>
        /// <param name="index">The mip level index.</param>
        /// <returns>The <see cref="RenderTextureDescriptor"/> for the given mip level.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
        public RenderTextureDescriptor this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Descriptors.Length)
                    throw new ArgumentOutOfRangeException(nameof(index), "Mip index is out of range.");
                return m_Descriptors[index];
            }
        }

        /// <summary>The number of mip levels in this chain.</summary>
        public int MipCount
        {
            get => m_Layout.MipCount;
            set => UpdateMipCount(value);
        }

        /// <summary>The base resolution (mip 0) of the chain.</summary>
        public Vector2Int Resolution
        {
            get => m_Layout.BaseResolution;
            set => UpdateBaseResolution(value);
        }

        /// <summary>Width of the base resolution (mip 0).</summary>
        public int Width
        {
            get => m_Layout.BaseResolution.x;
            set => UpdateBaseResolution(new Vector2Int(value, m_Layout.BaseResolution.y));
        }

        /// <summary>Height of the base resolution (mip 0).</summary>
        public int Height
        {
            get => m_Layout.BaseResolution.y;
            set => UpdateBaseResolution(new Vector2Int(m_Layout.BaseResolution.x, value));
        }

        /// <summary>Format used for all descriptors in the chain.</summary>
        public RenderTextureFormat Format
        {
            get => m_Format;
            set => UpdateFormat(value);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new mip chain with the given base resolution, mip count, optional custom mip resolution function, and format.
        /// </summary>
        /// <param name="width">Base resolution (mip 0) in x dimension.</param>
        /// <param name="height">Base resolution (mip 0) in y dimension.</param>
        /// <param name="mipCount">Number of mip levels.</param>
        /// <param name="mipFunc">Optional custom mip resolution function.</param>
        /// <param name="format">Render texture format to use for all descriptors.</param>
        public DescriptorMipChain(int width, int height, int mipCount = 1, MipCreateFunc mipFunc = null, RenderTextureFormat format = RenderTextureFormat.Default)
            : this(new MipChainLayout(new Vector2Int(width, height), mipCount, mipFunc ?? MipChainHelpers.DefaultMipResolution), format)
        { }

        /// <summary>
        /// Creates a new mip chain with the given base resolution, mip count, optional custom mip resolution function, and format.
        /// </summary>
        /// <param name="baseResolution">Base resolution (mip 0).</param>
        /// <param name="mipCount">Number of mip levels.</param>
        /// <param name="mipFunc">Optional custom mip resolution function.</param>
        /// <param name="format">Render texture format to use for all descriptors.</param>
        public DescriptorMipChain(Vector2Int baseResolution, int mipCount = 1, MipCreateFunc mipFunc = null, RenderTextureFormat format = RenderTextureFormat.Default)
            : this(new MipChainLayout(baseResolution, mipCount, mipFunc ?? MipChainHelpers.DefaultMipResolution), format)
        { }

        /// <summary>
        /// Creates a new mip chain with the given base resolution, mip count, optional custom mip resolution function, and format.
        /// </summary>
        /// <param name="mipChainLayout"><see cref="MipChainLayout"/> defining the mip chain.</param>
        /// <param name="format">Render texture format to use for all descriptors.</param>
        private DescriptorMipChain(MipChainLayout mipChainLayout, RenderTextureFormat format = RenderTextureFormat.Default)
        {
            m_Layout = mipChainLayout;
            m_Format = format;
            m_Descriptors = new RenderTextureDescriptor[m_Layout.MipCount];
            InitDescriptors();
        }

        #endregion

        #region Initialization & Management

        /// <summary>
        /// Initializes or refreshes all mip level descriptors based on the current layout and format.
        /// </summary>
        private void InitDescriptors()
        {
            for (int i = 0; i < m_Layout.MipCount; i++)
            {
                Vector2Int res = m_Layout.GetResolution(i);
                m_Descriptors[i] = new RenderTextureDescriptor(res.x, res.y, m_Format, 0);
            }
        }

        /// <summary>
        /// Updates the base resolution and recalculates all descriptors.
        /// </summary>
        /// <param name="newRes">New base resolution.</param>
        /// <exception cref="ArgumentException">Thrown if width or height is less than or equal to 0.</exception>
        public void UpdateBaseResolution(Vector2Int newRes)
        {
            if (newRes.x <= 0 || newRes.y <= 0)
                throw new ArgumentException("Base resolution must be greater than 0", nameof(newRes));

            if (m_Layout.BaseResolution != newRes)
            {
                m_Layout = new MipChainLayout(newRes, m_Layout.MipCount, m_Layout.MipFunc);
                InitDescriptors();
            }
        }

        /// <summary>
        /// Updates the number of mip levels in the chain and refreshes all descriptors.
        /// </summary>
        /// <param name="newMipCount">New mip count.</param>
        /// <exception cref="ArgumentException">Thrown if mip count is less than or equal to 0.</exception>
        public void UpdateMipCount(int newMipCount)
        {
            if (newMipCount <= 0)
                throw new ArgumentException("Mip count must be greater than 0", nameof(newMipCount));

            if (m_Layout.MipCount != newMipCount)
            {
                m_Layout = new MipChainLayout(m_Layout.BaseResolution, newMipCount, m_Layout.MipFunc);
                Array.Resize(ref m_Descriptors, newMipCount);
                InitDescriptors();
            }
        }

        /// <summary>
        /// Updates the render texture format for all descriptors in the chain.
        /// </summary>
        /// <param name="newFormat">New <see cref="RenderTextureFormat"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the format is not defined.</exception>
        private void UpdateFormat(RenderTextureFormat newFormat)
        {
            if (!Enum.IsDefined(typeof(RenderTextureFormat), newFormat))
                throw new ArgumentOutOfRangeException(nameof(newFormat), "Invalid render texture format.");

            if (m_Format != newFormat)
            {
                m_Format = newFormat;
                for (int i = 0; i < m_Descriptors.Length; i++)
                    m_Descriptors[i].colorFormat = m_Format;
            }
        }

        #endregion

        #region Span Utilities

        /// <summary>
        /// Returns a read-only span of descriptors.
        /// </summary>
        public ReadOnlySpan<RenderTextureDescriptor> AsSpan()
            => m_Descriptors.AsSpan(0, MipCount);

        /// <summary>
        /// Returns a read-only span of descriptors.
        /// </summary>
        /// <param name="start">Start index of the span.</param>
        /// <param name="length">Number of elements in the span.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="start"/> or <paramref name="length"/> is out of range.
        /// </exception>
        public ReadOnlySpan<RenderTextureDescriptor> AsSpan(int start, int length)
        {
            if (start < 0 || start > MipCount)
                throw new ArgumentOutOfRangeException(nameof(start), "Start index is out of range.");

            if (length < 0 || start + length > MipCount)
                throw new ArgumentOutOfRangeException(nameof(length), "Length is out of range.");

            return m_Descriptors.AsSpan(start, length);
        }

        #endregion
    }
}