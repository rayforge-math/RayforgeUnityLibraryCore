using Rayforge.Core.Environment.Abstractions;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    public abstract class LODChunk<T> : Chunk<T>, ILODState, ILODReceiver
        where T : LODChunk<T>
    {
        private const string Tag = "[LODReceiver]";

        /// <summary>
        /// The current Level of Detail index. Initialized to -1 to ensure the first update triggers.
        /// Used by the Registry to determine if an update is necessary.
        /// </summary>
        public int CurrentLOD { get; private set; } = -2;

        /// <summary>
        /// The maximum possible LOD index defined for this chunk.
        /// Used to normalize LOD-dependent logic (e.g., shaders or debug colors).
        /// </summary>
        public int MaxLOD { get; private set; }

        [SerializeField, ReadOnly(true)] private bool _isVisible = false;
        public bool IsVisible => _isVisible;

        public event Action<ILODState, int, int> OnLODChanged;
        public event Action<ILODState, bool> OnVisibilityChanged;

        /// <summary>
        /// Updates the LOD state and marks the chunk as dirty for the baking system.
        /// Triggered by LODChunkRegistry. Changing the LOD usually requires a resolution change.
        /// </summary>
        /// <param name="newLod">The new LOD level calculated by the registry.</param>
        bool ILODReceiver.UpdateLOD(int newLod, bool useHardDeactivation)
        {
            int sanitizedLod = Mathf.Clamp(newLod, -1, MaxLOD);

            if (CurrentLOD == sanitizedLod) return false;

            int oldLod = CurrentLOD;
            CurrentLOD = sanitizedLod;

            ((ILODReceiver)this).SetVisibility(CurrentLOD >= 0, useHardDeactivation);

            OnLODChanged?.Invoke(this, oldLod, CurrentLOD);
            MarkDirty();

            return true;
        }

        /// <summary>
        /// Internal helper to toggle visibility and fire events.
        /// </summary>
        void ILODReceiver.SetVisibility(bool visible, bool useHardDeactivation)
        {
            bool visibilityChanged = _isVisible != visible;

            bool targetActiveState = visible || !useHardDeactivation;
            bool stateChanged = gameObject.activeSelf != targetActiveState;

            if (!visibilityChanged && !stateChanged) return;

            _isVisible = visible;

            if (stateChanged)
            {
                gameObject.SetActive(targetActiveState);
            }

            OnVisibilityChanged?.Invoke(this, _isVisible);
        }

        /// <summary>
        /// Initializes or updates the LOD range metadata for this chunk.
        /// This should be called before the first LOD update to ensure correct scaling and visualization.
        /// </summary>
        /// <param name="maxLod">The highest possible LOD index (e.g., 4 for a system with 5 LOD levels).</param>
        void ILODReceiver.ConfigureLODRange(int maxLod)
        {
            if (maxLod < 0)
                throw new ArgumentException($"{Tag} MaxLOD cannot be negative. Value: {maxLod}");

            MaxLOD = maxLod;
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

        #region Debugging & Gizmos

        /// <summary>
        /// Calculates a semantic color by interpolating between green (near/detail) and red (far/culled).
        /// </summary>
        /// <param name="lod">The current LOD level.</param>
        /// <returns>A color representing the visual importance of the chunk.</returns>
        private Color GetLODColor(int lod)
        {
            if (lod == -1) return new Color(1f, 0f, 0.5f, 0.6f);

            float t = MaxLOD > 0 ? Mathf.Clamp01((float)lod / MaxLOD) : 0f;
            float hue = Mathf.Lerp(0.33f, 0.0f, t);

            Color lodColor = Color.HSVToRGB(hue, 1f, 1f);
            lodColor.a = Mathf.Lerp(0.8f, 0.4f, t);

            return lodColor;
        }

        /// <summary>
        /// Overrides the base gizmo to include LOD-specific coloring.
        /// This allows seeing at a glance which chunks are at which detail level.
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            if (localExtent.sqrMagnitude < 0.0001f) return;

            Vector3 pos = transform.position;
            Vector3 displaySize = GetLogicalSize();

            Gizmos.color = GetLODColor(CurrentLOD);
            Gizmos.DrawCube(pos, displaySize);

            Gizmos.color = new Color(0, 0, 0, 0.25f);
            Gizmos.DrawWireCube(pos, displaySize);
        }

        #endregion
    }
}
