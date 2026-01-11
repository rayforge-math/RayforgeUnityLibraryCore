namespace Rayforge.Core.Rendering.Collections
{
    /// <summary>
    /// Represents a source–destination pair of handles for a single mip level transition
    /// within a mip chain.
    ///
    /// <para>
    /// This structure is typically used when iterating over a mip chain to generate
    /// lower-resolution mip levels (e.g. for High-Z, depth pyramids, or custom mip generation).
    /// </para>
    ///
    /// <para>
    /// The <see cref="Source"/> handle refers to the previous mip level (<c>mipLevel - 1</c>),
    /// while <see cref="Destination"/> refers to the current mip level (<c>mipLevel</c>).
    /// </para>
    /// </summary>
    /// <typeparam name="THandle">
    /// The handle type stored in the mip chain (e.g. <see cref="RTHandle"/>).
    /// </typeparam>
    public readonly struct MipPair<THandle>
    {
        /// <summary>
        /// Handle of the source mip level (usually <c>mipLevel - 1</c>).
        /// </summary>
        public readonly THandle Source;

        /// <summary>
        /// Handle of the destination mip level (usually <c>mipLevel</c>).
        /// </summary>
        public readonly THandle Destination;

        /// <summary>
        /// Index of the destination mip level within the mip chain.
        /// </summary>
        public readonly int MipLevel;

        /// <summary>
        /// Initializes a new <see cref="MipPair{THandle}"/> representing a transition
        /// from one mip level to the next.
        /// </summary>
        /// <param name="src">Handle of the source mip level.</param>
        /// <param name="dst">Handle of the destination mip level.</param>
        /// <param name="mip">Index of the destination mip level.</param>
        public MipPair(THandle src, THandle dst, int mip)
        {
            Source = src;
            Destination = dst;
            MipLevel = mip;
        }
    }
}