using NUnit.Framework;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Environment.Spatial.Components;
using Rayforge.Core.Environment.Spatial.Rendering;
using Rayforge.Core.Environment.Spatial.Surfaces;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces.Tests
{
    [TestFixture]
    public class SurfaceRegistryTests
    {
        #region Test Env

        private SurfaceRegistry _registry;
        private ChunkRegistry<TextureChunk> _gridProvider;

        [SetUp]
        public void SetUp()
        {
            _registry = new SurfaceRegistry();
            _gridProvider = new ChunkRegistry<TextureChunk>();

            _gridProvider.Initialize(GridSize.Size16, Vector3.zero, null, "TextureChunkRegistry");
        }

        [TearDown]
        public void TearDown()
        {
            _registry?.Reset();
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void Initialize_WithValidProvider_SetsIsInitializedToTrue()
        {
            // Act
            _registry.Initialize(_gridProvider);

            // Assert
            Assert.IsTrue(_registry.IsInitialized, "Registry and sub-registries should be initialized with a valid grid provider.");
            Assert.AreEqual(0, _registry.StateCount, "Registry should start empty after initialization.");
            Assert.AreEqual(0, _registry.CellCount, "Registry should have zero active cells after initialization.");
        }

        [Test]
        public void Initialize_WithNullProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => _registry.Initialize(null));
            Assert.AreEqual("gridProvider", ex.ParamName, "Exception should reference the correct parameter name.");
        }

        [Test]
        public void Initialize_CalledMultipleTimes_ResetsPreviousState()
        {
            // Arrange
            _registry.Initialize(_gridProvider);
            var secondProvider = new ChunkRegistry<TextureChunk>();

            // Act
            _registry.Initialize(secondProvider);

            // Assert
            Assert.IsTrue(_registry.IsInitialized, "Registry should remain initialized with the new provider.");
        }

        #endregion

        #region Property Tests

        [Test]
        public void IsInitialized_BeforeInitialization_ReturnsFalse()
        {
            // Assert
            Assert.IsFalse(_registry.IsInitialized, "Registry should not be initialized by default.");
        }

        [Test]
        public void IsInitialized_AfterReset_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridProvider);
            Assert.IsTrue(_registry.IsInitialized, "Precondition: Registry must be initialized.");

            // Act
            _registry.Reset();

            // Assert
            Assert.IsFalse(_registry.IsInitialized, "Registry should report as uninitialized after Reset() is called.");
        }

        [Test]
        public void MeshRegistry_AfterInitialization_ReturnsValidInstance()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act
            var meshRegistry = _registry.MeshRegistry;

            // Assert
            Assert.IsNotNull(meshRegistry, "MeshRegistry property should return a valid instance after initialization.");
            Assert.IsInstanceOf<ISpatialRegistry<Vector3Int, MeshRenderer>>(meshRegistry, "MeshRegistry must implement ISpatialRegistry for MeshRenderers.");
        }

        [Test]
        public void TerrainRegistry_AfterInitialization_ReturnsValidInstance()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act
            var terrainRegistry = _registry.TerrainRegistry;

            // Assert
            Assert.IsNotNull(terrainRegistry, "TerrainRegistry property should return a valid instance after initialization.");
            Assert.IsInstanceOf<ISpatialRegistry<Vector3Int, Terrain>>(terrainRegistry, "TerrainRegistry must implement ISpatialRegistry for Terrains.");
        }

        #endregion

        #region Grid Structure Changed Tests

        [Test]
        public void HandleGridStructureChanged_WhenTriggeredViaProvider_DoesNotThrow()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act & Assert
            // Changing GridSize invokes OnGridStructureChanged, which triggers HandleGridStructureChanged internally.
            Assert.DoesNotThrow(() =>
            {
                _gridProvider.GridSize = GridSize.Size32;
            }, "Triggering grid structure change should execute successfully and invoke FullRemap on sub-registries.");
        }

        [Test]
        public void HandleGridStructureChanged_WhenGridResized_RemapsComponentsToCorrectNewCells()
        {
            // Arrange
            // Initial grid size is Size16
            _registry.Initialize(_gridProvider);

            // Position at (20, 0, 0). 
            // With Size16: floor(20 / 16) = 1 => Grid (1, 0, 0)
            // With Size32: floor(20 / 32) = 0 => Grid (0, 0, 0)
            Vector3 testPos = new Vector3(20f, 0f, 0f);
            GameObject testObj = new GameObject("TestObject");
            testObj.transform.position = testPos;
            testObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            testObj.AddComponent<MeshRenderer>();

            _registry.TryRegister(testObj);

            // Pre-assertion: Verify initial state
            Assert.IsTrue(_registry.IsCellActive(new Vector3Int(1, 0, 0)), "Object should initially be in cell (1,0,0) for Size16.");
            Assert.IsFalse(_registry.IsCellActive(Vector3Int.zero), "Cell (0,0,0) should initially be empty.");

            // Act
            // Change to a larger grid size (Size32) to trigger FullRemap via event propagation
            _gridProvider.GridSize = GridSize.Size32;

            // Assert
            Assert.IsFalse(_registry.IsCellActive(new Vector3Int(1, 0, 0)), "Object should have moved out of cell (1,0,0) after remap.");
            Assert.IsTrue(_registry.IsCellActive(Vector3Int.zero), "Object should have been remapped to cell (0,0,0) after GridSize changed to Size32.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(testObj);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_WhenEmpty_DoesNotThrow()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act & Assert
            Assert.DoesNotThrow(() => _registry.Clear(), "Clearing an empty registry should execute safely without throwing exceptions.");
        }

        [Test]
        public void Clear_WithRegisteredComponents_RemovesAllItemsAndResetsCounts()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            var testObj = new GameObject("TestObject");
            testObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            testObj.AddComponent<MeshRenderer>();

            bool registered = _registry.TryRegister(testObj);
            Assert.IsTrue(registered, "Precondition: Object must be successfully registered.");
            Assert.Greater(_registry.StateCount, 0, "Precondition: StateCount should be greater than zero after registration.");

            // Act
            _registry.Clear();

            // Assert
            Assert.AreEqual(0, _registry.StateCount, "StateCount should be zero after Clear() is called.");
            Assert.AreEqual(0, _registry.CellCount, "CellCount should be zero after Clear() is called.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(testObj);
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_WhenInitialized_SetsIsInitializedToFalse()
        {
            // Arrange
            _registry.Initialize(_gridProvider);
            Assert.IsTrue(_registry.IsInitialized, "Precondition: Registry must be initialized.");

            // Act
            _registry.Reset();

            // Assert
            Assert.IsFalse(_registry.IsInitialized, "Registry should no longer be initialized after Reset().");
        }

        [Test]
        public void Reset_AfterInitialization_SetsSubRegistriesToNull()
        {
            // Arrange
            _registry.Initialize(_gridProvider);
            Assert.IsNotNull(_registry.MeshRegistry, "Precondition: MeshRegistry should not be null after initialization.");

            // Act
            _registry.Reset();

            // Assert
            Assert.IsNull(_registry.MeshRegistry, "MeshRegistry should be null after Reset().");
            Assert.IsNull(_registry.TerrainRegistry, "TerrainRegistry should be null after Reset().");
        }

        [Test]
        public void Reset_UnsubscribesFromGridEvents_DoesNotThrowOnGridChange()
        {
            // Arrange
            _registry.Initialize(_gridProvider);
            _registry.Reset();

            // Act & Assert
            // Changing GridSize after reset should not throw because the event listener was unhooked 
            // and sub-registries were cleared/nullified.
            Assert.DoesNotThrow(() =>
            {
                _gridProvider.GridSize = GridSize.Size32;
            }, "Changing grid size after registry reset should execute safely without triggering remappings.");
        }

        #endregion

        #region ClearDirtyCells Tests

        [Test]
        public void ClearDirtyCells_BeforeInitialization_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _registry.ClearDirtyCells(), "Clearing dirty cells before initialization should execute safely.");
        }

        [Test]
        public void ClearDirtyCells_AfterInitialization_DoesNotThrow()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act & Assert
            Assert.DoesNotThrow(() => _registry.ClearDirtyCells(), "Clearing dirty cells after initialization should execute safely.");
        }

        [Test]
        public void ClearDirtyCells_WithRegisteredComponents_ExecutesSuccessfully()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            var testObj = new GameObject("DirtyTestObject");
            testObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            testObj.AddComponent<MeshRenderer>();
            _registry.TryRegister(testObj);

            // Act & Assert
            Assert.DoesNotThrow(() => _registry.ClearDirtyCells(), "Clearing dirty cells with active components should execute without errors.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(testObj);
        }

        #endregion

        #region TryRegister Tests

        [Test]
        public void TryRegister_WithNullObject_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act
            bool result = _registry.TryRegister(null);

            // Assert
            Assert.IsFalse(result, "Registering a null GameObject should return false.");
            Assert.AreEqual(0, _registry.StateCount, "StateCount should remain zero.");
        }

        [Test]
        public void TryRegister_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var testObj = new GameObject("TestObject");
            testObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            testObj.AddComponent<MeshRenderer>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.TryRegister(testObj), "Registering when the registry is not initialized should throw InvalidOperationException.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(testObj);
        }

        [Test]
        public void TryRegister_WithNoValidComponents_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridProvider);
            var emptyObj = new GameObject("EmptyObject");

            // Act
            bool result = _registry.TryRegister(emptyObj);

            // Assert
            Assert.IsFalse(result, "Registering a GameObject without supported spatial components should return false.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(emptyObj);
        }

        [Test]
        public void TryRegister_WithMeshRendererWithoutMeshFilterOrSharedMesh_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // GameObject has MeshRenderer but no MeshFilter/sharedMesh
            var invalidMeshObj = new GameObject("InvalidMeshObj");
            invalidMeshObj.AddComponent<MeshRenderer>();

            // Act
            bool result = _registry.TryRegister(invalidMeshObj);

            // Assert
            Assert.IsFalse(result, "Registering a MeshRenderer without a valid shared mesh should return false.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(invalidMeshObj);
        }

        [Test]
        public void TryRegister_WithValidMeshRendererAndFilter_ReturnsTrueAndRegisters()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            var validMeshObj = new GameObject("ValidMeshObj");
            validMeshObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            validMeshObj.AddComponent<MeshRenderer>();

            // Act
            bool result = _registry.TryRegister(validMeshObj);

            // Assert
            Assert.IsTrue(result, "Registering a GameObject with a valid MeshRenderer and Mesh should return true.");
            Assert.AreEqual(1, _registry.StateCount, "StateCount should increase after successful registration.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(validMeshObj);
        }

        [Test]
        public void TryRegister_WithValidTerrain_ReturnsTrueAndRegisters()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            var terrainData = new TerrainData();
            var validTerrainObj = new GameObject("ValidTerrainObj");
            var terrain = validTerrainObj.AddComponent<Terrain>();
            terrain.terrainData = terrainData;

            // Act
            bool result = _registry.TryRegister(validTerrainObj);

            // Assert
            Assert.IsTrue(result, "Registering a GameObject with a valid Terrain should return true.");
            Assert.AreEqual(1, _registry.StateCount, "StateCount should increase after successful registration.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(terrainData);
            UnityEngine.Object.DestroyImmediate(validTerrainObj);
        }

        #endregion

        #region Unregister Tests

        [Test]
        public void Unregister_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.Unregister(999), "Unregistering when the registry is not initialized should throw InvalidOperationException.");
        }

        [Test]
        public void Unregister_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act
            bool result = _registry.Unregister(999999);

            // Assert
            Assert.IsFalse(result, "Unregistering a non-existent ID should return false.");
        }

        [Test]
        public void Unregister_WithRegisteredMeshObject_ReturnsTrueAndRemoves()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            var testObj = new GameObject("MeshUnregisterObj");
            testObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            testObj.AddComponent<MeshRenderer>();

            int id = testObj.GetInstanceID();
            _registry.TryRegister(testObj);
            Assert.AreEqual(1, _registry.StateCount, "Precondition: StateCount should be 1 after registration.");

            // Act
            bool result = _registry.Unregister(id);

            // Assert
            Assert.IsTrue(result, "Unregistering an existing registered object should return true.");
            Assert.AreEqual(0, _registry.StateCount, "StateCount should be 0 after unregistration.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(testObj);
        }

        [Test]
        public void Unregister_WithRegisteredTerrainObject_ReturnsTrueAndRemoves()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            var terrainData = new TerrainData();
            var testObj = new GameObject("TerrainUnregisterObj");
            var terrain = testObj.AddComponent<Terrain>();
            terrain.terrainData = terrainData;

            int id = testObj.GetInstanceID();
            _registry.TryRegister(testObj);
            Assert.AreEqual(1, _registry.StateCount, "Precondition: StateCount should be 1 after registration.");

            // Act
            bool result = _registry.Unregister(id);

            // Assert
            Assert.IsTrue(result, "Unregistering an existing registered terrain should return true.");
            Assert.AreEqual(0, _registry.StateCount, "StateCount should be 0 after unregistration.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(terrainData);
            UnityEngine.Object.DestroyImmediate(testObj);
        }

        #endregion

        #region IsCellActive Tests

        [Test]
        public void IsCellActive_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.IsCellActive(Vector3Int.zero), "Calling IsCellActive when not initialized should throw InvalidOperationException.");
        }

        [Test]
        public void IsCellActive_WhenCellIsEmpty_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act & Assert
            Assert.IsFalse(_registry.IsCellActive(new Vector3Int(99, 99, 99)), "IsCellActive should return false for empty or non-existent cells.");
        }

        [Test]
        public void IsCellActive_WithRegisteredMeshObject_ReturnsTrueForItsCell()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            var testObj = new GameObject("MeshCellTestObj");
            testObj.transform.position = Vector3.zero;
            testObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            testObj.AddComponent<MeshRenderer>();

            _registry.TryRegister(testObj);

            Vector3Int expectedCell = _gridProvider.WorldToGrid(testObj.transform.position);

            // Act & Assert
            Assert.IsTrue(_registry.IsCellActive(expectedCell), "IsCellActive should return true for the cell containing the registered mesh object.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(testObj);
        }

        [Test]
        public void IsCellActive_WithRegisteredTerrainObject_ReturnsTrueForItsCell()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            var terrainData = new TerrainData();
            var testObj = new GameObject("TerrainCellTestObj");
            testObj.transform.position = Vector3.zero;
            var terrain = testObj.AddComponent<Terrain>();
            terrain.terrainData = terrainData;

            _registry.TryRegister(testObj);

            Vector3Int expectedCell = _gridProvider.WorldToGrid(testObj.transform.position);

            // Act & Assert
            Assert.IsTrue(_registry.IsCellActive(expectedCell), "IsCellActive should return true for the cell containing the registered terrain object.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(terrainData);
            UnityEngine.Object.DestroyImmediate(testObj);
        }

        #endregion

        #region ForEachCell Tests

        [Test]
        public void ForEachCell_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var handler = new TestCellHandler();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.ForEachCell(ref handler), "Calling ForEachCell when not initialized should throw InvalidOperationException.");
        }

        [Test]
        public void ForEachCell_WhenEmpty_DoesNotExecuteHandler()
        {
            // Arrange
            _registry.Initialize(_gridProvider);
            var handler = new TestCellHandler();

            // Act
            _registry.ForEachCell(ref handler);

            // Assert
            Assert.AreEqual(0, handler.CallCount, "Handler should not be invoked when the registry is empty.");
        }

        [Test]
        public void ForEachCell_WithRegisteredObjects_ExecutesHandlerOnActiveCells()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Register a mesh object
            var meshObj = new GameObject("MeshObj");
            meshObj.transform.position = new Vector3(0f, 0f, 0f);
            meshObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            meshObj.AddComponent<MeshRenderer>();
            _registry.TryRegister(meshObj);

            // Register a terrain object with a small size so it fits inside a single cell (Size16)
            var terrainData = new TerrainData();
            terrainData.size = new Vector3(10f, 10f, 10f);

            var terrainObj = new GameObject("TerrainObj");
            terrainObj.transform.position = new Vector3(64f, 0f, 0f);
            var terrain = terrainObj.AddComponent<Terrain>();
            terrain.terrainData = terrainData;
            _registry.TryRegister(terrainObj);

            var handler = new TestCellHandler();

            // Act
            _registry.ForEachCell(ref handler);

            // Assert
            Assert.AreEqual(2, handler.CallCount, "Handler should be executed for each active cell containing registered components.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(meshObj);
            UnityEngine.Object.DestroyImmediate(terrainData);
            UnityEngine.Object.DestroyImmediate(terrainObj);
        }

        private struct TestCellHandler : IExecutionHandler<Vector3Int>
        {
            public int CallCount { get; private set; }
            public List<Vector3Int> VisitedCells { get; private set; }

            public void Execute(Vector3Int cell)
            {
                CallCount++;
                VisitedCells ??= new List<Vector3Int>();
                VisitedCells.Add(cell);
            }
        }

        #endregion

        #region GetCellIterator Tests

        [Test]
        public void GetCellIterator_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.GetCellIterator(), "Calling GetCellIterator when not initialized should throw InvalidOperationException.");
        }

        [Test]
        public void GetCellIterator_WhenEmpty_ReturnsEmptyIterator()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act & Assert
            using (var iterator = _registry.GetCellIterator())
            {
                Assert.IsNotNull(iterator, "GetCellIterator should return a non-null iterator instance.");
                Assert.IsFalse(iterator.MoveNext(), "An empty registry iterator should not move to any cells.");
            }
        }

        [Test]
        public void GetCellIterator_WithRegisteredObjects_IteratesAllActiveCells()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Register a mesh object
            var meshObj = new GameObject("MeshIteratorObj");
            meshObj.transform.position = new Vector3(0f, 0f, 0f);
            meshObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            meshObj.AddComponent<MeshRenderer>();
            _registry.TryRegister(meshObj);

            // Register a small terrain object in a different cell
            var terrainData = new TerrainData { size = new Vector3(10f, 10f, 10f) };
            var terrainObj = new GameObject("TerrainIteratorObj");
            terrainObj.transform.position = new Vector3(64f, 0f, 0f);
            var terrain = terrainObj.AddComponent<Terrain>();
            terrain.terrainData = terrainData;
            _registry.TryRegister(terrainObj);

            // Act
            var visitedCells = new List<Vector3Int>();
            using (var iterator = _registry.GetCellIterator())
            {
                while (iterator.MoveNext())
                {
                    visitedCells.Add(iterator.Current);
                }
            }

            // Assert
            Assert.AreEqual(2, visitedCells.Count, "Iterator should visit exactly two active cells.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(meshObj);
            UnityEngine.Object.DestroyImmediate(terrainData);
            UnityEngine.Object.DestroyImmediate(terrainObj);
        }

        #endregion

        #region ForEachDirtyCell Tests

        [Test]
        public void ForEachDirtyCell_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var handler = new TestCellHandler();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.ForEachDirtyCell(ref handler), "Calling ForEachDirtyCell when not initialized should throw InvalidOperationException.");
        }

        [Test]
        public void ForEachDirtyCell_WhenNoDirtyCellsExist_DoesNotExecuteHandler()
        {
            // Arrange
            _registry.Initialize(_gridProvider);
            var handler = new TestCellHandler();

            // Act
            _registry.ForEachDirtyCell(ref handler);

            // Assert
            Assert.AreEqual(0, handler.CallCount, "Handler should not be invoked when there are no dirty cells.");
        }

        [Test]
        public void ForEachDirtyCell_WithNewlyRegisteredComponents_ExecutesHandlerAndRespectsClearDirtyCells()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Registering components typically marks their containing cells as dirty
            var meshObj = new GameObject("DirtyMeshObj");
            meshObj.transform.position = Vector3.zero;
            meshObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            meshObj.AddComponent<MeshRenderer>();
            _registry.TryRegister(meshObj);

            var handlerBefore = new TestCellHandler();

            // Act - First pass: should find dirty cells from recent registration
            _registry.ForEachDirtyCell(ref handlerBefore);

            // Assert - First pass
            Assert.AreEqual(1, handlerBefore.CallCount, "Handler should execute for the dirty cell created by registration.");

            // Clear dirty status
            _registry.ClearDirtyCells();

            var handlerAfter = new TestCellHandler();

            // Act - Second pass: dirty cells should now be cleared
            _registry.ForEachDirtyCell(ref handlerAfter);

            // Assert - Second pass
            Assert.AreEqual(0, handlerAfter.CallCount, "Handler should not execute after ClearDirtyCells() has been called.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(meshObj);
        }

        #endregion

        #region GetDirtyCellIterator Tests

        [Test]
        public void GetDirtyCellIterator_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.GetDirtyCellIterator(), "Calling GetDirtyCellIterator when not initialized should throw InvalidOperationException.");
        }

        [Test]
        public void GetDirtyCellIterator_WhenNoDirtyCellsExist_ReturnsEmptyIterator()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act & Assert
            using (var iterator = _registry.GetDirtyCellIterator())
            {
                Assert.IsNotNull(iterator, "GetDirtyCellIterator should return a non-null iterator instance.");
                Assert.IsFalse(iterator.MoveNext(), "An iterator for a registry with no dirty cells should not move to any items.");
            }
        }

        [Test]
        public void GetDirtyCellIterator_WithNewlyRegisteredComponents_IteratesDirtyCellsAndRespectedClear()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Register a mesh object (marks cell dirty)
            var meshObj = new GameObject("DirtyIteratorMeshObj");
            meshObj.transform.position = new Vector3(0f, 0f, 0f);
            meshObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            meshObj.AddComponent<MeshRenderer>();
            _registry.TryRegister(meshObj);

            // Act - Collect dirty cells via iterator
            var visitedCells = new List<Vector3Int>();
            using (var iterator = _registry.GetDirtyCellIterator())
            {
                while (iterator.MoveNext())
                {
                    visitedCells.Add(iterator.Current);
                }
            }

            // Assert initial dirty collection
            Assert.AreEqual(1, visitedCells.Count, "Dirty cell iterator should find the cell marked dirty by registration.");

            // Clear dirty cells
            _registry.ClearDirtyCells();

            // Act - Collect again after clearing
            var visitedCellsAfterClear = new List<Vector3Int>();
            using (var iterator = _registry.GetDirtyCellIterator())
            {
                while (iterator.MoveNext())
                {
                    visitedCellsAfterClear.Add(iterator.Current);
                }
            }

            // Assert after clear
            Assert.AreEqual(0, visitedCellsAfterClear.Count, "Dirty cell iterator should return no cells after ClearDirtyCells() is called.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(meshObj);
        }

        #endregion

        #region StateCount Tests

        [Test]
        public void StateCount_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => { var count = _registry.StateCount; }, "Accessing StateCount when not initialized should throw InvalidOperationException.");
        }

        [Test]
        public void StateCount_WhenInitializedAndEmpty_ReturnsZero()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act & Assert
            Assert.AreEqual(0, _registry.StateCount, "StateCount should be zero when initialized with no registered objects.");
        }

        [Test]
        public void StateCount_WithRegisteredComponents_ReturnsAccurateCount()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            var meshObj = new GameObject("MeshStateObj");
            meshObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            meshObj.AddComponent<MeshRenderer>();
            _registry.TryRegister(meshObj);

            var terrainData = new TerrainData();
            var terrainObj = new GameObject("TerrainStateObj");
            var terrain = terrainObj.AddComponent<Terrain>();
            terrain.terrainData = terrainData;
            _registry.TryRegister(terrainObj);

            // Act & Assert
            Assert.AreEqual(2, _registry.StateCount, "StateCount should accurately reflect the total number of registered components across all sub-registries.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(meshObj);
            UnityEngine.Object.DestroyImmediate(terrainData);
            UnityEngine.Object.DestroyImmediate(terrainObj);
        }

        #endregion

        #region CellCount Tests

        [Test]
        public void CellCount_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => { var count = _registry.CellCount; }, "Accessing CellCount when not initialized should throw InvalidOperationException.");
        }

        [Test]
        public void CellCount_WhenInitializedAndEmpty_ReturnsZero()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act & Assert
            Assert.AreEqual(0, _registry.CellCount, "CellCount should be zero when initialized with no registered objects.");
        }

        [Test]
        public void CellCount_WithRegisteredComponents_ReturnsAccurateCount()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Register a mesh object at origin
            var meshObj = new GameObject("MeshCellCountObj");
            meshObj.transform.position = new Vector3(0f, 0f, 0f);
            meshObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            meshObj.AddComponent<MeshRenderer>();
            _registry.TryRegister(meshObj);

            // Register a terrain object in a different cell position
            var terrainData = new TerrainData { size = new Vector3(10f, 10f, 10f) };
            var terrainObj = new GameObject("TerrainCellCountObj");
            terrainObj.transform.position = new Vector3(64f, 0f, 0f);
            var terrain = terrainObj.AddComponent<Terrain>();
            terrain.terrainData = terrainData;
            _registry.TryRegister(terrainObj);

            // Act & Assert
            Assert.AreEqual(2, _registry.CellCount, "CellCount should accurately reflect the total number of active cells across sub-registries.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(meshObj);
            UnityEngine.Object.DestroyImmediate(terrainData);
            UnityEngine.Object.DestroyImmediate(terrainObj);
        }

        #endregion

        #region DirtyCellCount Tests

        [Test]
        public void DirtyCellCount_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => { var count = _registry.DirtyCellCount; }, "Accessing DirtyCellCount when not initialized should throw InvalidOperationException.");
        }

        [Test]
        public void DirtyCellCount_WhenInitializedAndEmpty_ReturnsZero()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act & Assert
            Assert.AreEqual(0, _registry.DirtyCellCount, "DirtyCellCount should be zero when initialized with no registered objects.");
        }

        [Test]
        public void DirtyCellCount_WithNewlyRegisteredComponents_ReturnsAccurateCountAndRespectedClear()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            var meshObj = new GameObject("DirtyCountMeshObj");
            meshObj.transform.position = Vector3.zero;
            meshObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            meshObj.AddComponent<MeshRenderer>();
            _registry.TryRegister(meshObj);

            // Act & Assert - Initially registered items mark cells as dirty
            Assert.Greater(_registry.DirtyCellCount, 0, "DirtyCellCount should be greater than zero after registering a component.");

            // Clear dirty cells
            _registry.ClearDirtyCells();

            Assert.AreEqual(0, _registry.DirtyCellCount, "DirtyCellCount should be zero after ClearDirtyCells() is called.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(meshObj);
        }

        #endregion

        #region GetCellStateCount Tests

        [Test]
        public void GetCellStateCount_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.GetCellStateCount(Vector3Int.zero), "Calling GetCellStateCount when not initialized should throw InvalidOperationException.");
        }

        [Test]
        public void GetCellStateCount_WhenCellIsEmpty_ReturnsZero()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            // Act & Assert
            Assert.AreEqual(0, _registry.GetCellStateCount(new Vector3Int(99, 99, 99)), "GetCellStateCount should return zero for empty or non-existent cells.");
        }

        [Test]
        public void GetCellStateCount_WithRegisteredObject_ReturnsAccurateCountForCell()
        {
            // Arrange
            _registry.Initialize(_gridProvider);

            var meshObj = new GameObject("CellStateCountMeshObj");
            meshObj.transform.position = Vector3.zero;
            meshObj.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            meshObj.AddComponent<MeshRenderer>();
            _registry.TryRegister(meshObj);

            Vector3Int targetCell = _gridProvider.WorldToGrid(meshObj.transform.position);

            // Act & Assert
            Assert.AreEqual(1, _registry.GetCellStateCount(targetCell), "GetCellStateCount should return the correct number of states for the active cell.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(meshObj);
        }

        #endregion
    }
}
