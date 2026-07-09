using System;
using UnityEngine;

namespace Rayforge.Core.Common.Rendering
{
    /// <summary>
    /// Lightweight per-frame execution guard.
    /// Ensures a code path runs at most once per rendered frame.
    /// </summary>
    /// <remarks>
    /// Intended for systems that may be invoked multiple times per frame
    /// but must update state or upload data only once.
    /// 
    /// Each system must own its own instance.
    /// This struct holds no global state and relies on <see cref="Time.frameCount"/> 
    /// by default, but supports custom providers for unit testing.
    /// 
    /// Note: Do not copy this struct or pass it by value, as it maintains 
    /// internal state. Store it as a persistent field.
    /// </remarks>
    public struct FrameOnce
    {
        private int _lastFrame;
        private readonly Func<int> _frameProvider;

        /// <summary>
        /// Returns the last stored frame count.
        /// </summary>
        public int LastFrame => _lastFrame;

        /// <summary>
        /// Initializes a new instance of <see cref="FrameOnce"/>.
        /// </summary>
        /// <param name="frameProvider">
        /// Optional delegate to retrieve the current frame index. 
        /// Defaults to <see cref="Time.frameCount"/> if null.
        /// </param>
        public FrameOnce(Func<int> frameProvider = null)
        {
            _lastFrame = -1;
            _frameProvider = frameProvider ?? (() => Time.frameCount);
        }

        /// <summary>
        /// Attempts to initiate a per-frame execution.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if this is the first call in the current frame; 
        /// otherwise, returns <c>false</c>.
        /// </returns>
        public bool TryBegin()
        {
            int frame = _frameProvider();

            if (_lastFrame == frame)
                return false;

            _lastFrame = frame;
            return true;
        }
    }
}