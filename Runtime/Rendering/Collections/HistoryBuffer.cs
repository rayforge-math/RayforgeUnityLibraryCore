namespace Rayforge.Core.Rendering.Collections
{
    /// <summary>
    /// Manages a pair of persistent resources for frame-over-frame operations.
    /// One resource represents the current target (write), while the other holds the previous frame's data (read).
    /// Suitable for temporal effects like reprojection, motion blur, or any frame-history dependent process.
    /// </summary>
    /// <typeparam name="TResource">The type of resource managed (e.g., MipChains, RTHandles, or Buffers).</typeparam>
    public class HistoryBuffer<TResource> : PingPongBuffer<TResource>
    {
        /// <summary>
        /// Gets the resource used as the current frame's target (usually for writing).
        /// </summary>
        public TResource Target => First;

        /// <summary>
        /// Gets the resource containing the previous frame's data (usually for reading).
        /// </summary>
        public TResource History => Second;

        /// <summary>
        /// Gets the internal array index for the current frame's target.
        /// </summary>
        public int TargetIndex => FirstIndex;

        /// <summary>
        /// Gets the internal array index for the history data.
        /// </summary>
        public int HistoryIndex => SecondIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryBuffer{TResource}"/> class.
        /// </summary>
        /// <param name="initial0">Initial resource for the target slot.</param>
        /// <param name="initial1">Initial resource for the history slot.</param>
        public HistoryBuffer(TResource initial0, TResource resource1)
            : base(initial0, resource1)
        { }

        /// <summary>
        /// Updates the current target resource.
        /// </summary>
        public void SetTarget(TResource resource) => SetFirst(resource);

        /// <summary>
        /// Updates the history resource.
        /// </summary>
        public void SetHistory(TResource resource) => SetSecond(resource);
    }
}