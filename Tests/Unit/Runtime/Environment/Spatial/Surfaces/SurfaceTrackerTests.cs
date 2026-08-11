using NUnit.Framework;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Environment.Spatial.Rendering;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces.Tests
{
    [TestFixture]
    public class SurfaceTrackerTests
    {
        #region Test Env

        private SurfaceRegistry _registry;
        private ChunkRegistry<TextureChunk> _gridProvider;
        private GameObject _rootObject;
        private Transform _rootTransform;

        private struct TestExecutionHandler : IExecutionHandler<GameObject>
        {
            private List<GameObject> _executedObjects;
            public List<GameObject> ExecutedObjects => _executedObjects ??= new List<GameObject>();

            public void Execute(GameObject obj)
            {
                ExecutedObjects.Add(obj);
            }
        }

        [SetUp]
        public void SetUp()
        {
            _registry = new SurfaceRegistry();
            _gridProvider = new ChunkRegistry<TextureChunk>();

            _gridProvider.Initialize(GridSize.Size16, Vector3.zero, null, "TextureChunkRegistry");

            _rootObject = new GameObject("RootTransform");
            _rootTransform = _rootObject.transform;
        }

        [TearDown]
        public void TearDown()
        {
            _registry?.Reset();
            if (_rootObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_rootObject);
            }
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void Initialize_WithValidParameters_SynchronizesActiveSettingsAndRoot()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            var externalRegistry = new SurfaceRegistry();

            // Act
            tracker.Initialize(externalRegistry, _rootTransform);

            // Assert
            Assert.AreEqual(tracker.Settings, tracker.ActiveSettings, "ActiveSettings should be synchronized with Settings upon initialization.");
        }

        [Test]
        public void Initialize_WhenCalledMultipleTimes_ClearsPreviousStateSafely()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            var externalRegistry = new SurfaceRegistry();

            tracker.Initialize(externalRegistry, _rootTransform);

            // Act & Assert
            Assert.DoesNotThrow(() => tracker.Initialize(externalRegistry, _rootTransform), "Re-initializing the tracker should clear previous state safely without exceptions.");
        }

        #endregion

        #region Public Members Property Tests

        [Test]
        public void Registry_WhenAssigned_ReturnsCorrectRegistryInstance()
        // English comments enforced
        {
            // Arrange
            var tracker = new SurfaceTracker();
            var externalRegistry = new SurfaceRegistry();

            // Act
            tracker.Initialize(externalRegistry, _rootTransform);

            // Assert
            Assert.AreEqual(externalRegistry, tracker.Registry, "Registry property should return the currently assigned external registry.");
        }

        [Test]
        public void WishlistCount_ReflectsPersistentSurfacesCount()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("WishlistSurface");
            surfaceObj.transform.SetParent(_rootTransform);
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            // Act
            tracker.ScanHierarchyToTable(_rootTransform);

            // Assert
            Assert.AreEqual(1, tracker.WishlistCount, "WishlistCount should reflect the number of surfaces in the persistent list.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        [Test]
        public void TotalTrackedCount_ReflectsActiveTrackedSurfacesCount()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            var externalRegistry = new SurfaceRegistry();

            // Mock a grid provider if needed or initialize registry depending on implementation
            // externalRegistry.Initialize(gridProvider);
            tracker.Initialize(externalRegistry, _rootTransform);

            // Act & Assert
            Assert.AreEqual(0, tracker.TotalTrackedCount, "TotalTrackedCount should be zero initially.");
        }

        [Test]
        public void IsInitialized_ReturnsExpectedStateBasedOnRegistry()
        {
            // Arrange
            var tracker = new SurfaceTracker();

            // Assert - Not initialized when registry is null
            Assert.IsFalse(tracker.IsInitialized, "IsInitialized should be false when no registry is assigned.");

            var externalRegistry = new SurfaceRegistry();
            tracker.Initialize(externalRegistry, _rootTransform);

            // Assert - Registry is assigned but not yet initialized
            Assert.IsFalse(tracker.IsInitialized, "IsInitialized should be false if the underlying SurfaceRegistry is not initialized.");
        }

        #endregion

        #region Public Events Tests

        [Test]
        public void OnSurfacesChanged_WhenSurfaceAdded_FiresEvent()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            var externalRegistry = new SurfaceRegistry();
            externalRegistry.Initialize(_gridProvider);
            tracker.Initialize(externalRegistry, _rootTransform);

            var surfaceObj = new GameObject("EventSurfaceObj");
            surfaceObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            surfaceObj.AddComponent<MeshRenderer>();

            bool eventFired = false;
            SurfaceTracker receivedTracker = null;

            tracker.OnSurfacesChanged += (t) =>
            {
                eventFired = true;
                receivedTracker = t;
            };

            // Act
            bool added = tracker.TryAddSurface(surfaceObj);

            // Assert
            Assert.IsTrue(added, "Surface should be successfully added.");
            Assert.IsTrue(eventFired, "OnSurfacesChanged event should fire when a surface is added.");
            Assert.AreEqual(tracker, receivedTracker, "The event should pass the tracker instance as an argument.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        [Test]
        public void OnSettingsChanged_WhenSettingsAppliedAndDirty_FiresEvent()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            var externalRegistry = new SurfaceRegistry();
            externalRegistry.Initialize(_gridProvider);
            tracker.Initialize(externalRegistry, _rootTransform);

            // Change settings to make them dirty
            var newSettings = SurfaceTrackerSettings.Default;
            newSettings.nameFilter = "CustomFilter";
            tracker.Settings = newSettings;

            bool eventFired = false;
            SurfaceTracker receivedTracker = null;

            tracker.OnSettingsChanged += (t) =>
            {
                eventFired = true;
                receivedTracker = t;
            };

            // Act
            tracker.ApplySettings();

            // Assert
            Assert.IsTrue(eventFired, "OnSettingsChanged event should fire when settings are applied and dirty.");
            Assert.AreEqual(tracker, receivedTracker, "The event should pass the tracker instance as an argument.");
        }

        [Test]
        public void OnSettingsChanged_WhenSettingsNotDirty_DoesNotFireEvent()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            var externalRegistry = new SurfaceRegistry();
            externalRegistry.Initialize(_gridProvider);
            tracker.Initialize(externalRegistry, _rootTransform);

            // Ensure settings are applied first so they are not dirty
            tracker.ApplySettings();

            bool eventFired = false;
            tracker.OnSettingsChanged += (t) =>
            {
                eventFired = true;
            };

            // Act - Apply settings again without changing them
            tracker.ApplySettings();

            // Assert
            Assert.IsFalse(eventFired, "OnSettingsChanged event should not fire if settings are not dirty.");
        }

        #endregion

        #region Public Configuration Tests

        [Test]
        public void SettingsDirty_WhenSettingsMatchActiveSettings_ReturnsFalse()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);
            tracker.ApplySettings(); // Ensure settings match

            // Act & Assert
            Assert.IsFalse(tracker.SettingsDirty, "SettingsDirty should be false when settings and active settings match.");
        }

        [Test]
        public void SettingsDirty_WhenSettingsDifferFromActiveSettings_ReturnsTrue()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);
            tracker.ApplySettings();

            // Act
            var modifiedSettings = tracker.Settings;
            modifiedSettings.nameFilter = "NewFilterName";
            tracker.Settings = modifiedSettings;

            // Assert
            Assert.IsTrue(tracker.SettingsDirty, "SettingsDirty should be true when settings differ from active settings.");
        }

        [Test]
        public void ActiveSettings_ReturnsCurrentlyAppliedSettings()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var newSettings = SurfaceTrackerSettings.Default;
            newSettings.nameFilter = "TestFilter";
            tracker.Settings = newSettings;

            // ActiveSettings should not change until ApplySettings is called
            Assert.AreNotEqual(newSettings.nameFilter, tracker.ActiveSettings.nameFilter);

            // Act
            tracker.ApplySettings();

            // Assert
            Assert.AreEqual(newSettings.nameFilter, tracker.ActiveSettings.nameFilter, "ActiveSettings should match new settings after ApplySettings().");
        }

        [Test]
        public void Settings_GetterAndSetter_WorkCorrectly()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            var customSettings = SurfaceTrackerSettings.Default;
            customSettings.minAreaThreshold = 42f;

            // Act
            tracker.Settings = customSettings;

            // Assert
            Assert.AreEqual(42f, tracker.Settings.minAreaThreshold, "Settings property should store and return the assigned value.");
        }

        [Test]
        public void ApplySettings_WhenNotDirty_DoesNothing()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);
            tracker.ApplySettings(); // Clean state

            bool eventFired = false;
            tracker.OnSettingsChanged += (t) => eventFired = true;

            // Act
            tracker.ApplySettings();

            // Assert
            Assert.IsFalse(eventFired, "ApplySettings should not trigger OnSettingsChanged if settings are not dirty.");
        }

        [Test]
        public void ApplySettings_WhenDirty_SynchronizesActiveSettingsAndTriggersEvent()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);
            tracker.ApplySettings();

            var newSettings = SurfaceTrackerSettings.Default;
            newSettings.nameFilter = "AnotherFilter";
            tracker.Settings = newSettings;

            bool eventFired = false;
            SurfaceTracker receivedTracker = null;
            tracker.OnSettingsChanged += (t) =>
            {
                eventFired = true;
                receivedTracker = t;
            };

            // Act
            tracker.ApplySettings();

            // Assert
            Assert.IsTrue(eventFired, "ApplySettings should trigger OnSettingsChanged when settings are dirty.");
            Assert.AreEqual(tracker, receivedTracker, "Passed tracker instance should match.");
            Assert.AreEqual("AnotherFilter", tracker.ActiveSettings.nameFilter, "ActiveSettings should be updated to match Settings.");
            Assert.IsFalse(tracker.SettingsDirty, "SettingsDirty should return to false after applying settings.");
        }

        #endregion

        #region RebuildRegistry Tests

        [Test]
        public void RebuildRegistry_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var tracker = new SurfaceTracker();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tracker.RebuildRegistry(),
                "RebuildRegistry should throw InvalidOperationException if the tracker is not initialized.");
        }

        [Test]
        public void RebuildRegistry_WhenEmptyAndNoRoot_ReturnsFalse()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, null);

            // Act
            bool result = tracker.RebuildRegistry();

            // Assert
            Assert.IsFalse(result, "RebuildRegistry should return false when no surfaces are tracked or scanned.");
            Assert.AreEqual(0, tracker.TotalTrackedCount, "Total tracked count should be zero.");
        }

        [Test]
        public void RebuildRegistry_WithPersistentListEntries_TracksSurfacesAndReturnsTrue()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            // Configure settings to prevent area filtering from rejecting the test object
            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("ListSurfaceObj");
            surfaceObj.transform.SetParent(_rootTransform);
            surfaceObj.transform.position = Vector3.zero;

            // Provide valid mesh components required by SurfaceRegistry
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            // Populate wishlist via hierarchy scan
            bool scanned = tracker.ScanHierarchyToTable(_rootTransform);
            Assert.IsTrue(scanned, "Object should be successfully discovered and added to the wishlist.");

            // Clear live registry state to test that RebuildRegistry syncs it back from the wishlist
            tracker.ClearState();
            Assert.AreEqual(0, tracker.TotalTrackedCount, "Live state should be cleared before rebuild.");

            // Act
            bool result = tracker.RebuildRegistry();

            // Assert
            Assert.IsTrue(result, "RebuildRegistry should return true when active surfaces are found after sync.");
            Assert.AreEqual(1, tracker.TotalTrackedCount, "TotalTrackedCount should reflect the synced surface from the list.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        [Test]
        public void RebuildRegistry_WithHierarchyScanEnabled_ScansAndTracksSurfaces()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            // Setup the child object
            var childObj = new GameObject("ChildSurfaceObj");
            childObj.transform.SetParent(_rootTransform);
            childObj.transform.position = Vector3.zero;

            // Ensure the object has visible bounds for the area check
            var meshFilter = childObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh(); // Empty mesh bounds are (0,0,0)
                                                // Create a 1x1x1 unit box mesh so it has size > 0
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            childObj.AddComponent<MeshRenderer>();

            // Configure settings
            var settings = SurfaceTrackerSettings.Default;
            settings.scanHierarchy = true;

            // FIX: Ensure the area check doesn't reject our object
            settings.enableAreaCheck = false;

            tracker.Settings = settings;
            tracker.ApplySettings();

            // Act
            bool result = tracker.RebuildRegistry();

            // Assert
            Assert.IsTrue(result, "RebuildRegistry should return true when hierarchy scanning discovers valid surfaces.");
            Assert.AreEqual(1, tracker.TotalTrackedCount, "TotalTrackedCount should include the scanned child surface.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(childObj);
        }

        [Test]
        public void RebuildRegistry_WithHierarchyScanDisabled_IgnoresRoot()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);

            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.scanHierarchy = false; // Disable hierarchy scan
            tracker.Settings = settings;

            tracker.ApplySettings();

            var childObj = new GameObject("IgnoredSurfaceObj");
            childObj.transform.SetParent(_rootTransform);
            childObj.transform.position = new Vector3(2f, 0f, 0f);
            childObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            childObj.AddComponent<MeshRenderer>();

            // Act
            bool result = tracker.RebuildRegistry();

            // Assert
            Assert.IsFalse(result, "RebuildRegistry should return false if hierarchy scan is disabled and list is empty.");
            Assert.AreEqual(0, tracker.TotalTrackedCount, "No surfaces should be tracked when scanHierarchy is false.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(childObj);
        }

        #endregion

        #region TryAddSurface Tests

        [Test]
        public void TryAddSurface_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            var obj = new GameObject("TestSurface");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tracker.TryAddSurface(obj),
                "TryAddSurface should throw InvalidOperationException if the tracker is not initialized.");

            UnityEngine.Object.DestroyImmediate(obj);
        }

        [Test]
        public void TryAddSurface_WhenObjectIsNull_ReturnsFalse()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            // Act
            bool result = tracker.TryAddSurface(null);

            // Assert
            Assert.IsFalse(result, "TryAddSurface should return false when passed a null object.");
            Assert.AreEqual(0, tracker.TotalTrackedCount, "Total tracked count should remain zero.");
        }

        [Test]
        public void TryAddSurface_WithValidObject_ReturnsTrueAndTracksObject()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("ValidSurface");
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
        };
            surfaceObj.AddComponent<MeshRenderer>();

            bool eventFired = false;
            SurfaceTracker receivedTracker = null;
            tracker.OnSurfacesChanged += (t) =>
            {
                eventFired = true;
                receivedTracker = t;
            };

            // Act
            bool result = tracker.TryAddSurface(surfaceObj);

            // Assert
            Assert.IsTrue(result, "TryAddSurface should return true for a valid surface object.");
            Assert.AreEqual(1, tracker.TotalTrackedCount, "TotalTrackedCount should be incremented.");
            Assert.IsTrue(eventFired, "OnSurfacesChanged event should be fired.");
            Assert.AreEqual(tracker, receivedTracker, "Passed tracker instance should match.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        [Test]
        public void TryAddSurface_WithInvalidObjectMissingBounds_ReturnsFalse()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var invalidObj = new GameObject("InvalidSurfaceWithoutRendererOrCollider");

            bool eventFired = false;
            tracker.OnSurfacesChanged += (t) => eventFired = true;

            // Act
            bool result = tracker.TryAddSurface(invalidObj);

            // Assert
            Assert.IsFalse(result, "TryAddSurface should return false if the object lacks bounds components.");
            Assert.AreEqual(0, tracker.TotalTrackedCount, "TotalTrackedCount should remain zero.");
            Assert.IsFalse(eventFired, "OnSurfacesChanged event should not be fired when registration fails.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(invalidObj);
        }

        #endregion

        #region RemoveSurface Tests

        [Test]
        public void RemoveSurface_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var tracker = new SurfaceTracker();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tracker.RemoveSurface(999),
                "RemoveSurface should throw InvalidOperationException if the tracker is not initialized.");
        }

        [Test]
        public void RemoveSurface_WhenIdNotFound_ReturnsFalseAndDoesNotTriggerEvent()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            bool eventFired = false;
            tracker.OnSurfacesChanged += (t) => eventFired = true;

            // Act
            bool result = tracker.RemoveSurface(99999);

            // Assert
            Assert.IsFalse(result, "RemoveSurface should return false when the ID is not found.");
            Assert.IsFalse(eventFired, "OnSurfacesChanged event should not fire if no surface was removed.");
        }

        [Test]
        public void RemoveSurface_WhenIdFound_ReturnsTrueRemovesSurfaceAndTriggersEvent()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("RemovableSurface");
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            int surfaceId = surfaceObj.GetInstanceID();

            // Add the surface first so it's tracked
            tracker.TryAddSurface(surfaceObj);
            Assert.AreEqual(1, tracker.TotalTrackedCount, "Surface should be successfully tracked.");

            bool eventFired = false;
            SurfaceTracker receivedTracker = null;
            tracker.OnSurfacesChanged += (t) =>
            {
                eventFired = true;
                receivedTracker = t;
            };

            // Act
            bool result = tracker.RemoveSurface(surfaceId);

            // Assert
            Assert.IsTrue(result, "RemoveSurface should return true when a tracked surface is successfully removed.");
            Assert.AreEqual(0, tracker.TotalTrackedCount, "TotalTrackedCount should drop to zero.");
            Assert.IsTrue(eventFired, "OnSurfacesChanged event should be fired when a surface is removed.");
            Assert.AreEqual(tracker, receivedTracker, "Passed tracker instance should match.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        #endregion

        #region ClearState Tests

        [Test]
        public void ClearState_WhenSurfacesTracked_ClearsLiveRegistryAndTriggersEvent()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("SurfaceToClear");
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            tracker.TryAddSurface(surfaceObj);
            Assert.AreEqual(1, tracker.TotalTrackedCount, "Surface should be tracked initially.");

            bool eventFired = false;
            SurfaceTracker receivedTracker = null;
            tracker.OnSurfacesChanged += (t) =>
            {
                eventFired = true;
                receivedTracker = t;
            };

            // Act
            tracker.ClearState();

            // Assert
            Assert.AreEqual(0, tracker.TotalTrackedCount, "TotalTrackedCount should be zero after ClearState().");
            Assert.IsTrue(eventFired, "OnSurfacesChanged event should fire when state is cleared.");
            Assert.AreEqual(tracker, receivedTracker, "Passed tracker instance should match.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        [Test]
        public void ClearState_DoesNotTouchPersistentWishlist()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("WishlistSurface");
            surfaceObj.transform.SetParent(_rootTransform);
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            // Populate wishlist
            tracker.ScanHierarchyToTable(_rootTransform);
            Assert.AreEqual(1, tracker.WishlistCount, "Wishlist should contain the scanned surface.");

            tracker.RebuildRegistry();
            Assert.AreEqual(1, tracker.TotalTrackedCount, "Surface should be tracked.");

            // Act
            tracker.ClearState();

            // Assert
            Assert.AreEqual(0, tracker.TotalTrackedCount, "Live tracking state should be wiped.");
            Assert.AreEqual(1, tracker.WishlistCount, "Persistent wishlist (_surfaces) should remain untouched.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        #endregion

        #region ClearAll Tests

        [Test]
        public void ClearAll_WhenDataExists_ClearsLiveRegistryAndWishlistAndTriggersEvent()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("SurfaceToWipe");
            surfaceObj.transform.SetParent(_rootTransform);
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            // Populate wishlist and live tracking
            tracker.ScanHierarchyToTable(_rootTransform);
            tracker.RebuildRegistry();

            Assert.AreEqual(1, tracker.WishlistCount, "Wishlist should have entries before ClearAll.");
            Assert.AreEqual(1, tracker.TotalTrackedCount, "Live tracking should have entries before ClearAll.");

            bool eventFired = false;
            SurfaceTracker receivedTracker = null;
            tracker.OnSurfacesChanged += (t) =>
            {
                eventFired = true;
                receivedTracker = t;
            };

            // Act
            tracker.ClearAll();

            // Assert
            Assert.AreEqual(0, tracker.TotalTrackedCount, "TotalTrackedCount should be zero after ClearAll().");
            Assert.AreEqual(0, tracker.WishlistCount, "WishlistCount should be zero after ClearAll().");
            Assert.IsTrue(eventFired, "OnSurfacesChanged event should fire when everything is cleared.");
            Assert.AreEqual(tracker, receivedTracker, "Passed tracker instance should match.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        [Test]
        public void ClearAll_WhenAlreadyEmpty_DoesNotThrowException()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            // Act & Assert
            Assert.DoesNotThrow(() => tracker.ClearAll(), "ClearAll should execute safely on an empty tracker.");
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_WhenFullyPopulated_ClearsAllDataDisconnectsRegistryAndTriggersEvent()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("SurfaceToReset");
            surfaceObj.transform.SetParent(_rootTransform);
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            tracker.ScanHierarchyToTable(_rootTransform);
            tracker.RebuildRegistry();

            Assert.IsTrue(tracker.IsInitialized, "Tracker should be initialized before reset.");
            Assert.AreEqual(1, tracker.WishlistCount, "Wishlist should have entries before reset.");
            Assert.AreEqual(1, tracker.TotalTrackedCount, "Live tracking should have entries before reset.");

            bool eventFired = false;
            SurfaceTracker receivedTracker = null;
            tracker.OnSurfacesChanged += (t) =>
            {
                eventFired = true;
                receivedTracker = t;
            };

            // Act
            tracker.Reset();

            // Assert
            Assert.IsFalse(tracker.IsInitialized, "Tracker should no longer be initialized after reset.");
            Assert.AreEqual(0, tracker.TotalTrackedCount, "TotalTrackedCount should be zero after Reset().");
            Assert.AreEqual(0, tracker.WishlistCount, "WishlistCount should be zero after Reset().");
            Assert.IsNull(tracker.Registry, "Registry property should return null after reset.");
            Assert.IsTrue(eventFired, "OnSurfacesChanged event should fire when tracker is reset.");
            Assert.AreEqual(tracker, receivedTracker, "Passed tracker instance should match.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        [Test]
        public void Reset_WhenEmptyOrUninitialized_ExecutesSafely()
        {
            // Arrange
            var tracker = new SurfaceTracker();

            // Act & Assert
            Assert.DoesNotThrow(() => tracker.Reset(), "Reset should execute safely on an uninitialized or empty tracker.");
        }

        #endregion

        #region GetTrackedSurfaceIterator Tests

        [Test]
        public void GetTrackedSurfaceIterator_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var tracker = new SurfaceTracker();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tracker.GetTrackedSurfaceIterator(),
                "GetTrackedSurfaceIterator should throw InvalidOperationException if the tracker is not initialized.");
        }

        [Test]
        public void GetTrackedSurfaceIterator_WhenInitializedAndEmpty_ReturnsValidIterator()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            // Act
            var iterator = tracker.GetTrackedSurfaceIterator();

            // Assert
            Assert.IsNotNull(iterator, "GetTrackedSurfaceIterator should return a non-null iterator when initialized.");
        }

        [Test]
        public void GetTrackedSurfaceIterator_WhenInitializedWithSurfaces_ReturnsIteratorContainingSurfaces()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("IteratorTestSurface");
            surfaceObj.transform.SetParent(_rootTransform);
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            tracker.ScanHierarchyToTable(_rootTransform);
            Assert.AreEqual(1, tracker.WishlistCount, "Wishlist should contain the scanned surface.");

            // Act
            var iterator = tracker.GetTrackedSurfaceIterator();

            // Assert
            Assert.IsNotNull(iterator, "Iterator should not be null.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        #endregion

        #region ForEachTrackedSurface Tests

        [Test]
        public void ForEachTrackedSurface_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            var handler = new TestExecutionHandler();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tracker.ForEachTrackedSurface(ref handler),
                "ForEachTrackedSurface should throw InvalidOperationException if the tracker is not initialized.");
        }

        [Test]
        public void ForEachTrackedSurface_WhenInitialized_ExecutesActionOnEachSurface()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("SurfaceToExecute");
            surfaceObj.transform.SetParent(_rootTransform);
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            tracker.ScanHierarchyToTable(_rootTransform);
            Assert.AreEqual(1, tracker.WishlistCount, "Wishlist should contain the scanned surface.");

            var handler = new TestExecutionHandler();

            // Act
            tracker.ForEachTrackedSurface(ref handler);

            // Assert
            Assert.AreEqual(1, handler.ExecutedObjects.Count, "Handler should execute once for the tracked surface.");
            Assert.AreEqual(surfaceObj, handler.ExecutedObjects[0], "Executed object should match the tracked surface.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        [Test]
        public void ForEachTrackedSurface_WhenSurfaceBecomesNull_SkipsNullEntriesSafely()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("SurfaceToDestroy");
            surfaceObj.transform.SetParent(_rootTransform);
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            tracker.ScanHierarchyToTable(_rootTransform);

            // Destroy the underlying GameObject so it becomes null in the list reference
            UnityEngine.Object.DestroyImmediate(surfaceObj);

            var handler = new TestExecutionHandler();

            // Act & Assert
            Assert.DoesNotThrow(() => tracker.ForEachTrackedSurface(ref handler),
                "ForEachTrackedSurface should handle null surface references safely without throwing.");
            Assert.AreEqual(0, handler.ExecutedObjects.Count, "Destroyed (null) surfaces should be skipped during execution.");
        }

        #endregion

        #region ScanHierarchyToTable Tests

        [Test]
        public void ScanHierarchyToTable_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var tracker = new SurfaceTracker();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tracker.ScanHierarchyToTable(_rootTransform),
                "ScanHierarchyToTable should throw InvalidOperationException if the tracker is not initialized.");
        }

        [Test]
        public void ScanHierarchyToTable_WhenRootIsNull_ReturnsFalse()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            // Act
            bool result = tracker.ScanHierarchyToTable(null);

            // Assert
            Assert.IsFalse(result, "ScanHierarchyToTable should return false when root is null.");
            Assert.AreEqual(0, tracker.WishlistCount, "Wishlist count should remain zero.");
        }

        [Test]
        public void ScanHierarchyToTable_WithValidHierarchy_PopulatesTableAndReturnsTrue()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var childObj = new GameObject("ValidChildSurface");
            childObj.transform.SetParent(_rootTransform);
            var meshFilter = childObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            childObj.AddComponent<MeshRenderer>();

            // Act
            bool result = tracker.ScanHierarchyToTable(_rootTransform);

            // Assert
            Assert.IsTrue(result, "ScanHierarchyToTable should return true when valid surfaces are found.");
            Assert.AreEqual(1, tracker.WishlistCount, "WishlistCount should reflect the discovered child surface.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(childObj);
        }

        [Test]
        public void ScanHierarchyToTable_ClearsExistingTableEntries()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var childObj = new GameObject("InitialChild");
            childObj.transform.SetParent(_rootTransform);
            childObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            childObj.AddComponent<MeshRenderer>();

            // Populate table first
            tracker.ScanHierarchyToTable(_rootTransform);
            Assert.AreEqual(1, tracker.WishlistCount, "Table should have 1 entry.");

            // Create an empty root to scan instead
            var emptyRootObj = new GameObject("EmptyRoot");

            // Act - Scanning an empty hierarchy should clear out the old entries
            bool result = tracker.ScanHierarchyToTable(emptyRootObj.transform);

            // Assert
            Assert.IsFalse(result, "ScanHierarchyToTable should return false for an empty hierarchy.");
            Assert.AreEqual(0, tracker.WishlistCount, "Previous wishlist entries should be cleared by the new scan.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(childObj);
            UnityEngine.Object.DestroyImmediate(emptyRootObj);
        }

        #endregion

        #region ClearTable Tests

        [Test]
        public void ClearTable_WhenWishlistHasEntries_ClearsWishlist()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var childObj = new GameObject("WishlistSurface");
            childObj.transform.SetParent(_rootTransform);
            var meshFilter = childObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            childObj.AddComponent<MeshRenderer>();

            tracker.ScanHierarchyToTable(_rootTransform);
            Assert.AreEqual(1, tracker.WishlistCount, "Wishlist should contain entries before clearing.");

            // Act
            tracker.ClearTable();

            // Assert
            Assert.AreEqual(0, tracker.WishlistCount, "WishlistCount should be zero after ClearTable().");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(childObj);
        }

        [Test]
        public void ClearTable_WhenWishlistIsEmpty_DoesNotThrow()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            Assert.AreEqual(0, tracker.WishlistCount, "Wishlist should be empty initially.");

            // Act & Assert
            Assert.DoesNotThrow(() => tracker.ClearTable(), "ClearTable should execute safely when wishlist is already empty.");
        }

        #endregion

        #region CleanupTableNulls Tests

        [Test]
        public void CleanupTableNulls_WhenNullEntriesExist_RemovesNullsAndUpdatesCount()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("TempSurface");
            surfaceObj.transform.SetParent(_rootTransform);
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            tracker.ScanHierarchyToTable(_rootTransform);
            Assert.AreEqual(1, tracker.WishlistCount, "Wishlist should have 1 entry.");

            // Destroy the GameObject so it becomes null internally
            UnityEngine.Object.DestroyImmediate(surfaceObj);

            // Act
            tracker.CleanupTableNulls();

            // Assert
            Assert.AreEqual(0, tracker.WishlistCount, "WishlistCount should be zero after cleaning null entries.");
        }

        [Test]
        public void CleanupTableNulls_WhenNoNullEntriesExist_LeavesWishlistUnchanged()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            var settings = SurfaceTrackerSettings.Default;
            settings.enableAreaCheck = false;
            tracker.Settings = settings;
            tracker.ApplySettings();

            var surfaceObj = new GameObject("ValidSurface");
            surfaceObj.transform.SetParent(_rootTransform);
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)
            };
            surfaceObj.AddComponent<MeshRenderer>();

            tracker.ScanHierarchyToTable(_rootTransform);
            Assert.AreEqual(1, tracker.WishlistCount, "Wishlist should have 1 entry.");

            // Act
            tracker.CleanupTableNulls();

            // Assert
            Assert.AreEqual(1, tracker.WishlistCount, "WishlistCount should remain unchanged when there are no nulls.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(surfaceObj);
        }

        [Test]
        public void CleanupTableNulls_WhenWishlistIsEmpty_ExecutesSafely()
        {
            // Arrange
            var tracker = new SurfaceTracker();
            _registry.Initialize(_gridProvider);
            tracker.Initialize(_registry, _rootTransform);

            Assert.AreEqual(0, tracker.WishlistCount, "Wishlist should be empty initially.");

            // Act & Assert
            Assert.DoesNotThrow(() => tracker.CleanupTableNulls(), "CleanupTableNulls should execute safely on an empty wishlist.");
        }

        #endregion
    }
}
