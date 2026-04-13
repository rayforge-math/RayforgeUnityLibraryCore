using NUnit.Framework;
using Rayforge.Core.Caching.Abstractions;
using Rayforge.Core.Caching.Transforms;
using UnityEngine;

namespace Rayforge.Core.Tests.Caching.Transforms
{
    [TestFixture]
    public class CachedTransformTests
    {
        private static GameObject CreateCleanGameObject(string name)
        {
            return new GameObject(name);
        }

        private static GameObject CreateModifiedGameObject(string name, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;
            return go;
        }

        #region Initialization Tests

        [Test]
        public void Constructor_DefaultGameObject()
        {
            // Arrange
            string expectedName = "FactoryObject";
            var go = CreateCleanGameObject(expectedName);

            // Act
            var ct = new CachedTransform(go);

            // Assert
            Assert.IsNotNull(ct.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, ct.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.AreEqual(Vector3.zero, ct.Self.position, "Default position should be zero.");
            Assert.AreEqual(Quaternion.identity, ct.Self.rotation, "Default rotation should be identity.");
            Assert.AreEqual(Vector3.one, ct.Self.localScale, "Default scale should be one.");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Constructor_ModifiedGameObject()
        {
            // Arrange
            string expectedName = "FactoryObject";
            Vector3 customPos = new Vector3(10f, 20f, 30f);
            Quaternion customRot = Quaternion.Euler(45f, 45f, 45f);
            Vector3 customScale = new Vector3(2f, 2f, 2f);

            var modifiedGo = CreateModifiedGameObject(expectedName, customPos, customRot, customScale);

            // Act
            var ct = new CachedTransform(modifiedGo);

            // Assert
            Assert.IsNotNull(ct.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, ct.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.AreEqual(customPos, ct.Self.position, "Self position should match modified Transform position.");
            Assert.AreEqual(customRot, ct.Self.rotation, "Self rotation should match modified Transform rotation.");
            Assert.AreEqual(customScale, ct.Self.localScale, "Self scale should match modified Transform scale.");

            // Cleanup
            Object.DestroyImmediate(modifiedGo);
        }

        [Test]
        public void Constructor_NullGameObject()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                var ct = new CachedTransform(null);
            });
        }

        #endregion

        #region Dispose

        [Test]
        public void Dispose_DestroysUnderlyingGameObject()
        {
            // Arrange
            var go = CreateCleanGameObject("UnityDestroyTarget");
            var ct = new CachedTransform(go);

            // Act
            ct.Dispose();

            // Assert
            Assert.IsTrue(go == null, "The underlying GameObject should be destroyed in Unity.");
        }

        [Test]
        public void Dispose_NullifiesInternalReferences()
        {
            // Arrange
            var go = CreateCleanGameObject("ReferenceCleanupTarget");
            var ct = new CachedTransform(go);

            // Act
            ct.Dispose();

            // Assert
            Assert.IsNull(ct.Self, "The Self property should be null after Dispose.");
        }

        #endregion

        #region Factory Tests

        [Test]
        public void Create_WithName()
        {
            // Arrange
            string expectedName = "FactoryObject";

            // Act
            var ct = CachedTransform.Create(expectedName);

            // Assert
            Assert.IsNotNull(ct.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, ct.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.AreEqual(Vector3.zero, ct.Self.position, "Default position should be zero.");
            Assert.AreEqual(Quaternion.identity, ct.Self.rotation, "Default rotation should be identity.");
            Assert.AreEqual(Vector3.one, ct.Self.localScale, "Default scale should be one.");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Create_EmptyName()
        {
            // Arrange
            string expectedName = "";

            // Act
            var ct = CachedTransform.Create("");

            // Assert
            Assert.IsNotNull(ct.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, ct.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.AreEqual(Vector3.zero, ct.Self.position, "Default position should be zero.");
            Assert.AreEqual(Quaternion.identity, ct.Self.rotation, "Default rotation should be identity.");
            Assert.AreEqual(Vector3.one, ct.Self.localScale, "Default scale should be one.");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Create_WithParent_WithName()
        {
            // Arrange
            string expectedName = "GenericChild";
            var parentGo = CreateCleanGameObject("ParentObject");
            var parentCt = new CachedTransform(parentGo);

            // Act
            var childCt = CachedTransform.Create<ICachedTransform>(expectedName, parentCt);

            // Assert
            Assert.IsNotNull(childCt.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, childCt.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.AreEqual(parentGo.transform, childCt.Self.parent, "The Unity transform parent should be correctly set.");
            Assert.AreEqual(parentCt, childCt.Parent, "The Parent property should reference the passed ICachedTransform.");

            // Check initial values via Self
            Assert.AreEqual(Vector3.zero, childCt.Self.localPosition, "Default local position should be zero.");
            Assert.AreEqual(Quaternion.identity, childCt.Self.localRotation, "Default local rotation should be identity.");
            Assert.AreEqual(Vector3.one, childCt.Self.localScale, "Default local scale should be one.");

            // Cleanup
            Object.DestroyImmediate(childCt.Self.gameObject);
            Object.DestroyImmediate(parentGo);
        }

        [Test]
        public void Create_NullParent_WithName()
        {
            // Arrange
            string expectedName = "RootObject";

            // Act
            var ct = CachedTransform.Create<ICachedTransform>(expectedName, null);

            // Assert
            Assert.IsNotNull(ct.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, ct.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.IsNull(ct.Self.parent, "The transform should not have a parent in Unity.");
            Assert.IsNull(ct.Parent, "The Parent property should be null.");

            // Check initial values via Self
            Assert.AreEqual(Vector3.zero, ct.Self.position, "Default position should be zero.");
            Assert.AreEqual(Quaternion.identity, ct.Self.rotation, "Default rotation should be identity.");
            Assert.AreEqual(Vector3.one, ct.Self.localScale, "Default scale should be one.");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Create_WithParent_EmptyName()
        {
            // Arrange
            string expectedName = "";
            var parentGo = CreateCleanGameObject("ParentObject");
            var parentCt = new CachedTransform(parentGo);

            // Act
            var childCt = CachedTransform.Create<ICachedTransform>(expectedName, parentCt);

            // Assert
            Assert.IsNotNull(childCt.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, childCt.Self.name, "The name of the instantiated GameObject should be empty.");
            Assert.AreEqual(parentGo.transform, childCt.Self.parent, "The Unity transform parent should be correctly set.");
            Assert.AreEqual(parentCt, childCt.Parent, "The Parent property should reference the passed ICachedTransform.");

            // Check initial values via Self
            Assert.AreEqual(Vector3.zero, childCt.Self.localPosition, "Default local position should be zero.");
            Assert.AreEqual(Quaternion.identity, childCt.Self.localRotation, "Default local rotation should be identity.");
            Assert.AreEqual(Vector3.one, childCt.Self.localScale, "Default local scale should be one.");

            // Cleanup
            Object.DestroyImmediate(childCt.Self.gameObject);
            Object.DestroyImmediate(parentGo);
        }

        #endregion

        #region Property Tests

        [Test]
        public void Self_ReturnsCorrectTransform()
        {
            // Arrange
            var go = CreateCleanGameObject("TransformTest");
            var ct = new CachedTransform(go);

            // Act
            var transform = ct.Self;

            // Assert
            Assert.AreEqual(go.transform, transform, "Self should return the transform of the associated GameObject.");

            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Self_AfterDispose_ReturnsNull()
        {
            // Arrange
            var go = CreateCleanGameObject("DisposeRefTest");
            var ct = new CachedTransform(go);

            // Act
            ct.Dispose();

            // Assert
            Assert.IsNull(ct.Self, "Self should return null after the object has been disposed.");
        }

        [Test]
        public void Self_WhenGameObjectDestroyedExternally_ReturnsNull()
        {
            // Arrange
            var go = CreateCleanGameObject("ExternalDestructionTest");
            var ct = new CachedTransform(go);

            // Act
            Object.DestroyImmediate(go);

            // Assert
            Assert.IsNull(ct.Self, "Self should return null if the underlying GameObject was destroyed externally.");
        }

        #endregion

        #region Parent Property Tests

        [Test]
        public void Parent_Set_AssignsUnityParentAndReference()
        {
            // Arrange
            var parentGo = CreateCleanGameObject("Parent");
            var parentCt = new CachedTransform(parentGo);
            var childCt = CachedTransform.Create("Child");

            // Act
            childCt.Parent = parentCt;

            // Assert
            Assert.AreEqual(parentCt, childCt.Parent, "The Parent property should store the reference.");
            Assert.AreEqual(parentGo.transform, childCt.Self.parent, "The Unity transform should be parented correctly.");

            // Cleanup
            Object.DestroyImmediate(childCt.Self.gameObject);
            Object.DestroyImmediate(parentGo);
        }

        [Test]
        public void Parent_SetNull_UnparentsInUnityAndReference()
        {
            // Arrange
            var parentGo = CreateCleanGameObject("Parent");
            var parentCt = new CachedTransform(parentGo);
            var childCt = CachedTransform.Create("Child", parentCt);

            // Act
            childCt.Parent = null;

            // Assert
            Assert.IsNull(childCt.Parent, "The Parent property should be null.");
            Assert.IsNull(childCt.Self.parent, "The Unity transform should have no parent.");

            // Cleanup
            Object.DestroyImmediate(childCt.Self.gameObject);
            Object.DestroyImmediate(parentGo);
        }

        #endregion

        #region Parameterized Cache Tests

        private static readonly Vector3[] VectorTestCases = new Vector3[]
        {
            new Vector3(1000.01f, -500.05f, 0.123f), // Standard float values
            Vector3.zero,                            // Minimum/Origin
            new Vector3(float.MaxValue, 0, 0),       // Extreme Positive
            new Vector3(float.MinValue, 0, 0),       // Extreme Negative
            new Vector3(0.00001f, 0.00001f, 0.00001f) // Tiny precision values
        };

        private static readonly Quaternion[] RotationTestCases = new Quaternion[]
        {
            Quaternion.identity,                     // No rotation
            Quaternion.Euler(90, 0, 0),              // Right angle
            Quaternion.Euler(359.9f, 0, 0),          // Near full circle
            new Quaternion(0.5f, 0.5f, 0.5f, 0.5f),  // Normalized manual quaternion
            Quaternion.Euler(-45, -45, -45)          // Negative eulers
        };

        [Test]
        public void CachedPosition_Set_Get([ValueSource(nameof(VectorTestCases))] Vector3 testValue)
        {
            // Arrange
            var ct = CachedTransform.Create("PosExtremesTest");

            // Act
            ct.Position = testValue;

            // Assert
            Assert.AreEqual(testValue, ct.Position, $"Cache failed for value {testValue}");
            Assert.AreEqual(testValue, ct.Self.position, $"Unity Transform failed for value {testValue}");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void CachedRotation_Set_Get([ValueSource(nameof(RotationTestCases))] Quaternion testValue)
        {
            // Arrange
            var ct = CachedTransform.Create("RotExtremesTest");

            // Act
            ct.Rotation = testValue;

            // Assert
            // Note: Quaternions can sometimes represent the same rotation with different internal values (flip).
            // Unity's == operator or Quaternion.Dot are safer than direct value comparison.
            Assert.IsTrue(Quaternion.Dot(testValue, ct.Rotation) > 0.999f, $"Cache failed for rotation {testValue}");
            Assert.IsTrue(Quaternion.Dot(testValue, ct.Self.rotation) > 0.999f, $"Unity Transform failed for rotation {testValue}");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void CachedScale_Set_Get([ValueSource(nameof(VectorTestCases))] Vector3 testValue)
        {
            // Arrange
            var ct = CachedTransform.Create("ScaleExtremesTest");

            // Act
            ct.Scale = testValue;

            // Assert
            Assert.AreEqual(testValue, ct.Scale, $"Cache failed for scale {testValue}");
            Assert.AreEqual(testValue, ct.Self.localScale, $"Unity Transform failed for scale {testValue}");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        #endregion

        #region Refresh Synchronization Tests

        [Test]
        public void Refresh_UpdatesPosition([ValueSource(nameof(VectorTestCases))] Vector3 externalPos)
        {
            // Arrange
            var ct = CachedTransform.Create("RefreshPosTest");
            ct.Position = Vector3.zero; // Initial cache state

            // Act
            // Bypass the cache to simulate external modification (e.g., Physics)
            ct.Self.position = externalPos;
            ct.Refresh();

            // Assert
            Assert.AreEqual(externalPos, ct.Position, $"Refresh failed to sync position for value {externalPos}");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Refresh_UpdatesRotation([ValueSource(nameof(RotationTestCases))] Quaternion externalRot)
        {
            // Arrange
            var ct = CachedTransform.Create("RefreshRotTest");
            ct.Rotation = Quaternion.identity;

            // Act
            ct.Self.rotation = externalRot;
            ct.Refresh();

            // Assert
            // Using Dot product for mathematical rotation equality
            Assert.IsTrue(Quaternion.Dot(externalRot, ct.Rotation) > 0.999f, $"Refresh failed to sync rotation for {externalRot}");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Refresh_UpdatesScale([ValueSource(nameof(VectorTestCases))] Vector3 externalScale)
        {
            // Arrange
            var ct = CachedTransform.Create("RefreshScaleTest");
            ct.Scale = Vector3.one;

            // Act
            ct.Self.localScale = externalScale;
            ct.Refresh();

            // Assert
            Assert.AreEqual(externalScale, ct.Scale, $"Refresh failed to sync localScale for value {externalScale}");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        #endregion

        #region Component Management Tests

        [Test]
        public void AddComponent_AddsAndReturnsComponent()
        {
            // Arrange
            var ct = CachedTransform.Create("AddComponentTest");

            // Act
            var addedComponent = ct.AddComponent<BoxCollider>();

            // Assert
            Assert.IsNotNull(addedComponent, "The returned component should not be null.");
            var componentOnGo = ct.Self.GetComponent<BoxCollider>();
            Assert.AreEqual(addedComponent, componentOnGo, "The component should be attached to the underlying GameObject.");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void AddComponent_AddsMultipleComponents()
        {
            // Arrange
            var ct = CachedTransform.Create("MultiComponentTest");

            // Act
            var comp1 = ct.AddComponent<BoxCollider>();
            var comp2 = ct.AddComponent<BoxCollider>();

            // Assert
            Assert.AreNotEqual(comp1, comp2, "AddComponent should create a new instance each time.");
            Assert.AreEqual(2, ct.Self.GetComponents<BoxCollider>().Length, "The GameObject should have exactly two BoxColliders.");

            // Cleanup
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        #endregion
    }
}
