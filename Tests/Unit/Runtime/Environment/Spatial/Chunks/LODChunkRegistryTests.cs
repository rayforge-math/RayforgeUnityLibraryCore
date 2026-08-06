using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks.Tests
{
    [TestFixture]
    public class LODChunkRegistryTests
    {
        #region Test Env

        private GameObject _viewerObject1;
        private GameObject _viewerObject2;

        private LODChunkRegistry<TestLodChunk> _registry;

        private class TestLodChunk : LODChunk<TestLodChunk>
        { }

        private struct DummyConfigureHandler : IExecutionHandler<TestLodChunk>
        {
            public void Execute(TestLodChunk target) { }
        }

        private struct TrackingConfigureHandler : IExecutionHandler<TestLodChunk>
        {
            public bool WasExecuted { get; private set; }

            public void Execute(TestLodChunk target)
            {
                WasExecuted = true;
            }
        }

        private struct KeyCollectorAction : IExecutionHandler<Vector3Int>
        {
            private List<Vector3Int> _collectedKeys;

            public List<Vector3Int> CollectedKeys => _collectedKeys ??= new List<Vector3Int>();

            public KeyCollectorAction(int initialCapacity)
            {
                _collectedKeys = new List<Vector3Int>(initialCapacity);
            }

            public void Execute(Vector3Int value)
            {
                _collectedKeys ??= new List<Vector3Int>();
                _collectedKeys.Add(value);
            }
        }

        private struct CountAction : IExecutionHandler<Vector3Int>
        {
            public int ExecutionCount;

            public void Execute(Vector3Int value)
            {
                ExecutionCount++;
            }
        }

        private static List<T> ToList<T>(IIterator<T> iterator)
        {
            var list = new List<T>();
            while (iterator.MoveNext())
            {
                list.Add(iterator.Current);
            }
            return list;
        }

        private static List<Vector3Int> CollectKeys(GridLODEdgeState state)
        {
            var result = new List<Vector3Int>();

            // Iterates through the state until no more valid spatial keys match the LOD criteria
            while (state.MoveNext(ref state, out Vector3Int key))
            {
                result.Add(key);
            }

            return result;
        }

        [SetUp]
        public void SetUp()
        {
            // Create fresh GameObjects and a new registry before each test runs
            _viewerObject1 = new GameObject("Viewer1");
            _viewerObject2 = new GameObject("Viewer2");
            _registry = new LODChunkRegistry<TestLodChunk>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up the GameObjects to prevent memory leaks in the Unity Editor
            UnityEngine.Object.DestroyImmediate(_viewerObject1);
            UnityEngine.Object.DestroyImmediate(_viewerObject2);
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void Initialize_WithSingleLodDistance_Succeeds()
        {
            // Testing the boundary case: exactly one valid LOD distance
            var gridSize = GridSize.Size10;
            var anchor = Vector3.zero;
            float[] singleDistance = { 50f };

            // Should not throw
            Assert.DoesNotThrow(() =>
                _registry.Initialize(gridSize, anchor, singleDistance, _viewerObject1.transform)
            );
            Assert.IsTrue(_registry.IsInitialized, "Registry should be initialized with a single LOD distance.");
        }

        [Test]
        public void Initialize_WithParentAndCustomName_Succeeds()
        {
            // Testing the optional parameters passed to the base class
            var gridSize = GridSize.Size10;
            var anchor = Vector3.zero;
            float[] lodDistances = { 50f, 100f };
            string customName = "CustomTerrainLOD";

            Assert.DoesNotThrow(() =>
                _registry.Initialize(
                    gridSize,
                    anchor,
                    lodDistances,
                    _viewerObject1.transform,
                    deactivateOnCulled: true,
                    parent: _viewerObject2.transform,

                    name: customName
                )
            );
            Assert.IsTrue(_registry.IsInitialized);
        }

        [Test]
        public void Initialize_CalledMultipleTimes_ReinitializesSuccessfully()
        {
            // Testing if the registry can handle being re-initialized with new settings
            var gridSize = GridSize.Size10;
            var anchor = Vector3.zero;
            float[] initialDistances = { 50f, 100f };
            float[] newDistances = { 20f, 40f, 80f };

            // First initialization
            _registry.Initialize(gridSize, anchor, initialDistances, _viewerObject1.transform);
            Assert.IsTrue(_registry.IsInitialized);

            // Second initialization with a different viewer and distances
            Assert.DoesNotThrow(() =>
                _registry.Initialize(gridSize, anchor, newDistances, _viewerObject2.transform, deactivateOnCulled: false)
            );
            Assert.IsTrue(_registry.IsInitialized, "Registry should remain initialized after re-initialization.");
        }

        [Test]
        public void Initialize_WithLargeLodDistanceArray_Succeeds()
        {
            // Testing a stress-case for the Span conversion and distance processing
            var gridSize = GridSize.Size10;
            var anchor = Vector3.zero;

            // Create a large array of LOD distances
            float[] largeDistances = new float[100];
            for (int i = 0; i < largeDistances.Length; i++)
            {
                largeDistances[i] = (i + 1) * 10f;
            }

            Assert.DoesNotThrow(() =>
                _registry.Initialize(gridSize, anchor, largeDistances, _viewerObject1.transform)
            );
        }

        [Test]
        public void Initialize_WithValidArguments_SetsPropertiesSuccessfully()
        {
            var gridSize = GridSize.Size10;
            var anchor = Vector3.zero;
            float[] lodDistances = { 50f, 100f, 200f };

            _registry.Initialize(gridSize, anchor, lodDistances, _viewerObject1.transform, deactivateOnCulled: false);

            Assert.IsTrue(_registry.IsInitialized, "The registry must be marked as initialized.");
        }

        [Test]
        public void Initialize_WithNullViewer_ThrowsArgumentNullException()
        {
            var gridSize = GridSize.Size10;
            var anchor = Vector3.zero;
            float[] lodDistances = { 50f, 100f };
            Transform nullViewer = null;

            Assert.Throws<ArgumentNullException>(() =>
                _registry.Initialize(gridSize, anchor, lodDistances, nullViewer)
            );
        }

        [Test]
        public void Initialize_WithEmptyLodDistances_ThrowsArgumentException()
        {
            var gridSize = GridSize.Size10;
            var anchor = Vector3.zero;
            float[] emptyDistances = { };

            Assert.Throws<ArgumentException>(() =>
                _registry.Initialize(gridSize, anchor, emptyDistances, _viewerObject1.transform)
            );
        }

        [Test]
        public void Initialize_WithZeroOrNegativeFirstLodDistance_ThrowsArgumentException()
        {
            var gridSize = GridSize.Size10;
            var anchor = Vector3.zero;
            float[] invalidDistances = { 0f, 100f };

            Assert.Throws<ArgumentException>(() =>
                _registry.Initialize(gridSize, anchor, invalidDistances, _viewerObject1.transform)
            );

            float[] negativeDistances = { -10f, 100f };

            Assert.Throws<ArgumentException>(() =>
                _registry.Initialize(gridSize, anchor, negativeDistances, _viewerObject1.transform)
            );
        }

        #endregion

        #region Viewer Property Tests

        [Test]
        public void Viewer_Set_WithValidTransform_UpdatesViewerProperty()
        {
            // Act
            _registry.Viewer = _viewerObject1.transform;

            // Assert
            Assert.AreEqual(_viewerObject1.transform, _registry.Viewer, "The Viewer property should return the newly assigned Transform.");
        }

        [Test]
        public void Viewer_Set_WithNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => _registry.Viewer = null,
                "Setting Viewer to null must throw an ArgumentNullException.");

            Assert.AreEqual("value", exception.ParamName, "The exception parameter name must be 'value'.");
        }

        [Test]
        public void Viewer_Set_WithNewValue_ReplacesExistingReference()
        {
            // Arrange
            _registry.Viewer = _viewerObject1.transform;

            // Act
            _registry.Viewer = _viewerObject2.transform;

            // Assert
            Assert.AreEqual(_viewerObject2.transform, _registry.Viewer, "The Viewer property should update to the new reference.");
        }

        [Test]
        public void Viewer_Set_WithSameReference_SucceedsWithoutChangingState()
        {
            // Arrange
            _registry.Viewer = _viewerObject1.transform;

            // Act & Assert
            Assert.DoesNotThrow(() => _registry.Viewer = _viewerObject1.transform,
                "Assigning the same reference again should be a no-op and not throw.");
            Assert.AreEqual(_viewerObject1.transform, _registry.Viewer);
        }

        #endregion

        #region DeactivateOnCulled Property Tests

        [Test]
        public void DeactivateOnCulled_WhenInitializedAsTrue_ReturnsTrue()
        {
            // Arrange & Act
            _registry.Initialize(
                GridSize.Size10,
                Vector3.zero,
                new float[] { 50f },
                _viewerObject1.transform,
                deactivateOnCulled: true
            );

            // Assert
            Assert.IsTrue(_registry.DeactivateOnCulled, "DeactivateOnCulled should return true when passed true during initialization.");
        }

        [Test]
        public void DeactivateOnCulled_WhenInitializedAsFalse_ReturnsFalse()
        {
            // Arrange & Act
            _registry.Initialize(
                GridSize.Size10,
                Vector3.zero,
                new float[] { 50f },
                _viewerObject1.transform,
                deactivateOnCulled: false
            );

            // Assert
            Assert.IsFalse(_registry.DeactivateOnCulled, "DeactivateOnCulled should return false when passed false during initialization.");
        }

        [Test]
        public void DeactivateOnCulled_WhenReinitialized_UpdatesToNewValue()
        {
            // Arrange - Initialized with true
            _registry.Initialize(
                GridSize.Size10,
                Vector3.zero,
                new float[] { 50f },
                _viewerObject1.transform,
                deactivateOnCulled: true
            );
            Assert.IsTrue(_registry.DeactivateOnCulled);

            // Act - Re-initialize with false
            _registry.Initialize(
                GridSize.Size10,
                Vector3.zero,
                new float[] { 50f },
                _viewerObject1.transform,
                deactivateOnCulled: false
            );

            // Assert
            Assert.IsFalse(_registry.DeactivateOnCulled, "DeactivateOnCulled should reflect the updated parameter upon re-initialization.");
        }

        #endregion

        #region LodDistances & LodSqrDistances Tests

        [Test]
        public void LodDistances_BeforeInitialization_ReturnsEmptySpan()
        {
            // Act
            ReadOnlySpan<float> distances = _registry.LodDistances;

            // Assert
            Assert.IsTrue(distances.IsEmpty, "LodDistances must return an empty span prior to initialization.");
        }

        [Test]
        public void LodSqrDistances_BeforeInitialization_ReturnsEmptySpan()
        {
            // Act
            ReadOnlySpan<float> sqrDistances = _registry.LodSqrDistances;

            // Assert
            Assert.IsTrue(sqrDistances.IsEmpty, "LodSqrDistances must return an empty span prior to initialization.");
        }

        [Test]
        public void LodDistancesAndLodSqrDistances_AfterInitialization_ReturnCorrectValuesAndLengths()
        {
            // Arrange
            float[] inputDistances = { 10f, 50f, 100f };

            // Act
            _registry.Initialize(GridSize.Size10, Vector3.zero, inputDistances, _viewerObject1.transform);

            ReadOnlySpan<float> distances = _registry.LodDistances;
            ReadOnlySpan<float> sqrDistances = _registry.LodSqrDistances;

            // Assert
            Assert.AreEqual(3, distances.Length, "LodDistances length must match the initialized array length.");
            Assert.AreEqual(3, sqrDistances.Length, "LodSqrDistances length must match the initialized array length.");

            // Verify linear distances
            Assert.AreEqual(10f, distances[0]);
            Assert.AreEqual(50f, distances[1]);
            Assert.AreEqual(100f, distances[2]);

            // Verify squared distances
            Assert.AreEqual(100f, sqrDistances[0], 0.001f);
            Assert.AreEqual(2500f, sqrDistances[1], 0.001f);
            Assert.AreEqual(10000f, sqrDistances[2], 0.001f);
        }

        [Test]
        public void LodDistancesAndLodSqrDistances_AfterUpdateLodDistances_ReflectNewValues()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f }, _viewerObject1.transform);
            float[] newDistances = { 20f, 40f };

            // Act
            _registry.UpdateLodDistances(newDistances);

            ReadOnlySpan<float> distances = _registry.LodDistances;
            ReadOnlySpan<float> sqrDistances = _registry.LodSqrDistances;

            // Assert
            Assert.AreEqual(2, distances.Length);
            Assert.AreEqual(20f, distances[0]);
            Assert.AreEqual(40f, distances[1]);

            Assert.AreEqual(400f, sqrDistances[0], 0.001f);
            Assert.AreEqual(1600f, sqrDistances[1], 0.001f);
        }

        #endregion

        #region LodCount Property Tests

        [Test]
        public void LodCount_BeforeInitialization_ReturnsZero()
        {
            // Act
            int count = _registry.LodCount;

            // Assert
            Assert.AreEqual(0, count, "LodCount must return 0 prior to initialization when arrays are null.");
        }

        [Test]
        public void LodCount_AfterInitialization_ReturnsCorrectCount()
        {
            // Arrange
            float[] lodDistances = { 10f, 25f, 50f, 100f };

            // Act
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);

            // Assert
            Assert.AreEqual(4, _registry.LodCount, "LodCount must match the number of configured LOD distances.");
        }

        [Test]
        public void LodCount_AfterUpdateLodDistances_ReflectsNewCount()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 20f }, _viewerObject1.transform);
            Assert.AreEqual(2, _registry.LodCount);

            // Act
            _registry.UpdateLodDistances(new float[] { 15f, 30f, 60f });

            // Assert
            Assert.AreEqual(3, _registry.LodCount, "LodCount must update when a new array length of LOD distances is applied.");
        }

        #endregion

        #region ActiveCellCount Property Tests

        [Test]
        public void ActiveCellCount_OnInitialization_IsZero()
        {
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 20f }, _viewerObject1.transform);
            Assert.AreEqual(0, _registry.ActiveCellCount, "ActiveCellCount should be 0 upon initialization.");
        }

        [Test]
        public void ActiveCellCount_WhenChunkCreatedInActiveLODRange_IncrementsCount()
        {
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 20f }, _viewerObject1.transform);
            _viewerObject1.transform.position = Vector3.zero;

            var handler = new DummyConfigureHandler();
            _registry.GetOrCreateChunk(Vector3Int.zero, ref handler, out _);

            Assert.AreEqual(1, _registry.ActiveCellCount, "ActiveCellCount should increment when a chunk is created in active LOD range.");
        }

        [Test]
        public void ActiveCellCount_WhenChunkCreatedOutsideLODRange_StaysZero()
        {
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 20f }, _viewerObject1.transform);
            _viewerObject1.transform.position = new Vector3(1000f, 1000f, 1000f);

            var handler = new DummyConfigureHandler();
            _registry.GetOrCreateChunk(Vector3Int.zero, ref handler, out _);

            Assert.AreEqual(0, _registry.ActiveCellCount, "ActiveCellCount should remain 0 when a chunk is created outside LOD range (culled).");
        }

        [Test]
        public void ActiveCellCount_WhenChunkTransitionsFromCulledToActive_IncrementsCount()
        {
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 20f }, _viewerObject1.transform);
            _viewerObject1.transform.position = new Vector3(1000f, 1000f, 1000f);

            var handler = new DummyConfigureHandler();
            _registry.GetOrCreateChunk(Vector3Int.zero, ref handler, out _);
            Assert.AreEqual(0, _registry.ActiveCellCount);

            // Move viewer within range and trigger LOD update
            _viewerObject1.transform.position = Vector3.zero;
            _registry.UpdateLODs();

            Assert.AreEqual(1, _registry.ActiveCellCount, "ActiveCellCount should increment when chunk transitions from culled to active.");
        }

        [Test]
        public void ActiveCellCount_WhenChunkTransitionsFromActiveToCulled_DecrementsCount()
        {
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 20f }, _viewerObject1.transform, deactivateOnCulled: true);
            _viewerObject1.transform.position = Vector3.zero;

            var handler = new DummyConfigureHandler();
            _registry.GetOrCreateChunk(Vector3Int.zero, ref handler, out _);
            Assert.AreEqual(1, _registry.ActiveCellCount);

            // Move viewer far away and trigger LOD update
            _viewerObject1.transform.position = new Vector3(1000f, 1000f, 1000f);
            _registry.UpdateLODs();

            Assert.AreEqual(0, _registry.ActiveCellCount, "ActiveCellCount should decrement when chunk transitions from active to culled.");
        }

        [Test]
        public void ActiveCellCount_WhenChunkTransitionsBetweenActiveLODs_StaysUnchanged()
        {
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 20f }, _viewerObject1.transform);
            _viewerObject1.transform.position = Vector3.zero;

            var handler = new DummyConfigureHandler();
            _registry.GetOrCreateChunk(Vector3Int.zero, ref handler, out _);
            Assert.AreEqual(1, _registry.ActiveCellCount);

            // Shift viewer so chunk transitions from LOD 0 to LOD 1 (both are active)
            _viewerObject1.transform.position = new Vector3(12f, 0f, 0f);
            _registry.UpdateLODs();

            Assert.AreEqual(1, _registry.ActiveCellCount, "ActiveCellCount should stay unchanged when moving between active LOD levels.");
        }

        [Test]
        public void ActiveCellCount_WhenRegistryReinitialized_ResetsToZero()
        {
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 20f }, _viewerObject1.transform);
            _viewerObject1.transform.position = Vector3.zero;

            var handler = new DummyConfigureHandler();
            _registry.GetOrCreateChunk(Vector3Int.zero, ref handler, out _);
            Assert.AreEqual(1, _registry.ActiveCellCount);

            // Re-initialize registry with new settings
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 20f }, _viewerObject1.transform);

            Assert.AreEqual(0, _registry.ActiveCellCount, "ActiveCellCount should reset to 0 when registry is re-initialized.");
        }

        #endregion

        #region OnLODSettingsChanged Event Tests

        [Test]
        public void OnLODSettingsChanged_WhenUpdateLodDistancesCalledWithNewValues_InvokesEventWithRegistryInstance()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 50f, 100f }, _viewerObject1.transform);

            ILODGridConfiguration<Vector3Int> receivedConfig = null;
            int eventCallCount = 0;

            Action<ILODGridConfiguration<Vector3Int>> handler = config =>
            {
                eventCallCount++;
                receivedConfig = config;
            };

            _registry.OnLODSettingsChanged += handler;

            // Act
            _registry.UpdateLodDistances(new float[] { 30f, 60f });

            // Assert
            Assert.AreEqual(1, eventCallCount, "The OnLODSettingsChanged event should fire exactly once when LOD distances change.");
            Assert.IsNotNull(receivedConfig, "The event payload configuration should not be null.");
            Assert.AreSame(_registry, receivedConfig, "The event payload should pass the registry instance implementing ILODGridConfiguration.");
        }

        [Test]
        public void OnLODSettingsChanged_WhenUnsubscribed_DoesNotInvokeCallback()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 50f, 100f }, _viewerObject1.transform);

            int eventCallCount = 0;
            Action<ILODGridConfiguration<Vector3Int>> handler = _ => eventCallCount++;

            _registry.OnLODSettingsChanged += handler;
            _registry.OnLODSettingsChanged -= handler;

            // Act
            _registry.UpdateLodDistances(new float[] { 30f, 60f });

            // Assert
            Assert.AreEqual(0, eventCallCount, "Unsubscribed handler should not be invoked when LOD settings change.");
        }

        [Test]
        public void OnLODSettingsChanged_WhenUpdateLodDistancesCalledWithSameValues_DoesNotInvokeEvent()
        {
            // Arrange
            float[] distances = { 50f, 100f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, distances, _viewerObject1.transform);

            int eventCallCount = 0;
            _registry.OnLODSettingsChanged += _ => eventCallCount++;

            // Act
            _registry.UpdateLodDistances(new float[] { 50f, 100f });

            // Assert
            Assert.AreEqual(0, eventCallCount, "The event should not fire if the LOD distances are unchanged.");
        }

        #endregion

        #region UpdateLodDistances Tests

        [Test]
        public void UpdateLodDistances_BeforeInitialization_ThrowsInvalidOperationException()
        {
            // Arrange
            float[] validDistances = { 50f, 100f };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _registry.UpdateLodDistances(validDistances),
                "Calling UpdateLodDistances prior to registry initialization must throw an InvalidOperationException."
            );
        }

        [Test]
        public void UpdateLodDistances_WithEmptySpan_ThrowsArgumentException()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 50f }, _viewerObject1.transform);
            float[] emptyDistances = { };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _registry.UpdateLodDistances(emptyDistances)
            );
            Assert.AreEqual("newDistances", exception.ParamName);
        }

        [Test]
        public void UpdateLodDistances_WithZeroOrNegativeDistance_ThrowsArgumentException()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 50f }, _viewerObject1.transform);

            float[] zeroDistance = { 0f, 100f };
            float[] negativeDistance = { 50f, -10f };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _registry.UpdateLodDistances(zeroDistance));
            Assert.Throws<ArgumentException>(() => _registry.UpdateLodDistances(negativeDistance));
        }

        [Test]
        public void UpdateLodDistances_WithNonMonotonicallyIncreasingDistances_ThrowsArgumentException()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 50f }, _viewerObject1.transform);

            float[] equalDistances = { 50f, 50f };
            float[] decreasingDistances = { 100f, 50f };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _registry.UpdateLodDistances(equalDistances));
            Assert.Throws<ArgumentException>(() => _registry.UpdateLodDistances(decreasingDistances));
        }

        [Test]
        public void UpdateLodDistances_WithIdenticalValues_ReturnsFalseAndDoesNotFireEvent()
        {
            // Arrange
            float[] initialDistances = { 50f, 100f, 200f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, initialDistances, _viewerObject1.transform);

            bool eventFired = false;
            _registry.OnLODSettingsChanged += _ => eventFired = true;

            float[] sameDistances = { 50f, 100f, 200f };

            // Act
            bool result = _registry.UpdateLodDistances(sameDistances);

            // Assert
            Assert.IsFalse(result, "Updating with identical distances should return false.");
            Assert.IsFalse(eventFired, "OnLODSettingsChanged should not fire when distances have not changed.");
        }

        [Test]
        public void UpdateLodDistances_WithValidNewDistances_UpdatesPropertiesReturnsTrueAndFiresEvent()
        {
            // Arrange
            float[] initialDistances = { 50f, 100f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, initialDistances, _viewerObject1.transform);

            bool eventFired = false;
            _registry.OnLODSettingsChanged += _ => eventFired = true;

            float[] newDistances = { 30f, 60f, 120f };

            // Act
            bool result = _registry.UpdateLodDistances(newDistances);

            // Assert
            Assert.IsTrue(result, "Updating with valid new distances should return true.");
            Assert.IsTrue(eventFired, "OnLODSettingsChanged should fire when settings are successfully updated.");

            Assert.AreEqual(3, _registry.LodCount);
            Assert.AreEqual(30f, _registry.LodDistances[0]);
            Assert.AreEqual(60f, _registry.LodDistances[1]);
            Assert.AreEqual(120f, _registry.LodDistances[2]);
        }

        [Test]
        public void UpdateLodDistances_CalculatesSquaredDistancesCorrectly()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f }, _viewerObject1.transform);
            float[] newDistances = { 5f, 10f, 20f };

            // Act
            _registry.UpdateLodDistances(newDistances);

            // Assert
            ReadOnlySpan<float> sqrDistances = _registry.LodSqrDistances;
            Assert.AreEqual(25f, sqrDistances[0], 0.001f);
            Assert.AreEqual(100f, sqrDistances[1], 0.001f);
            Assert.AreEqual(400f, sqrDistances[2], 0.001f);
        }

        #endregion

        #region GetOrCreateChunk Tests

        [Test]
        public void GetOrCreateChunk_BeforeInitialization_ThrowsInvalidOperationException()
        {
            // Arrange
            var handler = new DummyConfigureHandler();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _registry.GetOrCreateChunk(Vector3Int.zero, ref handler, out TestLodChunk _),
                "Calling GetOrCreateChunk prior to initialization must throw an InvalidOperationException."
            );
        }

        [Test]
        public void GetOrCreateChunk_WhenChunkDoesNotExist_CreatesNewChunkAndReturnsTrue()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 50f, 100f }, _viewerObject1.transform);
            var key = new Vector3Int(1, 0, 0);
            var handler = new TrackingConfigureHandler();

            // Act
            bool isNew = _registry.GetOrCreateChunk(key, ref handler, out TestLodChunk chunk);

            // Assert
            Assert.IsTrue(isNew, "GetOrCreateChunk should return true when creating a new chunk.");
            Assert.IsNotNull(chunk, "The created chunk instance should not be null.");
            Assert.IsTrue(handler.WasExecuted, "The configuration handler should be executed for a new chunk.");
        }

        [Test]
        public void GetOrCreateChunk_WhenChunkAlreadyExists_ReturnsFalseAndSameInstance()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 50f, 100f }, _viewerObject1.transform);
            var key = new Vector3Int(2, 3, 4);
            var handler = new TrackingConfigureHandler();

            // First call - creates the chunk
            _registry.GetOrCreateChunk(key, ref handler, out TestLodChunk initialChunk);

            var secondHandler = new TrackingConfigureHandler();

            // Act - Second call with the same key
            bool isNew = _registry.GetOrCreateChunk(key, ref secondHandler, out TestLodChunk existingChunk);

            // Assert
            Assert.IsFalse(isNew, "GetOrCreateChunk should return false when retrieving an existing chunk.");
            Assert.AreSame(initialChunk, existingChunk, "GetOrCreateChunk should return the exact same chunk instance.");
        }

        [Test]
        public void GetOrCreateChunk_WhenCreatedNearViewer_InitializesChunkWithActiveLOD()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 50f, 100f }, _viewerObject1.transform);
            _viewerObject1.transform.position = Vector3.zero;

            var key = Vector3Int.zero;
            var handler = new DummyConfigureHandler();

            // Act
            _registry.GetOrCreateChunk(key, ref handler, out TestLodChunk chunk);

            // Assert
            Assert.AreEqual(0, chunk.CurrentLOD, "Chunk created near viewer should immediately be evaluated to LOD 0.");
            Assert.IsTrue(chunk.isActiveAndEnabled, "Chunk created near viewer should be active.");
        }

        [Test]
        public void GetOrCreateChunk_WhenCreatedFarFromViewer_InitializesChunkAsCulled()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 50f, 100f }, _viewerObject1.transform, deactivateOnCulled: true);
            _viewerObject1.transform.position = new Vector3(1000f, 1000f, 1000f);

            var key = Vector3Int.zero;
            var handler = new DummyConfigureHandler();

            // Act
            _registry.GetOrCreateChunk(key, ref handler, out TestLodChunk chunk);

            // Assert
            Assert.IsFalse(chunk.isActiveAndEnabled, "Chunk created far outside LOD range should be deactivated/culled.");
        }

        [Test]
        public void GetOrCreateChunk_WhenNewChunkCreated_ConfiguresMaxLODIndexCorrectly()
        {
            // Arrange - Configure with 3 LOD levels (max index = 2)
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 25f, 50f, 100f }, _viewerObject1.transform);
            var handler = new DummyConfigureHandler();

            // Act
            _registry.GetOrCreateChunk(Vector3Int.zero, ref handler, out TestLodChunk chunk);

            // Assert
            ILODState lodReceiver = chunk;
            Assert.AreEqual(_registry.LodCount - 1, lodReceiver.MaxLOD, "Chunk LOD range should be configured to registry's max LOD index.");
        }

        #endregion

        #region CalculateTargetLODSqr Tests

        [Test]
        public void CalculateTargetLODSqr_BeforeInitialization_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _registry.CalculateTargetLODSqr(100f),
                "CalculateTargetLODSqr must throw InvalidOperationException prior to registry initialization."
            );
        }

        [Test]
        public void CalculateTargetLODSqr_WhenSqrDistanceIsLessThanFirstThreshold_ReturnsZero()
        {
            // Arrange - Distances: 10f, 50f (Squared: 100f, 2500f)
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 50f }, _viewerObject1.transform);

            // Act
            int lod = _registry.CalculateTargetLODSqr(25f); // 25 < 100

            // Assert
            Assert.AreEqual(0, lod, "Squared distance strictly less than the first threshold should map to LOD 0.");
        }

        [Test]
        public void CalculateTargetLODSqr_WhenSqrDistanceIsBetweenThresholds_ReturnsCorrectLODIndex()
        {
            // Arrange - Distances: 10f, 50f, 100f (Squared: 100f, 2500f, 10000f)
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 50f, 100f }, _viewerObject1.transform);

            // Act
            int lod = _registry.CalculateTargetLODSqr(5000f); // 2500 <= 5000 < 10000

            // Assert
            Assert.AreEqual(2, lod, "Squared distance between threshold index 1 and index 2 should map to LOD 2.");
        }

        [Test]
        public void CalculateTargetLODSqr_WhenSqrDistanceExceedsAllThresholds_ReturnsMinusOne()
        {
            // Arrange - Distances: 10f, 50f (Squared: 100f, 2500f)
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 50f }, _viewerObject1.transform);

            // Act
            int lod = _registry.CalculateTargetLODSqr(3000f); // 3000 >= 2500

            // Assert
            Assert.AreEqual(-1, lod, "Squared distance exceeding maximum threshold should return -1 (culled).");
        }

        [Test]
        public void CalculateTargetLODSqr_OnExactThresholdBoundary_FallsThroughToNextLODLevel()
        {
            // Arrange - Distances: 10f, 50f (Squared: 100f, 2500f)
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 50f }, _viewerObject1.transform);

            // Act - Exact match for threshold 0 (100f is not strictly less than 100f)
            int lodAtExactBoundary = _registry.CalculateTargetLODSqr(100f);

            // Assert
            Assert.AreEqual(1, lodAtExactBoundary, "Exact threshold value is not '< 100f', so it should fall through to LOD 1.");
        }

        #endregion

        #region CalculateTargetLOD (Linear Distance) Tests

        [Test]
        public void CalculateTargetLOD_BeforeInitialization_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _registry.CalculateTargetLOD(10f),
                "CalculateTargetLOD must throw InvalidOperationException prior to registry initialization."
            );
        }

        [Test]
        public void CalculateTargetLOD_WhenDistanceIsLessThanFirstThreshold_ReturnsZero()
        {
            // Arrange - Linear Distances: 10f, 50f
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 50f }, _viewerObject1.transform);

            // Act
            int lod = _registry.CalculateTargetLOD(5f); // 5 < 10

            // Assert
            Assert.AreEqual(0, lod, "Linear distance strictly less than the first threshold should map to LOD 0.");
        }

        [Test]
        public void CalculateTargetLOD_WhenDistanceIsBetweenThresholds_ReturnsCorrectLODIndex()
        {
            // Arrange - Linear Distances: 10f, 50f, 100f
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 50f, 100f }, _viewerObject1.transform);

            // Act
            int lod = _registry.CalculateTargetLOD(75f); // 50 <= 75 < 100

            // Assert
            Assert.AreEqual(2, lod, "Linear distance between threshold index 1 and index 2 should map to LOD 2.");
        }

        [Test]
        public void CalculateTargetLOD_WhenDistanceExceedsAllThresholds_ReturnsMinusOne()
        {
            // Arrange - Linear Distances: 10f, 50f
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 50f }, _viewerObject1.transform);

            // Act
            int lod = _registry.CalculateTargetLOD(60f); // 60 >= 50

            // Assert
            Assert.AreEqual(-1, lod, "Linear distance exceeding maximum threshold should return -1 (culled).");
        }

        [Test]
        public void CalculateTargetLOD_OnExactThresholdBoundary_FallsThroughToNextLODLevel()
        {
            // Arrange - Linear Distances: 10f, 50f
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f, 50f }, _viewerObject1.transform);

            // Act - Exact match for threshold 0 (10f is not strictly less than 10f)
            int lodAtExactBoundary = _registry.CalculateTargetLOD(10f);

            // Assert
            Assert.AreEqual(1, lodAtExactBoundary, "Exact threshold value is not '< 10f', so it should fall through to LOD 1.");
        }

        #endregion

        #region GetKeysInLOD Tests

        #region Error & Boundary Cases

        [Test]
        public void GetKeysInLOD_BeforeInitialization_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.GetKeysInLOD(0, Vector3.zero),
                "Calling GetKeysInLOD prior to Initialize() must throw an InvalidOperationException.");
        }

        [Test]
        public void GetKeysInLOD_WithNegativeLodIndex_ReturnsEmptyIterator()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f }, _viewerObject1.transform);

            // Act
            var iterator = _registry.GetKeysInLOD(-1, Vector3.zero);

            // Assert
            Assert.IsFalse(iterator.MoveNext(), "Iterator for lodIndex < 0 must be empty.");
            Assert.AreEqual(0, ToList(iterator).Count);
        }

        [Test]
        public void GetKeysInLOD_WithLodIndexEqualToLodCount_ReturnsEmptyIterator()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f }, _viewerObject1.transform);

            // Act (LodCount is 2, valid indices are 0 and 1)
            var iterator = _registry.GetKeysInLOD(2, Vector3.zero);

            // Assert
            Assert.IsFalse(iterator.MoveNext(), "Iterator for lodIndex == LodCount must be empty.");
        }

        [Test]
        public void GetKeysInLOD_WithLodIndexFarOutOfBounds_ReturnsEmptyIterator()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f }, _viewerObject1.transform);

            // Act
            var iterator = _registry.GetKeysInLOD(999, Vector3.zero);

            // Assert
            Assert.IsFalse(iterator.MoveNext(), "Iterator for out-of-bounds lodIndex must be empty.");
        }

        #endregion

        #region Valid Value Tests - LOD 0

        [Test]
        public void GetKeysInLOD_LOD0_AtOrigin_ReturnsCorrectCenterAndSurroundingKeys()
        {
            // Arrange
            float[] lodDistances = { 30f, 60f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);
            Vector3 center = Vector3.zero;
            Vector3Int centerKey = _registry.WorldToGrid(center);

            // Act
            List<Vector3Int> keys = ToList(_registry.GetKeysInLOD(0, center));

            // Assert
            Assert.IsNotEmpty(keys, "LOD 0 iterator should return chunk keys surrounding the origin.");
            CollectionAssert.Contains(keys, centerKey, "LOD 0 must include the chunk key at the center.");

            // Verify all returned keys actually belong to LOD 0
            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                int evaluatedLod = _registry.CalculateTargetLODSqr(sqrDist);
                Assert.AreEqual(0, evaluatedLod, $"Key {key} returned in LOD 0 must evaluate to LOD 0.");
            }
        }

        [Test]
        public void GetKeysInLOD_LOD0_WithOffsetCenter_TranslatesKeyCoordinatesCorrectly()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 25f, 50f }, _viewerObject1.transform);
            Vector3 offsetCenter = new Vector3(150f, 0f, -230f);
            Vector3Int expectedCenterKey = _registry.WorldToGrid(offsetCenter);

            // Act
            List<Vector3Int> keys = ToList(_registry.GetKeysInLOD(0, offsetCenter));

            // Assert
            Assert.IsNotEmpty(keys, "LOD 0 iterator should yield keys for an offset center.");
            CollectionAssert.Contains(keys, expectedCenterKey, "LOD 0 must contain the chunk key for the offset center.");

            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, offsetCenter);
                int evaluatedLod = _registry.CalculateTargetLODSqr(sqrDist);
                Assert.AreEqual(0, evaluatedLod, $"Key {key} must evaluate to LOD 0 relative to offset center.");
            }
        }

        [Test]
        public void GetKeysInLOD_LOD0_SingleLODConfigured_ReturnsAllKeysInRadius()
        {
            // Arrange - Testing single LOD level registry setup
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 40f }, _viewerObject1.transform);
            Vector3 center = new Vector3(10f, 0f, 10f);

            // Act
            List<Vector3Int> keys = ToList(_registry.GetKeysInLOD(0, center));

            // Assert
            Assert.IsNotEmpty(keys, "LOD 0 should return keys when only 1 LOD distance is configured.");
            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                Assert.LessOrEqual(sqrDist, 1600f, $"Key {key} sqr distance must be <= outer radius squared (40^2 = 1600).");
            }
        }

        #endregion

        #region Valid Value Tests - Higher LOD Levels (Hollow Rings)

        [Test]
        public void GetKeysInLOD_IntermediateLOD_YieldsKeysStrictlyInRingBounds()
        {
            // Arrange - 3 LOD tiers: LOD 0 (0..20m), LOD 1 (20..50m), LOD 2 (50..100m)
            float[] lodDistances = { 20f, 50f, 100f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);
            Vector3 center = Vector3.zero;
            int targetLod = 1;

            float minSqrRadius = _registry.LodSqrDistances[0]; // 400
            float maxSqrRadius = _registry.LodSqrDistances[1]; // 2500

            // Act
            List<Vector3Int> lod1Keys = ToList(_registry.GetKeysInLOD(targetLod, center));

            // Assert
            Assert.IsNotEmpty(lod1Keys, "LOD 1 ring should contain keys.");

            foreach (Vector3Int key in lod1Keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);

                Assert.GreaterOrEqual(sqrDist, minSqrRadius, $"Key {key} sqr distance ({sqrDist}) must be >= inner LOD bound ({minSqrRadius}).");
                Assert.LessOrEqual(sqrDist, maxSqrRadius, $"Key {key} sqr distance ({sqrDist}) must be <= outer LOD bound ({maxSqrRadius}).");

                int evaluatedLod = _registry.CalculateTargetLODSqr(sqrDist);
                Assert.AreEqual(targetLod, evaluatedLod, $"Key {key} in LOD 1 must evaluate to LOD 1.");
            }
        }

        [Test]
        public void GetKeysInLOD_HighestLOD_YieldsKeysInOuterMostRing()
        {
            // Arrange - 3 LOD tiers
            float[] lodDistances = { 15f, 35f, 75f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);
            Vector3 center = new Vector3(45f, 0f, -10f);
            int highestLod = _registry.LodCount - 1; // LOD 2

            float innerSqrRadius = _registry.LodSqrDistances[1]; // 35^2 = 1225
            float outerSqrRadius = _registry.LodSqrDistances[2]; // 75^2 = 5625

            // Act
            List<Vector3Int> lod2Keys = ToList(_registry.GetKeysInLOD(highestLod, center));

            // Assert
            Assert.IsNotEmpty(lod2Keys, "The highest LOD ring must yield keys.");

            foreach (Vector3Int key in lod2Keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);

                Assert.GreaterOrEqual(sqrDist, innerSqrRadius, $"Key {key} must lie outside LOD 1 boundary.");
                Assert.LessOrEqual(sqrDist, outerSqrRadius, $"Key {key} must lie inside LOD 2 boundary.");

                int evaluatedLod = _registry.CalculateTargetLODSqr(sqrDist);
                Assert.AreEqual(highestLod, evaluatedLod, $"Key {key} must evaluate to max LOD level ({highestLod}).");
            }
        }

        #endregion

        #region Valid Value Tests - Iteration Integrity & Grid Sizes

        [Test]
        public void GetKeysInLOD_ValidLOD_ContainsNoDuplicateKeys()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f, 90f }, _viewerObject1.transform);
            Vector3 center = new Vector3(12.3f, 4.5f, 67.8f);

            for (int lod = 0; lod < _registry.LodCount; lod++)
            {
                // Act
                List<Vector3Int> keys = ToList(_registry.GetKeysInLOD(lod, center));
                HashSet<Vector3Int> uniqueKeys = new HashSet<Vector3Int>(keys);

                // Assert
                Assert.AreEqual(uniqueKeys.Count, keys.Count, $"LOD {lod} iterator produced duplicate keys.");
            }
        }

        [Test]
        public void GetKeysInLOD_WithDifferentGridSize_CalculatesCorrectKeyBounds()
        {
            // Arrange - Testing GridSize16
            _registry.Initialize(GridSize.Size16, Vector3.zero, new float[] { 32f, 64f }, _viewerObject1.transform);
            Vector3 center = Vector3.zero;

            // Act
            List<Vector3Int> lod0Keys = ToList(_registry.GetKeysInLOD(0, center));
            List<Vector3Int> lod1Keys = ToList(_registry.GetKeysInLOD(1, center));

            // Assert
            Assert.IsNotEmpty(lod0Keys);
            Assert.IsNotEmpty(lod1Keys);

            foreach (Vector3Int key in lod0Keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                Assert.AreEqual(0, _registry.CalculateTargetLODSqr(sqrDist));
            }

            foreach (Vector3Int key in lod1Keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                Assert.AreEqual(1, _registry.CalculateTargetLODSqr(sqrDist));
            }
        }

        [Test]
        public void GetKeysInLOD_IteratorContract_AdvancesCorrectly()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 15f }, _viewerObject1.transform);
            Vector3 center = Vector3.zero;

            // Act
            IIterator<Vector3Int> iterator = _registry.GetKeysInLOD(0, center);

            // Assert
            int count = 0;
            while (iterator.MoveNext())
            {
                Vector3Int currentKey = iterator.Current;
                count++;

                // Verify Current remains stable for the step
                Assert.AreEqual(currentKey, iterator.Current, "Current property must return the same value until MoveNext is called.");
            }

            Assert.Greater(count, 0, "Iterator should have yielded elements.");
            Assert.IsFalse(iterator.MoveNext(), "MoveNext should return false after consuming all elements.");
        }

        #endregion

        #endregion

        #region GetKeysInFullRange Tests

        #region Error & Boundary Cases

        [Test]
        public void GetKeysInFullRange_BeforeInitialization_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.GetKeysInFullRange(Vector3.zero),
                "Calling GetKeysInFullRange prior to Initialize() must throw an InvalidOperationException.");
        }

        [Test]
        public void GetKeysInFullRange_WhenInitializedWithEmptyLodDistances_ReturnsEmptyIterator()
        {
            // Arrange & Act
            // If the registry ends up in a state where LodCount is 0, it must return IIterator<Vector3Int>.Empty()
            // Note: Standard Initialize throws for empty arrays, but we test the LodCount == 0 guard directly if accessible
            ReadOnlySpan<float> distances = _registry.LodDistances;
            Assert.IsTrue(distances.IsEmpty);

            // Assert prior to initialization behavior
            Assert.Throws<InvalidOperationException>(() => _registry.GetKeysInFullRange(Vector3.zero));
        }

        #endregion

        #region Valid Value Tests - Basic Coverage & Bounds

        [Test]
        public void GetKeysInFullRange_AtOrigin_ReturnsNonEmptyKeysIncludingCenterKey()
        {
            // Arrange
            float[] lodDistances = { 20f, 50f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);
            Vector3 center = Vector3.zero;
            Vector3Int centerKey = _registry.WorldToGrid(center);

            // Act
            List<Vector3Int> keys = ToList(_registry.GetKeysInFullRange(center));

            // Assert
            Assert.IsNotEmpty(keys, "GetKeysInFullRange should return chunk keys covering the full LOD radius.");
            CollectionAssert.Contains(keys, centerKey, "GetKeysInFullRange must contain the center chunk key.");
        }

        [Test]
        public void GetKeysInFullRange_WithOffsetCenter_TranslatesAllKeyCoordinatesCorrectly()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 30f, 60f }, _viewerObject1.transform);
            Vector3 offsetCenter = new Vector3(-350f, 12.5f, 480f);
            Vector3Int expectedCenterKey = _registry.WorldToGrid(offsetCenter);

            // Act
            List<Vector3Int> keys = ToList(_registry.GetKeysInFullRange(offsetCenter));

            // Assert
            Assert.IsNotEmpty(keys, "GetKeysInFullRange should yield keys for an offset center.");
            CollectionAssert.Contains(keys, expectedCenterKey, "Full range iterator must contain the chunk key at the offset center.");

            float maxSqrRadius = _registry.LodSqrDistances[_registry.LodCount - 1]; // 60^2 = 3600
            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, offsetCenter);
                Assert.LessOrEqual(sqrDist, maxSqrRadius, $"Key {key} distance squared ({sqrDist}) must be <= max LOD radius squared ({maxSqrRadius}).");
            }
        }

        [Test]
        public void GetKeysInFullRange_AllReturnedKeysAreWithinActiveLODRangesNotCulled()
        {
            // Arrange
            float[] lodDistances = { 15f, 35f, 75f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);
            Vector3 center = new Vector3(10f, 0f, -10f);

            // Act
            List<Vector3Int> keys = ToList(_registry.GetKeysInFullRange(center));

            // Assert
            Assert.IsNotEmpty(keys);
            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                int evaluatedLod = _registry.CalculateTargetLODSqr(sqrDist);

                Assert.AreNotEqual(-1, evaluatedLod, $"Key {key} returned in full range must not evaluate to culled (-1).");
                Assert.GreaterOrEqual(evaluatedLod, 0, $"Key {key} evaluated LOD must be valid (>= 0).");
                Assert.Less(evaluatedLod, _registry.LodCount, $"Key {key} evaluated LOD must be within max LOD index.");
            }
        }

        #endregion

        #region Union & Equivalence Tests

        [Test]
        public void GetKeysInFullRange_MatchesExactUnionOfAllIndividualLODRings()
        {
            // Arrange
            float[] lodDistances = { 10f, 20f, 30f };
            Vector3 center = new Vector3(10f, 0f, -10f);

            // Ensure viewer position matches center before initializing/querying
            _viewerObject1.transform.position = center;
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);

            // Build the expected union set by combining GetKeysInLOD for all levels
            HashSet<Vector3Int> expectedUnionKeys = new HashSet<Vector3Int>();
            for (int lod = 0; lod < _registry.LodCount; lod++)
            {
                List<Vector3Int> lodKeys = ToList(_registry.GetKeysInLOD(lod, center));
                foreach (Vector3Int key in lodKeys)
                {
                    bool added = expectedUnionKeys.Add(key);
                    Assert.IsTrue(added, $"Key {key} was duplicated across individual LOD rings (LOD {lod}).");
                }
            }

            // Act
            List<Vector3Int> actualFullRangeList = ToList(_registry.GetKeysInFullRange(center));
            HashSet<Vector3Int> actualFullRangeSet = new HashSet<Vector3Int>(actualFullRangeList);

            // Assert
            Assert.AreEqual(actualFullRangeList.Count, actualFullRangeSet.Count, "GetKeysInFullRange must not produce duplicate keys.");
            CollectionAssert.AreEquivalent(
                expectedUnionKeys,
                actualFullRangeSet,
                "GetKeysInFullRange must match the exact combined set of keys from all individual LOD rings."
            );
        }

        [Test]
        public void GetKeysInFullRange_SingleLODLevelConfigured_MatchesLOD0KeysExactly()
        {
            // Arrange - Single LOD tier
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 40f }, _viewerObject1.transform);
            Vector3 center = Vector3.zero;

            // Act
            List<Vector3Int> lod0Keys = ToList(_registry.GetKeysInLOD(0, center));
            List<Vector3Int> fullRangeKeys = ToList(_registry.GetKeysInFullRange(center));

            // Assert
            CollectionAssert.AreEquivalent(lod0Keys, fullRangeKeys,
                "When only 1 LOD is configured, GetKeysInFullRange must yield the exact same set as GetKeysInLOD(0).");
        }

        #endregion

        #region Spatial & Iterator Integrity Tests

        [Test]
        public void GetKeysInFullRange_WithLargeNegativeCenterCoordinates_ExecutesCorrectly()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 25f, 55f }, _viewerObject1.transform);
            Vector3 negativeCenter = new Vector3(-8000.5f, -120.0f, -4000.75f);
            Vector3Int expectedCenterKey = _registry.WorldToGrid(negativeCenter);

            // Act
            List<Vector3Int> keys = ToList(_registry.GetKeysInFullRange(negativeCenter));

            // Assert
            Assert.IsNotEmpty(keys);
            CollectionAssert.Contains(keys, expectedCenterKey);
        }

        [Test]
        public void GetKeysInFullRange_ContainsNoDuplicateKeys()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 15f, 35f, 70f }, _viewerObject1.transform);
            Vector3 center = new Vector3(14.2f, 8.1f, -99.9f);

            // Act
            List<Vector3Int> keys = ToList(_registry.GetKeysInFullRange(center));
            HashSet<Vector3Int> uniqueKeys = new HashSet<Vector3Int>(keys);

            // Assert
            Assert.AreEqual(uniqueKeys.Count, keys.Count, "GetKeysInFullRange iterator should not produce duplicate keys.");
        }

        [Test]
        public void GetKeysInFullRange_DifferentGridSizes_CalculatesCorrectKeyVolume()
        {
            // Arrange - Test GridSize16
            _registry.Initialize(GridSize.Size16, Vector3.zero, new float[] { 32f, 64f }, _viewerObject1.transform);
            Vector3 center = Vector3.zero;

            // Act
            List<Vector3Int> keys = ToList(_registry.GetKeysInFullRange(center));

            // Assert
            Assert.IsNotEmpty(keys);
            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                Assert.LessOrEqual(sqrDist, 64f * 64f, $"Key {key} distance squared must be <= max LOD squared (4096).");
            }
        }

        [Test]
        public void GetKeysInFullRange_IteratorContract_AdvancesCorrectly()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 40f }, _viewerObject1.transform);
            Vector3 center = Vector3.zero;

            // Act
            IIterator<Vector3Int> iterator = _registry.GetKeysInFullRange(center);

            // Assert
            int count = 0;
            while (iterator.MoveNext())
            {
                Vector3Int currentKey = iterator.Current;
                count++;

                // Verify Current remains stable for the step
                Assert.AreEqual(currentKey, iterator.Current, "Current property must return the same value until MoveNext is called.");
            }

            Assert.Greater(count, 0, "Iterator should have yielded elements.");
            Assert.IsFalse(iterator.MoveNext(), "MoveNext should return false after consuming all elements.");
        }

        [Test]
        public void GetKeysInFullRange_SequentiallyCalledWithSameCenter_YieldsIdenticalSequence()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f }, _viewerObject1.transform);
            Vector3 center = new Vector3(25f, 0f, 50f);

            // Act
            List<Vector3Int> run1 = ToList(_registry.GetKeysInFullRange(center));
            List<Vector3Int> run2 = ToList(_registry.GetKeysInFullRange(center));

            // Assert
            CollectionAssert.AreEqual(run1, run2, "Sequential calls to GetKeysInFullRange with identical parameters must yield identical key sequences.");
        }

        #endregion

        #endregion

        #region ForEachKeyInLOD Tests

        #region Error & Boundary Cases

        [Test]
        public void ForEachKeyInLOD_BeforeInitialization_ThrowsInvalidOperationException()
        {
            // Arrange
            var action = new KeyCollectorAction();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.ForEachKeyInLOD(0, Vector3.zero, ref action),
                "Calling ForEachKeyInLOD prior to Initialize() must throw an InvalidOperationException.");
        }

        [Test]
        public void ForEachKeyInLOD_WithNegativeLodIndex_DoesNotExecuteAction()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f }, _viewerObject1.transform);
            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInLOD(-1, Vector3.zero, ref action);

            // Assert
            Assert.IsEmpty(action.CollectedKeys, "Action should not be executed when lodIndex < 0.");
        }

        [Test]
        public void ForEachKeyInLOD_WithLodIndexEqualToLodCount_DoesNotExecuteAction()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f }, _viewerObject1.transform);
            var action = new KeyCollectorAction();

            // Act (LodCount is 2, valid indices are 0 and 1)
            _registry.ForEachKeyInLOD(2, Vector3.zero, ref action);

            // Assert
            Assert.IsEmpty(action.CollectedKeys, "Action should not be executed when lodIndex == LodCount.");
        }

        [Test]
        public void ForEachKeyInLOD_WithLodIndexFarOutOfBounds_DoesNotExecuteAction()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f }, _viewerObject1.transform);
            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInLOD(999, Vector3.zero, ref action);

            // Assert
            Assert.IsEmpty(action.CollectedKeys, "Action should not be executed when lodIndex is far out of bounds.");
        }

        #endregion

        #region Valid Value Tests - LOD 0

        [Test]
        public void ForEachKeyInLOD_LOD0_AtOrigin_ReturnsCorrectCenterAndSurroundingKeys()
        {
            // Arrange
            float[] lodDistances = { 30f, 60f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);
            Vector3 center = Vector3.zero;
            Vector3Int centerKey = _registry.WorldToGrid(center);
            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInLOD(0, center, ref action);
            List<Vector3Int> keys = action.CollectedKeys;

            // Assert
            Assert.IsNotEmpty(keys, "LOD 0 action execution should process chunk keys surrounding origin.");
            CollectionAssert.Contains(keys, centerKey, "LOD 0 execution must include the center chunk key.");

            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                int evaluatedLod = _registry.CalculateTargetLODSqr(sqrDist);
                Assert.AreEqual(0, evaluatedLod, $"Key {key} processed in LOD 0 must evaluate to LOD 0.");
            }
        }

        [Test]
        public void ForEachKeyInLOD_LOD0_WithOffsetCenter_TranslatesKeyCoordinatesCorrectly()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 25f, 50f }, _viewerObject1.transform);
            Vector3 offsetCenter = new Vector3(150f, 0f, -230f);
            Vector3Int expectedCenterKey = _registry.WorldToGrid(offsetCenter);
            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInLOD(0, offsetCenter, ref action);
            List<Vector3Int> keys = action.CollectedKeys;

            // Assert
            Assert.IsNotEmpty(keys, "LOD 0 action execution should yield keys for offset center.");
            CollectionAssert.Contains(keys, expectedCenterKey, "LOD 0 execution must contain chunk key for offset center.");

            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, offsetCenter);
                int evaluatedLod = _registry.CalculateTargetLODSqr(sqrDist);
                Assert.AreEqual(0, evaluatedLod, $"Key {key} must evaluate to LOD 0 relative to offset center.");
            }
        }

        [Test]
        public void ForEachKeyInLOD_LOD0_SingleLODConfigured_ReturnsAllKeysInRadius()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 40f }, _viewerObject1.transform);
            Vector3 center = new Vector3(10f, 0f, 10f);
            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInLOD(0, center, ref action);
            List<Vector3Int> keys = action.CollectedKeys;

            // Assert
            Assert.IsNotEmpty(keys, "LOD 0 action execution should yield keys when only 1 LOD is configured.");
            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                Assert.LessOrEqual(sqrDist, 1600f, $"Key {key} sqr distance must be <= outer radius squared (40^2 = 1600).");
            }
        }

        #endregion

        #region Valid Value Tests - Higher LOD Levels (Hollow Rings)

        [Test]
        public void ForEachKeyInLOD_IntermediateLOD_YieldsKeysStrictlyInRingBounds()
        {
            // Arrange
            float[] lodDistances = { 20f, 50f, 100f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);
            Vector3 center = Vector3.zero;
            int targetLod = 1;

            float minSqrRadius = _registry.LodSqrDistances[0]; // 400
            float maxSqrRadius = _registry.LodSqrDistances[1]; // 2500

            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInLOD(targetLod, center, ref action);
            List<Vector3Int> lod1Keys = action.CollectedKeys;

            // Assert
            Assert.IsNotEmpty(lod1Keys, "LOD 1 execution should collect keys.");

            foreach (Vector3Int key in lod1Keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);

                Assert.GreaterOrEqual(sqrDist, minSqrRadius, $"Key {key} sqr distance ({sqrDist}) must be >= inner LOD bound ({minSqrRadius}).");
                Assert.LessOrEqual(sqrDist, maxSqrRadius, $"Key {key} sqr distance ({sqrDist}) must be <= outer LOD bound ({maxSqrRadius}).");

                int evaluatedLod = _registry.CalculateTargetLODSqr(sqrDist);
                Assert.AreEqual(targetLod, evaluatedLod, $"Key {key} in LOD 1 must evaluate to LOD 1.");
            }
        }

        [Test]
        public void ForEachKeyInLOD_HighestLOD_YieldsKeysInOuterMostRing()
        {
            // Arrange
            float[] lodDistances = { 15f, 35f, 75f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);
            Vector3 center = new Vector3(45f, 0f, -10f);
            int highestLod = _registry.LodCount - 1; // LOD 2

            float innerSqrRadius = _registry.LodSqrDistances[1]; // 35^2 = 1225
            float outerSqrRadius = _registry.LodSqrDistances[2]; // 75^2 = 5625

            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInLOD(highestLod, center, ref action);
            List<Vector3Int> lod2Keys = action.CollectedKeys;

            // Assert
            Assert.IsNotEmpty(lod2Keys, "The highest LOD execution must yield keys.");

            foreach (Vector3Int key in lod2Keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);

                Assert.GreaterOrEqual(sqrDist, innerSqrRadius, $"Key {key} must lie outside LOD 1 boundary.");
                Assert.LessOrEqual(sqrDist, outerSqrRadius, $"Key {key} must lie inside LOD 2 boundary.");

                int evaluatedLod = _registry.CalculateTargetLODSqr(sqrDist);
                Assert.AreEqual(highestLod, evaluatedLod, $"Key {key} must evaluate to max LOD level ({highestLod}).");
            }
        }

        #endregion

        #region Valid Value Tests - Execution Integrity & Parity

        [Test]
        public void ForEachKeyInLOD_ValidLOD_ContainsNoDuplicateKeys()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f, 90f }, _viewerObject1.transform);
            Vector3 center = new Vector3(12.3f, 4.5f, 67.8f);

            for (int lod = 0; lod < _registry.LodCount; lod++)
            {
                var action = new KeyCollectorAction();

                // Act
                _registry.ForEachKeyInLOD(lod, center, ref action);
                List<Vector3Int> keys = action.CollectedKeys;
                HashSet<Vector3Int> uniqueKeys = new HashSet<Vector3Int>(keys);

                // Assert
                Assert.AreEqual(uniqueKeys.Count, keys.Count, $"ForEachKeyInLOD at LOD {lod} processed duplicate keys.");
            }
        }

        [Test]
        public void ForEachKeyInLOD_WithDifferentGridSize_CalculatesCorrectKeyBounds()
        {
            // Arrange
            _registry.Initialize(GridSize.Size16, Vector3.zero, new float[] { 32f, 64f }, _viewerObject1.transform);
            Vector3 center = Vector3.zero;

            var lod0Action = new KeyCollectorAction();
            var lod1Action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInLOD(0, center, ref lod0Action);
            _registry.ForEachKeyInLOD(1, center, ref lod1Action);

            // Assert
            Assert.IsNotEmpty(lod0Action.CollectedKeys);
            Assert.IsNotEmpty(lod1Action.CollectedKeys);

            foreach (Vector3Int key in lod0Action.CollectedKeys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                Assert.AreEqual(0, _registry.CalculateTargetLODSqr(sqrDist));
            }

            foreach (Vector3Int key in lod1Action.CollectedKeys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                Assert.AreEqual(1, _registry.CalculateTargetLODSqr(sqrDist));
            }
        }

        [Test]
        public void ForEachKeyInLOD_MutatesStructStateByRef_ReflectsInCaller()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f }, _viewerObject1.transform);
            Vector3 center = Vector3.zero;
            var countAction = new CountAction();

            // Act
            _registry.ForEachKeyInLOD(0, center, ref countAction);

            // Assert
            Assert.Greater(countAction.ExecutionCount, 0, "Passing action by ref must mutate the caller's struct state.");
        }

        [Test]
        public void ForEachKeyInLOD_YieldsIdenticalKeysAs_GetKeysInLOD()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 15f, 35f, 70f }, _viewerObject1.transform);
            Vector3 center = new Vector3(25.5f, -10f, 40.2f);

            for (int lod = 0; lod < _registry.LodCount; lod++)
            {
                // Act - Iterator
                List<Vector3Int> iteratorKeys = ToList(_registry.GetKeysInLOD(lod, center));

                // Act - Action Execution
                var action = new KeyCollectorAction();
                _registry.ForEachKeyInLOD(lod, center, ref action);
                List<Vector3Int> actionKeys = action.CollectedKeys;

                // Assert Parity
                CollectionAssert.AreEqual(iteratorKeys, actionKeys,
                    $"ForEachKeyInLOD must yield the exact same sequence of keys as GetKeysInLOD for LOD {lod}.");
            }
        }

        #endregion

        #endregion

        #region ForEachKeyInFullRange Tests

        #region Error & Boundary Cases

        [Test]
        public void ForEachKeyInFullRange_BeforeInitialization_ThrowsInvalidOperationException()
        {
            // Arrange
            var action = new KeyCollectorAction();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.ForEachKeyInFullRange(Vector3.zero, ref action),
                "Calling ForEachKeyInFullRange prior to Initialize() must throw an InvalidOperationException.");
        }

        #endregion

        #region Valid Value Tests - Basic Coverage & Bounds

        [Test]
        public void ForEachKeyInFullRange_AtOrigin_ProcessesKeysIncludingCenterKey()
        {
            // Arrange
            float[] lodDistances = { 20f, 50f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);
            Vector3 center = Vector3.zero;
            Vector3Int centerKey = _registry.WorldToGrid(center);
            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInFullRange(center, ref action);
            List<Vector3Int> keys = action.CollectedKeys;

            // Assert
            Assert.IsNotEmpty(keys, "ForEachKeyInFullRange should process chunk keys covering the full LOD radius.");
            CollectionAssert.Contains(keys, centerKey, "ForEachKeyInFullRange must process the center chunk key.");
        }

        [Test]
        public void ForEachKeyInFullRange_WithOffsetCenter_TranslatesAllKeyCoordinatesCorrectly()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 30f, 60f }, _viewerObject1.transform);
            Vector3 offsetCenter = new Vector3(-350f, 12.5f, 480f);
            Vector3Int expectedCenterKey = _registry.WorldToGrid(offsetCenter);
            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInFullRange(offsetCenter, ref action);
            List<Vector3Int> keys = action.CollectedKeys;

            // Assert
            Assert.IsNotEmpty(keys, "ForEachKeyInFullRange should yield keys for an offset center.");
            CollectionAssert.Contains(keys, expectedCenterKey, "Full range action execution must contain the chunk key at the offset center.");

            float maxSqrRadius = _registry.LodSqrDistances[_registry.LodCount - 1]; // 60^2 = 3600
            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, offsetCenter);
                Assert.LessOrEqual(sqrDist, maxSqrRadius, $"Key {key} distance squared ({sqrDist}) must be <= max LOD radius squared ({maxSqrRadius}).");
            }
        }

        [Test]
        public void ForEachKeyInFullRange_AllProcessedKeysAreWithinActiveLODRangesNotCulled()
        {
            // Arrange
            float[] lodDistances = { 15f, 35f, 75f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);
            Vector3 center = new Vector3(10f, 0f, -10f);
            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInFullRange(center, ref action);
            List<Vector3Int> keys = action.CollectedKeys;

            // Assert
            Assert.IsNotEmpty(keys);
            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                int evaluatedLod = _registry.CalculateTargetLODSqr(sqrDist);

                Assert.AreNotEqual(-1, evaluatedLod, $"Key {key} processed in full range must not evaluate to culled (-1).");
                Assert.GreaterOrEqual(evaluatedLod, 0, $"Key {key} evaluated LOD must be valid (>= 0).");
                Assert.Less(evaluatedLod, _registry.LodCount, $"Key {key} evaluated LOD must be within max LOD index.");
            }
        }

        #endregion

        #region Union, Equivalence & Parity Tests

        [Test]
        public void ForEachKeyInFullRange_MatchesExactUnionOfAllIndividualLODRings()
        {
            // Arrange
            Vector3Int min = new Vector3Int(-1, -1, 0);
            Vector3Int max = new Vector3Int(1, 1, 0);
            Vector3 worldCenter = Vector3.zero;
            Vector3 gridSize = new Vector3(1f, 1f, 1f);

            ReadOnlySpan<float> lodSqrDistances = stackalloc float[] { 2f, 5f };

            // Act 1: Collect keys from the full combined LOD range [0..1]
            var fullRangeState = new GridLODEdgeState(
                min, max, worldCenter,
                0, 1, lodSqrDistances,
                gridSize, SpatialAxes.X | SpatialAxes.Y
            );
            List<Vector3Int> fullRangeKeys = CollectKeys(fullRangeState);

            // Act 2: Collect keys from each individual LOD ring
            List<Vector3Int> combinedRingKeys = new List<Vector3Int>();
            for (int lod = 0; lod < lodSqrDistances.Length; lod++)
            {
                var ringState = new GridLODEdgeState(
                    min, max, worldCenter,
                    lod, lodSqrDistances,
                    gridSize, SpatialAxes.X | SpatialAxes.Y
                );
                combinedRingKeys.AddRange(CollectKeys(ringState));
            }

            // Assert
            CollectionAssert.AreEquivalent(
                combinedRingKeys,
                fullRangeKeys,
                "ForEachKeyInFullRange must match the exact combined set of keys from all individual ForEachKeyInLOD executions."
            );
        }

        [Test]
        public void ForEachKeyInFullRange_SingleLODLevelConfigured_MatchesLOD0KeysExactly()
        {
            // Arrange - Single LOD tier
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 40f }, _viewerObject1.transform);
            Vector3 center = Vector3.zero;

            var lod0Action = new KeyCollectorAction();
            var fullRangeAction = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInLOD(0, center, ref lod0Action);
            _registry.ForEachKeyInFullRange(center, ref fullRangeAction);

            // Assert
            CollectionAssert.AreEquivalent(lod0Action.CollectedKeys, fullRangeAction.CollectedKeys,
                "When only 1 LOD is configured, ForEachKeyInFullRange must yield the exact same set as ForEachKeyInLOD(0).");
        }

        [Test]
        public void ForEachKeyInFullRange_YieldsIdenticalKeysAs_GetKeysInFullRange()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 15f, 35f, 70f }, _viewerObject1.transform);
            Vector3 center = new Vector3(25.5f, -10f, 40.2f);

            // Act - Iterator
            List<Vector3Int> iteratorKeys = ToList(_registry.GetKeysInFullRange(center));

            // Act - Action Execution
            var action = new KeyCollectorAction();
            _registry.ForEachKeyInFullRange(center, ref action);
            List<Vector3Int> actionKeys = action.CollectedKeys;

            // Assert Parity
            CollectionAssert.AreEqual(iteratorKeys, actionKeys,
                "ForEachKeyInFullRange must yield the exact same sequence of keys as GetKeysInFullRange.");
        }

        #endregion

        #region Spatial & Struct Execution Integrity Tests

        [Test]
        public void ForEachKeyInFullRange_WithLargeNegativeCenterCoordinates_ExecutesCorrectly()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 25f, 55f }, _viewerObject1.transform);
            Vector3 negativeCenter = new Vector3(-8000.5f, -120.0f, -4000.75f);
            Vector3Int expectedCenterKey = _registry.WorldToGrid(negativeCenter);
            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInFullRange(negativeCenter, ref action);
            List<Vector3Int> keys = action.CollectedKeys;

            // Assert
            Assert.IsNotEmpty(keys);
            CollectionAssert.Contains(keys, expectedCenterKey);
        }

        [Test]
        public void ForEachKeyInFullRange_ProcessesNoDuplicateKeys()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 15f, 35f, 70f }, _viewerObject1.transform);
            Vector3 center = new Vector3(14.2f, 8.1f, -99.9f);
            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInFullRange(center, ref action);
            List<Vector3Int> keys = action.CollectedKeys;
            HashSet<Vector3Int> uniqueKeys = new HashSet<Vector3Int>(keys);

            // Assert
            Assert.AreEqual(uniqueKeys.Count, keys.Count, "ForEachKeyInFullRange action execution should not process duplicate keys.");
        }

        [Test]
        public void ForEachKeyInFullRange_DifferentGridSizes_CalculatesCorrectKeyVolume()
        {
            // Arrange - Test GridSize16
            _registry.Initialize(GridSize.Size16, Vector3.zero, new float[] { 32f, 64f }, _viewerObject1.transform);
            Vector3 center = Vector3.zero;
            var action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInFullRange(center, ref action);
            List<Vector3Int> keys = action.CollectedKeys;

            // Assert
            Assert.IsNotEmpty(keys);
            foreach (Vector3Int key in keys)
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(key, center);
                Assert.LessOrEqual(sqrDist, 64f * 64f, $"Key {key} distance squared must be <= max LOD squared (4096).");
            }
        }

        [Test]
        public void ForEachKeyInFullRange_MutatesStructStateByRef_ReflectsInCaller()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f }, _viewerObject1.transform);
            Vector3 center = Vector3.zero;
            var countAction = new CountAction();

            // Act
            _registry.ForEachKeyInFullRange(center, ref countAction);

            // Assert
            Assert.Greater(countAction.ExecutionCount, 0, "Passing action by ref must mutate the caller's struct state.");
        }

        [Test]
        public void ForEachKeyInFullRange_SequentiallyCalledWithSameCenter_YieldsIdenticalSequence()
        {
            // Arrange
            _registry.Initialize(GridSize.Size10, Vector3.zero, new float[] { 20f, 50f }, _viewerObject1.transform);
            Vector3 center = new Vector3(25f, 0f, 50f);

            var run1Action = new KeyCollectorAction();
            var run2Action = new KeyCollectorAction();

            // Act
            _registry.ForEachKeyInFullRange(center, ref run1Action);
            _registry.ForEachKeyInFullRange(center, ref run2Action);

            // Assert
            CollectionAssert.AreEqual(run1Action.CollectedKeys, run2Action.CollectedKeys,
                "Sequential calls to ForEachKeyInFullRange with identical parameters must yield identical key sequences.");
        }

        #endregion

        #endregion

        #region GetKeyCountInLODLevel Tests

        [Test]
        public void GetKeyCountInLODLevel_MatchesActualCollectedKeysCount()
        {
            // Arrange
            float[] lodDistances = { 10f, 20f, 30f };
            Vector3 center = Vector3.zero;
            _viewerObject1.transform.position = center;
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);

            for (int lod = 0; lod < _registry.LodCount; lod++)
            {
                // Act: Get actual keys via iterator
                List<Vector3Int> keys = ToList(_registry.GetKeysInLOD(lod, center));
                int countedKeys = _registry.GetKeyCountInLODLevel(lod, center);

                // Assert
                Assert.AreEqual(keys.Count, countedKeys, $"Key count for LOD {lod} does not match the actual number of collected keys.");
            }
        }

        [Test]
        [TestCase(-1)]
        [TestCase(99)]
        public void GetKeyCountInLODLevel_InvalidLodIndex_ReturnsZero(int invalidLod)
        {
            // Arrange
            float[] lodDistances = { 10f, 20f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);

            // Act
            int count = _registry.GetKeyCountInLODLevel(invalidLod, Vector3.zero);

            // Assert
            Assert.AreEqual(0, count);
        }

        [Test]
        public void GetKeyCountInLODLevel_UninitializedRegistry_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.GetKeyCountInLODLevel(0, Vector3.zero));
        }

        #endregion

        #region GetKeyCountInFullRange Tests

        [Test]
        public void GetKeyCountInFullRange_MatchesActualCollectedFullRangeKeysCount()
        {
            // Arrange
            float[] lodDistances = { 10f, 20f, 30f };
            Vector3 center = Vector3.zero;
            _viewerObject1.transform.position = center;
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);

            // Act
            List<Vector3Int> fullRangeKeys = ToList(_registry.GetKeysInFullRange(center));
            int countedFullRange = _registry.GetKeyCountInFullRange(center);

            // Assert
            Assert.AreEqual(fullRangeKeys.Count, countedFullRange, "Full range key count does not match the actual number of collected full range keys.");
        }

        [Test]
        public void GetKeyCountInFullRange_UninitializedRegistry_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.GetKeyCountInFullRange(Vector3.zero));
        }

        #endregion

        #region GetMaxCapacityForLODLevel Tests

        [Test]
        public void GetMaxCapacityForLODLevel_ValidIndices_ReturnsNonNegativeCapacity()
        {
            // Arrange
            float[] lodDistances = { 10f, 20f, 30f };
            _viewerObject1.transform.position = Vector3.zero;
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);

            // Act & Assert for each LOD level
            for (int lod = 0; lod < _registry.LodCount; lod++)
            {
                int capacity = _registry.GetMaxCapacityForLODLevel(lod);
                Assert.IsTrue(capacity >= 0, $"Max capacity for LOD {lod} must be greater than or equal to zero.");
            }
        }

        [Test]
        [TestCase(-1)]
        [TestCase(99)]
        public void GetMaxCapacityForLODLevel_InvalidLodIndex_ReturnsZero(int invalidLod)
        {
            // Arrange
            float[] lodDistances = { 10f, 20f };
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);

            // Act
            int capacity = _registry.GetMaxCapacityForLODLevel(invalidLod);

            // Assert
            Assert.AreEqual(0, capacity);
        }

        [Test]
        public void GetMaxCapacityForLODLevel_UninitializedRegistry_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.GetMaxCapacityForLODLevel(0));
        }

        #endregion

        #region UpdateLODs Tests

        [Test]
        public void UpdateLODs_UninitializedRegistry_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new LODChunkRegistry<TestLodChunk>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => uninitializedRegistry.UpdateLODs());
            Assert.Throws<InvalidOperationException>(() => uninitializedRegistry.UpdateLODs(Vector3.zero));
        }

        [Test]
        public void UpdateLODs_NoChunks_ReturnsZeroChanges()
        {
            // Arrange
            float[] lodDistances = { 10f, 20f };
            _viewerObject1.transform.position = Vector3.zero;
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);

            // Act
            int changes = _registry.UpdateLODs();

            // Assert
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void UpdateLODs_WithFocusPosition_UpdatesChunksAndReturnsChangeCount()
        {
            // Arrange
            float[] lodDistances = { 10f, 20f, 30f };
            _viewerObject1.transform.position = Vector3.zero;
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);

            // Create a chunk using proper handler
            Vector3Int chunkKey = new Vector3Int(5, 0, 0);
            var handler = new DummyConfigureHandler();
            _registry.GetOrCreateChunk(chunkKey, ref handler, out TestLodChunk chunk);
            int initialLod = chunk.CurrentLOD;

            // Act: Move focus position close to the chunk to force a LOD transition
            Vector3 newFocusPos = new Vector3(50f, 0f, 0f);
            int changes = _registry.UpdateLODs(newFocusPos);

            // Assert
            Assert.AreEqual(1, changes, "Exactly one chunk should have changed its LOD level.");
            Assert.AreNotEqual(initialLod, chunk.CurrentLOD, "Chunk LOD should have been updated.");
        }

        [Test]
        public void UpdateLODs_DefaultOverload_UsesViewerPosition()
        {
            // Arrange
            float[] lodDistances = { 10f, 20f, 30f };
            _viewerObject1.transform.position = new Vector3(50f, 0f, 0f);
            _registry.Initialize(GridSize.Size10, Vector3.zero, lodDistances, _viewerObject1.transform);

            Vector3Int chunkKey = new Vector3Int(5, 0, 0);
            var handler = new DummyConfigureHandler();
            _registry.GetOrCreateChunk(chunkKey, ref handler, out TestLodChunk chunk);

            // Act: Reposition the viewer transform directly and call the parameterless overload
            _viewerObject1.transform.position = Vector3.zero;
            int changes = _registry.UpdateLODs();

            // Assert
            Assert.IsTrue(changes > 0, "UpdateLODs() should detect changes based on the updated viewer position.");
        }

        #endregion
    }
}
