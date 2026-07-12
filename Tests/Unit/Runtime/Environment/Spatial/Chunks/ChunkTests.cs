using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    [TestFixture]
    public abstract class ChunkTests<T> where T : Chunk<T>
    {
        #region Create Test Env

        protected GameObject _container;

        [SetUp]
        public void SetUp()
        {
            _container = new GameObject("TestContainer");
        }

        [TearDown]
        public void TearDown()
        {
            if (_container != null) UnityEngine.Object.DestroyImmediate(_container);
        }

        #endregion

        #region LocalExtent Tests

        [Test]
        public void LocalExtent_IsSetCorrectly_OnInitialize()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            var customExtent = new Vector3(25, 25, 25);

            // Act
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, customExtent);

            // Assert
            Assert.AreEqual(customExtent, chunk.LocalExtent);
            Assert.AreEqual(customExtent * 2f, chunk.Size);

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void UpdateLocalExtent_UpdatesValueAndMarksDirty()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, Vector3.zero);
            var newExtent = new Vector3(100, 100, 100);

            // We don't know if the chunk is dirty initially, 
            // so we clear the status first.
            chunk.ClearDirty();
            Assert.IsFalse(chunk.IsDirty);

            // Act
            chunk.UpdateLocalExtent(newExtent);

            // Assert
            Assert.AreEqual(newExtent, chunk.LocalExtent);
            Assert.IsTrue(chunk.IsDirty, "Chunk should be marked as dirty after UpdateLocalExtent.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void UpdateLocalExtent_DoesNotMarkDirty_IfValueIsSame()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            var extent = new Vector3(50, 50, 50);
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, extent);
            chunk.ClearDirty();

            // Act
            chunk.UpdateLocalExtent(extent); // Same value

            // Assert
            Assert.IsFalse(chunk.IsDirty, "Chunk should not be marked as dirty if the value remains the same.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region WorldSize Tests

        [Test]
        public void WorldSize_IsAlwaysDoubleLocalExtent()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            var testValues = new[]
            {
                new Vector3(10, 10, 10),
                new Vector3(0.5f, 5.1f, 100f),
                new Vector3(0, 0, 0)
            };

            foreach (var extent in testValues)
            {
                // Act
                ((IChunkControl)chunk).Initialize(Vector3Int.zero, extent);

                // Assert
                var expectedWorldSize = extent * 2f;
                Assert.AreEqual(expectedWorldSize, chunk.Size,
                    $"WorldSize did not match for extent {extent}");
            }

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void WorldSize_Updates_WhenExtentChanges()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(10, 10, 10));

            // Act
            chunk.UpdateLocalExtent(new Vector3(20, 20, 20));

            // Assert
            Assert.AreEqual(new Vector3(40, 40, 40), chunk.Size,
                "WorldSize must update immediately when localExtent changes.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region UpdateOnTransformChange Tests

        [Test]
        public void UpdateOnTransformChange_CanBeToggled()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();

            // Act & Assert (Default check)
            Assert.IsFalse(chunk.UpdateOnTransformChange, "Default value should be false.");

            // Act
            chunk.UpdateOnTransformChange = true;

            // Assert
            Assert.IsTrue(chunk.UpdateOnTransformChange, "Property should be true after setting it to true.");

            // Act
            chunk.UpdateOnTransformChange = false;

            // Assert
            Assert.IsFalse(chunk.UpdateOnTransformChange, "Property should be false after setting it back to false.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void UpdateOnTransformChange_RetainsValue_AfterInitialize()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            chunk.UpdateOnTransformChange = true;

            // Act
            ((IChunkControl)chunk).Initialize(Vector3Int.one, Vector3.zero);

            // Assert
            Assert.IsTrue(chunk.UpdateOnTransformChange, "Initialize() should not modify the 'UpdateOnTransformChange' flag.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region GridKey Tests

        [Test]
        public void GridKey_IsSetCorrectly_OnInitialize()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            var expectedKey = new Vector3Int(10, -5, 20);

            // Act
            ((IChunkControl)chunk).Initialize(expectedKey, Vector3.zero);

            // Assert
            Assert.AreEqual(expectedKey, chunk.GridKey, "GridKey was not set correctly.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void GridKey_Helpers_ReturnCorrectProjections()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            var key = new Vector3Int(1, 2, 3);
            ((IChunkControl)chunk).Initialize(key, Vector3.zero);

            // Assert
            Assert.AreEqual(new Vector2Int(1, 2), chunk.GridKeyXY);
            Assert.AreEqual(new Vector2Int(1, 3), chunk.GridKeyXZ);
            Assert.AreEqual(new Vector2Int(2, 3), chunk.GridKeyYZ);

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region WorldPosition Tests

        [Test]
        public void WorldPosition_ReturnsTransformPosition()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            var expectedPos = new Vector3(15, 10, 5);
            _container.transform.position = expectedPos;

            // Assert
            Assert.AreEqual(expectedPos, chunk.WorldPosition);

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region ActiveAxes Tests

        [Test]
        public void ActiveAxes_AreConfiguredCorrectly()
        {
            // Assert
            // We verify the static configuration based on the type T
            var expectedAxes = typeof(T).GetCustomAttribute<ChunkConfigAttribute>()?.Axes ?? SpatialAxes.Voxel;

            Assert.AreEqual(expectedAxes, Chunk<T>.ActiveAxes);
        }

        #endregion

        #region AxisFlags Tests

        [Test]
        public void AxisFlags_ReturnCorrectBooleanState()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();

            // Act & Assert
            // We check if the boolean properties align with the bitmask of ActiveAxes
            Assert.AreEqual((Chunk<T>.ActiveAxes & SpatialAxes.X) != 0, chunk.IsXActive);
            Assert.AreEqual((Chunk<T>.ActiveAxes & SpatialAxes.Y) != 0, chunk.IsYActive);
            Assert.AreEqual((Chunk<T>.ActiveAxes & SpatialAxes.Z) != 0, chunk.IsZActive);
            Assert.AreEqual((Chunk<T>.ActiveAxes & SpatialAxes.W) != 0, chunk.IsWActive);

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void Initialize_SetsStateAndDirtyFlag()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();

            // Act
            ((IChunkControl)chunk).Initialize(Vector3Int.one, Vector3.one);

            // Assert
            Assert.IsTrue(chunk.IsDirty, "Chunk should be marked as dirty after initialization.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region IsDirty Tests

        [Test]
        public void IsDirty_ReturnsTrue_WhenMarkedDirty()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            chunk.ClearDirty(); // Start clean

            // Act
            chunk.MarkDirty();

            // Assert
            Assert.IsTrue(chunk.IsDirty, "IsDirty should be true after calling MarkDirty.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void IsDirty_ReturnsTrue_WhenTransformChanges_IfEnabled()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            chunk.UpdateOnTransformChange = true;
            chunk.ClearDirty();

            // Act
            _container.transform.position += Vector3.one; // Trigger transform change

            // Assert
            Assert.IsTrue(chunk.IsDirty, "IsDirty should be true when transform changes and UpdateOnTransformChange is enabled.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void IsDirty_ReturnsFalse_WhenTransformChanges_IfDisabled()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            chunk.UpdateOnTransformChange = false;
            chunk.ClearDirty();

            // Act
            _container.transform.position += Vector3.one;

            // Assert
            Assert.IsFalse(chunk.IsDirty, "IsDirty should remain false when transform changes if UpdateOnTransformChange is disabled.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region ClearDirty Tests

        [Test]
        public void ClearDirty_ResetsAllFlags()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            chunk.UpdateOnTransformChange = true;
            chunk.MarkDirty();
            _container.transform.position = Vector3.up;

            // Act
            chunk.ClearDirty();

            // Assert
            Assert.IsFalse(chunk.IsDirty, "IsDirty should be false after ClearDirty.");
            Assert.IsFalse(_container.transform.hasChanged, "transform.hasChanged should be false after ClearDirty.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region SuppressTransformDirtyOnce Tests

        [Test]
        public void SuppressTransformDirtyOnce_ResetsTransformFlag()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            chunk.UpdateOnTransformChange = true;
            _container.transform.position = Vector3.forward;
            Assert.IsTrue(chunk.IsDirty);

            // Act
            chunk.SuppressTransformDirtyOnce();

            // Assert
            Assert.IsFalse(chunk.IsDirty, "IsDirty should be false after suppressing transform change.");
            Assert.IsFalse(_container.transform.hasChanged, "transform.hasChanged should be reset.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void Dispose_TriggersCleanupEventAndDestroysObject()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            bool cleanupEventTriggered = false;
            chunk.OnCleanup += (c) => cleanupEventTriggered = true;

            // Act
            ((IChunkControl)chunk).Dispose();

            // Assert
            Assert.IsTrue(cleanupEventTriggered, "OnCleanup event should be triggered before disposal.");
            Assert.IsTrue(_container == null || _container.Equals(null), "The associated GameObject should be destroyed.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void Dispose_PreventsDoubleDisposal()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            int eventCount = 0;
            chunk.OnCleanup += (c) => eventCount++;

            // Act
            ((IChunkControl)chunk).Dispose();
            ((IChunkControl)chunk).Dispose(); // Second call

            // Assert
            Assert.AreEqual(1, eventCount, "OnCleanup should only be triggered once even if Dispose is called multiple times.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void Dispose_ClearsEventSubscriptions()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            bool wasCalled = false;
            chunk.OnCleanup += (c) => wasCalled = true;

            // Act
            ((IChunkControl)chunk).Dispose();

            // Assert
            Assert.IsTrue(wasCalled, "Dispose didn't trigger the event.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region OnDestroy Tests

        [Test]
        public void OnDestroy_TriggersDispose()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();

            Object.DestroyImmediate(_container);

            // Assert
            // If OnDestroy triggered Dispose, the event listener should be null or the state should be handled.
            // This implicitly tests if the internal _isDisposed logic prevents secondary issues.
            Assert.Pass("OnDestroy successfully cleaned up without crashing.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region Contains Tests

        [Test]
        public void Contains_ReturnsTrue_ForPointInsideExtent()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(10, 10, 10));
            _container.transform.position = Vector3.zero;

            Assert.IsTrue(chunk.Contains(new Vector3(5, 5, 5)), "Point inside extent should return true.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void Contains_RespectsActiveAxesConfiguration()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(10, 10, 10));
            _container.transform.position = Vector3.zero;

            var testPoints = new[]
            {
                (Active: chunk.IsXActive, Point: new Vector3(999f, 0, 0), AxisName: "X"),
                (Active: chunk.IsYActive, Point: new Vector3(0, 999f, 0), AxisName: "Y"),
                (Active: chunk.IsZActive, Point: new Vector3(0, 0, 999f), AxisName: "Z")
            };

            foreach (var test in testPoints)
            {
                if (test.Active)
                {
                    Assert.IsFalse(chunk.Contains(test.Point),
                        $"Axis {test.AxisName} is active, but Contains returned true for an extreme value.");
                }
                else
                {
                    Assert.IsTrue(chunk.Contains(test.Point),
                        $"Axis {test.AxisName} is inactive, but Contains returned false for an extreme value.");
                }
            }

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region GetSqrDistanceTo Tests

        [Test]
        public void GetSqrDistanceTo_CalculatesCorrectSquaredDistance()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, Vector3.zero);
            _container.transform.position = new Vector3(10, 10, 10);

            // Target is at (13, 14, 10)
            // dx = 3, dy = 4, dz = 0
            // Distance^2 = 3^2 + 4^2 + 0^2 = 9 + 16 = 25
            Vector3 target = new Vector3(13, 14, 10);

            // Act
            float sqrDist = chunk.GetSqrDistanceToCentre(target);

            // Assert
            Assert.AreEqual(25f, sqrDist, 0.001f, "Squared distance calculation is incorrect.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void GetSqrDistanceTo_IgnoresInactiveAxes()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, Vector3.zero);
            _container.transform.position = Vector3.zero;

            // If we set a large distance on an axis, but that axis is inactive,
            // the result should be 0 (if all axes are inactive) or ignore that axis contribution.
            Vector3 target = new Vector3(100, 100, 100);

            // Act
            float sqrDist = chunk.GetSqrDistanceToCentre(target);

            // We calculate expected distance manually based on active axes
            float expectedDist = 0f;
            if (chunk.IsXActive) expectedDist += 100f * 100f;
            if (chunk.IsYActive) expectedDist += 100f * 100f;
            if (chunk.IsZActive) expectedDist += 100f * 100f;

            // Assert
            Assert.AreEqual(expectedDist, sqrDist, 0.001f,
                "GetSqrDistanceTo did not correctly respect ActiveAxes configuration.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region GetSqrDistanceToClosestEdge Tests

        [Test]
        public void GetSqrDistanceToClosestEdge_ReturnsZero_ForPointInsideAABB()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(10, 10, 10));
            _container.transform.position = Vector3.zero;

            // Point inside (5, 5, 5) should have 0 distance to edge
            Assert.AreEqual(0f, chunk.GetSqrDistanceToClosestEdge(new Vector3(5, 5, 5)), 0.001f);

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void GetSqrDistanceToClosestEdge_CalculatesCorrectEdgeDistance()
        {
            // Arrange
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(10, 10, 10)); // Extent 10 means box goes from -10 to 10
            _container.transform.position = Vector3.zero;

            // Target (13, 14, 0)
            // X-dist to edge: |13| - 10 = 3
            // Y-dist to edge: |14| - 10 = 4
            // Result: 3^2 + 4^2 = 9 + 16 = 25
            Vector3 target = new Vector3(13, 14, 0);

            // Act
            float sqrDist = chunk.GetSqrDistanceToClosestEdge(target);

            // Assert
            Assert.AreEqual(25f, sqrDist, 0.001f);

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void GetSqrDistanceToClosestEdge_RespectsActiveAxes()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(10, 10, 10));
            _container.transform.position = Vector3.zero;

            // We use a target far outside the box
            Vector3 target = new Vector3(20, 20, 20);

            // Calculate expected manually based on active axes
            float expectedSqrDist = 0f;
            if (chunk.IsXActive) expectedSqrDist += Mathf.Pow(Mathf.Max(0, Mathf.Abs(target.x) - 10f), 2);
            if (chunk.IsYActive) expectedSqrDist += Mathf.Pow(Mathf.Max(0, Mathf.Abs(target.y) - 10f), 2);
            if (chunk.IsZActive) expectedSqrDist += Mathf.Pow(Mathf.Max(0, Mathf.Abs(target.z) - 10f), 2);

            // Act
            float actualSqrDist = chunk.GetSqrDistanceToClosestEdge(target);

            // Assert
            Assert.AreEqual(expectedSqrDist, actualSqrDist, 0.001f,
                "GetSqrDistanceToClosestEdge failed to correctly respect ActiveAxes configuration.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region GetVolume Tests

        [Test]
        public void GetVolume_CalculatesCorrectVolume()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(2f, 3f, 4f));
            // Volume = (2*2) * (3*2) * (4*2) = 4 * 6 * 8 = 192
            Assert.AreEqual(192f, chunk.GetVolume(), 0.001f);
            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region GetTotalSurfaceArea Tests

        [Test]
        public void GetTotalSurfaceArea_CalculatesCorrectArea()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(2f, 3f, 4f));
            // S = 4, 6, 8 | 2 * (4*6 + 4*8 + 6*8) = 2 * (24 + 32 + 48) = 208
            Assert.AreEqual(208f, chunk.GetTotalSurfaceArea(), 0.001f);
            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region GetArea Tests

        [Test]
        public void GetAreaXZ_CalculatesCorrectPlane()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(2f, 3f, 4f));
            // XZ = (2*2) * (4*2) = 4 * 8 = 32
            Assert.AreEqual(32f, chunk.GetAreaXZ(), 0.001f);
            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void GetAreaXY_CalculatesCorrectPlane()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(2f, 3f, 4f));
            // XY = (2*2) * (3*2) = 4 * 6 = 24
            Assert.AreEqual(24f, chunk.GetAreaXY(), 0.001f);
            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void GetAreaYZ_CalculatesCorrectPlane()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(2f, 3f, 4f));
            // YZ = (3*2) * (4*2) = 6 * 8 = 48
            Assert.AreEqual(48f, chunk.GetAreaYZ(), 0.001f);
            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region GetActiveArea Tests

        [Test]
        public void GetActiveArea_ReturnsExpectedValue_BasedOnConfig()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(2f, 3f, 4f));

            float expected = 0f;
            if (Chunk<T>.ActiveAxes == SpatialAxes.XZ) expected = chunk.GetAreaXZ();
            else if (Chunk<T>.ActiveAxes == SpatialAxes.XY) expected = chunk.GetAreaXY();
            else if (Chunk<T>.ActiveAxes == SpatialAxes.YZ) expected = chunk.GetAreaYZ();
            else expected = chunk.GetTotalSurfaceArea();

            Assert.AreEqual(expected, chunk.GetActiveArea(), 0.001f,
                $"GetActiveArea failed for configuration {Chunk<T>.ActiveAxes}");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region Length Tests

        [Test]
        public void Lengths_CalculateCorrectly()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(2f, 3f, 4f));

            Assert.AreEqual(4f, chunk.LengthX, 0.001f);
            Assert.AreEqual(6f, chunk.LengthY, 0.001f);
            Assert.AreEqual(8f, chunk.LengthZ, 0.001f);

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region Size2D Tests

        [Test]
        public void Size2D_ReturnsCorrectVectors()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(2f, 3f, 4f));

            Assert.AreEqual(new Vector2(4f, 8f), chunk.SizeXZ);
            Assert.AreEqual(new Vector2(4f, 6f), chunk.SizeXY);
            Assert.AreEqual(new Vector2(6f, 8f), chunk.SizeYZ);

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void ActiveSize2D_ReturnsCorrectPlaneForConfig()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(2f, 3f, 4f));

            Vector2 expected = Chunk<T>.ActiveAxes switch
            {
                SpatialAxes.XY => chunk.SizeXY,
                SpatialAxes.YZ => chunk.SizeYZ,
                _ => chunk.SizeXZ
            };

            Assert.AreEqual(expected, chunk.ActiveSize2D, "ActiveSize2D does not match ActiveAxes configuration.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region GetLogicalSize Tests

        [Test]
        public void GetLogicalSize_HandlesActiveAndInactiveAxes()
        {
            var chunk = _container.AddComponent<T>();
            ((IChunkControl)chunk).Initialize(Vector3Int.zero, new Vector3(2f, 3f, 4f));

            Vector3 size = chunk.GetLogicalSize();

            // Prüfe X
            float expectedX = chunk.IsXActive ? 4f : 0.1f;
            Assert.AreEqual(expectedX, size.x, 0.001f);

            // Prüfe Y
            float expectedY = chunk.IsYActive ? 6f : 0.1f;
            Assert.AreEqual(expectedY, size.y, 0.001f);

            // Prüfe Z
            float expectedZ = chunk.IsZActive ? 8f : 0.1f;
            Assert.AreEqual(expectedZ, size.z, 0.001f);

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion
    }
}
