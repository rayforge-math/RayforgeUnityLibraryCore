using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.Diagnostics;
using Rayforge.Core.EditorExtensions.EditorStructures;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Environment.Spatial.Rendering;
using Rayforge.Core.Environment.Spatial.Rendering.Helpers;
using Rayforge.Core.Environment.Spatial.Surfaces;
using Rayforge.Core.Environment.Tracking;
using Rayforge.Core.ManagedResources.NativeMemory;
using Rayforge.Core.Rendering.EditorStructures;
using Rayforge.Core.Rendering.Helpers;
using Rayforge.Core.Rendering.Textures;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// Scans the scene hierarchy for valid world objects and synchronizes them with the SurfaceRegistry.
    /// Handles filtering by name and physical area to ensure only relevant surfaces are processed.
    /// </summary>
    public class HeightmapManager : MonoBehaviour
    {
        #region Serialized Fields: Configuration
        [Header("Debug & Diagnostics")]
        public bool showDebugLogs = false;

        [Header("Floating Origin")]
        public OriginShiftRelay shiftRelay;

        [Header("LOD & Culling Settings")]
        public Transform lodReference;
        public GridSizeBinary chunkSize = GridSizeBinary.Huge;
        [Range(0.01f, 0.5f)] public float updateSensitivity = 0.1f;

        [Header("Atlas & Batching")]
        [Range(16, 256)] public int batchSize = 64;
        public TextureLodTable lodTable = new();
        public float minRelativeY;
        public float maxRelativeY;

        [Header("Surface Detection Settings")]
        public SurfaceTracker surfaceTracker = new();

        [Header("Debug Visualization")]
        [Range(0, 511)] public int debugSliceIndex = 0;
        public Texture2D debugView;
        #endregion

        #region Private Runtime State

        private SpatialObjectRegistry _objectRegistry;
        private TextureChunkCoordinator _bakeCoordinator;
        private ManagedRenderTexture _atlasArray;

        private Vector3 _lastUpdatePos;

        private bool IsReady => lodReference != null && _bakeCoordinator != null && _atlasArray != null;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            lodTable.OnTableChanged += HandleLodTableChanged;

            SetupDependencies();

            EnsureSystemsReady(true);

            if (shiftRelay != null)
                shiftRelay.OnWorldShiftDetected += HandleOriginShift;
        }

        private void OnValidate()
        {
            lodTable.Sanitize();
        }

        private void OnDestroy()
        {
            lodTable.OnTableChanged -= HandleLodTableChanged;
        }

        private void Update()
        {
            if (!IsReady) return;
            /*
            if (CheckMovementThreshold())
            {
                _chunkRegistry.UpdateLODs();
            }

            if (surfaceTracker.IsDirty)
            {
                _bakeCoordinator.UpdateTopology(surfaceTracker.Registry);
                surfaceTracker.ClearDirty();
            }

            _bakeCoordinator.ExecuteBake((key, mapping) =>
            {
                PerformGpuBake(key, mapping);
            });
            */
        }

        private void HandleLodTableChanged(UniversalLodTable<TextureLOD> lodTable)
        {
            if(_bakeCoordinator.)
        }

        private void RefreshEditor()
        {
            SetupDependencies();
            lodTable.Sanitize();
        }

        #endregion

        #region Execution Logic

        private void PerformGpuBake(Vector3Int key, TextureMappingData mapping)
        {
            if (!_chunkRegistry.TryGetEntry(key, out var chunk)) return;

            // English: Your specific Heightmap baking logic using the mapping data.
            // mapping.SliceIndex and mapping.RelativeOffset/Scale tell the shader where to draw.

            // LogDebug($"Baking Chunk {key} into Slice {mapping.SliceIndex}");
        }

        #endregion

        #region Initialization

        private void EnsureSystemsReady(bool force = false)
        {
            if (_chunkRegistry == null || force)
            {
                _chunkRegistry?.Dispose();
                _chunkRegistry = new LODChunkRegistry<TextureLodChunk>(
                    (GridSize)chunkSize,
                    transform.position,
                    lodTable.ValidDistances,
                    true,
                    lodReference,
                    transform
                );


                _objectRe

                surfaceTracker.Initialize(_chunkRegistry);
            }

            if (_atlasMapper == null || force)
            {
                _atlasMapper = new LodAtlasMapper<Vector3Int>();
                _atlasMapper.Initialize(_chunkRegistry, lodTable.ValidEntries, batchSize);
            }

            if (_bakeCoordinator == null || force)
            {
                _bakeCoordinator = new TextureChunkCoordinator<TextureLodChunk>(_atlasMapper, _chunkRegistry);
            }

            CreateAtlasTexture(force);
        }

        private void CreateAtlasTexture(bool force)
        {
            if (_atlasArray == null || force)
            {
                _atlasArray?.Dispose();

                int totalSlices = _atlasMapper.RequiredSliceCount;
                int res = (int)lodTable.ValidEntries[0].mapResolution;

                var desc = DefaultDescriptors.HeightmapPrecision(res, res);
                desc.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
                desc.volumeDepth = totalSlices;

                _atlasArray = new ManagedRenderTexture(new RenderTextureDescriptorWrapper { Descriptor = desc });
                _atlasArray.Create();
            }
        }

        public void SetupDependencies()
        {
            if (shiftRelay == null)
                shiftRelay = GetComponentInParent<OriginShiftRelay>(true);

            if (lodReference == null && Camera.main != null)
                lodReference = Camera.main.transform;
        }

        #endregion
        /*
        #region Initialization & Dependencies

        private void EnsureSystemsReady(bool force = false)
        {
            CreateChunkRegistry(force);
            CreateLodAtlas(force);
            ResetTrackingPosition();
        }

        private void CreateChunkRegistry(bool force = false)
        {
            if (_chunkRegistry == null || force)
            {
                _chunkRegistry?.Dispose();

                _chunkRegistry = new LODChunkRegistry<AtlasLODChunk>(
                    (GridSize)chunkSize,
                    transform.position,
                    lodTable.ValidDistances,
                    true,
                    lodReference,
                    transform
                );

                surfaceTracker.Initialize(_chunkRegistry);
                _atlasController?.Initialize(
                    provider: _chunkRegistry,
                    lodConfigs: lodTable.ValidEntries,
                    batchSize: batchSize
                );
                LogDebug($"Chunk Registry created. GridSize: {(int)chunkSize}");
            }
        }

        private void CreateLodAtlas(bool force = false)
        {
            if (_atlasController == null || force)
            {
                var validLodLevels = lodTable.ValidEntries;

                _atlasController = new LodAtlasController<Vector3Int>();
                _atlasController.Initialize(
                    provider: _chunkRegistry,
                    lodConfigs: validLodLevels,
                    batchSize: batchSize
                );

                int totalSlices = _atlasController.RequiredSliceCount;
                int baseRes = (int)validLodLevels[0].mapResolution;

                _atlasArray?.Dispose();

                var desc = DefaultDescriptors.HeightmapPrecision(baseRes, baseRes);
                desc.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
                desc.volumeDepth = totalSlices;
                var wrapper = new RenderTextureDescriptorWrapper { Descriptor = desc };

                _atlasArray = new ManagedRenderTexture(wrapper);
                _atlasArray.Create();

                LogDebug($"Atlas System ready. Slices allocated: {totalSlices}");
            }
        }

        #endregion
        */

        #region Configuration & Validation

        private void UpdateGridSize()
        {
            if (_chunkRegistry != null && _chunkRegistry.GridSize != (GridSize)chunkSize)
            {
                LogDebug("GridSize changed. Re-initializing entire pipeline.");

                _chunkRegistry?.ClearChunks();
                EnsureSystemsReady(force: true);
                surfaceTracker.RebuildRegistry(transform);
            }
        }

        private void UpdateLODSettings()
        {
            if (_chunkRegistry == null) return;

            var distances = lodTable.ValidDistances;

            bool updateLod = _chunkRegistry.SetViewer(lodReference);
            updateLod |= _chunkRegistry.UpdateLodDistances(distances);

            if (updateLod)
            {
                _chunkRegistry.UpdateLODs();
                LogDebug("LOD Settings synchronized and LODs recalculated.");
            }
            else
            {
                LogDebug("LOD update skipped: Viewer and distances have not changed.");
            }
        }

        #endregion
        
        #region Runtime Processing & Baking
        
        private bool CheckMovementThreshold()
        {
            float distSqr = (lodReference.position - _lastUpdatePos).sqrMagnitude;
            float threshold = (float)chunkSize * updateSensitivity;

            if (distSqr > threshold * threshold)
            {
                _lastUpdatePos = lodReference.position;
                return true;
            }
            return false;
        }



        #endregion

        #region System Events & Cleanup

        private void HandleOriginShift(Vector3 delta)
        {
            _chunkRegistry?.NotifyOriginShift(delta);
            _lastUpdatePos += delta;
        }

        public void ResetTrackingPosition() => _lastUpdatePos = lodReference ? lodReference.position : Vector3.zero;

       

        /// <summary>
        /// English: Completely wipes all registered surfaces, destroys all chunks, 
        /// and resets the registries to a clean state.
        /// </summary>
        public void ClearAll()
        {
            LogDebug("Performing full system cleanup...");
            /*
            _surfaceIds.Clear();
            surfaces.Clear();
            _cleanupBuffer.Clear();

            if (_objectRegistry != null)
            {
                _objectRegistry = new SpatialObjectRegistry { showDebugLogs = this.showDebugLogs };
                _objectRegistry.Initialize(_chunkRegistry);
            }

            if (_chunkRegistry != null)
            {
                _chunkRegistry.Dispose();
                CreateChunkRegistry(true);
            }

            if (_atlasController != null)
            {
                _atlasController.ClearAll();
                CreateLodAtlas(true);
            }

            ResetTrackingPosition();
            */
            LogDebug("System cleared. All chunks destroyed and registries reset.");
        }
        #endregion

        #region Debug Helper
        [Conditional("UNITY_EDITOR")]
        private void LogDebug(string msg, [CallerLineNumber] int line = 0) { DebugOutput.Log(msg, showDebugLogs, lineNumber: line); }
        #endregion
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom Editor to provide convenient buttons for surface management in the Inspector.
    /// </summary>
    [UnityEditor.CustomEditor(typeof(HeightmapManager))]
    public class SurfaceManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            HeightmapManager script = (HeightmapManager)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Refresh Surfaces", GUILayout.Height(30)))
            {
                //script.RebuildRegistry();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Clear Surfaces", GUILayout.Height(30)))
            {
                script.ClearAll();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Bake Heightmaps", GUILayout.Height(30)))
            {
                //script.RebakeAll();
            }
        }
    }
#endif
}