using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Diagnostics;
using Rayforge.Core.EditorExtensions.EditorStructures;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Environment.Spatial.Surfaces;
using Rayforge.Core.Environment.Tracking;
using Rayforge.Core.ManagedResources.NativeMemory;
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
        private Transform _activeLodReference = null;
        public GridSizeBinary chunkSize = GridSizeBinary.Size512;
        [Range(0.01f, 0.5f)] public float updateSensitivity = 0.1f;
        public PowerOfTwoResolution baseResolution = PowerOfTwoResolution.Res256;
        public float[] lodDistances;

        [Header("Atlas & Batching")]
        [Range(2, 64)] public int batchSize = 16;
        public float minRelativeY;
        private float _activeMinRelativeY = 0;
        public float maxRelativeY;
        private float _activeMaxRelativeY = 0;

        [Header("Surface Detection Settings")]
        public SurfaceTracker surfaceTracker = new();

        [Header("Debug Visualization")]
        [Range(0, 511)] public int debugSliceIndex = 0;
        public Texture2D debugView;
        #endregion

        #region Private Runtime State

        private readonly SurfaceRegistry _surfaceRegistry = new();
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
            ValidateSettings();

            UpdateShiftRelaySubscription(shiftRelay);
            UpdateLodReferenceSubscription(lodReference);

            EnsureSystemsReady(true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RefreshEditor();

            ValidateSettings();

            UpdateShiftRelaySubscription(shiftRelay);
            UpdateLodReferenceSubscription(lodReference);
        }
#endif

        private void Update()
        {
            if (!IsReady) return;
            
            if (CheckMovementThreshold())
            {
                _textureCoordinator?.UpdateLODs();
            }

            /*
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
            UpdateShiftRelaySubscription(null);
            UpdateLodReferenceSubscription(null);

            ShutdownSystems();
        }

        public void RefreshEditor()
        {
            LogDebug("Refreshing dependencies and sanitizing LOD table...");
            SetupDependencies();
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
                    CheckAndAllocateAtlas(_textureCoordinator);
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
            if (lodReference == null)
                throw new InvalidOperationException("Cannot initialize Coordinator without a LOD Reference (Viewer)!");

            if (!_textureCoordinator.IsInitialized || force)
            {
                LogDebug("Initializing TextureCoordinator...");
                _textureCoordinator.Clear();

                _textureCoordinator.Initialize((GridSize)chunkSize, transform.position, lodDistances, baseResolution, batchSize, lodReference, transform);
                LogDebug("<color=green>TextureCoordinator initialized.</color>");
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

        private void InitSurfaceTracker(SurfaceRegistry registry, bool force = false)
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

                surfaceTracker.Initialize(registry, transform);
                RebuildSurfaceRegistry();

                surfaceTracker.OnRegistryChanged += HandleSurfacesChanged;
                LogDebug($"<color=green>SurfaceTracker initialized and synced.</color>");

                HandleSurfacesChanged(surfaceTracker);
            }
        }

        #endregion

        #region Editor Update

        /// <summary>
        /// Editor-Only: Completely refreshes the serialized wishlist from the hierarchy.
        /// Use this via Inspector buttons to update the designer-facing list.
        /// </summary>
        public void RefreshPersistentSurfaceTable()
        {
            LogDebug("Scanning hierarchy for persistent surfaces...");

            if (surfaceTracker.ScanHierarchyToTable(transform))
            {
                LogDebug($"<color=cyan>Table Refreshed: {surfaceTracker.WishlistCount} entries saved to wishlist.</color>");
            }
            else
            {
                LogDebug("Scan complete. No valid surfaces found in hierarchy.");
            }
        }

        /// <summary>
        /// Editor-Only: Removes all 'null' or 'Missing' references from the persistent wishlist.
        /// Keeps the Inspector list clean without losing valid surface assignments.
        /// </summary>
        public void CleanupPersistentSurfaceTable()
        {
            LogDebug("Cleaning up null references in surface table...");

            int countBefore = surfaceTracker.WishlistCount;
            surfaceTracker.CleanupTableNulls();
            int removed = countBefore - surfaceTracker.WishlistCount;

            if (removed > 0)
            {
                LogDebug($"<color=cyan>Cleanup complete: Removed {removed} empty entries.</color>");
            }
            else
            {
                LogDebug("Cleanup complete: No null references found.");
            }
        }

        /// <summary>
        /// Editor-Only: Wipes the persistent list.
        /// </summary>
        public void ClearPersistentSurfaceTable()
        {
            LogDebug("Clearing persistent surface wishlist...");
            surfaceTracker.ClearTable();
            LogDebug("<color=orange>Wishlist cleared.</color>");
        }

        #endregion

        #region Runtime Udpate

        public void ResetTrackingPosition() => _lastUpdatePos = lodReference ? lodReference.position : Vector3.zero;

        public void RebuildSurfaceRegistry()
        {
            LogDebug("Rebuilding SurfaceRegistry...");
            if (surfaceTracker.RebuildRegistry())
            {
                LogDebug($"<color=green>Registry Rebuilt: {surfaceTracker.TotalTrackedCount} surfaces live.</color>");
            }
            else
            {
                LogDebug("<color=orange>Registry Rebuilt: No valid surfaces found. System is idling.</color>");
            }
        }

        /// <summary>
        /// Clears all live tracking data without touching the persistent list.
        /// </summary>
        public void ResetLiveTracking()
        {
            LogDebug("Resetting live tracking state...");
            surfaceTracker.ClearState();
            LogDebug("<color=orange>Live state cleared. Registry is now empty.</color>");
        }

        #endregion

        #region Internal Update

        private void CheckAndAllocateAtlas(TextureChunkCoordinator coordinator)
        {
            if (coordinator == null || !coordinator.IsInitialized)
            {
                LogDebug("Atlas creation deferred: Coordinator not ready.");
                return;
            }

            int requiredSlices = coordinator.RequiredSliceCount;
            int resolution = (int)coordinator.BaseResolution;

            if (EnsureAtlasCapacity(requiredSlices, resolution))
            {
                LogDebug($"Atlas texture <color=yellow>re-allocated</color>. Size: {resolution}px, Slices: {requiredSlices}.");
            }
            else
            {
                LogDebug($"Atlas texture <color=green>reused</color>. Current configuration is compatible.");
            }
        }

        /// <summary>
        /// Ensures the Atlas Texture Array matches the required dimensions.
        /// Returns true if the texture was (re)created, false if the existing one was compatible.
        /// Pure resource management logic. Decoupled from the coordinator to allow independent calls.
        /// </summary>
        private bool EnsureAtlasCapacity(int sliceCount, int resolution)
        {
            if (sliceCount <= 0 || resolution <= 0) return false;

            var newDesc = DefaultDescriptors.HeightmapPrecision(resolution, resolution).ToAtlasArray(sliceCount);
            bool shouldRecreate = _atlasArray == null || !_atlasArray.IsCreated;

            if (!shouldRecreate && _atlasArray != null)
            {
                if (!_atlasArray.Descriptor.InternalDescriptor.IsCompatible(newDesc))
                {
                    shouldRecreate = true;
                }
            }

            if (shouldRecreate)
            {
                string reason = (_atlasArray == null) ? "Initial Allocation" : "Descriptor/Size Mismatch";
                LogDebug($"Rebuilding GPU Resource: {reason} ({sliceCount} Slices @ {resolution}px)");

                if (_atlasArray != null) _atlasArray.Release();

                var newWrapper = new RenderTextureDescriptorWrapper { InternalDescriptor = newDesc };
                _atlasArray = new ManagedRenderTexture(newWrapper);
                _atlasArray.Create();

                return true;
            }

            return false;
        }

        private void ValidateSettings()
        {
            ValidateGridSettings();
            ValidateBatchSize();
            ValidateYRange();
            ValidateTrackerSettings();
        }

        private void ValidateGridSettings()
        {
            if (_textureCoordinator == null || !_textureCoordinator.IsInitialized) return;

            if (_textureCoordinator.UpdateGridSize((GridSize)chunkSize))
            {
                LogDebug($"Spatial Rebuild Triggered: GridSize changed to {chunkSize}. " +
                    $"Re-mapping SurfaceRegistry and synchronizing Topology.");

                _textureCoordinator.UpdateTopology(_surfaceRegistry);

                LogDebug("Spatial Rebuild Complete: Topology is now in sync with new GridSize.");
            }
        }

        private void ValidateBatchSize()
        {
            int sanitizedSize = Mathf.Max(1, batchSize);
            int potSize = Mathf.NextPowerOfTwo(sanitizedSize);

            if (batchSize != potSize)
            {
                batchSize = potSize;
            }

            if (_textureCoordinator == null || !_textureCoordinator.IsInitialized) return;

            if (_textureCoordinator.UpdateBatchSize(batchSize))
            {
                LogDebug($"Batch Configuration Updated: New BatchSize is {batchSize}. " +
                 $"This will affect the number of chunks processed per frame.");
            }
        }

        private void ValidateTrackerSettings()
        {
            if (surfaceTracker.SettingsDirty)
            {
                surfaceTracker.ApplySettings();
                LogDebug("SurfaceTracker Settings synchronized.");
            }
        }

        /// <summary>
        /// Ensures Y-range values are logical and syncs them to the active runtime state.
        /// Ensures min is below max and updates active baking bounds.
        /// </summary>
        private void ValidateYRange()
        {
            if (minRelativeY >= maxRelativeY)
            {
                maxRelativeY = minRelativeY + 1.0f;
            }

            if (!Mathf.Approximately(_activeMinRelativeY, minRelativeY) ||
                !Mathf.Approximately(_activeMaxRelativeY, maxRelativeY))
            {
                _activeMinRelativeY = minRelativeY;
                _activeMaxRelativeY = maxRelativeY;

                LogDebug($"Baking Y-Range updated: Min {_activeMinRelativeY} / Max {_activeMaxRelativeY}");

                if (IsReady)
                {
                    _textureCoordinator?.ForceRequeueAll();
                }
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

        private void UpdateLodReferenceSubscription(Transform targetReference)
        {
            if (_activeLodReference == targetReference) return;

            string oldName = _activeLodReference != null ? _activeLodReference.name : "None";
            string newName = targetReference != null ? targetReference.name : "None";

            _activeLodReference = targetReference;

            if (_textureCoordinator != null && _textureCoordinator.IsInitialized)
            {
                _textureCoordinator.UpdateLODs();
            }

            LogDebug($"<color=cyan>LOD Reference updated: {oldName} -> {newName}</color>");
        }

        #endregion

        #region Internal Event Udpate

        private void HandleOriginShift(Vector3 delta)
        {
            _textureCoordinator?.NotifyOriginShift(delta);
            _lastUpdatePos += delta;
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

            LogDebug($"<color=green>Topology sync completed.</color> Coordinator is now managing {_textureCoordinator.LodGridProvider.Count} chunk(s).");
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
            if (GUILayout.Button("Scan Surfaces", GUILayout.Height(30)))
            {
                script.RefreshPersistentSurfaceTable();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Clean-up Surfaces", GUILayout.Height(30)))
            {
                script.CleanupPersistentSurfaceTable();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Clear Surfaces", GUILayout.Height(30)))
            {
                script.ClearPersistentSurfaceTable();
            }
        }
    }
#endif
}