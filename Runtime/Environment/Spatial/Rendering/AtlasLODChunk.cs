using UnityEngine;
using UnityEditor;
using Rayforge.Core.Environment.Spatial.Chunks;


namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// A minimal, generic LOD chunk that only stores its view into an atlas.
    /// It does not hold references to textures or material blocks, keeping the memory footprint minimal.
    /// </summary>
    [ChunkConfig(SpatialAxes.Surface)]
    public class AtlasLODChunk : LODChunk<AtlasLODChunk>
    {
        /// <summary>
        /// The view metadata (Slice, Scale, Offset) for this chunk in the global atlas.
        /// Stored by value (struct) to minimize memory overhead.
        /// </summary>
        [Header("Atlas Mapping")]
        public AtlasMappingData Mapping;

        /// <summary>
        /// Checks if the current mapping has been initialized. 
        /// (Assumes SliceIndex -1 or a similar sentinel value in the struct if not yet assigned).
        /// </summary>
        public bool HasMapping => Mapping.SliceIndex >= 0;

        /// <summary>
        /// Updates the view coordinates for this chunk.
        /// English: Because Mapping is a struct, this performs a quick bitwise copy of the data.
        /// </summary>
        /// <param name="data">The mapping data provided by the AtlasController.</param>
        public void SetAtlasMapping(AtlasMappingData data)
        {
            Mapping = data;
        }

        /// <summary>
        /// Resets the mapping data to an invalid state.
        /// English: Useful for pooling to ensure old data isn't used for rendering.
        /// </summary>
        public void ClearAtlasMapping()
        {
            Mapping = default;
        }

        #region Debugging & Gizmos

        /// <summary>
        /// Visualizes the chunk bounds and its assigned atlas slot in the scene view.
        /// </summary>
        protected virtual void OnDrawGizmos()
        {
            Vector3 pos = transform.position;
            Vector3 size = new Vector3(localExtent.x * 2f, 0.1f, localExtent.z * 2f);

            Gizmos.color = GetLODColor(CurrentLOD);
            Gizmos.DrawCube(pos, size);
            Gizmos.DrawWireCube(pos, size);

#if UNITY_EDITOR
            if (HasMapping)
            {
                string debugInfo = $"Slice: {Mapping.SliceIndex}\nScale: {Mapping.RelativeScale:F2}";
                Handles.Label(pos + Vector3.up, debugInfo);
            }
#endif
        }

        private Color GetLODColor(int lod)
        {
            return lod switch
            {
                -1 => new Color(1, 0, 0, 0.2f),
                0 => new Color(0, 1, 0, 0.4f),
                1 => new Color(1, 1, 0, 0.4f),
                _ => new Color(1, 0.5f, 0, 0.4f)
            };
        }
        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AtlasLODChunk))]
    public class AtlasLODChunkEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var chunk = (AtlasLODChunk)target;

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
