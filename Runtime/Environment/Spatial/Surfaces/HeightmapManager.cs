using Rayforge.Core.Diagnostics;
using Rayforge.Core.EditorExtensions.EditorStructures;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
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

        private readonly SpatialSurfaceRegistry _surfaceRegistry = new();
        private readonly TextureChunkCoordinator _textureCoordinator = new();
        private ManagedRenderTexture _atlasArray;

        private Vector3 _lastUpdatePos;

        private bool IsReady => lodReference != null && _textureCoordinator != null && _atlasArray != null;

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
            //if(_bakeCoordinator.)
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
            //if (!_chunkRegistry.TryGetEntry(key, out var chunk)) return;

            // English: Your specific Heightmap baking logic using the mapping data.
            // mapping.SliceIndex and mapping.RelativeOffset/Scale tell the shader where to draw.

            // LogDebug($"Baking Chunk {key} into Slice {mapping.SliceIndex}");
        }

        #endregion

        #region Initialization

        private void EnsureSystemsReady(bool force = false)
        {
            InitCoordinator(force);

            if (_textureCoordinator.IsInitialized)
            {
                InitSurfaceRegistry(_textureCoordinator.LodGridProvider, force);
            }

            if (_surfaceRegistry.IsInitialized)
            {
                InitSurfaceTracker(_surfaceRegistry, force);
            }

            if (_textureCoordinator.IsInitialized)
            {
                CreateAtlasTexture(_textureCoordinator, force);
            }
        }

        public void SetupDependencies()
        {
            if (shiftRelay == null)
                shiftRelay = GetComponentInParent<OriginShiftRelay>(true);

            if (lodReference == null && Camera.main != null)
                lodReference = Camera.main.transform;
        }

        private void InitCoordinator(bool force = false)
        {
            if (!_textureCoordinator.IsInitialized || force)
            {
                _textureCoordinator.Reset();

                SpatialSettings spatialSettings = new SpatialSettings
                {
                    GridSize = (GridSize)chunkSize,
                    Anchor = transform.position
                };
                _textureCoordinator.Initialize(spatialSettings, lodTable.ValidEntries, batchSize, lodReference, transform);
            }
        }

        private void InitSurfaceRegistry(ILODGridProvider<Vector3Int> gridProvider, bool force = false)
        {
            if (gridProvider == null || !gridProvider.IsInitialized) return;

            if (!_surfaceRegistry.IsInitialized || force)
            {
                _surfaceRegistry.Reset();

                _surfaceRegistry.Initialize(gridProvider);
            }
        }

        private void InitSurfaceTracker(SpatialSurfaceRegistry registry, bool force = false)
        {
            if (registry == null || !registry.IsInitialized) return;

            if (surfaceTracker == null || !surfaceTracker.IsInitialized || force)
            {
                if (surfaceTracker == null)
                    surfaceTracker = new SurfaceTracker();
                else
                    surfaceTracker.ClearState();

                surfaceTracker.Initialize(registry);
                surfaceTracker.RebuildRegistry(transform);
            }
        }

        private void CreateAtlasTexture(TextureChunkCoordinator coordinator, bool force)
        {
            if (coordinator == null || ! coordinator.IsInitialized) return;

            if (_atlasArray == null || force)
            {
                var sliceCount = coordinator.RequiredSliceCount;
                var resolution = (int)coordinator.BaseResolution;

                var newDesc = DefaultDescriptors.HeightmapPrecision(resolution, resolution).ToAtlasArray(sliceCount);

                bool shouldRecreate = force || _atlasArray == null || !_atlasArray.IsCreated;

                if (!shouldRecreate && _atlasArray != null)
                {
                    if (!_atlasArray.Descriptor.InternalDescriptor.IsCompatible(newDesc))
                    {
                        shouldRecreate = true;
                    }
                }

                if (shouldRecreate)
                {
                    if (_atlasArray != null)
                    {
                        _atlasArray.Release();
                    }

                    var newWrapper = new RenderTextureDescriptorWrapper { InternalDescriptor = newDesc };
                    _atlasArray = new ManagedRenderTexture(newWrapper);
                    _atlasArray.Create();
                }
            }
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
        /*
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
        */
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
            _textureCoordinator?.NotifyOriginShift(delta);
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