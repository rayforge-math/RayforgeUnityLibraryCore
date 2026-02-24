using Rayforge.Core.Diagnostics;
using Rayforge.Core.EditorExtensions.EditorStructures;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Environment.Spatial.Surfaces;
using Rayforge.Core.Environment.Tracking;
using Rayforge.Core.ManagedResources.NativeMemory;
using Rayforge.Core.Rendering.EditorStructures;
using Rayforge.Core.Rendering.Helpers;
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
        private OriginShiftRelay _activeShiftRelay = null;

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
            RefreshEditor();
            lodTable.OnTableChanged += HandleLodTableChanged;
            UpdateShiftRelaySubscription(shiftRelay);

            EnsureSystemsReady(true);
        }

        private void OnValidate()
        {
            RefreshEditor();
            UpdateShiftRelaySubscription(shiftRelay);
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

        private void OnDestroy()
        {
            lodTable.OnTableChanged -= HandleLodTableChanged;
            UpdateShiftRelaySubscription(null);

            ShutdownSystems();
        }

        public void RefreshEditor()
        {
            LogDebug("Refreshing dependencies and sanitizing LOD table...");
            SetupDependencies();
            lodTable.Sanitize();
            LogDebug("<color=green>Editor refresh completed.</color>");
        }

        public void SetupDependencies()
        {
            if (shiftRelay == null)
            {
                shiftRelay = GetComponentInParent<OriginShiftRelay>(true);
                if (shiftRelay != null)
                    LogDebug($"OriginShiftRelay linked from: {shiftRelay.gameObject.name}");
            }

            if (lodReference == null && Camera.main != null)
            {
                lodReference = Camera.main.transform;
                LogDebug("LOD Reference: Auto-assigned Main Camera.");
            }
        }

        private void UpdateShiftRelaySubscription(OriginShiftRelay targetRelay)
        {
            if (_activeShiftRelay == targetRelay) return;

            if (_activeShiftRelay != null)
            {
                _activeShiftRelay.OnWorldShiftDetected -= HandleOriginShift;
                LogDebug($"Unsubscribed from old OriginShiftRelay: {_activeShiftRelay.name}");
            }

            _activeShiftRelay = targetRelay;

            if (_activeShiftRelay != null && Application.isPlaying)
            {
                _activeShiftRelay.OnWorldShiftDetected += HandleOriginShift;
                LogDebug($"<color=green>Shift subscription updated: {_activeShiftRelay.name}</color>");
            }
        }

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

        #region Initialization

        private void EnsureSystemsReady(bool force = false)
        {
            try
            {
                LogDebug($"Ensuring systems are ready (force: {force})...");

                InitCoordinator(force);

                if (_textureCoordinator.IsInitialized)
                {
                    CreateAtlasTexture(_textureCoordinator, force);
                }

                if (_textureCoordinator.IsInitialized)
                {
                    InitSurfaceRegistry(_textureCoordinator.LodGridProvider, force);
                }
                else
                {
                    LogDebug("<color=orange>SurfaceRegistry skipped: Coordinator not initialized.</color>");
                }

                if (_surfaceRegistry.IsInitialized)
                {
                    InitSurfaceTracker(_surfaceRegistry, force);
                }
                else
                {
                    LogDebug("<color=orange>SurfaceTracker skipped: Registry not initialized.</color>");
                }

                LogDebug("<color=green><b>EnsureSystemsReady completed.</b></color>");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"<color=orange><b>Critical failure: {e.Message}</b></color>");
                throw;
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
                _textureCoordinator.Clear();

                SpatialSettings spatialSettings = new SpatialSettings
                {
                    GridSize = (GridSize)chunkSize,
                    Anchor = transform.position
                };

                _textureCoordinator.Initialize(spatialSettings, lodTable.ValidEntries, batchSize, lodReference, transform);
                LogDebug("<color=green>TextureCoordinator initialized.</color>");
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

                LogDebug($"Checking Atlas Texture requirements (Slices: {sliceCount}, Res: {resolution})...");

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
                    string reason = force ? "Forced" : (_atlasArray == null ? "Initial" : "Descriptor Mismatch");
                    LogDebug($"<b>Atlas Rebuild:</b> {reason}. Slices: {sliceCount}, Res: {resolution}");

                    if (_atlasArray != null)
                    {
                        _atlasArray.Release();
                    }

                    var newWrapper = new RenderTextureDescriptorWrapper { InternalDescriptor = newDesc };
                    _atlasArray = new ManagedRenderTexture(newWrapper);
                    _atlasArray.Create();

                    LogDebug("<color=green>Atlas Texture Array created successfully.</color>");
                }
            }
        }

        private void InitSurfaceRegistry(ILODGridProvider<Vector3Int> gridProvider, bool force = false)
        {
            if (gridProvider == null)
                throw new ArgumentNullException(nameof(gridProvider), "Cannot init SurfaceRegistry with a null GridProvider!");

            if (!gridProvider.IsInitialized)
            {
                LogDebug("<color=orange>SurfaceRegistry waiting for GridProvider to be ready.</color>");
                return;
            }

            if (!_surfaceRegistry.IsInitialized || force)
            {
                LogDebug("Initializing SurfaceRegistry...");
                _surfaceRegistry.Reset();
                _surfaceRegistry.Initialize(gridProvider);
                LogDebug("<color=green>SurfaceRegistry initialized.</color>");
            }
        }

        private void InitSurfaceTracker(SpatialSurfaceRegistry registry, bool force = false)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry), "Cannot init SurfaceTracker with a null Registry!");

            if (!registry.IsInitialized)
            {
                LogDebug("<color=orange>SurfaceTracker waiting for Registry to be ready.</color>");
                return;
            }

            if (surfaceTracker == null || !surfaceTracker.IsInitialized || force)
            {
                LogDebug(force ? "Forcing SurfaceTracker re-initialization..." : "Initializing SurfaceTracker...");

                if (surfaceTracker == null)
                {
                    surfaceTracker = new SurfaceTracker();
                }
                else
                {
                    LogDebug("Cleaning up SurfaceTracker state.");
                    surfaceTracker.OnRegistryChanged -= HandleSurfacesChanged;
                    surfaceTracker.ClearState();
                }

                surfaceTracker.Initialize(registry);
                RebuildSurfaceRegistry();

                surfaceTracker.OnRegistryChanged += HandleSurfacesChanged;
                LogDebug($"<color=green>SurfaceTracker initialized and synced.</color>");

                HandleSurfacesChanged(surfaceTracker);
            }
        }

        #endregion

        #region Update

        public void RebuildSurfaceRegistry()
        {
            LogDebug("Rebuilding SurfaceRegistry...");
            surfaceTracker.RebuildRegistry(transform);
            LogDebug($"<color=green>Registry Rebuilt: {surfaceTracker.TotalTrackedCount} surfaces live.</color>");
        }

        public void ResetTrackingPosition() => _lastUpdatePos = lodReference ? lodReference.position : Vector3.zero;

        private void HandleOriginShift(Vector3 delta)
        {
            _textureCoordinator?.NotifyOriginShift(delta);
            _lastUpdatePos += delta;
        }

        private void HandleLodTableChanged(UniversalLodTable<TextureLOD> lodTable)
        {
            LogDebug("LodTable reported LOD update. Syncing with components...");

            if(lodTable == null || _textureCoordinator == null || !_textureCoordinator.IsInitialized)
            {
                LogDebug("<color=orange>LOD sync aborted: LodTable is null or TextureCoordinator is null or uninitialized.</color>");
                return;
            }


        }

        private void HandleSurfacesChanged(SurfaceTracker sender)
        {
            LogDebug("SurfaceTracker reported changes. Syncing topology with TextureCoordinator...");

            if (sender == null || _textureCoordinator == null || !_textureCoordinator.IsInitialized)
            {
                LogDebug("<color=orange>Topology sync aborted: SurfaceTracker is null or TextureCoordinator is null or uninitialized.</color>");
                return;
            }

            _textureCoordinator.UpdateTopology(sender.Registry);

            LogDebug($"<color=green>Topology sync completed.</color> Coordinator is now managing {_textureCoordinator.LodGridProvider.TotalCellCount} chunk(s).");
        }

        #endregion

        #region Cleanup Logic

        /// <summary>
        /// Completely shuts down all systems and releases resources.
        /// Use this for a hard reset or during OnDestroy.
        /// </summary>
        private void ShutdownSystems()
        {
            LogDebug("<color=red>Shutting down all systems...</color>");

            CleanupSurfaceTracker();

            if (_surfaceRegistry != null) _surfaceRegistry.Reset();

            CleanupAtlas();

            if (_textureCoordinator != null) _textureCoordinator.Reset();

            LogDebug("<color=green>Shutdown completed.</color>");
        }

        private void CleanupSurfaceTracker()
        {
            if (surfaceTracker != null)
            {
                LogDebug("Cleaning up SurfaceTracker...");
                surfaceTracker.OnRegistryChanged -= HandleSurfacesChanged;
                surfaceTracker.ClearState();
            }
        }

        private void CleanupAtlas()
        {
            if (_atlasArray != null)
            {
                LogDebug("Releasing Atlas RenderTextures...");
                _atlasArray.Release();
                _atlasArray = null;
            }
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
                //script.();
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