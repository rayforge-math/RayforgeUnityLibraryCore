using NUnit.Framework;

using UnityEngine;

namespace Rayforge.Core.Caching.Abstractions.Tests
{
    public abstract class CachedTransformContractTests<TDerived>
        where TDerived : ICachedTransform
    {
        #region Abstract Members

        protected abstract TDerived CreateInstance(GameObject go);
        protected abstract TDerived CallCreateFactory(string name);
        protected abstract TDerived CallTemplateCreateFactory(string name, ICachedTransform parent = null);

        #endregion

        #region Static Helpers

        protected static GameObject CreateCleanGameObject(string name) => new GameObject(name);

        protected static GameObject CreateModifiedGameObject(string name, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;
            return go;
        }

        #endregion

        #region Vector & Rotation Parameters

        protected static readonly Vector3[] VectorTestCases = new Vector3[]
        {
            new Vector3(1000.01f, -500.05f, 0.123f), // Standard float values
            Vector3.zero,                            // Minimum/Origin
            new Vector3(float.MaxValue, 0, 0),       // Extreme Positive
            new Vector3(float.MinValue, 0, 0),       // Extreme Negative
            new Vector3(0.00001f, 0.00001f, 0.00001f) // Tiny precision values
        };

        protected static readonly Quaternion[] RotationTestCases = new Quaternion[]
        {
            Quaternion.identity,                     // No rotation
            Quaternion.Euler(90, 0, 0),              // Right angle
            Quaternion.Euler(359.9f, 0, 0),          // Near full circle
            new Quaternion(0.5f, 0.5f, 0.5f, 0.5f),  // Normalized manual quaternion
            Quaternion.Euler(-45, -45, -45)          // Negative eulers
        };

        #endregion

        #region Initialization Tests

        [Test]
        public void Constructor_DefaultGameObject()
        {
            string expectedName = "FactoryObject";
            var go = CreateCleanGameObject(expectedName);
            var ct = CreateInstance(go);

            Assert.IsNotNull(ct.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, ct.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.AreEqual(Vector3.zero, ct.Self.position, "Default position should be zero.");
            Assert.AreEqual(Quaternion.identity, ct.Self.rotation, "Default rotation should be identity.");
            Assert.AreEqual(Vector3.one, ct.Self.localScale, "Default scale should be one.");

            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Constructor_ModifiedGameObject()
        {
            string expectedName = "ModifiedObject";
            Vector3 customPos = new Vector3(10f, 20f, 30f);
            Quaternion customRot = Quaternion.Euler(45f, 45f, 45f);
            Vector3 customScale = new Vector3(2f, 2f, 2f);

            var modifiedGo = CreateModifiedGameObject(expectedName, customPos, customRot, customScale);
            var ct = CreateInstance(modifiedGo);

            Assert.IsNotNull(ct.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, ct.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.AreEqual(customPos, ct.Self.position, "Self position should match modified Transform position.");
            Assert.AreEqual(customRot, ct.Self.rotation, "Self rotation should match modified Transform rotation.");
            Assert.AreEqual(customScale, ct.Self.localScale, "Self scale should match modified Transform scale.");

            Object.DestroyImmediate(modifiedGo);
        }

        [Test]
        public void Constructor_NullGameObject()
        {
            Assert.Throws<System.ArgumentNullException>(() => CreateInstance(null));
        }

        #endregion

        #region Dispose

        [Test]
        public void Dispose_DestroysUnderlyingGameObject()
        {
            var go = CreateCleanGameObject("UnityDestroyTarget");
            var ct = CreateInstance(go);
            ct.Dispose();
            Assert.IsTrue(go == null, "The underlying GameObject should be destroyed in Unity.");
        }

        [Test]
        public void Dispose_NullifiesInternalReferences()
        {
            var go = CreateCleanGameObject("ReferenceCleanupTarget");
            var ct = CreateInstance(go);
            ct.Dispose();
            Assert.IsNull(ct.Self, "The Self property should be null after Dispose.");
        }

        #endregion

        #region Factory Tests

        [Test]
        public void Create_WithName()
        {
            string expectedName = "FactoryObject";
            var ct = CallCreateFactory(expectedName);

            Assert.IsNotNull(ct.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, ct.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.AreEqual(Vector3.zero, ct.Self.position, "Default position should be zero.");
            Assert.AreEqual(Quaternion.identity, ct.Self.rotation, "Default rotation should be identity.");
            Assert.AreEqual(Vector3.one, ct.Self.localScale, "Default scale should be one.");
            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Create_EmptyName()
        {
            string expectedName = "";
            var ct = CallCreateFactory("");

            Assert.IsNotNull(ct.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, ct.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.AreEqual(Vector3.zero, ct.Self.position, "Default position should be zero.");
            Assert.AreEqual(Quaternion.identity, ct.Self.rotation, "Default rotation should be identity.");
            Assert.AreEqual(Vector3.one, ct.Self.localScale, "Default scale should be one.");

            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Create_WithParent_WithName()
        {
            string expectedName = "Child";
            var parentGo = CreateCleanGameObject("Parent");
            var parentCt = CreateInstance(parentGo);

            var childCt = CallTemplateCreateFactory(expectedName, parentCt);

            Assert.IsNotNull(childCt.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, childCt.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.AreEqual(parentGo.transform, childCt.Self.parent, "The Unity transform parent should be correctly set.");
            Assert.AreEqual(parentCt, childCt.Parent, "The Parent property should reference the passed ICachedTransform.");

            Assert.AreEqual(Vector3.zero, childCt.Self.localPosition, "Default local position should be zero.");
            Assert.AreEqual(Quaternion.identity, childCt.Self.localRotation, "Default local rotation should be identity.");
            Assert.AreEqual(Vector3.one, childCt.Self.localScale, "Default local scale should be one.");

            Object.DestroyImmediate(childCt.Self.gameObject);
            Object.DestroyImmediate(parentGo);
        }

        [Test]
        public void Create_NullParent_WithName()
        {
            string expectedName = "RootObject";
            var ct = CallTemplateCreateFactory(expectedName, null);

            Assert.IsNotNull(ct.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, ct.Self.name, "The name of the instantiated GameObject should match the input.");
            Assert.IsNull(ct.Self.parent, "The transform should not have a parent in Unity.");
            Assert.IsNull(ct.Parent, "The Parent property should be null.");

            Assert.AreEqual(Vector3.zero, ct.Self.position, "Default position should be zero.");
            Assert.AreEqual(Quaternion.identity, ct.Self.rotation, "Default rotation should be identity.");
            Assert.AreEqual(Vector3.one, ct.Self.localScale, "Default scale should be one.");

            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Create_WithParent_EmptyName()
        {
            string expectedName = "";
            var parentGo = CreateCleanGameObject("ParentObject");
            var parentCt = CreateInstance(parentGo);

            var childCt = CallTemplateCreateFactory(expectedName, parentCt);

            Assert.IsNotNull(childCt.Self, "Factory should have created a valid transform reference.");
            Assert.AreEqual(expectedName, childCt.Self.name, "The name of the instantiated GameObject should be empty.");
            Assert.AreEqual(parentGo.transform, childCt.Self.parent, "The Unity transform parent should be correctly set.");
            Assert.AreEqual(parentCt, childCt.Parent, "The Parent property should reference the passed ICachedTransform.");

            Assert.AreEqual(Vector3.zero, childCt.Self.localPosition, "Default local position should be zero.");
            Assert.AreEqual(Quaternion.identity, childCt.Self.localRotation, "Default local rotation should be identity.");
            Assert.AreEqual(Vector3.one, childCt.Self.localScale, "Default local scale should be one.");

            Object.DestroyImmediate(childCt.Self.gameObject);
            Object.DestroyImmediate(parentGo);
        }

        #endregion

        #region Self Property Tests

        [Test]
        public void Self_ReturnsCorrectTransform()
        {
            var go = CreateCleanGameObject("TransformTest");
            var ct = CreateInstance(go);

            Assert.AreEqual(go.transform, ct.Self, "Self should return the transform of the associated GameObject.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Self_AfterDispose_ReturnsNull()
        {
            var go = CreateCleanGameObject("DisposeRefTest");
            var ct = CreateInstance(go);

            ct.Dispose();

            Assert.IsNull(ct.Self, "Self should return null after the object has been disposed.");
        }

        [Test]
        public void Self_WhenGameObjectDestroyedExternally_ReturnsNull()
        {
            var go = CreateCleanGameObject("ExternalDestructionTest");
            var ct = CreateInstance(go);

            Object.DestroyImmediate(go);

            Assert.IsNull(ct.Self, "Self should return null if the underlying GameObject was destroyed externally.");
        }

        #endregion

        #region Parent Property Tests

        [Test]
        public void Parent_Set_AssignsUnityParentAndReference()
        {
            var parentGo = CreateCleanGameObject("Parent");
            var parentCt = CreateInstance(parentGo);
            var childCt = CallCreateFactory("Child");

            childCt.Parent = parentCt;

            Assert.AreEqual(parentCt, childCt.Parent, "The Parent property should store the reference.");
            Assert.AreEqual(parentGo.transform, childCt.Self.parent, "The Unity transform should be parented correctly.");

            Object.DestroyImmediate(childCt.Self.gameObject);
            Object.DestroyImmediate(parentGo);
        }

        [Test]
        public void Parent_SetNull_UnparentsInUnityAndReference()
        {
            var parentGo = CreateCleanGameObject("Parent");
            var parentCt = CreateInstance(parentGo);
            var childCt = CallTemplateCreateFactory("Child", parentCt);

            childCt.Parent = null;

            Assert.IsNull(childCt.Parent, "The Parent property should be null.");
            Assert.IsNull(childCt.Self.parent, "The Unity transform should have no parent.");

            Object.DestroyImmediate(childCt.Self.gameObject);
            Object.DestroyImmediate(parentGo);
        }

        [Test]
        public void Parent_SwitchingParents_UpdatesReferencesAndMaintainsConsistency()
        {
            var p1 = CallCreateFactory("Parent_1");
            var p2 = CallCreateFactory("Parent_2");
            var child = CallCreateFactory("Child");

            p1.Position = new Vector3(10, 0, 0);
            p2.Position = new Vector3(20, 0, 0);

            child.Parent = p1;
            Assert.AreEqual(p1, child.Parent, "Failed to assign first parent.");
            Assert.AreEqual(p1.Self, child.Self.parent, "Unity parent mismatch for p1.");

            child.Parent = p2;
            Assert.AreEqual(p2, child.Parent, "Failed to switch to second parent.");
            Assert.AreEqual(p2.Self, child.Self.parent, "Unity parent mismatch for p2.");

            child.Parent = null;
            Assert.IsNull(child.Parent, "Failed to clear parent reference.");
            Assert.IsNull(child.Self.parent, "Unity parent was not cleared.");

            Object.DestroyImmediate(child.Self.gameObject);
            Object.DestroyImmediate(p1.Self.gameObject);
            Object.DestroyImmediate(p2.Self.gameObject);
        }

        [Test]
        public void Parent_Set_UpdatesWorldCacheCorrectly()
        {
            var parent = CallCreateFactory("Parent");
            var child = CallCreateFactory("Child");

            Vector3 customPos = new Vector3(100f, -50f, 25f);
            Quaternion customRot = Quaternion.Euler(45f, 90f, 0f);
            Vector3 customScale = new Vector3(2f, 2f, 2f);

            parent.Position = customPos;
            parent.Rotation = customRot;
            parent.Scale = customScale;

            child.Position = Vector3.zero;
            child.Rotation = Quaternion.identity;
            child.Scale = Vector3.one;

            child.Parent = parent;

            Assert.AreEqual(child.Self.position, child.Position, "Child cached position out of sync with its Transform.");
            Assert.AreEqual(child.Self.rotation, child.Rotation, "Child cached rotation out of sync with its Transform.");
            Assert.AreEqual(child.Self.localScale, child.Scale, "Child cached scale out of sync with its Transform.");

            Assert.AreEqual(parent.Self, child.Self.parent, "Unity parenting failed.");
            Assert.AreEqual(parent, child.Parent, "Cached parent reference mismatch.");

            Object.DestroyImmediate(child.Self.gameObject);
            Object.DestroyImmediate(parent.Self.gameObject);
        }

        #endregion

        #region SetParent Method Tests

        [Test]
        public void SetParent_WithWorldPositionStaysTrue_MaintainsWorldPosition()
        {
            var parent = CallCreateFactory("Parent");
            var child = CallCreateFactory("Child");

            parent.Position = new Vector3(100, 100, 100);
            Vector3 childInitialWorldPos = new Vector3(10, 10, 10);
            child.Position = childInitialWorldPos;

            child.SetParent(parent, true);

            Assert.AreEqual(childInitialWorldPos, child.Position, "World position should stay the same.");
            Assert.AreEqual(child.Self.position, child.Position, "Cache out of sync after SetParent(true).");

            Assert.AreEqual(parent.Self, child.Self.parent);

            Object.DestroyImmediate(child.Self.gameObject);
            Object.DestroyImmediate(parent.Self.gameObject);
        }

        [Test]
        public void SetParent_WithWorldPositionStaysFalse_ResetsToParentSpace()
        {
            var parent = CallCreateFactory("Parent");
            var child = CallCreateFactory("Child");

            Vector3 parentPos = new Vector3(50, 0, 0);
            Vector3 childPos = new Vector3(10, 10, 10);
            Vector3 expected = parentPos + childPos;
            parent.Position = parentPos;
            child.Position = childPos;

            child.SetParent(parent, false);

            Assert.AreEqual(expected, child.Position, "Child should have position relative to parent.");
            Assert.AreEqual(child.Self.position, child.Position, "Cache out of sync after SetParent(false).");

            Assert.AreEqual(childPos, child.Self.localPosition);

            Object.DestroyImmediate(child.Self.gameObject);
            Object.DestroyImmediate(parent.Self.gameObject);
        }

        [Test]
        public void SetParent_ToNull_MaintainsWorldPosition()
        {
            var parent = CallCreateFactory("Parent");
            var child = CallTemplateCreateFactory("Child", parent);

            Vector3 initialPos = new Vector3(25, 25, 25);
            child.Position = initialPos;

            child.SetParent(null, true);

            Assert.IsNull(child.Parent);
            Assert.IsNull(child.Self.parent);
            Assert.AreEqual(initialPos, child.Position, "Child should maintain position when unparenting.");

            Object.DestroyImmediate(child.Self.gameObject);
            Object.DestroyImmediate(parent.Self.gameObject);
        }

        #endregion

        #region Parameterized Cache Tests

        [Test]
        public void CachedPosition_Set_Get([ValueSource(nameof(VectorTestCases))] Vector3 testValue)
        {
            var ct = CallCreateFactory("PosExtremesTest");

            ct.Position = testValue;

            Assert.AreEqual(testValue, ct.Position, $"Cache failed for value {testValue}");
            Assert.AreEqual(testValue, ct.Self.position, $"Unity Transform failed for value {testValue}");

            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void CachedRotation_Set_Get([ValueSource(nameof(RotationTestCases))] Quaternion testValue)
        {
            var ct = CallCreateFactory("RotExtremesTest");

            ct.Rotation = testValue;

            Assert.IsTrue(Quaternion.Dot(testValue, ct.Rotation) > 0.999f, $"Cache failed for rotation {testValue}");
            Assert.IsTrue(Quaternion.Dot(testValue, ct.Self.rotation) > 0.999f, $"Unity Transform failed for rotation {testValue}");

            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void CachedScale_Set_Get([ValueSource(nameof(VectorTestCases))] Vector3 testValue)
        {
            var ct = CallCreateFactory("ScaleExtremesTest");

            ct.Scale = testValue;

            Assert.AreEqual(testValue, ct.Scale, $"Cache failed for scale {testValue}");
            Assert.AreEqual(testValue, ct.Self.localScale, $"Unity Transform failed for scale {testValue}");

            Object.DestroyImmediate(ct.Self.gameObject);
        }

        #endregion

        #region Refresh Synchronization Tests

        [Test]
        public void Refresh_UpdatesPosition([ValueSource(nameof(VectorTestCases))] Vector3 externalPos)
        {
            var ct = CallCreateFactory("RefreshPosTest");
            ct.Position = Vector3.zero;

            ct.Self.position = externalPos;
            ct.Refresh();

            Assert.AreEqual(externalPos, ct.Position, $"Refresh failed to sync position for value {externalPos}");

            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Refresh_UpdatesRotation([ValueSource(nameof(RotationTestCases))] Quaternion externalRot)
        {
            var ct = CallCreateFactory("RefreshRotTest");
            ct.Rotation = Quaternion.identity;

            ct.Self.rotation = externalRot;
            ct.Refresh();

            Assert.IsTrue(Quaternion.Dot(externalRot, ct.Rotation) > 0.999f, $"Refresh failed to sync rotation for {externalRot}");

            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void Refresh_UpdatesScale([ValueSource(nameof(VectorTestCases))] Vector3 externalScale)
        {
            var ct = CallCreateFactory("RefreshScaleTest");
            ct.Scale = Vector3.one;

            ct.Self.localScale = externalScale;
            ct.Refresh();

            Assert.AreEqual(externalScale, ct.Scale, $"Refresh failed to sync localScale for value {externalScale}");

            Object.DestroyImmediate(ct.Self.gameObject);
        }

        #endregion

        #region Component Management Tests

        [Test]
        public void AddComponent_AddsAndReturnsComponent()
        {
            var ct = CallCreateFactory("AddComponentTest");

            var addedComponent = ct.AddComponent<BoxCollider>();

            Assert.IsNotNull(addedComponent, "The returned component should not be null.");
            var componentOnGo = ct.Self.GetComponent<BoxCollider>();
            Assert.AreEqual(addedComponent, componentOnGo, "The component should be attached to the underlying GameObject.");

            Object.DestroyImmediate(ct.Self.gameObject);
        }

        [Test]
        public void AddComponent_AddsMultipleComponents()
        {
            var ct = CallCreateFactory("MultiComponentTest");

            var comp1 = ct.AddComponent<BoxCollider>();
            var comp2 = ct.AddComponent<BoxCollider>();

            Assert.AreNotEqual(comp1, comp2, "AddComponent should create a new instance each time.");
            Assert.AreEqual(2, ct.Self.GetComponents<BoxCollider>().Length, "The GameObject should have exactly two BoxColliders.");

            Object.DestroyImmediate(ct.Self.gameObject);
        }

        #endregion
    }
}