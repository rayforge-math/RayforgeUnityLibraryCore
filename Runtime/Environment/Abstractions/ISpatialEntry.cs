using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Minimum requirements for an object to be managed by a SpatialRegistry.
    /// </summary>
    public interface ISpatialEntry
    {
        bool IsDirty();
        void ClearDirty();
        // Provides access to the underlying GameObject for lifecycle management
        GameObject gameObject { get; }
    }
}
