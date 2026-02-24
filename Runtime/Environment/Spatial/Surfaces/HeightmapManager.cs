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

        private bool IsReady =>
            _textureCoordinator != null && _textureCoordinator.IsInitialized &&
            _surfaceRegistry != null && _surfaceRegistry.IsInitialized &&
            surfaceTracker != null && surfaceTracker.IsInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            lodTable.OnTableChanged += HandleLodTableChanged;

            RefreshEditor();
            EnsureSystemsReady(true);

            if (shiftRelay != null)
                shiftRelay.OnWorldShiftDetected += HandleOriginShift;
        }

        private void OnValidate()
        {
            RefreshEditor();

            if (!Application.isPlaying)
            {
                try
                {
                    EnsureSystemsReady(false);
                }
                catch { }
            }
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

        public void RefreshEditor()
        {
            LogDebug($"Refreshing Editor...");
            SetupDependencies();
            lodTable.Sanitize();
        }

        public void RebuildSurfaceRegistry()
        {
            LogDebug($"Rebuilding Surface Registry...");
            surfaceTracker.RebuildRegistry(transform);
        }

        #endregion

        #region Initialization

        private void EnsureSystemsReady(bool force = false)
        {
            try
            {
                LogDebug($"Ensuring systems are ready (force: {force})...");

                InitCoordinator(force);

                if (_textureCoordinator.IsInitialized)
                {
                    InitSurfaceRegistry(_textureCoordinator.LodGridProvider, force);
                }
                else
                {
                    LogDebug("SurfaceRegistry init skipped: Coordinator not initialized.");
                }

                if (_surfaceRegistry.IsInitialized)
                {
                    InitSurfaceTracker(_surfaceRegistry, force);
                }
                else
                {
                    LogDebug("SurfaceTracker init skipped: Registry not initialized.");
                }

                if (_textureCoordinator.IsInitialized)
                {
                    CreateAtlasTexture(_textureCoordinator, force);
                }

                LogDebug("EnsureSystemsReady completed.");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"<b>[SurfaceSystem]</b> Critical initialization failure: {e.Message}");
                throw;
            }
        }

        public void SetupDependencies()
        {
            if (shiftRelay == null)
            {
                shiftRelay = GetComponentInParent<OriginShiftRelay>(true);
                if (shiftRelay != null) LogDebug("OriginShiftRelay found and assigned.");
            }

            if (lodReference == null && Camera.main != null)
            {
                lodReference = Camera.main.transform;
                LogDebug("LOD Reference automatically assigned to Main Camera.");
            }
        }

        private void InitCoordinator(bool force = false)
        {
            if (lodTable == null)
                throw new NullReferenceException($"{nameof(lodTable)} is missing on {gameObject.name}!");

            if (lodReference == null)
                throw new InvalidOperationException("Cannot initialize Coordinator without a LOD Reference (Viewer)!");

            if (!_textureCoordinator.IsInitialized || force)
            {
                LogDebug("Initializing TextureCoordinator...");
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
            if (gridProvider == null)
                throw new ArgumentNullException(nameof(gridProvider), "Cannot init SurfaceRegistry with a null GridProvider!");

            if (!gridProvider.IsInitialized)
            {
                LogDebug("SurfaceRegistry waiting for GridProvider to be initialized...");
                return;
            }

            if (!_surfaceRegistry.IsInitialized || force)
            {
                LogDebug("Initializing SurfaceRegistry...");
                _surfaceRegistry.Reset();
                _surfaceRegistry.Initialize(gridProvider);
            }
        }

        private void InitSurfaceTracker(SpatialSurfaceRegistry registry, bool force = false)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Cannot init SurfaceTracker with a null Registry!");

            if (!registry.IsInitialized)
            {
                LogDebug("SurfaceTracker waiting for Registry to be initialized...");
                return;
            }

            if (surfaceTracker == null || !surfaceTracker.IsInitialized || force)
            {
                LogDebug("Initializing SurfaceTracker...");

                if (surfaceTracker == null)
                    surfaceTracker = new SurfaceTracker();
                else
                    surfaceTracker.ClearState();

                surfaceTracker.Initialize(registry);
                RebuildSurfaceRegistry();

                LogDebug($"SurfaceTracker rebuild complete. Tracking {surfaceTracker.TotalTrackedCount} surfaces.");
            }
        }

        private void CreateAtlasTexture(TextureChunkCoordinator coordinator, bool force)
        {
            if (coordinator == null || !coordinator.IsInitialized)
            {
                LogDebug("Atlas creation deferred: Coordinator not ready.");
                return;
            }

            if (_atlasArray == null || force)
            {
                var sliceCount = coordinator.RequiredSliceCount;
                var resolution = (int)coordinator.BaseResolution;

                LogDebug($"Checking Atlas Texture (Slices: {sliceCount}, Res: {resolution})...");

                var newDesc = DefaultDescriptors.HeightmapPrecision(resolution, resolution).ToAtlasArray(sliceCount);

                bool shouldRecreate = force || _atlasArray == null || !_atlasArray.IsCreated;

                if (!shouldRecreate && _atlasArray != null)
                {
                    if (!_atlasArray.Descriptor.InternalDescriptor.IsCompatible(newDesc))
                    {
                        LogDebug("Atlas descriptor mismatch. Recreating...");
                        shouldRecreate = true;
                    }
                }

                if (shouldRecreate)
                {
                    if (_atlasArray != null)
                    {
                        LogDebug("Releasing old Atlas Texture.");
                        _atlasArray.Release();
                    }

                    var newWrapper = new RenderTextureDescriptorWrapper { InternalDescriptor = newDesc };
                    _atlasArray = new ManagedRenderTexture(newWrapper);
                    _atlasArray.Create();

                    LogDebug("New Atlas Texture Array created successfully.");
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
        private void LogDebug(string msg, [CallerLineNumber] int line = 0)
        {
            string formattedMsg = $"<b>[SurfaceSystem]</b> {msg}";
            DebugOutput.Log(formattedMsg, showDebugLogs, lineNumber: line);
        }
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
                script.RebuildSurfaceRegistry();
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