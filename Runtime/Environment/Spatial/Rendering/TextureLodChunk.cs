using Rayforge.Core.Rendering.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using UnityEditor;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// A minimal, generic LOD chunk that only stores its view into an atlas.
    /// It does not hold references to textures or material blocks, keeping the memory footprint minimal.
    /// </summary>
    [ChunkConfig(SpatialAxes.Surface)]
    public class TextureLodChunk : LODChunk<TextureLodChunk>, ITextureMapped
    {
        /// <summary>
        /// The view metadata (Slice, Scale, Offset) for this chunk in the global atlas.
        /// Stored by value (struct) to minimize memory overhead.
        /// </summary>
        [Header("Atlas Mapping")]
        public TextureMappingData Mapping { get; private set; }

        /// <summary>
        /// Checks if the current mapping has been initialized. 
        /// (Assumes SliceIndex -1 or a similar sentinel value in the struct if not yet assigned).
        /// </summary>
        public bool HasMapping => Mapping.SliceIndex >= 0;

        /// <summary>
        /// Updates the view coordinates for this chunk.
        /// </summary>
        /// <param name="data">The mapping data provided by the AtlasController.</param>
        public void SetTextureMapping(TextureMappingData data)
        {
            Mapping = data;
        }

        /// <summary>
        /// Resets the mapping data to an invalid state.
        /// Useful for pooling to ensure old data isn't used for rendering.
        /// </summary>
        public void ClearMapping()
        {
            Mapping = default;
        }

        #region Debugging & Gizmos

        /// <summary>
        /// Visualizes the chunk bounds and its assigned atlas slot in the scene view.
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

#if UNITY_EDITOR
            if (IsVisible && HasMapping)
            {
                Vector3 pos = transform.position;

                Vector3 displaySize = GetLogicalSize();
                Vector3 labelPos = pos + Vector3.up * (displaySize.y + 1f);

                string debugInfo = $"LOD: {CurrentLOD}\nSlice: {Mapping.SliceIndex}\nScale: {Mapping.RelativeScale:F2}";

                Handles.Label(labelPos, debugInfo);
            }
#endif
        }

        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TextureLodChunk))]
    public class TextureLodChunkEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var chunk = (TextureLodChunk)target;

            EditorGUILayout.LabelField("LOD Configuration", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Current LOD", chunk.CurrentLOD);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Atlas View", EditorStyles.boldLabel);

            if (chunk.HasMapping)
            {
                EditorGUILayout.HelpBox($"Slice: {chunk.Mapping.SliceIndex}\n" +
                    $"Scale: {chunk.Mapping.RelativeScale:F4}\n" +
                    $"Offset: {chunk.Mapping.RelativeOffset}", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("No mapping assigned.", MessageType.Warning);
            }
        }
    }
#endif
}
