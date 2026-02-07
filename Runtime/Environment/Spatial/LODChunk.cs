using Rayforge.Core.Environment.Abstractions;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    public abstract class LODChunk<T> : Chunk<T>, ILODState, ILODReceiver
        where T : LODChunk<T>
    {
        /// <summary>
        /// The current Level of Detail index. Initialized to -1 to ensure the first update triggers.
        /// Used by the Registry to determine if an update is necessary.
        /// </summary>
        public int CurrentLOD { get; private set; } = -1;

        [SerializeField, ReadOnly(true)] private bool _isVisible = false;
        public bool IsVisible => _isVisible;

        public event Action<ILODState, int, int> OnLODChanged;
        public event Action<ILODState, bool> OnVisibilityChanged;

        /// <summary>
        /// Updates the LOD state and marks the chunk as dirty for the baking system.
        /// Triggered by LODChunkRegistry. Changing the LOD usually requires a resolution change.
        /// </summary>
        /// <param name="newLod">The new LOD level calculated by the registry.</param>
        bool ILODReceiver.UpdateLOD(int newLod)
        {
            if (CurrentLOD == newLod) return false;

            int oldLod = CurrentLOD;
            CurrentLOD = newLod;

            ((ILODReceiver)this).SetVisibility(newLod >= 0);

            OnLODChanged?.Invoke(this, oldLod, newLod);
            MarkDirty();

            return true;
        }

        /// <summary>
        /// Internal helper to toggle visibility and fire events.
        /// </summary>
        void ILODReceiver.SetVisibility(bool visible)
        {
            if (_isVisible == visible) return;
            _isVisible = visible;

            gameObject.SetActive(_isVisible);
            OnVisibilityChanged?.Invoke(this, _isVisible);
        }

        /// <summary>
        /// Cleans up GPU resources when the chunk is removed.
        /// Essential to prevent VRAM leaks when chunks are pooled or destroyed.
        /// </summary>
        protected override void OnDispose()
        {
            OnLODChanged = null;
            OnVisibilityChanged = null;
        }
    }
}
