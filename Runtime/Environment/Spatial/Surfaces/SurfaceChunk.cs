using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// A specialized 2D chunk representing a portion of the world surface.
    /// Stores surface-specific data like heightmaps and reacts to LOD changes to trigger re-bakes.
    /// </summary>
    [ChunkConfig(SpatialAxes.Surface)]
    public class SurfaceChunk : LODChunk<SurfaceChunk>
    {
        /// <summary>
        /// The GPU texture holding height data for this chunk.
        /// This reference is managed by the SurfaceRegistry or a Baker system.
        /// </summary>
        [field: SerializeField]
        public RenderTexture Heightmap { get; private set; }

        /// <summary>
        /// Assigns a new heightmap to this chunk. 
        /// Usually called by the Baker after a successful generation.
        /// </summary>
        public void SetHeightmap(RenderTexture map)
        {
            Heightmap = map;
        }

        #region Debugging
        protected override void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;
            Vector3 size = new Vector3(localExtent.x * 2f, 0.1f, localExtent.z * 2f);

            Gizmos.color = GetLODColor(CurrentLOD);
            Gizmos.DrawCube(pos, size);
            Gizmos.DrawWireCube(pos, size);
        }

        private Color GetLODColor(int lod)
        {
            return lod switch
            {
                -1 => Color.red,
                0 => Color.green,
                1 => Color.yellow,
                _ => Color.orange
            };
        }
        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SurfaceChunk))]
    public class SurfaceChunkEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var chunk = (SurfaceChunk)target;

            if (chunk.Heightmap != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Heightmap Preview", EditorStyles.boldLabel);

                Rect previewRect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(previewRect, chunk.Heightmap);
            }
        }
    }
#endif
}
