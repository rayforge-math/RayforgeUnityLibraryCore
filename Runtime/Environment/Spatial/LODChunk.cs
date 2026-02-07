using Rayforge.Core.Environment.Abstractions;
using System;

namespace Rayforge.Core.Environment.Spatial
{
    public abstract class LODChunk<T> : Chunk<T>, ILODState
        where T : LODChunk<T>
    {
        /// <summary>
        /// The current Level of Detail index. Initialized to -1 to ensure the first update triggers.
        /// Used by the Registry to determine if an update is necessary.
        /// </summary>
        public int CurrentLOD { get; private set; } = -1;

        public event Action<ILODState, int, int> OnLODChanged;

        /// <summary>
        /// Updates the LOD state and marks the chunk as dirty for the baking system.
        /// Triggered by LODChunkRegistry. Changing the LOD usually requires a resolution change.
        /// </summary>
        /// <param name="newLod">The new LOD level calculated by the registry.</param>
        public bool UpdateLOD(int newLod)
        {
            if (CurrentLOD == newLod) return false;

            int oldLod = CurrentLOD;
            CurrentLOD = newLod;

            OnLODChanged?.Invoke(this, oldLod, newLod);
            MarkDirty();

            return true;
        }

        /// <summary>
        /// Optional hook for children to react to LOD changes 
        /// (e.g., resizing internal arrays or buffers).
        /// </summary>
        protected abstract void OnLODChangedInternal(int oldLod, int newLod);

        /// <summary>
        /// Cleans up GPU resources when the chunk is removed.
        /// Essential to prevent VRAM leaks when chunks are pooled or destroyed.
        /// </summary>
        protected sealed override void OnDispose()
        {
            OnLODChanged = null;
            OnDisposeInternal();
        }

        /// <summary>
        /// Template method for inheriting classes to release specific resources (e.g., RenderTextures).
        /// </summary>
        protected abstract void OnDisposeInternal();
    }
}
