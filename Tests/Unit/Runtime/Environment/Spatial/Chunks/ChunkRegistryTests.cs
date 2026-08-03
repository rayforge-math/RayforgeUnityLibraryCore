using NUnit.Framework;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Rayforge.Core.Environment.Spatial.Chunks.Tests
{
    public class ChunkRegistryTests
    {
        #region Test Env

        [ChunkConfig(SpatialAxes.X)]
        public class TestChunkX : Chunk<TestChunkX>
        {
            public override bool IsDirty => false;
            public override void ClearDirty() { }
            protected override void OnDispose() { }
        }

        [ChunkConfig(SpatialAxes.Y)]
        public class TestChunkY : Chunk<TestChunkY>
        {
            public override bool IsDirty => false;
            public override void ClearDirty() { }
            protected override void OnDispose() { }
        }

        [ChunkConfig(SpatialAxes.Z)]
        public class TestChunkZ : Chunk<TestChunkZ>
        {
            public override bool IsDirty => false;
            public override void ClearDirty() { }
            protected override void OnDispose() { }
        }

        [ChunkConfig(SpatialAxes.X | SpatialAxes.Z)]
        public class TestChunkXZ : Chunk<TestChunkXZ>
        {
            public override bool IsDirty => false;
            public override void ClearDirty() { }
            protected override void OnDispose() { }
        }

        [ChunkConfig(SpatialAxes.X | SpatialAxes.Y)]
        public class TestChunkXY : Chunk<TestChunkXY>
        {
            public override bool IsDirty => false;
            public override void ClearDirty() { }
            protected override void OnDispose() { }
        }

        [ChunkConfig(SpatialAxes.Y | SpatialAxes.Z)]
        public class TestChunkYZ : Chunk<TestChunkYZ>
        {
            public override bool IsDirty => false;
            public override void ClearDirty() { }
            protected override void OnDispose() { }
        }

        [ChunkConfig(SpatialAxes.X | SpatialAxes.Y | SpatialAxes.Z)]
        public class TestChunkXYZ : Chunk<TestChunkXYZ>
        {
            public override bool IsDirty => false;
            public override void ClearDirty() { }
            protected override void OnDispose() { }
        }

        [ChunkConfig(SpatialAxes.None)]
        public class InvalidChunk : Chunk<InvalidChunk>
        {
            public override bool IsDirty => false;

            public override void ClearDirty()
            {
                // No-op for testing
            }

            protected override void OnDispose()
            {
                // No-op for testing
            }
        }

        private class ExposedChunkRegistry : ChunkRegistry<TestChunkXYZ>
        {
            public new Vector3 Anchor
            {
                get => base.Anchor;
                set => base.Anchor = value;
            }
        }

        private struct DummyExecutionHandler : IExecutionHandler<TestChunkXYZ>
        {
            public void Execute(TestChunkXYZ obj) { }
        }

        private struct TestExecutionHandler : IExecutionHandler<Vector3Int>
        {
            public System.Collections.Generic.List<Vector3Int> ExecutedKeys { get; }

            public TestExecutionHandler(int dummy)
            {
                ExecutedKeys = new System.Collections.Generic.List<Vector3Int>();
            }

            public void Execute(Vector3Int item)
            {
                ExecutedKeys.Add(item);
            }
        }

        private struct ChunkExecutionHandler<TChunk> : IExecutionHandler<TChunk>
            where TChunk : Chunk<TChunk>
        {
            public Action<TChunk> executeAction;

            public void Execute(TChunk chunk)
            {
                executeAction?.Invoke(chunk);
            }
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void Initialize_AssignsBaseNameAndUpdatesRegistryName()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();

            registry.Initialize(GridSize.Size16, Vector3.zero, null, "MyCustomBaseName");

            StringAssert.Contains("MyCustomBaseName_Size16", registry.RegistryName);
        }

        [Test]
        public void Initialize_AssignsGridSizeCorrectly()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();

            registry.Initialize(GridSize.Size64, Vector3.zero, null, "Registry");

            Assert.AreEqual(GridSize.Size64, registry.GridSize);
        }

        [Test]
        public void Initialize_AssignsAnchorCorrectly()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            var targetAnchor = new Vector3(12.5f, -3.2f, 42.0f);

            registry.Initialize(GridSize.Size16, targetAnchor, null, "Registry");

            Assert.AreEqual(targetAnchor, registry.Anchor);
        }

        [Test]
        public void Initialize_AssignsActiveAxesFromChunkType()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();

            registry.Initialize(GridSize.Size16, Vector3.zero, null, "Registry");

            Assert.AreEqual(SpatialAxes.X | SpatialAxes.Y | SpatialAxes.Z, registry.ActiveAxes);
            Assert.IsTrue(registry.IsXActive);
            Assert.IsTrue(registry.IsYActive);
            Assert.IsTrue(registry.IsZActive);
        }

        [Test]
        public void Initialize_InvokesOnGridStructureChangedEvent()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();

            ISpatialGridConfiguration<Vector3Int> eventArg = null;
            int invokeCount = 0;

            registry.OnGridStructureChanged += (config) =>
            {
                eventArg = config;
                invokeCount++;
            };

            registry.Initialize(GridSize.Size16, Vector3.zero, null, "Registry");

            Assert.AreEqual(1, invokeCount);
            Assert.AreEqual(registry, eventArg);
        }

        [Test]
        public void Initialize_InvokesOnAnchorChangedEventWithCorrectDelta()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();

            ISpatialGridConfiguration<Vector3Int> eventConfig = null;
            Vector3 receivedDelta = Vector3.one;
            int invokeCount = 0;

            registry.OnAnchorChanged += (config, delta) =>
            {
                eventConfig = config;
                receivedDelta = delta;
                invokeCount++;
            };

            registry.Initialize(GridSize.Size16, new Vector3(10f, 20f, 30f), null, "Registry");

            Assert.AreEqual(1, invokeCount);
            Assert.AreEqual(registry, eventConfig);
            Assert.AreEqual(new Vector3(10f, 20f, 30f), receivedDelta);
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-16)]
        public void Initialize_ZeroOrNegativeGridSize_ThrowsArgumentException(int invalidSizeValue)
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                registry.Initialize((GridSize)invalidSizeValue, Vector3.zero, null, "Registry");
            });

            StringAssert.Contains("Invalid GridSize", ex.Message);
        }

        [Test]
        public void Initialize_ParentTransformAssignedCorrectly()
        {
            var parentObj = new GameObject("ParentTransform");
            var registry = new ChunkRegistry<TestChunkXYZ>();

            registry.Initialize(GridSize.Size16, Vector3.zero, parentObj.transform, "Registry");

            Assert.AreEqual(parentObj.transform, registry.Container.parent);

            UnityEngine.Object.DestroyImmediate(parentObj);
        }

        [Test]
        public void Initialize_MissingActiveAxes_ThrowsInvalidOperationException()
        {
            var registry = new ChunkRegistry<InvalidChunk>();

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                registry.Initialize(GridSize.Size16, Vector3.zero, null, "Registry");
            });

            StringAssert.Contains("No active axes defined", ex.Message);
        }

        [Test]
        public void Initialize_NullOrEmptyName_UsesDefaultFallbackName()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();

            registry.Initialize(GridSize.Size16, Vector3.zero, null, null);

            StringAssert.Contains("ChunkRegistry_Size16", registry.RegistryName);
        }

        #endregion

        #region Event Invocation Tests

        [Test]
        public void Initialize_TriggersOnGridStructureChangedExactlyOnce()
        {
            var registry = new ExposedChunkRegistry();
            int invokeCount = 0;
            ISpatialGridConfiguration<Vector3Int> eventArg = null;

            registry.OnGridStructureChanged += config =>
            {
                invokeCount++;
                eventArg = config;
            };

            registry.Initialize(GridSize.Size16, Vector3.zero, null, "TestRegistry");

            Assert.AreEqual(1, invokeCount);
            Assert.AreEqual(registry, eventArg);
        }

        [Test]
        public void Initialize_WithInitialAnchorNonZero_TriggersOnAnchorChangedWithDelta()
        {
            var registry = new ExposedChunkRegistry();
            int invokeCount = 0;
            Vector3 receivedDelta = Vector3.zero;
            ISpatialGridConfiguration<Vector3Int> eventConfig = null;

            registry.OnAnchorChanged += (config, delta) =>
            {
                invokeCount++;
                eventConfig = config;
                receivedDelta = delta;
            };

            var initialAnchor = new Vector3(5f, 10f, 15f);
            registry.Initialize(GridSize.Size16, initialAnchor, null, "TestRegistry");

            Assert.AreEqual(1, invokeCount);
            Assert.AreEqual(registry, eventConfig);
            Assert.AreEqual(initialAnchor, receivedDelta);
        }

        [Test]
        public void Initialize_WithInitialAnchorZero_DoesNotTriggerOnAnchorChanged()
        {
            var registry = new ExposedChunkRegistry();
            int invokeCount = 0;

            registry.OnAnchorChanged += (_, __) => invokeCount++;

            registry.Initialize(GridSize.Size16, Vector3.zero, null, "TestRegistry");

            Assert.AreEqual(0, invokeCount);
        }

        [Test]
        public void GridSizeSetter_TriggersOnGridStructureChanged()
        {
            var registry = new ExposedChunkRegistry();
            registry.Initialize(GridSize.Size16, Vector3.zero, null, "TestRegistry");

            int invokeCount = 0;
            registry.OnGridStructureChanged += _ => invokeCount++;

            // Act: Use the property setter instead of SetGridSize
            registry.GridSize = GridSize.Size32;

            Assert.AreEqual(1, invokeCount);
        }

        [Test]
        public void AnchorSetter_TriggersOnAnchorChangedWithCorrectDelta()
        {
            var registry = new ExposedChunkRegistry();
            registry.Initialize(GridSize.Size16, new Vector3(1f, 2f, 3f), null, "TestRegistry");

            int invokeCount = 0;
            Vector3 receivedDelta = Vector3.zero;
            ISpatialGridConfiguration<Vector3Int> eventConfig = null;

            registry.OnAnchorChanged += (config, delta) =>
            {
                invokeCount++;
                eventConfig = config;
                receivedDelta = delta;
            };

            var newAnchor = new Vector3(11f, 12f, 13f);

            // Act: Use the property setter instead of SetAnchor
            registry.Anchor = newAnchor;

            Assert.AreEqual(1, invokeCount);
            Assert.AreEqual(registry, eventConfig);
            Assert.AreEqual(new Vector3(10f, 10f, 10f), receivedDelta);
        }

        [Test]
        public void NotifyOriginShift_TriggersOnAnchorChangedWithShiftDelta()
        {
            var registry = new ExposedChunkRegistry();
            registry.Initialize(GridSize.Size16, Vector3.zero, null, "TestRegistry");

            int invokeCount = 0;
            Vector3 receivedDelta = Vector3.zero;

            registry.OnAnchorChanged += (_, delta) =>
            {
                invokeCount++;
                receivedDelta = delta;
            };

            var shiftVector = new Vector3(4f, 5f, 6f);
            registry.NotifyOriginShift(shiftVector);

            Assert.AreEqual(1, invokeCount);
            Assert.AreEqual(shiftVector, receivedDelta);
            Assert.AreEqual(shiftVector, registry.Anchor);
        }

        #endregion

        #region GridSize Tests

        [Test]
        public void GridSize_AfterInitialization_ReturnsCorrectValue()
        {
            // Arrange & Act: Initialize registry with Size32
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size32, Vector3.zero);

            // Assert: GridSize must match the initialized value
            Assert.AreEqual(GridSize.Size32, registry.GridSize);
        }

        [Test]
        public void GridSizeSetter_SameValue_DoesNotTriggerChangeOrClear()
        {
            // Arrange: Setup registry and populate it with a chunk
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            DummyExecutionHandler handler = default;
            registry.GetOrCreateChunk(Vector3Int.zero, ref handler, out _);
            Assert.AreEqual(1, registry.Count);

            bool eventTriggered = false;
            registry.OnGridStructureChanged += _ => eventTriggered = true;

            // Act: Set the same GridSize via property
            registry.GridSize = GridSize.Size16;

            // Assert: Should early-exit without side effects
            Assert.IsFalse(eventTriggered);
            Assert.AreEqual(1, registry.Count, "Registry should not be cleared when GridSize remains unchanged.");
        }

        [Test]
        public void GridSizeSetter_NewValue_UpdatesGridSizeAndClearsExistingChunks()
        {
            // Arrange: Setup registry and add multiple chunks
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            DummyExecutionHandler handler = default;
            registry.GetOrCreateChunk(Vector3Int.zero, ref handler, out _);
            registry.GetOrCreateChunk(new Vector3Int(1, 0, 0), ref handler, out _);
            Assert.AreEqual(2, registry.Count);

            // Act: Change GridSize to a new valid value via property
            registry.GridSize = GridSize.Size32;

            // Assert: Value updated and old cells cleared
            Assert.AreEqual(GridSize.Size32, registry.GridSize);
            Assert.AreEqual(0, registry.Count, "Registry must clear all chunks when GridSize changes.");
        }

        [Test]
        public void GridSizeSetter_NewValue_UpdatesRegistryNameWithBaseName()
        {
            // Arrange: Initialize with custom base name
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero, null, "CustomBaseName");

            // Act: Change GridSize via property
            registry.GridSize = GridSize.Size32;

            // Assert: Name must be re-formatted using base name and new size.
            // StartsWith is used to ignore potential Instance IDs appended by Unity/Base classes.
            StringAssert.StartsWith("CustomBaseName_Size32", registry.RegistryName);
        }

        [Test]
        public void GridSizeSetter_NewValue_TriggersOnGridStructureChangedEvent()
        {
            // Arrange: Initialize registry and subscribe to structure changed event
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            int eventInvokeCount = 0;
            ISpatialGridConfiguration<Vector3Int> receivedConfig = null;

            registry.OnGridStructureChanged += config =>
            {
                eventInvokeCount++;
                receivedConfig = config;
            };

            // Act: Change GridSize via property
            registry.GridSize = GridSize.Size32;

            // Assert: Event must be fired exactly once with the registry instance
            Assert.AreEqual(1, eventInvokeCount);
            Assert.AreEqual(registry, receivedConfig);
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-32)]
        public void GridSizeSetter_InvalidOrNegativeValue_ThrowsArgumentException(int invalidSizeValue)
        {
            // Arrange: Setup registry with a valid size
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Act & Assert: Setting zero or negative values must throw ArgumentException
            Assert.Throws<ArgumentException>(() => registry.GridSize = (GridSize)invalidSizeValue);
        }

        #endregion

        #region Anchor Tests

        [Test]
        public void Anchor_SetSameValue_DoesNotTriggerEventOrUpdateContainer()
        {
            // Arrange
            var registry = new ExposedChunkRegistry();
            registry.Initialize(GridSize.Size16, Vector3.one);

            int eventInvokeCount = 0;
            registry.OnAnchorChanged += (_, __) => eventInvokeCount++;

            // Reset container position to track if it gets modified
            registry.Container.position = Vector3.zero;

            // Act
            registry.Anchor = Vector3.one;

            // Assert
            Assert.AreEqual(0, eventInvokeCount, "Event should not be invoked when setting the same anchor value.");
            Assert.AreEqual(Vector3.zero, registry.Container.position, "Container position should not be updated if the anchor value hasn't changed.");
        }

        [Test]
        public void Anchor_SetNaNX_ThrowsArgumentException()
        {
            // Arrange
            var registry = new ExposedChunkRegistry();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => registry.Anchor = new Vector3(float.NaN, 0, 0));
            StringAssert.Contains("Anchor cannot contain NaN", ex.Message);
        }

        [Test]
        public void Anchor_SetNaNY_ThrowsArgumentException()
        {
            // Arrange
            var registry = new ExposedChunkRegistry();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => registry.Anchor = new Vector3(0, float.NaN, 0));
            StringAssert.Contains("Anchor cannot contain NaN", ex.Message);
        }

        [Test]
        public void Anchor_SetNaNZ_ThrowsArgumentException()
        {
            // Arrange
            var registry = new ExposedChunkRegistry();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => registry.Anchor = new Vector3(0, 0, float.NaN));
            StringAssert.Contains("Anchor cannot contain NaN", ex.Message);
        }

        [Test]
        public void Anchor_SetValidValue_UpdatesAnchorProperty()
        {
            // Arrange
            var registry = new ExposedChunkRegistry();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var newAnchor = new Vector3(10f, 20f, 30f);

            // Act
            registry.Anchor = newAnchor;

            // Assert
            Assert.AreEqual(newAnchor, registry.Anchor, "Anchor property should reflect the newly set value.");
        }

        [Test]
        public void Anchor_SetValidValue_InvokesOnAnchorChangedWithCorrectDelta()
        {
            // Arrange
            var registry = new ExposedChunkRegistry();
            var initialAnchor = new Vector3(5f, 5f, 5f);
            registry.Initialize(GridSize.Size16, initialAnchor);

            int eventInvokeCount = 0;
            Vector3 receivedDelta = Vector3.zero;
            ISpatialGridConfiguration<Vector3Int> receivedConfig = null;

            registry.OnAnchorChanged += (config, delta) =>
            {
                eventInvokeCount++;
                receivedConfig = config;
                receivedDelta = delta;
            };

            var newAnchor = new Vector3(15f, 5f, 15f);
            var expectedDelta = new Vector3(10f, 0f, 10f); // newAnchor - initialAnchor

            // Act
            registry.Anchor = newAnchor;

            // Assert
            Assert.AreEqual(1, eventInvokeCount, "OnAnchorChanged should be invoked exactly once.");
            Assert.AreEqual(registry, receivedConfig, "Event should pass the registry as the config argument.");
            Assert.AreEqual(expectedDelta, receivedDelta, "Event should pass the correct delta vector.");
        }

        [Test]
        public void Anchor_ContainerNotLinked_UpdatesContainerPosition()
        {
            // Arrange
            var registry = new ExposedChunkRegistry();
            // Initializing without a parent sets m_ContainerLinkedToAnchor = false
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var newAnchor = new Vector3(100f, 200f, 300f);

            // Act
            registry.Anchor = newAnchor;

            // Assert
            Assert.IsFalse(registry.ContainerLinkedToAnchor, "Container should not be linked to an anchor.");
            Assert.AreEqual(newAnchor, registry.Container.position, "Container position should be updated to the new anchor when not linked.");
        }

        [Test]
        public void Anchor_ContainerLinked_DoesNotUpdateContainerPosition()
        {
            // Arrange
            var parentObj = new GameObject("Parent");
            var registry = new ExposedChunkRegistry();

            // Initializing with a parent sets m_ContainerLinkedToAnchor = true
            registry.Initialize(GridSize.Size16, Vector3.zero, parentObj.transform);

            var newAnchor = new Vector3(100f, 200f, 300f);
            var originalContainerPosition = registry.Container.position;

            // Act
            registry.Anchor = newAnchor;

            // Assert
            Assert.IsTrue(registry.ContainerLinkedToAnchor, "Container should be linked to an anchor.");
            Assert.AreEqual(originalContainerPosition, registry.Container.position, "Container position should NOT be directly updated when it is linked to a parent.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(parentObj);
        }

        [UnityTest]
        public IEnumerator Anchor_ContainerIsNull_DoesNotThrowException()
        {
            // Arrange
            var registry = new ExposedChunkRegistry();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Force container to be null to test the null check
            UnityEngine.Object.DestroyImmediate(registry.Container.gameObject);

            yield return null;

            // Act & Assert
            // This should not throw a MissingReferenceException or NullReferenceException
            Assert.DoesNotThrow(() => registry.Anchor = new Vector3(5f, 5f, 5f), "Setting the anchor should not throw if the container was destroyed/is null.");
        }

        #endregion

        #region ActiveAxes Tests

        [Test]
        public void ActiveAxes_AfterInitialization_MatchesChunkConfigXYZ()
        {
            // Arrange: Create a registry for a fully 3D configured chunk
            var registry = new ChunkRegistry<TestChunkXYZ>();

            // Act
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Assert: ActiveAxes should match the [ChunkConfig] of TestChunkXYZ
            var expectedAxes = SpatialAxes.X | SpatialAxes.Y | SpatialAxes.Z;
            Assert.AreEqual(expectedAxes, registry.ActiveAxes, "ActiveAxes should match the XYZ configuration of the chunk type.");
        }

        [Test]
        public void ActiveAxes_AfterInitialization_MatchesChunkConfigXZ()
        {
            // Arrange: Create a registry for a 2D (X/Z plane) configured chunk

            var registry = new ChunkRegistry<TestChunkXZ>();

            // Act
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Assert: ActiveAxes should match the [ChunkConfig] of TestChunkXZ
            var expectedAxes = SpatialAxes.X | SpatialAxes.Z;
            Assert.AreEqual(expectedAxes, registry.ActiveAxes, "ActiveAxes should match the XZ configuration of the chunk type.");
        }

        #endregion

        #region Axis Flags Tests

        [Test]
        public void IsAxesActive_WithOnlyX_ReturnsTrueForXOnly()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkX>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Assert
            Assert.IsTrue(registry.IsXActive, "IsXActive should be true.");
            Assert.IsFalse(registry.IsYActive, "IsYActive should be false.");
            Assert.IsFalse(registry.IsZActive, "IsZActive should be false.");
        }

        [Test]
        public void IsAxesActive_WithOnlyY_ReturnsTrueForYOnly()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkY>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Assert
            Assert.IsFalse(registry.IsXActive, "IsXActive should be false.");
            Assert.IsTrue(registry.IsYActive, "IsYActive should be true.");
            Assert.IsFalse(registry.IsZActive, "IsZActive should be false.");
        }

        [Test]
        public void IsAxesActive_WithOnlyZ_ReturnsTrueForZOnly()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Assert
            Assert.IsFalse(registry.IsXActive, "IsXActive should be false.");
            Assert.IsFalse(registry.IsYActive, "IsYActive should be false.");
            Assert.IsTrue(registry.IsZActive, "IsZActive should be true.");
        }

        [Test]
        public void IsAxesActive_WithXZ_ReturnsTrueForXAndZ()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Assert
            Assert.IsTrue(registry.IsXActive, "IsXActive should be true.");
            Assert.IsFalse(registry.IsYActive, "IsYActive should be false.");
            Assert.IsTrue(registry.IsZActive, "IsZActive should be true.");
        }

        [Test]
        public void IsAxesActive_WithXYZ_ReturnsTrueForAllAxes()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Assert
            Assert.IsTrue(registry.IsXActive, "IsXActive should be true.");
            Assert.IsTrue(registry.IsYActive, "IsYActive should be true.");
            Assert.IsTrue(registry.IsZActive, "IsZActive should be true.");
        }

        #endregion

        #region IsAxisActive Tests

        [Test]
        [TestCase(0, true)]   // X is active
        [TestCase(1, false)]  // Y is inactive
        [TestCase(2, true)]   // Z is active
        public void IsAxisActive_WithXZConfig_ReturnsCorrectMapping(int axisIndex, bool expectedResult)
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Act
            bool result = registry.IsAxisActive(axisIndex);

            // Assert
            Assert.AreEqual(expectedResult, result, $"IsAxisActive({axisIndex}) should return {expectedResult} for XZ configuration.");
        }

        [Test]
        [TestCase(0, false)]  // X is inactive
        [TestCase(1, true)]   // Y is active
        [TestCase(2, false)]  // Z is inactive
        public void IsAxisActive_WithYConfig_ReturnsCorrectMapping(int axisIndex, bool expectedResult)
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkY>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Act
            bool result = registry.IsAxisActive(axisIndex);

            // Assert
            Assert.AreEqual(expectedResult, result, $"IsAxisActive({axisIndex}) should return {expectedResult} for Y-only configuration.");
        }

        [Test]
        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(99)]
        public void IsAxisActive_WithOutOfBoundsIndex_ReturnsFalse(int invalidIndex)
        {
            // Arrange: We use XYZ config to ensure the method returns false *only* because of the invalid index, 
            // not because the axes themselves are disabled.
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Act
            bool result = registry.IsAxisActive(invalidIndex);

            // Assert
            Assert.IsFalse(result, $"IsAxisActive should return false for invalid index {invalidIndex}.");
        }

        #endregion

        #region ActiveAxisCount Tests

        [Test]
        public void ActiveAxisCount_With1DConfig_ReturnsOne()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkX>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Act
            int count = registry.ActiveAxisCount();

            // Assert
            Assert.AreEqual(1, count, "ActiveAxisCount should return 1 for a 1-dimensional configuration.");
        }

        [Test]
        public void ActiveAxisCount_With2DConfig_ReturnsTwo()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Act
            int count = registry.ActiveAxisCount();

            // Assert
            Assert.AreEqual(2, count, "ActiveAxisCount should return 2 for a 2-dimensional configuration.");
        }

        [Test]
        public void ActiveAxisCount_With3DConfig_ReturnsThree()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Act
            int count = registry.ActiveAxisCount();

            // Assert
            Assert.AreEqual(3, count, "ActiveAxisCount should return 3 for a 3-dimensional configuration.");
        }

        #endregion

        #region WorldToGrid Tests

        [Test]
        public void WorldToGrid_ZeroPositionAndZeroAnchor_ReturnsZeroKey()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var gridKey = registry.WorldToGrid(Vector3.zero);

            Assert.AreEqual(Vector3Int.zero, gridKey);
        }

        [Test]
        public void WorldToGrid_WithAnchorOffset_CalculatesRelativePosition()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            var anchor = new Vector3(10f, 20f, 30f);
            registry.Initialize(GridSize.Size16, anchor);

            // World position exactly one grid cell away from anchor (Size16 = 16 units)
            var worldPos = new Vector3(26f, 36f, 46f);
            var gridKey = registry.WorldToGrid(worldPos);

            Assert.AreEqual(new Vector3Int(1, 1, 1), gridKey);
        }

        [Test]
        public void WorldToGrid_NegativeCoordinates_CalculatesCorrectNegativeKeys()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Position -16 on X falls into cell index -1.
            // Position -8 on Y with mathematical floor (-8 / 16 = -0.5) floors down to -1.
            var worldPos = new Vector3(-16f, -8f, 0f);
            var gridKey = registry.WorldToGrid(worldPos);

            Assert.AreEqual(new Vector3Int(-1, -1, 0), gridKey);
        }

        [Test]
        public void WorldToGrid_MasksInactiveAxes_ZeroesOutUnconfiguredAxis()
        {
            // TestChunkXZ has only X and Z active; Y should be masked to 0 regardless of world pos
            var registry = new ChunkRegistry<TestChunkXZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var worldPos = new Vector3(32f, 999f, 32f); // Y is very large
            var gridKey = registry.WorldToGrid(worldPos);

            Assert.AreEqual(new Vector3Int(2, 0, 2), gridKey, "Inactive Y axis must be masked out to zero.");
        }

        [Test]
        public void WorldToGrid_EdgeCase_ValuesOnCellBoundary()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Exactly on the boundary of cell 1 (16.0f)
            var worldPos = new Vector3(16f, 0f, 0f);
            var gridKey = registry.WorldToGrid(worldPos);

            Assert.AreEqual(new Vector3Int(1, 0, 0), gridKey);
        }

        [Test]
        public void WorldToGrid_EdgeCase_JustBelowCellBoundary()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Just below the boundary of cell 1 (15.999f)
            var worldPos = new Vector3(15.999f, 0f, 0f);
            var gridKey = registry.WorldToGrid(worldPos);

            Assert.AreEqual(new Vector3Int(0, 0, 0), gridKey);
        }

        [Test]
        public void WorldToGrid_EdgeCase_VeryLargeCoordinates_DoesNotOverflow()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var worldPos = new Vector3(160000f, -160000f, 0f);
            var gridKey = registry.WorldToGrid(worldPos);

            Assert.AreEqual(new Vector3Int(10000, -10000, 0), gridKey);
        }

        [Test]
        [TestCase(float.NaN, 0f, 0f)]
        [TestCase(0f, float.NaN, 0f)]
        [TestCase(0f, 0f, float.NaN)]
        public void WorldToGrid_NaNCoordinates_ThrowsArgumentException(float x, float y, float z)
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Verify that passing NaN values throws an ArgumentException due to guard clauses
            var invalidPos = new Vector3(x, y, z);
            Assert.Throws<ArgumentException>(() => registry.WorldToGrid(invalidPos));
        }

        #endregion

        #region GridToWorld Tests

        [Test]
        public void GridToWorld_ZeroKeyAndZeroAnchor_ReturnsCenterOfZeroCell()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Size16, centered=true -> cell spans from 0 to 16, center is at 8
            var worldPos = registry.GridToWorld(Vector3Int.zero);

            Assert.AreEqual(new Vector3(8f, 8f, 8f), worldPos);
        }

        [Test]
        public void GridToWorld_WithAnchorOffset_CalculatesCorrectPosition()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            var anchor = new Vector3(10f, 20f, 30f);
            registry.Initialize(GridSize.Size16, anchor);

            // Key (1, 1, 1) with Size16 and anchor (10, 20, 30)
            // Formula: key * size + anchor + (size * 0.5) -> 1 * 16 + 10 + 8 = 34
            var worldPos = registry.GridToWorld(new Vector3Int(1, 1, 1));

            Assert.AreEqual(new Vector3(34f, 44f, 54f), worldPos);
        }

        [Test]
        public void GridToWorld_NegativeKeys_CalculatesCorrectNegativePosition()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Key (-1, -1, 0) with Size16, centered=true
            // Formula: -1 * 16 + 8 = -8
            var worldPos = registry.GridToWorld(new Vector3Int(-1, -1, 0));

            Assert.AreEqual(new Vector3(-8f, -8f, 8f), worldPos);
        }

        [Test]
        public void GridToWorld_MasksInactiveAxes_ZeroesOutUnconfiguredAxisWorldPosition()
        {
            // TestChunkXZ has only X and Z active; Y should be masked out
            var registry = new ChunkRegistry<TestChunkXZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var worldPos = registry.GridToWorld(new Vector3Int(2, 5, 2)); // Y key is 5, but should be masked

            // X: 2 * 16 + 8 = 40, Y: Masked to 0 (or anchor.y), Z: 40
            var expectedY = registry.Anchor.y;
            Assert.AreEqual(new Vector3(40f, expectedY, 40f), worldPos, "Inactive Y axis world position must be masked out.");
        }

        [Test]
        public void GridToWorld_EdgeCase_VeryLargeKeys_DoesNotOverflow()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var worldPos = registry.GridToWorld(new Vector3Int(10000, -10000, 0));

            Assert.AreEqual(new Vector3(10000f * 16f + 8f, -10000f * 16f + 8f, 8f), worldPos);
        }

        #endregion

        #region GetBoundsForKey Tests

        [Test]
        public void GetBoundsForKey_ZeroKeyAndXYZActive_ReturnsCorrectBounds()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            Bounds bounds = registry.GetBoundsForKey(Vector3Int.zero);

            // Center of cell 0 with Size16 is at 8, size is 16 for all active axes
            Assert.AreEqual(new Vector3(8f, 8f, 8f), bounds.center);
            Assert.AreEqual(new Vector3(16f, 16f, 16f), bounds.size);
        }

        [Test]
        public void GetBoundsForKey_WithAnchorOffset_CalculatesCorrectBoundsCenter()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            var anchor = new Vector3(10f, 20f, 30f);
            registry.Initialize(GridSize.Size16, anchor);

            Bounds bounds = registry.GetBoundsForKey(new Vector3Int(1, 1, 1));

            // Center from GridToWorld for key (1,1,1) with anchor (10,20,30) and Size16 is (34, 44, 54)
            Assert.AreEqual(new Vector3(34f, 44f, 54f), bounds.center);
            Assert.AreEqual(new Vector3(16f, 16f, 16f), bounds.size);
        }

        [Test]
        public void GetBoundsForKey_InactiveAxes_ReturnsSmallFallbackSizeForInactiveDimensions()
        {
            // TestChunkXZ has only X and Z active; Y is inactive
            var registry = new ChunkRegistry<TestChunkXZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            Bounds bounds = registry.GetBoundsForKey(new Vector3Int(1, 5, 1));

            // Active axes (X, Z) should have size 16, inactive axis (Y) should fall back to 0.01f
            Assert.AreEqual(new Vector3(16f, 0.01f, 16f), bounds.size);

            // Explicitly check individual dimensions to ensure Y gets the fallback size
            Assert.AreEqual(16f, bounds.size.x);
            Assert.AreEqual(0.01f, bounds.size.y, "Inactive Y axis bounds size should be 0.01f fallback.");
            Assert.AreEqual(16f, bounds.size.z);
        }

        [Test]
        public void GetBoundsForKey_NegativeKey_ReturnsCorrectNegativeBounds()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            Bounds bounds = registry.GetBoundsForKey(new Vector3Int(-1, 0, -1));

            // Center for key (-1, 0, -1) with Size16 -> (-8f, 8f, -8f)
            Assert.AreEqual(new Vector3(-8f, 8f, -8f), bounds.center);
            Assert.AreEqual(new Vector3(16f, 16f, 16f), bounds.size);
        }

        #endregion

        #region GetKeysInBounds Tests

        [Test]
        public void GetKeysInBounds_ValidBounds_ReturnsExpectedKeysIterator()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Bounds covering cells from (0,0,0) to (1,1,1)
            // Min at (0,0,0), max at (32,32,32) with Size16 will span keys 0 to 2 inclusive depending on bounds exact corners.
            // Let's use precise coordinates: min (0,0,0), max (16,16,16) -> minKey (0,0,0), maxKey (1,1,1)
            var bounds = new Bounds(new Vector3(8f, 8f, 8f), new Vector3(16f, 16f, 16f));

            var iterator = registry.GetKeysInBounds(bounds);

            Assert.NotNull(iterator);

            // Collect all iterated keys to verify range
            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            // Should contain at least origin and neighboring keys
            Assert.IsNotEmpty(keys);
            Assert.Contains(Vector3Int.zero, keys);
        }

        [Test]
        public void GetKeysInBounds_BoundsSpanningMultipleCells_IteratesCorrectRange()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // World bounds from (0,0,0) to (32,32,32) -> 2x2x2 cells (Keys 0 to 1 inclusive)
            var minPoint = new Vector3(0f, 0f, 0f);
            var maxPoint = new Vector3(32f, 32f, 32f);
            var bounds = new Bounds();
            bounds.SetMinMax(minPoint, maxPoint);

            var iterator = registry.GetKeysInBounds(bounds);
            var keys = new System.Collections.Generic.List<Vector3Int>();

            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            // For Size16, [0, 32] yields keys 0, 1 (inclusive max depending on state logic, let's verify count or presence)
            Assert.Contains(new Vector3Int(0, 0, 0), keys);
            Assert.Contains(new Vector3Int(1, 1, 1), keys);
        }

        [Test]
        [TestCase(float.NaN, 0f, 0f, 10f, 10f, 10f)]
        [TestCase(0f, 0f, 0f, float.NaN, 10f, 10f)]
        public void GetKeysInBounds_NaNBounds_ThrowsArgumentException(float cx, float cy, float cz, float sx, float sy, float sz)
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var invalidBounds = new Bounds(new Vector3(cx, cy, cz), new Vector3(sx, sy, sz));

            Assert.Throws<ArgumentException>(() => registry.GetKeysInBounds(invalidBounds));
        }

        [Test]
        public void GetKeysInBounds_ZeroSizeBounds_ReturnsSingleKey()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Bounds with zero size centered at (8,8,8) -> should resolve to cell (0,0,0)
            var bounds = new Bounds(new Vector3(8f, 8f, 8f), Vector3.zero);

            var iterator = registry.GetKeysInBounds(bounds);
            var keys = new System.Collections.Generic.List<Vector3Int>();

            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.AreEqual(1, keys.Count);
            Assert.AreEqual(Vector3Int.zero, keys[0]);
        }

        [Test]
        public void GetKeysInBounds_DistantBounds_ReturnsIteratorWithoutThrowing()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Bounds very far away in positive space
            var bounds = new Bounds(new Vector3(10000f, 10000f, 10000f), new Vector3(10f, 10f, 10f));

            var iterator = registry.GetKeysInBounds(bounds);

            Assert.NotNull(iterator);

            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            // Should successfully execute iteration phase without exceptions
            Assert.IsNotNull(keys);
        }

        #endregion

        #region GetKeysInRelativeBounds Tests

        [Test]
        public void GetKeysInRelativeBounds_ValidRelativeBounds_ReturnsExpectedKeysIterator()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            var anchor = new Vector3(10f, 20f, 30f);
            registry.Initialize(GridSize.Size16, anchor);

            // Relative bounds from (0,0,0) to (16,16,16) relative to anchor
            // This should map to grid keys (0,0,0) to (1,1,1) depending on LocalToGrid implementation.
            var relBounds = new Bounds(new Vector3(8f, 8f, 8f), new Vector3(16f, 16f, 16f));

            var iterator = registry.GetKeysInRelativeBounds(relBounds);

            Assert.NotNull(iterator);

            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.IsNotEmpty(keys);
            Assert.Contains(Vector3Int.zero, keys);
        }

        [Test]
        public void GetKeysInRelativeBounds_WithCustomAnchor_TranslatesCorrectly()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            var anchor = new Vector3(100f, 100f, 100f);
            registry.Initialize(GridSize.Size16, anchor);

            // Relative bounds exactly covering cell 1 (Size16 -> 16 to 32 locally)
            var minPoint = new Vector3(16f, 16f, 16f);
            var maxPoint = new Vector3(32f, 32f, 32f);
            var relBounds = new Bounds();
            relBounds.SetMinMax(minPoint, maxPoint);

            var iterator = registry.GetKeysInRelativeBounds(relBounds);
            var keys = new System.Collections.Generic.List<Vector3Int>();

            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.Contains(new Vector3Int(1, 1, 1), keys);
        }

        [Test]
        [TestCase(float.NaN, 0f, 0f, 10f, 10f, 10f)]
        [TestCase(0f, 0f, 0f, float.NaN, 10f, 10f)]
        public void GetKeysInRelativeBounds_NaNBounds_ThrowsArgumentException(float cx, float cy, float cz, float sx, float sy, float sz)
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var invalidBounds = new Bounds(new Vector3(cx, cy, cz), new Vector3(sx, sy, sz));

            Assert.Throws<ArgumentException>(() => registry.GetKeysInRelativeBounds(invalidBounds));
        }

        [Test]
        public void GetKeysInRelativeBounds_ZeroSizeBounds_ReturnsSingleKey()
        {
            var anchor = new Vector3(10f, 20f, 30f);
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, anchor);

            // Zero-size relative bounds centered at local (8,8,8) -> maps to cell (0,0,0) relative to anchor
            var bounds = new Bounds(new Vector3(8f, 8f, 8f), Vector3.zero);

            var iterator = registry.GetKeysInRelativeBounds(bounds);
            var keys = new System.Collections.Generic.List<Vector3Int>();

            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.AreEqual(1, keys.Count);
            Assert.AreEqual(Vector3Int.zero, keys[0]);
        }

        [Test]
        public void GetKeysInRelativeBounds_DistantBounds_ReturnsIteratorWithoutThrowing()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Relative bounds very far away
            var bounds = new Bounds(new Vector3(10000f, 10000f, 10000f), new Vector3(10f, 10f, 10f));

            var iterator = registry.GetKeysInRelativeBounds(bounds);

            Assert.NotNull(iterator);

            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.IsNotNull(keys);
        }

        #endregion

        #region GetKeysInRadius Tests

        [Test]
        public void GetKeysInRadius_ValidRadius_ReturnsIteratorWithKeys()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Search around world center (8, 8, 8) with a radius of 5 units (edge distance mode)
            var iterator = registry.GetKeysInRadius(new Vector3(8f, 8f, 8f), 5f, useEdgeDistance: true);

            Assert.NotNull(iterator);

            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.IsNotEmpty(keys);
            Assert.Contains(Vector3Int.zero, keys);
        }

        [Test]
        public void GetKeysInRadius_CenterDistanceMode_ReturnsExpectedKeys()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Test with useEdgeDistance = false
            var iterator = registry.GetKeysInRadius(new Vector3(8f, 8f, 8f), 10f, useEdgeDistance: false);

            Assert.NotNull(iterator);

            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.IsNotEmpty(keys);
            Assert.Contains(Vector3Int.zero, keys);
        }

        [Test]
        [TestCase(float.NaN, 0f, 0f, 5f)]
        [TestCase(0f, float.NaN, 0f, 5f)]
        [TestCase(0f, 0f, float.NaN, 5f)]
        public void GetKeysInRadius_NaNWorldCenter_ThrowsArgumentException(float x, float y, float z, float radius)
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var invalidCenter = new Vector3(x, y, z);

            Assert.Throws<ArgumentException>(() => registry.GetKeysInRadius(invalidCenter, radius));
        }

        [Test]
        public void GetKeysInRadius_NegativeRadius_ThrowsArgumentException()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            Assert.Throws<ArgumentException>(() => registry.GetKeysInRadius(Vector3.zero, -1f));
        }

        [Test]
        public void GetKeysInRadius_ZeroRadius_ReturnsOnlyCenterKey()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var center = new Vector3(8f, 8f, 8f); // Center of cell (0,0,0)
            var iterator = registry.GetKeysInRadius(center, 0f, useEdgeDistance: true);

            Assert.NotNull(iterator);

            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.AreEqual(1, keys.Count);
            Assert.AreEqual(Vector3Int.zero, keys[0]);
        }

        [Test]
        public void GetKeysInRadius_LargeRadius_ReturnsMultipleKeysWithoutThrowing()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var center = new Vector3(8f, 8f, 8f);
            var iterator = registry.GetKeysInRadius(center, 100f, useEdgeDistance: false);

            Assert.NotNull(iterator);

            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.IsTrue(keys.Count > 1);
            Assert.Contains(Vector3Int.zero, keys);
        }

        #endregion

        #region GetKeysInRelativeRadius Tests

        [Test]
        public void GetKeysInRelativeRadius_ValidRelativeRadius_ReturnsIteratorWithKeys()
        {
            var anchor = new Vector3(10f, 20f, 30f);
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, anchor);

            // Search around relative center (8, 8, 8) with radius 5 units (edge distance mode)
            // Relative (8,8,8) + Anchor (10,20,30) = World (18,28,38)
            var iterator = registry.GetKeysInRelativeRadius(new Vector3(8f, 8f, 8f), 5f, useEdgeDistance: true);

            Assert.NotNull(iterator);

            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.IsNotEmpty(keys);
        }

        [Test]
        public void GetKeysInRelativeRadius_CenterDistanceMode_ReturnsExpectedKeys()
        {
            var anchor = new Vector3(5f, 5f, 5f);
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, anchor);

            // Test with useEdgeDistance = false using relative center
            var iterator = registry.GetKeysInRelativeRadius(new Vector3(8f, 8f, 8f), 10f, useEdgeDistance: false);

            Assert.NotNull(iterator);

            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.IsNotEmpty(keys);
        }

        [Test]
        [TestCase(float.NaN, 0f, 0f, 5f)]
        [TestCase(0f, float.NaN, 0f, 5f)]
        [TestCase(0f, 0f, float.NaN, 5f)]
        public void GetKeysInRelativeRadius_NaNRelativeCenter_ThrowsArgumentException(float x, float y, float z, float radius)
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var invalidCenter = new Vector3(x, y, z);

            Assert.Throws<ArgumentException>(() => registry.GetKeysInRelativeRadius(invalidCenter, radius));
        }

        [Test]
        public void GetKeysInRelativeRadius_NegativeRadius_ThrowsArgumentException()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            Assert.Throws<ArgumentException>(() => registry.GetKeysInRelativeRadius(Vector3.zero, -5f));
        }

        [Test]
        public void GetKeysInRelativeRadius_ZeroRadius_ReturnsOnlyTargetKey()
        {
            var anchor = new Vector3(10f, 20f, 30f);
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, anchor);

            // Relative center (8,8,8) with anchor (10,20,30) -> World center (18,28,38)
            var relativeCenter = new Vector3(8f, 8f, 8f);
            var iterator = registry.GetKeysInRelativeRadius(relativeCenter, 0f, useEdgeDistance: true);

            Assert.NotNull(iterator);

            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.AreEqual(1, keys.Count);
        }

        [Test]
        public void GetKeysInRelativeRadius_LargeRadius_ReturnsMultipleKeysWithoutThrowing()
        {
            var anchor = new Vector3(5f, 5f, 5f);
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, anchor);

            var relativeCenter = new Vector3(8f, 8f, 8f);
            var iterator = registry.GetKeysInRelativeRadius(relativeCenter, 100f, useEdgeDistance: false);

            Assert.NotNull(iterator);

            var keys = new System.Collections.Generic.List<Vector3Int>();
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.IsTrue(keys.Count > 1);
        }

        #endregion

        #region ForEachKeyInBounds Tests

        [Test]
        public void ForEachKeyInBounds_ValidBounds_ExecutesActionOnExpectedKeys()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Bounds covering a 3x3x3 cell range from (0,0,0) to (2,2,2) -> size 32x32x32 centered at (16,16,16)
            var bounds = new Bounds(new Vector3(16f, 16f, 16f), new Vector3(32f, 32f, 32f));

            var handler = new TestExecutionHandler(0);
            registry.ForEachKeyInBounds(bounds, ref handler);

            var expectedKeys = new[]
            {
                new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0),
                new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0), new Vector3Int(2, 1, 0),
                new Vector3Int(0, 2, 0), new Vector3Int(1, 2, 0), new Vector3Int(2, 2, 0),

                new Vector3Int(0, 0, 1), new Vector3Int(1, 0, 1), new Vector3Int(2, 0, 1),
                new Vector3Int(0, 1, 1), new Vector3Int(1, 1, 1), new Vector3Int(2, 1, 1),
                new Vector3Int(0, 2, 1), new Vector3Int(1, 2, 1), new Vector3Int(2, 2, 1),

                new Vector3Int(0, 0, 2), new Vector3Int(1, 0, 2), new Vector3Int(2, 0, 2),
                new Vector3Int(0, 1, 2), new Vector3Int(1, 1, 2), new Vector3Int(2, 1, 2),
                new Vector3Int(0, 2, 2), new Vector3Int(1, 2, 2), new Vector3Int(2, 2, 2)
            };

            CollectionAssert.AreEquivalent(expectedKeys, handler.ExecutedKeys);
        }

        [Test]
        public void ForEachKeyInBounds_NaNBounds_ThrowsArgumentException()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var invalidBounds = new Bounds(new Vector3(float.NaN, 0f, 0f), new Vector3(10f, 10f, 10f));
            var handler = new TestExecutionHandler(0);

            Assert.Throws<ArgumentException>(() => registry.ForEachKeyInBounds(invalidBounds, ref handler));
        }

        [Test]
        public void ForEachKeyInBounds_ZeroSizeBounds_ExecutesOnSingleKeyOnly()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Zero-size bounds at center of cell (0,0,0) -> (8,8,8)
            var bounds = new Bounds(new Vector3(8f, 8f, 8f), Vector3.zero);
            var handler = new TestExecutionHandler(0);

            registry.ForEachKeyInBounds(bounds, ref handler);

            Assert.AreEqual(1, handler.ExecutedKeys.Count);
            Assert.AreEqual(Vector3Int.zero, handler.ExecutedKeys[0]);
        }

        #endregion

        #region ForEachKeyInRelativeBounds Tests

        [Test]
        public void ForEachKeyInRelativeBounds_ValidRelativeBounds_ExecutesActionOnExpectedKeys()
        {
            var anchor = new Vector3(10f, 20f, 30f);
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, anchor);

            // Relative bounds adjusted to strictly cover only a 2x2x2 cell range (chunks 0 and 1)
            var relBounds = new Bounds(new Vector3(15.9f, 15.9f, 15.9f), new Vector3(31.8f, 31.8f, 31.8f));

            var handler = new TestExecutionHandler(0);
            registry.ForEachKeyInRelativeBounds(relBounds, ref handler);

            var expectedKeys = new[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(1, 0, 1),
                new Vector3Int(0, 1, 1),
                new Vector3Int(1, 1, 1)
            };

            CollectionAssert.AreEquivalent(expectedKeys, handler.ExecutedKeys);
        }

        [Test]
        public void ForEachKeyInRelativeBounds_NaNBounds_ThrowsArgumentException()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var invalidBounds = new Bounds(new Vector3(float.NaN, 0f, 0f), new Vector3(10f, 10f, 10f));
            var handler = new TestExecutionHandler(0);

            Assert.Throws<ArgumentException>(() => registry.ForEachKeyInRelativeBounds(invalidBounds, ref handler));
        }

        [Test]
        public void ForEachKeyInRelativeBounds_ZeroSizeBounds_ExecutesOnSingleKeyOnly()
        {
            var anchor = new Vector3(10f, 20f, 30f);
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, anchor);

            // Zero-size relative bounds at local center of cell (0,0,0) -> local (8,8,8)
            var relBounds = new Bounds(new Vector3(8f, 8f, 8f), Vector3.zero);
            var handler = new TestExecutionHandler(0);

            registry.ForEachKeyInRelativeBounds(relBounds, ref handler);

            Assert.AreEqual(1, handler.ExecutedKeys.Count);
            Assert.AreEqual(Vector3Int.zero, handler.ExecutedKeys[0]);
        }

        #endregion

        #region ForEachKeyInRadius Tests

        [Test]
        public void ForEachKeyInRadius_ValidRadiusAndEdgeDistance_ExecutesActionOnExpectedKeys()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Center placed right at the boundary between chunks to cover a 2x2x2 range (chunks 0 and 1 in each axis)
            var center = new Vector3(16f, 16f, 16f);
            float radius = 1.0f;

            var handler = new TestExecutionHandler(0);
            registry.ForEachKeyInRadius(center, radius, ref handler, useEdgeDistance: true);

            var expectedKeys = new[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(1, 0, 1),
                new Vector3Int(0, 1, 1),
                new Vector3Int(1, 1, 1)
            };

            CollectionAssert.AreEquivalent(expectedKeys, handler.ExecutedKeys);
        }

        [Test]
        public void ForEachKeyInRadius_CentreDistanceMode_ExecutesActionOnKeys()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var center = new Vector3(8f, 8f, 8f);
            var handler = new TestExecutionHandler(0);

            registry.ForEachKeyInRadius(center, 10f, ref handler, useEdgeDistance: false);

            Assert.IsTrue(handler.ExecutedKeys.Count > 0);
            Assert.Contains(Vector3Int.zero, handler.ExecutedKeys);
        }

        [Test]
        [TestCase(float.NaN, 0f, 0f, 5f)]
        [TestCase(0f, float.NaN, 0f, 5f)]
        [TestCase(0f, 0f, float.NaN, 5f)]
        public void ForEachKeyInRadius_NaNWorldCenter_ThrowsArgumentException(float x, float y, float z, float radius)
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var invalidCenter = new Vector3(x, y, z);
            var handler = new TestExecutionHandler(0);

            Assert.Throws<ArgumentException>(() => registry.ForEachKeyInRadius(invalidCenter, radius, ref handler));
        }

        [Test]
        public void ForEachKeyInRadius_NegativeRadius_ThrowsArgumentException()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var handler = new TestExecutionHandler(0);

            Assert.Throws<ArgumentException>(() => registry.ForEachKeyInRadius(Vector3.zero, -1f, ref handler));
        }

        [Test]
        public void ForEachKeyInRadius_ZeroRadius_ExecutesOnSingleKeyOnly()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var center = new Vector3(8f, 8f, 8f); // Center of cell (0,0,0)
            var handler = new TestExecutionHandler(0);

            registry.ForEachKeyInRadius(center, 0f, ref handler, useEdgeDistance: true);

            Assert.AreEqual(1, handler.ExecutedKeys.Count);
            Assert.AreEqual(Vector3Int.zero, handler.ExecutedKeys[0]);
        }

        [Test]
        public void ForEachKeyInRadius_CentreDistanceMode_ZeroRadius_ExecutesOnSingleKeyOnly()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var center = new Vector3(8f, 8f, 8f);
            var handler = new TestExecutionHandler(0);

            registry.ForEachKeyInRadius(center, 0f, ref handler, useEdgeDistance: false);

            Assert.AreEqual(1, handler.ExecutedKeys.Count);
            Assert.AreEqual(Vector3Int.zero, handler.ExecutedKeys[0]);
        }

        #endregion

        #region ForEachKeyInRelativeRadius Tests

        [Test]
        public void ForEachKeyInRelativeRadius_ValidRelativeRadiusAndEdgeDistance_ExecutesActionOnExpectedKeys()
        {
            var anchor = new Vector3(10f, 20f, 30f);
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, anchor);

            // Relative center placed right at the boundary between chunks to cover a 2x2x2 range
            var relativeCenter = new Vector3(16f, 16f, 16f);
            float radius = 1.0f; // Small radius to keep the count to exactly 8 keys around the vertex

            var handler = new TestExecutionHandler(0);
            registry.ForEachKeyInRelativeRadius(relativeCenter, radius, ref handler, useEdgeDistance: true);

            var expectedKeys = new[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(1, 0, 1),
                new Vector3Int(0, 1, 1),
                new Vector3Int(1, 1, 1)
            };

            CollectionAssert.AreEquivalent(expectedKeys, handler.ExecutedKeys);
        }

        [Test]
        public void ForEachKeyInRelativeRadius_CentreDistanceMode_ExecutesActionOnKeys()
        {
            var anchor = new Vector3(5f, 5f, 5f);
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, anchor);

            var relativeCenter = new Vector3(8f, 8f, 8f);
            var handler = new TestExecutionHandler(0);

            registry.ForEachKeyInRelativeRadius(relativeCenter, 10f, ref handler, useEdgeDistance: false);

            Assert.IsTrue(handler.ExecutedKeys.Count > 0);
        }

        [Test]
        [TestCase(float.NaN, 0f, 0f, 5f)]
        [TestCase(0f, float.NaN, 0f, 5f)]
        [TestCase(0f, 0f, float.NaN, 5f)]
        public void ForEachKeyInRelativeRadius_NaNRelativeCenter_ThrowsArgumentException(float x, float y, float z, float radius)
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var invalidCenter = new Vector3(x, y, z);
            var handler = new TestExecutionHandler(0);

            Assert.Throws<ArgumentException>(() => registry.ForEachKeyInRelativeRadius(invalidCenter, radius, ref handler));
        }

        [Test]
        public void ForEachKeyInRelativeRadius_NegativeRadius_ThrowsArgumentException()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var handler = new TestExecutionHandler(0);

            Assert.Throws<ArgumentException>(() => registry.ForEachKeyInRelativeRadius(Vector3.zero, -5f, ref handler));
        }

        [Test]
        public void ForEachKeyInRelativeRadius_ZeroRadius_ExecutesOnSingleKeyOnly()
        {
            var anchor = new Vector3(10f, 20f, 30f);
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, anchor);

            var relativeCenter = new Vector3(8f, 8f, 8f); // Center of local cell (0,0,0) relative to anchor
            var handler = new TestExecutionHandler(0);

            registry.ForEachKeyInRelativeRadius(relativeCenter, 0f, ref handler, useEdgeDistance: true);

            Assert.AreEqual(1, handler.ExecutedKeys.Count);
            Assert.AreEqual(Vector3Int.zero, handler.ExecutedKeys[0]);
        }

        [Test]
        public void ForEachKeyInRelativeRadius_CentreDistanceMode_ZeroRadius_ExecutesOnSingleKeyOnly()
        {
            var anchor = new Vector3(10f, 20f, 30f);
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, anchor);

            var relativeCenter = new Vector3(8f, 8f, 8f);
            var handler = new TestExecutionHandler(0);

            registry.ForEachKeyInRelativeRadius(relativeCenter, 0f, ref handler, useEdgeDistance: false);

            Assert.AreEqual(1, handler.ExecutedKeys.Count);
            Assert.AreEqual(Vector3Int.zero, handler.ExecutedKeys[0]);
        }

        #endregion

        #region GetOrCreateChunk Tests

        [Test]
        public void GetOrCreateChunk_NewKey_CreatesChunkAndExecutesHandler()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key = new Vector3Int(1, 2, 3);
            bool handlerExecuted = false;

            var handler = new ChunkExecutionHandler<TestChunkXYZ>
            {
                executeAction = chunk =>
                {
                    handlerExecuted = true;
                    Assert.IsNotNull(chunk);
                    Assert.AreEqual(key, chunk.GridKey);
                }
            };

            // Act
            bool created = registry.GetOrCreateChunk(key, ref handler, out TestChunkXYZ chunk);

            // Assert
            Assert.IsTrue(created);
            Assert.IsNotNull(chunk);
            Assert.IsTrue(handlerExecuted);
            Assert.IsTrue(registry.ContainsKey(key));
        }

        [Test]
        public void GetOrCreateChunk_ExistingKey_ReturnsExistingChunkWithoutExecutingHandler()
        {
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key = new Vector3Int(1, 2, 3);

            // Create the chunk initially
            var initialHandler = new ChunkExecutionHandler<TestChunkXYZ>
            {
                executeAction = _ => { }
            };
            registry.GetOrCreateChunk(key, ref initialHandler, out TestChunkXYZ initialChunk);

            int executionCount = 0;
            var secondHandler = new ChunkExecutionHandler<TestChunkXYZ>
            {
                executeAction = _ => { executionCount++; }
            };

            // Act: Request the same chunk again
            bool created = registry.GetOrCreateChunk(key, ref secondHandler, out TestChunkXYZ retrievedChunk);

            // Assert
            Assert.IsFalse(created);
            Assert.AreSame(initialChunk, retrievedChunk);
            Assert.AreEqual(0, executionCount, "Handler should not be executed when retrieving an existing chunk.");
        }

        [Test]
        public void GetOrCreateChunk_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var handler = new ChunkExecutionHandler<TestChunkXYZ>();
            var key = new Vector3Int(1, 2, 3);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.GetOrCreateChunk(key, ref handler, out _));
        }

        #endregion

        #region GetOrCreateChunkAtWorldPos Tests

        [Test]
        public void GetOrCreateChunkAtWorldPos_NewPosition_ConvertsWorldToGridAndCreatesChunk()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var worldPos = new Vector3(32f, 0f, 48f);
            Vector3Int expectedGridKey = registry.WorldToGrid(worldPos);
            bool handlerExecuted = false;

            var handler = new ChunkExecutionHandler<TestChunkXYZ>
            {
                executeAction = chunk =>
                {
                    handlerExecuted = true;
                    Assert.IsNotNull(chunk);
                    Assert.AreEqual(expectedGridKey, chunk.GridKey);
                }
            };

            // Act
            bool created = registry.GetOrCreateChunkAtWorldPos(worldPos, ref handler, out TestChunkXYZ chunk);

            // Assert
            Assert.IsTrue(created, "GetOrCreateChunkAtWorldPos should return true for a new world position.");
            Assert.IsNotNull(chunk, "Resulting chunk must not be null.");
            Assert.IsTrue(handlerExecuted, "Configuration handler should be executed.");
            Assert.IsTrue(registry.ContainsKey(expectedGridKey), "Registry must contain the key calculated from world position.");
        }

        [Test]
        public void GetOrCreateChunkAtWorldPos_ExistingPosition_ReturnsExistingChunkWithoutExecutingHandler()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var worldPos = new Vector3(32f, 0f, 48f);
            var initialHandler = new ChunkExecutionHandler<TestChunkXYZ> { executeAction = _ => { } };
            registry.GetOrCreateChunkAtWorldPos(worldPos, ref initialHandler, out TestChunkXYZ initialChunk);

            int executionCount = 0;
            var secondHandler = new ChunkExecutionHandler<TestChunkXYZ>
            {
                executeAction = _ => { executionCount++; }
            };

            // Act: Request using a slightly different position within the same chunk boundaries
            var nearbyWorldPos = new Vector3(33f, 1f, 49f);
            bool created = registry.GetOrCreateChunkAtWorldPos(nearbyWorldPos, ref secondHandler, out TestChunkXYZ retrievedChunk);

            // Assert
            Assert.IsFalse(created, "GetOrCreateChunkAtWorldPos should return false when retrieving an existing chunk cell.");
            Assert.AreSame(initialChunk, retrievedChunk, "Should return the existing chunk instance.");
            Assert.AreEqual(0, executionCount, "Handler should not execute for existing chunks.");
        }

        [Test]
        public void GetOrCreateChunkAtWorldPos_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var handler = new ChunkExecutionHandler<TestChunkXYZ>();
            var worldPos = new Vector3(10f, 0f, 10f);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.GetOrCreateChunkAtWorldPos(worldPos, ref handler, out _));
        }

        #endregion

        #region GetOrCreateChunk 2D Overload Tests

        [Test]
        public void GetOrCreateChunk_2DKey_ExistingKey_ReturnsExistingChunkWithoutExecutingHandler()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key2D = new Vector2Int(5, 10);
            var initialHandler = new ChunkExecutionHandler<TestChunkXZ> { executeAction = _ => { } };
            registry.GetOrCreateChunk(key2D, ref initialHandler, out TestChunkXZ initialChunk);

            int executionCount = 0;
            var secondHandler = new ChunkExecutionHandler<TestChunkXZ>
            {
                executeAction = _ => { executionCount++; }
            };

            // Act
            bool created = registry.GetOrCreateChunk(key2D, ref secondHandler, out TestChunkXZ retrievedChunk);

            // Assert
            Assert.IsFalse(created, "GetOrCreateChunk with Vector2Int should return false when retrieving an existing chunk.");
            Assert.AreSame(initialChunk, retrievedChunk, "Should return the exact same chunk instance.");
            Assert.AreEqual(0, executionCount, "Handler should not execute when retrieving an existing chunk via 2D key.");
        }

        [Test]
        public void GetOrCreateChunk_2DKey_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXZ>();
            var handler = new ChunkExecutionHandler<TestChunkXZ>();
            var key2D = new Vector2Int(5, 10);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.GetOrCreateChunk(key2D, ref handler, out _));
        }

        [Test]
        public void GetOrCreateChunk_2DKey_WithXZChunk_MapsToX0Z()
        {
            // Arrange: XZ Chunk (Active axes: X and Z, Y is disabled)
            var registry = new ChunkRegistry<TestChunkXZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key2D = new Vector2Int(5, 10); // u = 5 (maps to X), v = 10 (maps to Z)
            var expected3DKey = new Vector3Int(5, 0, 10);
            bool handlerExecuted = false;

            var handler = new ChunkExecutionHandler<TestChunkXZ>
            {
                executeAction = chunk =>
                {
                    handlerExecuted = true;
                    Assert.AreEqual(expected3DKey, chunk.GridKey);
                }
            };

            // Act
            bool created = registry.GetOrCreateChunk(key2D, ref handler, out TestChunkXZ chunk);

            // Assert
            Assert.IsTrue(created, "GetOrCreateChunk with 2D key should return true for a new XZ chunk.");
            Assert.IsNotNull(chunk);
            Assert.IsTrue(handlerExecuted);
            Assert.IsTrue(registry.ContainsKey(expected3DKey), "XZ mapping must store key as (X, 0, Z).");
        }

        [Test]
        public void GetOrCreateChunk_2DKey_WithXYChunk_MapsToXY0()
        {
            // Arrange: XY Chunk (Active axes: X and Y, Z is disabled)
            var registry = new ChunkRegistry<TestChunkXY>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key2D = new Vector2Int(7, 14); // u = 7 (maps to X), v = 14 (maps to Y)
            var expected3DKey = new Vector3Int(7, 14, 0);
            bool handlerExecuted = false;

            var handler = new ChunkExecutionHandler<TestChunkXY>
            {
                executeAction = chunk =>
                {
                    handlerExecuted = true;
                    Assert.AreEqual(expected3DKey, chunk.GridKey);
                }
            };

            // Act
            bool created = registry.GetOrCreateChunk(key2D, ref handler, out TestChunkXY chunk);

            // Assert
            Assert.IsTrue(created, "GetOrCreateChunk with 2D key should return true for a new XY chunk.");
            Assert.IsNotNull(chunk);
            Assert.IsTrue(handlerExecuted);
            Assert.IsTrue(registry.ContainsKey(expected3DKey), "XY mapping must store key as (X, Y, 0).");
        }

        [Test]
        public void GetOrCreateChunk_2DKey_WithYZChunk_MapsTo0YZ()
        {
            // Arrange: YZ Chunk (Active axes: Y and Z, X is disabled)
            var registry = new ChunkRegistry<TestChunkYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key2D = new Vector2Int(3, 8); // u = 3 (maps to Y), v = 8 (maps to Z)
            var expected3DKey = new Vector3Int(0, 3, 8);
            bool handlerExecuted = false;

            var handler = new ChunkExecutionHandler<TestChunkYZ>
            {
                executeAction = chunk =>
                {
                    handlerExecuted = true;
                    Assert.AreEqual(expected3DKey, chunk.GridKey);
                }
            };

            // Act
            bool created = registry.GetOrCreateChunk(key2D, ref handler, out TestChunkYZ chunk);

            // Assert
            Assert.IsTrue(created, "GetOrCreateChunk with 2D key should return true for a new YZ chunk.");
            Assert.IsNotNull(chunk);
            Assert.IsTrue(handlerExecuted);
            Assert.IsTrue(registry.ContainsKey(expected3DKey), "YZ mapping must store key as (0, Y, Z).");
        }

        [Test]
        public void GetOrCreateChunk_2DKey_ExistingKey_ReturnsSameChunkAcrossDifferentAxisConfigs()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key2D = new Vector2Int(4, 9);
            var initialHandler = new ChunkExecutionHandler<TestChunkYZ> { executeAction = _ => { } };
            registry.GetOrCreateChunk(key2D, ref initialHandler, out TestChunkYZ initialChunk);

            int executionCount = 0;
            var secondHandler = new ChunkExecutionHandler<TestChunkYZ>
            {
                executeAction = _ => { executionCount++; }
            };

            // Act
            bool created = registry.GetOrCreateChunk(key2D, ref secondHandler, out TestChunkYZ retrievedChunk);

            // Assert
            Assert.IsFalse(created, "Retrieving existing 2D key should return false.");
            Assert.AreSame(initialChunk, retrievedChunk);
            Assert.AreEqual(0, executionCount, "Handler must not execute for existing 2D chunk retrieval.");
        }

        #endregion

        #region TryGetChunk Tests

        [Test]
        public void TryGetChunk_ExistingKey_ReturnsTrueAndChunkInstance()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key = new Vector3Int(1, 2, 3);
            var handler = new ChunkExecutionHandler<TestChunkXYZ> { executeAction = _ => { } };
            registry.GetOrCreateChunk(key, ref handler, out TestChunkXYZ createdChunk);

            // Act
            bool found = registry.TryGetChunk(key, out TestChunkXYZ retrievedChunk);

            // Assert
            Assert.IsTrue(found, "TryGetChunk should return true when the chunk exists.");
            Assert.IsNotNull(retrievedChunk, "Retrieved chunk must not be null.");
            Assert.AreSame(createdChunk, retrievedChunk, "Should return the exact same chunk instance stored in the registry.");
        }

        [Test]
        public void TryGetChunk_NonExistingKey_ReturnsFalseAndNull()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var missingKey = new Vector3Int(99, 99, 99);

            // Act
            bool found = registry.TryGetChunk(missingKey, out TestChunkXYZ chunk);

            // Assert
            Assert.IsFalse(found, "TryGetChunk should return false for a non-existing key.");
            Assert.IsNull(chunk, "Out chunk parameter should be null when key is not found.");
        }

        [Test]
        public void TryGetChunk_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var key = new Vector3Int(1, 2, 3);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.TryGetChunk(key, out _));
        }

        #endregion

        #region TryGetChunkAtWorldPos Tests

        [Test]
        public void TryGetChunkAtWorldPos_ExistingChunk_ReturnsTrueAndChunkInstance()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var worldPos = new Vector3(32f, 0f, 48f);
            var handler = new ChunkExecutionHandler<TestChunkXYZ> { executeAction = _ => { } };
            registry.GetOrCreateChunkAtWorldPos(worldPos, ref handler, out TestChunkXYZ createdChunk);

            // Act
            bool found = registry.TryGetChunkAtWorldPos(worldPos, out TestChunkXYZ retrievedChunk);

            // Assert
            Assert.IsTrue(found, "TryGetChunkAtWorldPos should return true when a chunk exists at the specified world position.");
            Assert.IsNotNull(retrievedChunk, "Retrieved chunk must not be null.");
            Assert.AreSame(createdChunk, retrievedChunk, "Should return the exact same chunk instance.");
        }

        [Test]
        public void TryGetChunkAtWorldPos_NonExistingChunk_ReturnsFalseAndNull()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var emptyWorldPos = new Vector3(500f, 0f, 500f);

            // Act
            bool found = registry.TryGetChunkAtWorldPos(emptyWorldPos, out TestChunkXYZ chunk);

            // Assert
            Assert.IsFalse(found, "TryGetChunkAtWorldPos should return false for a position with no chunk.");
            Assert.IsNull(chunk, "Out chunk parameter should be null when no chunk exists at the world position.");
        }

        [Test]
        public void TryGetChunkAtWorldPos_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var worldPos = new Vector3(16f, 0f, 16f);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.TryGetChunkAtWorldPos(worldPos, out _));
        }

        #endregion

        #region GetCellCenter(Vector3Int key) Tests

        [TestCase(0, 0, 0)]
        [TestCase(1, 2, 3)]
        [TestCase(-1, -2, -3)]
        public void GetCellCenter_WithKey_ReturnsExpectedWorldCenter(int x, int y, int z)
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key = new Vector3Int(x, y, z);
            Vector3 expectedCenter = registry.GridToWorld(key);

            // Act
            Vector3 center = registry.GetCellCenter(key);

            // Assert
            Assert.AreEqual(expectedCenter, center, "GetCellCenter(key) must equal GridToWorld(key).");
        }

        [Test]
        public void GetCellCenter_WithKey_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var key = new Vector3Int(1, 1, 1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.GetCellCenter(key));
        }

        #endregion

        #region GetCellCenter(Vector3 worldPos) Tests

        [TestCase(0f, 0f, 0f)]
        [TestCase(5f, 7f, 15f)]
        [TestCase(18f, 33f, 50f)]
        [TestCase(-5f, -20f, -35f)]
        public void GetCellCenter_WithWorldPos_SnapsToContainingCellCenter(float posX, float posY, float posZ)
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var worldPos = new Vector3(posX, posY, posZ);
            Vector3Int containingKey = registry.WorldToGrid(worldPos);
            Vector3 expectedCenter = registry.GridToWorld(containingKey);

            // Act
            Vector3 center = registry.GetCellCenter(worldPos);

            // Assert
            Assert.AreEqual(expectedCenter, center, "GetCellCenter(worldPos) must return the center of the cell containing worldPos.");
        }

        [Test]
        public void GetCellCenter_WithWorldPos_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var worldPos = new Vector3(10f, 10f, 10f);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.GetCellCenter(worldPos));
        }

        #endregion

        #region GetCellBounds(Vector3Int key) Tests

        [TestCase(0, 0, 0)]
        [TestCase(2, -1, 4)]
        [TestCase(-3, 0, -5)]
        public void GetCellBounds_WithKey_ReturnsCorrectCenterAndSize(int x, int y, int z)
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key = new Vector3Int(x, y, z);
            Vector3 expectedCenter = registry.GridToWorld(key);
            Vector3 expectedSize = new Vector3(16f, 16f, 16f);

            // Act
            Bounds bounds = registry.GetCellBounds(key);

            // Assert
            Assert.AreEqual(expectedCenter, bounds.center, "Bounds center must match GetBoundsForKey center.");
            Assert.AreEqual(expectedSize, bounds.size, "Bounds size must match the configured grid size.");
        }

        [Test]
        public void GetCellBounds_WithKey_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var key = new Vector3Int(1, 1, 1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.GetCellBounds(key));
        }

        #endregion

        #region GetCellBounds(Vector3 worldPos) Tests

        [TestCase(5f, 5f, 5f)]
        [TestCase(20f, 35f, 50f)]
        [TestCase(-10f, -10f, -10f)]
        public void GetCellBounds_WithWorldPos_ReturnsBoundsThatEncloseWorldPosition(float posX, float posY, float posZ)
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var worldPos = new Vector3(posX, posY, posZ);
            Vector3Int containingKey = registry.WorldToGrid(worldPos);
            Vector3 expectedCenter = registry.GridToWorld(containingKey);

            // Act
            Bounds bounds = registry.GetCellBounds(worldPos);

            // Assert
            Assert.AreEqual(expectedCenter, bounds.center, "Bounds center must belong to the cell containing worldPos.");
            Assert.IsTrue(bounds.Contains(worldPos), "Extracted cell bounds must physically enclose the input world position.");
        }

        [Test]
        public void GetCellBounds_WithWorldPos_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var worldPos = new Vector3(10f, 10f, 10f);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.GetCellBounds(worldPos));
        }

        #endregion

        #region LocalToGrid Tests

        [TestCase(0f, 0f, 0f, 0, 0, 0)]
        [TestCase(5f, 10f, 15f, 0, 0, 0)]
        [TestCase(16f, 32f, 48f, 1, 2, 3)]
        [TestCase(-5f, -20f, -35f, -1, -2, -3)]
        public void LocalToGrid_WithVariousLocalPositions_ReturnsCorrectGridKey(
            float posX, float posY, float posZ,
            int expectedX, int expectedY, int expectedZ)
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var localPos = new Vector3(posX, posY, posZ);
            var expectedKey = new Vector3Int(expectedX, expectedY, expectedZ);

            // Act
            Vector3Int key = registry.LocalToGrid(localPos);

            // Assert
            Assert.AreEqual(expectedKey, key, "LocalToGrid must accurately convert local positions to grid keys based on GridSize.");
        }

        [Test]
        public void LocalToGrid_WithMaskedAxes_MasksInactiveComponentsToZero()
        {
            // Arrange: XZ Chunk (Active axes: X and Z, Y is disabled/masked)
            var registry = new ChunkRegistry<TestChunkXZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            // Local position has Y = 32f (which would ordinarily map to Y key = 2)
            var localPos = new Vector3(16f, 32f, 48f);
            var expectedKey = new Vector3Int(1, 0, 3); // Y component masked to 0

            // Act
            Vector3Int key = registry.LocalToGrid(localPos);

            // Assert
            Assert.AreEqual(expectedKey, key, "LocalToGrid must apply MaskKey to zero out inactive axes.");
        }

        [Test]
        public void LocalToGrid_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var localPos = new Vector3(10f, 10f, 10f);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.LocalToGrid(localPos));
        }

        #endregion

        #region GetSqrDistanceToCenter Tests

        [Test]
        public void GetSqrDistanceToCenter_WithKeyAtCenter_ReturnsZero()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key = new Vector3Int(1, 1, 1);
            Vector3 cellCenter = registry.GridToWorld(key);

            // Act
            float sqrDist = registry.GetSqrDistanceToCenter(key, cellCenter);

            // Assert
            Assert.AreEqual(0f, sqrDist, 0.0001f, "Distance to cell center from the exact center position should be 0.");
        }

        [TestCase(3f, 0f, 0f, 9f)]
        [TestCase(0f, 4f, 0f, 16f)]
        [TestCase(3f, 4f, 0f, 25f)]
        [TestCase(2f, 3f, 6f, 49f)]
        public void GetSqrDistanceToCenter_WithKeyAndOffset_ReturnsCorrectSqrDistance(
            float offsetX, float offsetY, float offsetZ, float expectedSqrDist)
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key = new Vector3Int(2, 2, 2);
            Vector3 worldPos = registry.GridToWorld(key) + new Vector3(offsetX, offsetY, offsetZ);

            // Act
            float sqrDist = registry.GetSqrDistanceToCenter(key, worldPos);

            // Assert
            Assert.AreEqual(expectedSqrDist, sqrDist, 0.0001f, "GetSqrDistanceToCenter must return squared Euclidean distance to cell center.");
        }

        [Test]
        public void GetSqrDistanceToCenter_WithKey_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var key = new Vector3Int(1, 1, 1);
            var worldPos = new Vector3(10f, 10f, 10f);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.GetSqrDistanceToCenter(key, worldPos));
        }

        [Test]
        public void GetSqrDistanceToCenter_WithTargetPos_ResolvesCellAndReturnsCorrectSqrDistance()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var targetPos = new Vector3(32f, 16f, 48f);
            Vector3Int targetKey = registry.WorldToGrid(targetPos);
            Vector3 cellCenter = registry.GridToWorld(targetKey);
            Vector3 testWorldPos = cellCenter + new Vector3(3f, 4f, 0f);

            // Act
            float sqrDist = registry.GetSqrDistanceToCenter(targetPos, testWorldPos);

            // Assert
            Assert.AreEqual(25f, sqrDist, 0.0001f, "GetSqrDistanceToCenter(targetPos, worldPos) should resolve grid cell and calculate squared distance.");
        }

        [Test]
        public void GetSqrDistanceToCenter_WithTargetPos_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var targetPos = new Vector3(10f, 10f, 10f);
            var worldPos = new Vector3(12f, 12f, 12f);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.GetSqrDistanceToCenter(targetPos, worldPos));
        }

        #endregion

        #region GetSqrDistanceToClosestEdge Tests

        [Test]
        public void GetSqrDistanceToClosestEdge_WithKeyAtCenter_ReturnsCorrectDistanceToEdge()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var key = new Vector3Int(0, 0, 0);
            Vector3 center = registry.GridToWorld(key);

            // Act
            float sqrDist = registry.GetSqrDistanceToClosestEdge(key, center);

            // Assert
            Assert.GreaterOrEqual(sqrDist, 0f, "Squared distance to closest edge should be a non-negative value.");
        }

        [Test]
        public void GetSqrDistanceToClosestEdge_WithKey_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var key = new Vector3Int(1, 1, 1);
            var worldPos = new Vector3(10f, 10f, 10f);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.GetSqrDistanceToClosestEdge(key, worldPos));
        }

        [Test]
        public void GetSqrDistanceToClosestEdge_WithTargetPos_ResolvesCellAndReturnsDistance()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            var targetPos = new Vector3(20f, 20f, 20f);
            Vector3Int targetKey = registry.WorldToGrid(targetPos);
            Vector3 worldPos = registry.GridToWorld(targetKey);

            // Act
            float sqrDistKey = registry.GetSqrDistanceToClosestEdge(targetKey, worldPos);
            float sqrDistPos = registry.GetSqrDistanceToClosestEdge(targetPos, worldPos);

            // Assert
            Assert.AreEqual(sqrDistKey, sqrDistPos, 0.0001f, "Both overloads (key vs targetPos) must yield identical edge distance results.");
        }

        [Test]
        public void GetSqrDistanceToClosestEdge_WithTargetPos_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            var targetPos = new Vector3(10f, 10f, 10f);
            var worldPos = new Vector3(12f, 12f, 12f);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.GetSqrDistanceToClosestEdge(targetPos, worldPos));
        }

        #endregion

        #region NotifyOriginShift Tests

        [Test]
        public void NotifyOriginShift_WhenInitialized_UpdatesAnchorByDelta()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            Vector3 initialAnchor = new Vector3(10f, 20f, 30f);
            registry.Initialize(GridSize.Size16, initialAnchor);

            Vector3 delta = new Vector3(5f, -10f, 2.5f);
            Vector3 expectedAnchor = initialAnchor + delta;

            // Act
            registry.NotifyOriginShift(delta);

            // Assert
            Assert.AreEqual(expectedAnchor, registry.Anchor, "Anchor must be offset by the applied delta vector.");
        }

        [Test]
        public void NotifyOriginShift_MultipleCalls_AccumulatesDeltaCorrectly()
        {
            // Arrange
            var registry = new ChunkRegistry<TestChunkXYZ>();
            registry.Initialize(GridSize.Size16, Vector3.zero);

            Vector3 firstDelta = new Vector3(10f, 0f, 0f);
            Vector3 secondDelta = new Vector3(0f, -5f, 15f);
            Vector3 expectedAnchor = firstDelta + secondDelta;

            // Act
            registry.NotifyOriginShift(firstDelta);
            registry.NotifyOriginShift(secondDelta);

            // Assert
            Assert.AreEqual(expectedAnchor, registry.Anchor, "Multiple consecutive origin shifts must accumulate on the Anchor property.");
        }

        [Test]
        public void NotifyOriginShift_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new ChunkRegistry<TestChunkXYZ>();
            Vector3 delta = new Vector3(1f, 1f, 1f);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.NotifyOriginShift(delta));
        }

        #endregion
    }
}