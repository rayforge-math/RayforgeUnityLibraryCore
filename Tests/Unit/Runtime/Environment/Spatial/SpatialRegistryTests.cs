using NUnit.Framework;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace Rayforge.Core.Environment.Spatial.Tests
{
    [TestFixture]
    public class SpatialRegistryTests
    {
        #region Test Env

        private class TestSpatialEntry : MonoBehaviour, ISpatialEntry, IDisposable
        {
            public bool IsDirty { get; set; } = false;
            public bool IsDisposed { get; private set; } = false;

            public void ClearDirty() => IsDirty = false;
            public void MarkDirty() => IsDirty = true;
            public void Dispose() => IsDisposed = true;
        }

        private struct TestCreateHandler : IFunctionHandler<EntryCreateData<int>, TestSpatialEntry>
        {
            public TestSpatialEntry Execute(EntryCreateData<int> data)
            {
                return data.gameObject.AddComponent<TestSpatialEntry>();
            }
        }

        private class TestSpatialRegistry : SpatialRegistry<int, TestSpatialEntry>
        {
            public bool PublicGetOrCreate<THandler>(int key, string name, Vector3 position, ref THandler onCreate, out TestSpatialEntry result)
                where THandler : struct, IFunctionHandler<EntryCreateData<int>, TestSpatialEntry>
            {
                return GetOrCreate(key, name, position, ref onCreate, out result);
            }
        }

        private struct TestFailingCreateHandler : IFunctionHandler<EntryCreateData<int>, TestSpatialEntry>
        {
            public TestSpatialEntry Execute(EntryCreateData<int> data)
            {
                return null;
            }
        }

        private struct TestKeyListActionHandler : IExecutionHandler<int>
        {
            public List<int> CollectedKeys;

            public void Execute(int key)
            {
                if (CollectedKeys == null)
                    CollectedKeys = new List<int>();

                CollectedKeys.Add(key);
            }
        }

        private struct TestEntryListActionHandler : IExecutionHandler<TestSpatialEntry>
        {
            public List<TestSpatialEntry> CollectedEntries;

            public void Execute(TestSpatialEntry entry)
            {
                if (CollectedEntries == null)
                    CollectedEntries = new List<TestSpatialEntry>();

                CollectedEntries.Add(entry);
            }
        }

        private struct TestActionHandler : IExecutionHandler<TestSpatialEntry>
        {
            public Action OnExecute;

            public void Execute(TestSpatialEntry value)
            {
                OnExecute?.Invoke();
            }
        }

        private struct TestKeyActionHandler : IExecutionHandler<int>
        {
            public Action OnExecute;

            public void Execute(int key)
            {
                OnExecute?.Invoke();
            }
        }

        private TestSpatialRegistry m_Registry;

        [SetUp]
        public void SetUp()
        {
            m_Registry = new TestSpatialRegistry();
            m_Registry.Initialize(null, "TestRegistry");
        }

        [TearDown]
        public void TearDown()
        {
            m_Registry?.Dispose();
        }

        #endregion

        #region Initialize

        [Test]
        public void Initialize_DefaultParameters_CreatesContainerAndSetsProperties()
        {
            m_Registry.Initialize();

            Assert.IsTrue(m_Registry.IsInitialized);
            Assert.IsNotNull(m_Registry.Container);
            Assert.AreEqual(0, m_Registry.Count);
            Assert.IsFalse(m_Registry.ContainerLinkedToAnchor);
            StringAssert.Contains("SpatialRegistry", m_Registry.RegistryName);
        }

        [Test]
        public void Initialize_WithParentAndCustomName_SetsHierarchyAndNameCorrectly()
        {
            var parentGameObject = new GameObject("ParentAnchor");
            Transform parentTransform = parentGameObject.transform;

            m_Registry.Initialize(parentTransform, "CustomRegistryName");

            Assert.IsTrue(m_Registry.IsInitialized);
            Assert.IsNotNull(m_Registry.Container);
            Assert.AreEqual(parentTransform, m_Registry.Container.parent);
            Assert.IsTrue(m_Registry.ContainerLinkedToAnchor);
            StringAssert.Contains("CustomRegistryName", m_Registry.RegistryName);

            // Cleanup parent
            if (Application.isPlaying) UnityEngine.Object.Destroy(parentGameObject);
            else UnityEngine.Object.DestroyImmediate(parentGameObject);
        }

        [Test]
        public void Initialize_NullOrEmptyName_FallsBackToDefaultName()
        {
            m_Registry.Initialize(null, null);

            Assert.IsTrue(m_Registry.IsInitialized);
            StringAssert.Contains("SpatialRegistry", m_Registry.RegistryName);

            m_Registry.Initialize(null, string.Empty);

            Assert.IsTrue(m_Registry.IsInitialized);
            StringAssert.Contains("SpatialRegistry", m_Registry.RegistryName);
        }

        [Test]
        public void Initialize_AlreadyInitialized_CreatesNewContainer()
        {
            // Arrange
            m_Registry.Initialize(null, "FirstInit");
            var firstContainer = m_Registry.Container;

            // Act
            m_Registry.Initialize(null, "SecondInit");
            var secondContainer = m_Registry.Container;

            // Assert
            Assert.IsTrue(m_Registry.IsInitialized);
            Assert.IsNotNull(secondContainer);
            Assert.IsFalse(ReferenceEquals(firstContainer, secondContainer), "A brand new container transform should be created.");
            Assert.AreEqual(0, m_Registry.Count);
            StringAssert.Contains("SecondInit", m_Registry.RegistryName);
        }

        [UnityTest]
        public IEnumerator Initialize_AlreadyInitialized_DestroysOldContainer()
        {
            // Arrange
            m_Registry.Initialize(null, "FirstInit");
            var firstGameObject = m_Registry.Container.gameObject;

            // Act
            m_Registry.Initialize(null, "SecondInit");

            // simulate one frame
            yield return null;

            // Assert
            bool isOldDestroyed = ReferenceEquals(firstGameObject, null) || firstGameObject == null || firstGameObject.Equals(null);
            Assert.IsTrue(isOldDestroyed, "The old container GameObject must be destroyed during reset.");
        }

        #endregion

        #region Property Tests

        [Test]
        public void Container_ReturnsCorrectTransform()
        {
            m_Registry.Initialize(null, "TestContainer");
            var container = m_Registry.Container;

            Assert.IsNotNull(container);
            Assert.IsTrue(container.name.StartsWith("TestContainer"));
        }

        [Test]
        public void ContainerLinkedToAnchor_WhenInitializedWithoutParent_ReturnsFalse()
        {
            m_Registry.Initialize(null, "UnlinkedRegistry");

            Assert.IsFalse(m_Registry.ContainerLinkedToAnchor);
        }

        [Test]
        public void ContainerLinkedToAnchor_WhenInitializedWithParent_ReturnsTrue()
        {
            var parentObj = new GameObject("ParentAnchor");
            try
            {
                m_Registry.Initialize(parentObj.transform, "LinkedRegistry");

                Assert.IsTrue(m_Registry.ContainerLinkedToAnchor);
            }
            finally
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(parentObj);
                else UnityEngine.Object.DestroyImmediate(parentObj);
            }
        }

        [Test]
        public void GlobalDirty_TracksChangesCorrectly()
        {
            var handler = new TestCreateHandler();

            // Initial state after setup should be dirty or become dirty on creation
            m_Registry.ResetDirtyFlags();
            Assert.IsFalse(m_Registry.GlobalDirty);

            m_Registry.PublicGetOrCreate(1, "Entry1", Vector3.zero, ref handler, out _);
            Assert.IsTrue(m_Registry.GlobalDirty, "Adding an entry must set GlobalDirty to true.");

            m_Registry.ResetDirtyFlags();
            Assert.IsFalse(m_Registry.GlobalDirty);

            m_Registry.RemoveAndDestroy(1);
            Assert.IsTrue(m_Registry.GlobalDirty, "Removing an entry must set GlobalDirty to true.");
        }

        [UnityTest]
        public IEnumerator IsInitialized_ReflectsInitializationState()
        {
            var freshRegistry = new TestSpatialRegistry();
            Assert.IsFalse(freshRegistry.IsInitialized);

            freshRegistry.Initialize(null, "InitTest");
            Assert.IsTrue(freshRegistry.IsInitialized);

            // Destroy container manually to test robustness
            if (Application.isPlaying) UnityEngine.Object.Destroy(freshRegistry.Container.gameObject);
            else UnityEngine.Object.DestroyImmediate(freshRegistry.Container.gameObject);

            yield return null;

            Assert.IsFalse(freshRegistry.IsInitialized);

            freshRegistry.Dispose();
        }

        [Test]
        public void Count_ReflectsNumberOfEntries()
        {
            var handler = new TestCreateHandler();
            Assert.AreEqual(0, m_Registry.Count);

            m_Registry.PublicGetOrCreate(1, "Entry1", Vector3.zero, ref handler, out _);
            Assert.AreEqual(1, m_Registry.Count);

            m_Registry.PublicGetOrCreate(2, "Entry2", Vector3.zero, ref handler, out _);
            Assert.AreEqual(2, m_Registry.Count);

            m_Registry.RemoveAndDestroy(1);
            Assert.AreEqual(1, m_Registry.Count);

            m_Registry.Clear();
            Assert.AreEqual(0, m_Registry.Count);
        }

        [Test]
        public void ContainsKey_WhenKeyExists_ReturnsTrue()
        {
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "Entry1", Vector3.zero, ref handler, out _);

            Assert.IsTrue(m_Registry.ContainsKey(1));
        }

        [Test]
        public void ContainsKey_WhenKeyDoesNotExist_ReturnsFalse()
        {
            Assert.IsFalse(m_Registry.ContainsKey(999));
        }

        [Test]
        public void TryGetValue_WhenKeyExists_ReturnsTrueAndCorrectValue()
        {
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(42, "EntryObj", Vector3.zero, ref handler, out var expectedEntry);

            bool success = m_Registry.TryGetValue(42, out var retrievedValue);

            Assert.IsTrue(success);
            Assert.AreEqual(expectedEntry, retrievedValue);
        }

        [Test]
        public void TryGetValue_WhenKeyDoesNotExist_ReturnsFalseAndDefault()
        {
            bool success = m_Registry.TryGetValue(999, out var retrievedValue);

            Assert.IsFalse(success);
            Assert.IsNull(retrievedValue);
        }

        #endregion

        #region RegistryName Property Tests

        [Test]
        public void RegistryName_Get_ReturnsCurrentName()
        {
            m_Registry.Initialize(null, "MyCustomRegistry");

            string name = m_Registry.RegistryName;

            Assert.IsNotNull(name);
            Assert.IsTrue(name.StartsWith("MyCustomRegistry_"));
        }

        [Test]
        public void RegistryName_Set_ValidName_UpdatesContainerNameAndProperty()
        {
            m_Registry.Initialize(null, "OldName");
            int expectedId = m_Registry.Container.gameObject.GetInstanceID();

            m_Registry.RegistryName = "NewRegistryName";

            string expectedFullName = $"NewRegistryName_{expectedId}";
            Assert.AreEqual(expectedFullName, m_Registry.RegistryName);
            Assert.AreEqual(expectedFullName, m_Registry.Container.name);
        }

        [Test]
        public void RegistryName_Set_NullOrWhitespace_ThrowsArgumentException()
        {
            m_Registry.Initialize(null, "ValidName");

            Assert.Throws<ArgumentException>(() => m_Registry.RegistryName = null);
            Assert.Throws<ArgumentException>(() => m_Registry.RegistryName = "   ");
        }

        [Test]
        public void RegistryName_Set_WhenContainerIsNull_ThrowsInvalidOperationException()
        {
            var freshRegistry = new TestSpatialRegistry();
            // Container is not assigned/initialized yet

            Assert.Throws<InvalidOperationException>(() => freshRegistry.RegistryName = "SomeName");
        }

        [UnityTest]
        public IEnumerator RegistryName_Set_WhenContainerIsDestroyed_ThrowsObjectDisposedException()
        {
            m_Registry.Initialize(null, "ValidName");
            var containerGo = m_Registry.Container.gameObject;

            if (Application.isPlaying) UnityEngine.Object.Destroy(containerGo);
            else UnityEngine.Object.DestroyImmediate(containerGo);
            
            yield return null;

            Assert.Throws<InvalidOperationException>(() => m_Registry.RegistryName = "NewName");
        }

        #endregion

        #region TryGetEntry Tests

        [Test]
        public void TryGetEntry_WithExistingKey_ReturnsTrueAndCorrectValue()
        {
            // Arrange
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(42, "EntryObj", Vector3.zero, ref handler, out var createdEntry);

            // Act
            bool found = m_Registry.TryGetEntry(42, out var retrievedEntry);

            // Assert
            Assert.IsTrue(found, "TryGetEntry should return true for an existing key.");
            Assert.IsNotNull(retrievedEntry, "The retrieved entry should not be null.");
            Assert.AreEqual(createdEntry, retrievedEntry, "The retrieved entry should match the created entry.");
        }

        [Test]
        public void TryGetEntry_WithNonExistingKey_ReturnsFalseAndNull()
        {
            // Act
            bool found = m_Registry.TryGetEntry(999, out var retrievedEntry);

            // Assert
            Assert.IsFalse(found, "TryGetEntry should return false for a non-existing key.");
            Assert.IsNull(retrievedEntry, "The retrieved entry should be null for a non-existing key.");
        }

        [Test]
        public void TryGetEntry_AfterRemovingKey_ReturnsFalse()
        {
            // Arrange
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(10, "EntryObj", Vector3.zero, ref handler, out _);

            // Remove the entry
            m_Registry.RemoveAndDestroy(10);

            // Act
            bool found = m_Registry.TryGetEntry(10, out var retrievedEntry);

            // Assert
            Assert.IsFalse(found, "TryGetEntry should return false after the key has been removed.");
            Assert.IsNull(retrievedEntry, "The retrieved entry should be null after removal.");
        }

        [Test]
        public void TryGetEntry_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new TestSpatialRegistry();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.TryGetEntry(42, out _));
        }

        #endregion

        #region Contains Tests

        [Test]
        public void Contains_WithExistingKey_ReturnsTrue()
        {
            // Arrange
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(42, "EntryObj", Vector3.zero, ref handler, out _);

            // Act & Assert
            Assert.IsTrue(m_Registry.Contains(42), "Contains should return true for an existing key.");
        }

        [Test]
        public void Contains_WithNonExistingKey_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(m_Registry.Contains(999), "Contains should return false for a non-existing key.");
        }

        [Test]
        public void Contains_AfterRemovingKey_ReturnsFalse()
        {
            // Arrange
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(10, "EntryObj", Vector3.zero, ref handler, out _);

            // Remove the entry
            m_Registry.RemoveAndDestroy(10);

            // Act & Assert
            Assert.IsFalse(m_Registry.Contains(10), "Contains should return false after the key has been removed.");
        }

        [Test]
        public void Contains_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new TestSpatialRegistry();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                uninitializedRegistry.Contains(42));
        }

        #endregion

        #region RemoveAndDestroy Tests

        [UnityTest]
        public IEnumerator RemoveAndDestroy_WithExistingKey_RemovesEntryAndSetsGlobalDirty()
        {
            // Arrange
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.zero, ref handler, out _);
            m_Registry.ResetDirtyFlags();

            Assert.AreEqual(1, m_Registry.Count);
            Assert.IsFalse(m_Registry.GlobalDirty);

            // Act
            m_Registry.RemoveAndDestroy(1);

            yield return null;

            // Assert
            Assert.AreEqual(0, m_Registry.Count);
            Assert.IsTrue(m_Registry.GlobalDirty, "Removing an entry must mark the registry as globally dirty.");
        }

        [UnityTest]
        public IEnumerator RemoveAndDestroy_WithExistingKey_DestroysAssociatedGameObject()
        {
            // Arrange
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.zero, ref handler, out var entry);
            var go = entry.gameObject;

            // Act
            m_Registry.RemoveAndDestroy(1);

            yield return null;

            // Assert
            bool isDestroyed = ReferenceEquals(go, null) || go == null || go.Equals(null);
            Assert.IsTrue(isDestroyed, "The GameObject associated with the removed entry must be destroyed.");
        }

        [Test]
        public void RemoveAndDestroy_WithNonExistingKey_DoesNotAffectStateOrThrow()
        {
            // Arrange
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.zero, ref handler, out _);
            m_Registry.ResetDirtyFlags();

            Assert.AreEqual(1, m_Registry.Count);

            // Act & Assert (Should not throw)
            Assert.DoesNotThrow(() => m_Registry.RemoveAndDestroy(999));

            Assert.AreEqual(1, m_Registry.Count, "Count should remain unchanged when removing a non-existing key.");
            Assert.IsFalse(m_Registry.GlobalDirty, "GlobalDirty should not change when a non-existing key is targeted.");
        }

        #endregion

        #region Clear Tests

        [UnityTest]
        public IEnumerator Clear_WithEntries_RemovesAllAndDestroysGameObjects()
        {
            // Arrange
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "Entry1", Vector3.zero, ref handler, out var entry1);
            m_Registry.PublicGetOrCreate(2, "Entry2", Vector3.zero, ref handler, out var entry2);

            var go1 = entry1.gameObject;
            var go2 = entry2.gameObject;

            Assert.AreEqual(2, m_Registry.Count);

            // Act
            m_Registry.Clear();

            yield return null;

            // Assert
            Assert.AreEqual(0, m_Registry.Count, "Clear should remove all entries from storage.");

            bool isGo1Destroyed = ReferenceEquals(go1, null) || go1 == null || go1.Equals(null);
            bool isGo2Destroyed = ReferenceEquals(go2, null) || go2 == null || go2.Equals(null);

            Assert.IsTrue(isGo1Destroyed, "The first entry's GameObject must be destroyed.");
            Assert.IsTrue(isGo2Destroyed, "The second entry's GameObject must be destroyed.");
        }

        [Test]
        public void Clear_WhenEmpty_DoesNotThrow()
        {
            // Arrange
            Assert.AreEqual(0, m_Registry.Count);

            // Act & Assert
            Assert.DoesNotThrow(() => m_Registry.Clear());
            Assert.AreEqual(0, m_Registry.Count);
        }

        #endregion

        #region GetOrCreate Tests

        [Test]
        public void GetOrCreate_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var freshRegistry = new TestSpatialRegistry();
            var handler = new TestCreateHandler();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                freshRegistry.PublicGetOrCreate(1, "TestObj", Vector3.zero, ref handler, out _));
        }

        [Test]
        public void GetOrCreate_NewKey_CreatesInstanceParentsAndReturnsTrue()
        {
            // Arrange
            m_Registry.Initialize(null, "Container");
            var handler = new TestCreateHandler();
            var targetPosition = new Vector3(1, 2, 3);

            // Act
            bool isNew = m_Registry.PublicGetOrCreate(1, "EntryObj", targetPosition, ref handler, out var result);

            // Assert
            Assert.IsTrue(isNew, "Method should return true indicating a brand new entry was created.");
            Assert.IsNotNull(result, "The resulting entry must not be null.");

            // Verify GameObject setup
            Assert.AreEqual("EntryObj", result.gameObject.name);
            Assert.AreEqual(targetPosition, result.gameObject.transform.position);
            Assert.AreEqual(m_Registry.Container, result.gameObject.transform.parent, "The new GameObject must be parented to the registry's container.");

            // Verify registry state
            Assert.IsTrue(m_Registry.GlobalDirty, "Creating a new entry must flag the registry as dirty.");
            Assert.AreEqual(1, m_Registry.Count);
        }

        [Test]
        public void GetOrCreate_ExistingValidKey_RetrievesInstanceAndReturnsFalse()
        {
            // Arrange
            m_Registry.Initialize(null, "Container");
            var handler = new TestCreateHandler();

            // Create initial entry and reset dirty state
            m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.zero, ref handler, out var firstResult);
            m_Registry.ResetDirtyFlags();

            // Act
            bool isNew = m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.one, ref handler, out var secondResult);

            // Assert
            Assert.IsFalse(isNew, "Method should return false indicating an existing entry was retrieved.");
            Assert.AreEqual(firstResult, secondResult, "Should return the exact same entry instance.");

            // Verify registry state remains unaffected
            Assert.IsFalse(m_Registry.GlobalDirty, "Retrieving an existing entry should not flag the registry as dirty.");
            Assert.AreEqual(1, m_Registry.Count);
        }

        [UnityTest]
        public IEnumerator GetOrCreate_ExistingKeyButDestroyedGameObject_OverwritesAndCreatesNew()
        {
            // Arrange
            m_Registry.Initialize(null, "Container");
            var handler = new TestCreateHandler();

            m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.zero, ref handler, out var firstResult);

            // Simulate destruction of the underlying native Unity object
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(firstResult.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(firstResult.gameObject);

            // Wait one frame to ensure Unity's lifecycle processes the destruction
            yield return null;

            m_Registry.ResetDirtyFlags();

            // Act
            // The registry should recognize the native object is dead via the IsValidEntry check
            bool isNew = m_Registry.PublicGetOrCreate(1, "NewEntryObj", Vector3.zero, ref handler, out var secondResult);

            // Assert
            Assert.IsTrue(isNew, "Method should detect the destroyed native object, clean up the stale reference, and create a new entry.");
            Assert.AreNotEqual(firstResult, secondResult, "A brand new entry instance should have been created.");
            Assert.IsTrue(m_Registry.GlobalDirty, "Creating the replacement entry must flag the registry as dirty.");
        }

        [Test]
        public void GetOrCreate_HandlerReturnsNull_DestroysGameObjectAndThrowsNullReferenceException()
        {
            // Arrange
            m_Registry.Initialize(null, "Container");
            var failingHandler = new TestFailingCreateHandler();

            // Act & Assert
            Assert.Throws<NullReferenceException>(() =>
                m_Registry.PublicGetOrCreate(1, "FailObj", Vector3.zero, ref failingHandler, out _));

            // Verify cleanup: The dictionary should not contain the key if creation failed
            Assert.IsFalse(m_Registry.Contains(1), "Storage should not contain the key if creation failed.");
            Assert.AreEqual(0, m_Registry.Count, "Storage count should be 0.");
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void Dispose_WhenCalledOnce_DisposesAndClearsRegistry()
        {
            m_Registry.Initialize(null, "Container");
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.zero, ref handler, out _);

            Assert.AreEqual(1, m_Registry.Count);

            // Act
            m_Registry.Dispose();

            // Assert
            Assert.AreEqual(0, m_Registry.Count, "Dispose should clear all entries from storage via Reset.");
        }

        [Test]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            m_Registry.Initialize(null, "Container");

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                m_Registry.Dispose();
                m_Registry.Dispose(); // Second call should safely return early due to m_Disposed flag
            }, "Calling Dispose multiple times should be safe and idempotent.");
        }

        [Test]
        public void Dispose_AfterDisposal_PreventsFurtherOperationsOrHandlesThemSafely()
        {
            m_Registry.Initialize(null, "Container");
            m_Registry.Dispose();

            Assert.IsFalse(m_Registry.IsInitialized);
        }

        #endregion

        #region Reset Tests

        [UnityTest]
        public IEnumerator Reset_ClearsAllEntriesAndDestroysContainer()
        {
            // Arrange
            m_Registry.Initialize(null, "TestContainer");
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.zero, ref handler, out var entry);

            var containerTransform = m_Registry.Container;
            Assert.IsNotNull(containerTransform, "Container should be created upon initialization.");

            // Act
            m_Registry.Reset();

            yield return null;

            // Assert
            Assert.AreEqual(0, m_Registry.Count, "Reset should clear all entries from storage.");
            Assert.IsFalse(m_Registry.IsInitialized, "Reset should set IsInitialized to false.");

            // Verify container was destroyed
            bool isContainerDestroyed = containerTransform == null || containerTransform.gameObject == null;
            Assert.IsTrue(isContainerDestroyed, "Reset should destroy the container GameObject.");
            Assert.IsNull(m_Registry.Container, "Container property should be null after reset.");
        }

        [Test]
        public void Reset_WhenNotInitialized_DoesNotThrow()
        {
            // Arrange
            var freshRegistry = new TestSpatialRegistry();

            // Act & Assert
            Assert.DoesNotThrow(() => freshRegistry.Reset(), "Calling Reset on an uninitialized registry should be safe.");
        }

        #endregion

        #region NeedsUpdate Tests

        [Test]
        public void NeedsUpdate_WhenRegistryIsCleanAndEntriesAreClean_ReturnsFalse()
        {
            m_Registry.Initialize(null, "Container");
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.zero, ref handler, out _);

            // Set everything to clean
            m_Registry.ResetDirtyFlags();

            // Act & Assert
            Assert.IsFalse(m_Registry.NeedsUpdate(), "NeedsUpdate should return false when neither global nor any entry is dirty.");
        }

        [Test]
        public void NeedsUpdate_WhenGlobalDirtyIsTrue_ReturnsTrue()
        {
            m_Registry.Initialize(null, "Container");
            m_Registry.ResetDirtyFlags(); // m_GlobalDirty is now false

            // Force global dirty true
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.zero, ref handler, out _);

            // Act & Assert
            Assert.IsTrue(m_Registry.NeedsUpdate(), "NeedsUpdate should return true when m_GlobalDirty is set.");
        }

        [Test]
        public void NeedsUpdate_WhenAnEntryIsDirty_ReturnsTrue()
        {
            m_Registry.Initialize(null, "Container");
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.zero, ref handler, out var entry);

            m_Registry.ResetDirtyFlags(); // Everything clean now
            Assert.IsFalse(m_Registry.NeedsUpdate());

            // Mark a specific entry as dirty
            entry.MarkDirty();

            // Act & Assert
            Assert.IsTrue(m_Registry.NeedsUpdate(), "NeedsUpdate should return true when at least one entry is dirty.");
        }

        [Test]
        public void NeedsUpdate_WhenEmpty_ReturnsFalse()
        {
            m_Registry.Initialize(null, "Container");
            m_Registry.ResetDirtyFlags();

            // Act & Assert
            Assert.IsFalse(m_Registry.NeedsUpdate(), "NeedsUpdate should return false for an empty registry.");
        }

        #endregion

        #region ResetDirtyFlags Tests

        [Test]
        public void ResetDirtyFlags_ClearsGlobalDirtyAndAllEntryFlags()
        {
            // Arrange
            m_Registry.Initialize(null, "Container");
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "EntryObj", Vector3.zero, ref handler, out var entry);

            // Force global dirty and entry dirty to true
            entry.IsDirty = true;

            Assert.IsTrue(m_Registry.GlobalDirty, "Precondition: Global dirty should be true.");
            Assert.IsTrue(entry.IsDirty, "Precondition: Entry dirty should be true.");

            // Act
            m_Registry.ResetDirtyFlags();

            // Assert
            Assert.IsFalse(m_Registry.GlobalDirty, "ResetDirtyFlags should set m_GlobalDirty to false.");
            Assert.IsFalse(entry.IsDirty, "ResetDirtyFlags should call ClearDirty on all valid entries.");
            Assert.IsFalse(m_Registry.NeedsUpdate(), "NeedsUpdate should return false after resetting all flags.");
        }

        [Test]
        public void ResetDirtyFlags_WhenEmpty_DoesNotThrow()
        {
            // Arrange
            m_Registry.Initialize(null, "Container");
            m_Registry.ResetDirtyFlags();

            // Act & Assert
            Assert.DoesNotThrow(() => m_Registry.ResetDirtyFlags(), "Calling ResetDirtyFlags on an empty registry should be safe.");
        }

        #endregion

        #region Iteration Tests

        [Test]
        public void AllKeys_ReturnsAllStoredKeys()
        {
            m_Registry.Initialize(null, "Container");
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "Entry1", Vector3.zero, ref handler, out _);
            m_Registry.PublicGetOrCreate(2, "Entry2", Vector3.zero, ref handler, out _);

            var iterator = m_Registry.AllKeys;
            var keys = new List<int>();

            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current);
            }

            Assert.AreEqual(2, keys.Count);
            Assert.Contains(1, keys);
            Assert.Contains(2, keys);
        }

        [Test]
        public void AllEntries_ReturnsAllStoredValues()
        {
            m_Registry.Initialize(null, "Container");
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "Entry1", Vector3.zero, ref handler, out var entry1);
            m_Registry.PublicGetOrCreate(2, "Entry2", Vector3.zero, ref handler, out var entry2);

            var iterator = m_Registry.AllEntries;
            var entries = new List<TestSpatialEntry>();

            while (iterator.MoveNext())
            {
                entries.Add(iterator.Current);
            }

            Assert.AreEqual(2, entries.Count);
            Assert.Contains(entry1, entries);
            Assert.Contains(entry2, entries);
        }

        [Test]
        public void ForEachKey_ExecutesActionOnEveryKey()
        {
            m_Registry.Initialize(null, "Container");
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(10, "Entry1", Vector3.zero, ref handler, out _);
            m_Registry.PublicGetOrCreate(20, "Entry2", Vector3.zero, ref handler, out _);

            var actionHandler = new TestKeyActionHandler();
            m_Registry.ForEachKey(ref actionHandler);
        }

        [Test]
        public void ForEachEntry_ExecutesActionOnEveryEntry()
        {
            m_Registry.Initialize(null, "Container");
            var handler = new TestCreateHandler();
            m_Registry.PublicGetOrCreate(1, "Entry1", Vector3.zero, ref handler, out var entry1);
            m_Registry.PublicGetOrCreate(2, "Entry2", Vector3.zero, ref handler, out var entry2);

            int executionCount = 0;
            var resultHandler = new TestActionHandler { OnExecute = () => executionCount++ };
            m_Registry.ForEachEntry(ref resultHandler);

            Assert.AreEqual(2, executionCount, "ForEachEntry should execute the handler action for every entry in storage.");
        }

        [Test]
        public void AllKeys_WhenRegistryIsEmpty_IteratorMovesFalseImmediately()
        {
            m_Registry.Initialize(null, "Container");

            var iterator = m_Registry.AllKeys;

            Assert.IsFalse(iterator.MoveNext(), "MoveNext should return false immediately on an empty registry.");
        }

        [Test]
        public void AllEntries_WhenRegistryIsEmpty_IteratorMovesFalseImmediately()
        {
            m_Registry.Initialize(null, "Container");

            var iterator = m_Registry.AllEntries;

            Assert.IsFalse(iterator.MoveNext(), "MoveNext should return false immediately on an empty registry.");
        }

        [Test]
        public void ForEachKey_WhenRegistryIsEmpty_DoesNotExecuteAction()
        {
            m_Registry.Initialize(null, "Container");

            int executionCount = 0;
            var keyHandler = new TestKeyActionHandler { OnExecute = () => executionCount++ };
            m_Registry.ForEachKey(ref keyHandler);

            Assert.AreEqual(0, executionCount, "ForEachKey should not execute any actions when the registry is empty.");
        }

        [Test]
        public void ForEachEntry_WhenRegistryIsEmpty_DoesNotExecuteAction()
        {
            m_Registry.Initialize(null, "Container");

            int executionCount = 0;
            var resultHandler = new TestActionHandler { OnExecute = () => executionCount++ };
            m_Registry.ForEachEntry(ref resultHandler);

            Assert.AreEqual(0, executionCount, "ForEachEntry should not execute any actions when the registry is empty.");
        }

        #endregion
    }
}
