using UnityEngine;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Defines a contract for data structures that possess a specific 'invalid' or 'default-empty' state.
    /// Useful for identifying entries in GPU buffers that do not contain valid scene information.
    /// </summary>
    /// <typeparam name="T">The unmanaged type representing the data structure.</typeparam>
    public interface IGpuData<T>
        where T : unmanaged
    {
        /// <summary>
        /// Returns true if the entry is considered valid, false otherwise.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Returns the sentinel value representing an invalid or inactive entry in the buffer.
        /// Used to initialize unused slots or explicitly mark data as released for GPU-side culling.
        /// </summary>
        /// <returns>A valid instance of <typeparamref name="T"/> containing invalid state markers.</returns>
        T InvalidData();
    }
}
