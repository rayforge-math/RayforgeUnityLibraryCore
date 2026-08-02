using NUnit.Framework;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Components.Tests
{
    [TestFixture]
    public class ComponentStateTests
    {
        #region Test Env

        private GameObject m_MeshTestObject;
        private MeshRenderer m_MeshRenderer;
        private MeshFilter m_MeshFilter;
        private Mesh m_TestMesh;

        private GameObject m_TerrainTestObject;
        private Terrain m_Terrain;
        private TerrainData m_TerrainData;

        [SetUp]
        public void SetUp()
        {
            // Setup MeshRenderer test objects
            m_MeshTestObject = new GameObject("TestMeshObject");
            m_MeshRenderer = m_MeshTestObject.AddComponent<MeshRenderer>();
            m_MeshFilter = m_MeshTestObject.AddComponent<MeshFilter>();

            m_TestMesh = new Mesh { name = "GranularTestMesh" };
            m_MeshFilter.sharedMesh = m_TestMesh;

            // Setup Terrain test objects
            m_TerrainTestObject = new GameObject("TestTerrainObject");
            m_Terrain = m_TerrainTestObject.AddComponent<Terrain>();

            m_TerrainData = new TerrainData { name = "GranularTestTerrainData" };
            m_Terrain.terrainData = m_TerrainData;
        }

        [TearDown]
        public void TearDown()
        {
            if (Application.isPlaying)
            {
                Object.Destroy(m_MeshTestObject);
                Object.Destroy(m_TestMesh);
                Object.Destroy(m_TerrainTestObject);
                Object.Destroy(m_TerrainData);
            }
            else
            {
                Object.DestroyImmediate(m_MeshTestObject);
                Object.DestroyImmediate(m_TestMesh);
                Object.DestroyImmediate(m_TerrainTestObject);
                Object.DestroyImmediate(m_TerrainData);
            }
        }

        #endregion

        #region Create MeshRenderer Tests

        [Test]
        public void Create_WithMeshRenderer_AssignsCorrectComponentReference()
        {
            var anchor = Vector3.zero;
            var state = ComponentState<MeshRenderer>.Create(anchor, m_MeshRenderer);

            Assert.AreEqual(m_MeshRenderer, state.component, "The component reference must match the passed MeshRenderer.");
        }

        [Test]
        public void Create_WithValidMesh_AssignsCorrectDataHash()
        {
            var anchor = Vector3.zero;
            int expectedHash = m_TestMesh.GetInstanceID();

            var state = ComponentState<MeshRenderer>.Create(anchor, m_MeshRenderer);

            Assert.AreEqual(expectedHash, state.dataHash, "The dataHash must correspond to the sharedMesh InstanceID.");
        }

        [Test]
        public void Create_WithNullMesh_AssignsZeroHash()
        {
            var anchor = Vector3.zero;
            m_MeshFilter.sharedMesh = null;

            var state = ComponentState<MeshRenderer>.Create(anchor, m_MeshRenderer);

            Assert.AreEqual(0, state.dataHash, "The dataHash must be 0 if no mesh or sharedMesh is present.");
        }

        [Test]
        public void Create_WithMeshRenderer_CalculatesCorrectAnchorBoundsCenter()
        {
            var anchor = new Vector3(10f, 5f, 2f);
            m_MeshTestObject.transform.position = new Vector3(12f, 5f, 4f);

            var expectedWorldBounds = m_MeshRenderer.bounds;
            var expectedCenter = expectedWorldBounds.center - anchor;

            var state = ComponentState<MeshRenderer>.Create(anchor, m_MeshRenderer);

            Assert.AreEqual(expectedCenter.x, state.anchorBounds.center.x, 0.0001f);
            Assert.AreEqual(expectedCenter.y, state.anchorBounds.center.y, 0.0001f);
            Assert.AreEqual(expectedCenter.z, state.anchorBounds.center.z, 0.0001f);
        }

        [Test]
        public void Create_WithMeshRenderer_CalculatesCorrectAnchorBoundsSize()
        {
            var anchor = Vector3.zero;
            var expectedSize = m_MeshRenderer.bounds.size;

            var state = ComponentState<MeshRenderer>.Create(anchor, m_MeshRenderer);

            Assert.AreEqual(expectedSize.x, state.anchorBounds.size.x, 0.0001f);
            Assert.AreEqual(expectedSize.y, state.anchorBounds.size.y, 0.0001f);
            Assert.AreEqual(expectedSize.z, state.anchorBounds.size.z, 0.0001f);
        }

        [Test]
        public void Create_WithMeshRenderer_CalculatesCorrectLocalToAnchorMatrix()
        {
            var anchor = new Vector3(5f, 0f, 5f);
            m_MeshTestObject.transform.position = new Vector3(8f, 0f, 8f);
            m_MeshTestObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            Matrix4x4 expectedWorldToAnchor = Matrix4x4.Translate(-anchor);
            Matrix4x4 expectedLocalToAnchor = expectedWorldToAnchor * m_MeshRenderer.transform.localToWorldMatrix;

            var state = ComponentState<MeshRenderer>.Create(anchor, m_MeshRenderer);

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    Assert.AreEqual(
                        expectedLocalToAnchor[row, col],
                        state.localToAnchor[row, col],
                        0.0001f,
                        $"Matrix mismatch at index [{row},{col}]"
                    );
                }
            }
        }

        [Test]
        public void Create_WithNullMeshRenderer_ThrowsArgumentNullException()
        {
            var anchor = Vector3.zero;
            MeshRenderer nullRenderer = null;

            Assert.Throws<System.ArgumentNullException>(() =>
                ComponentState<MeshRenderer>.Create(anchor, nullRenderer)
            );
        }

        [Test]
        public void Create_WithMissingMeshFilter_ThrowsInvalidOperationException()
        {
            var anchor = Vector3.zero;
            // Remove MeshFilter from the test object
            Object.DestroyImmediate(m_MeshFilter);

            Assert.Throws<System.InvalidOperationException>(() =>
                ComponentState<MeshRenderer>.Create(anchor, m_MeshRenderer)
            );
        }

        [Test]
        public void Create_WithRotatedMeshRenderer_CalculatesCorrectRotatedBoundsSize()
        {
            var anchor = Vector3.zero;

            var meshFilter = m_MeshTestObject.GetComponent<MeshFilter>();
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meshFilter.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(cube);

            m_MeshTestObject.transform.localScale = new Vector3(4f, 1f, 1f);
            m_MeshTestObject.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            var expectedSize = m_MeshRenderer.bounds.size;

            var state = ComponentState<MeshRenderer>.Create(anchor, m_MeshRenderer);

            Assert.AreEqual(expectedSize.x, state.anchorBounds.size.x, 0.0001f);
            Assert.AreEqual(expectedSize.y, state.anchorBounds.size.y, 0.0001f);
            Assert.AreEqual(expectedSize.z, state.anchorBounds.size.z, 0.0001f);
        }

        [Test]
        public void Create_WithRotatedAndTranslatedObject_CalculatesCorrectLocalToAnchorMatrix()
        {
            var anchor = new Vector3(10f, 0f, 10f);
            m_MeshTestObject.transform.position = new Vector3(20f, 5f, 20f);
            m_MeshTestObject.transform.rotation = Quaternion.Euler(30f, 60f, 90f);
            m_MeshTestObject.transform.localScale = new Vector3(2f, 2f, 2f);

            Matrix4x4 expectedWorldToAnchor = Matrix4x4.Translate(-anchor);
            Matrix4x4 expectedLocalToAnchor = expectedWorldToAnchor * m_MeshRenderer.transform.localToWorldMatrix;

            var state = ComponentState<MeshRenderer>.Create(anchor, m_MeshRenderer);

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    Assert.AreEqual(
                        expectedLocalToAnchor[row, col],
                        state.localToAnchor[row, col],
                        0.0001f,
                        $"Rotated matrix mismatch at index [{row},{col}]"
                    );
                }
            }
        }

        #endregion

        #region Create Terrain Tests

        [Test]
        public void Create_WithTerrain_AssignsCorrectComponentReference()
        {
            var anchor = Vector3.zero;
            var state = ComponentState<Terrain>.Create(anchor, m_Terrain);

            Assert.AreEqual(m_Terrain, state.component, "The component reference must match the passed Terrain.");
        }

        [Test]
        public void Create_WithTerrain_AssignsCorrectDataHash()
        {
            var anchor = Vector3.zero;
            int expectedHash = m_TerrainData.GetInstanceID();

            var state = ComponentState<Terrain>.Create(anchor, m_Terrain);

            Assert.AreEqual(expectedHash, state.dataHash, "The dataHash must correspond to the terrainData InstanceID.");
        }

        [Test]
        public void Create_WithTerrain_CalculatesCorrectAnchorBoundsCenter()
        {
            var anchor = new Vector3(5f, 0f, 5f);
            m_TerrainTestObject.transform.position = new Vector3(10f, 2f, 10f);

            Vector3 size = m_TerrainData.size;
            Bounds expectedWorldBounds = new Bounds(m_TerrainTestObject.transform.position + size * 0.5f, size);
            Vector3 expectedCenter = expectedWorldBounds.center - anchor;

            var state = ComponentState<Terrain>.Create(anchor, m_Terrain);

            Assert.AreEqual(expectedCenter.x, state.anchorBounds.center.x, 0.0001f);
            Assert.AreEqual(expectedCenter.y, state.anchorBounds.center.y, 0.0001f);
            Assert.AreEqual(expectedCenter.z, state.anchorBounds.center.z, 0.0001f);
        }

        [Test]
        public void Create_WithTerrain_CalculatesCorrectAnchorBoundsSize()
        {
            var anchor = Vector3.zero;
            m_TerrainData.size = new Vector3(100f, 50f, 100f);
            var expectedSize = m_TerrainData.size;

            var state = ComponentState<Terrain>.Create(anchor, m_Terrain);

            Assert.AreEqual(expectedSize.x, state.anchorBounds.size.x, 0.0001f);
            Assert.AreEqual(expectedSize.y, state.anchorBounds.size.y, 0.0001f);
            Assert.AreEqual(expectedSize.z, state.anchorBounds.size.z, 0.0001f);
        }

        [Test]
        public void Create_WithTerrain_CalculatesCorrectLocalToAnchorMatrix()
        {
            var anchor = new Vector3(10f, 0f, 10f);
            m_TerrainTestObject.transform.position = new Vector3(15f, 0f, 15f);

            Matrix4x4 expectedWorldToAnchor = Matrix4x4.Translate(-anchor);
            Matrix4x4 expectedLocalToAnchor = expectedWorldToAnchor * m_Terrain.transform.localToWorldMatrix;

            var state = ComponentState<Terrain>.Create(anchor, m_Terrain);

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    Assert.AreEqual(
                        expectedLocalToAnchor[row, col],
                        state.localToAnchor[row, col],
                        0.0001f,
                        $"Matrix mismatch at index [{row},{col}]"
                    );
                }
            }
        }

        [Test]
        public void Create_WithNullTerrain_ThrowsArgumentNullException()
        {
            var anchor = Vector3.zero;
            Terrain nullTerrain = null;

            Assert.Throws<System.ArgumentNullException>(() =>
                ComponentState<Terrain>.Create(anchor, nullTerrain)
            );
        }

        [Test]
        public void Create_WithMissingTerrainData_ThrowsInvalidOperationException()
        {
            var anchor = Vector3.zero;
            // Remove TerrainData from the terrain component
            m_Terrain.terrainData = null;

            Assert.Throws<System.InvalidOperationException>(() =>
                ComponentState<Terrain>.Create(anchor, m_Terrain)
            );
        }

        #endregion

        #region Equality Tests

        [Test]
        public void Equals_WithIdenticalStates_ReturnsTrue()
        {
            var state1 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                localToAnchor = Matrix4x4.identity,
                component = m_MeshRenderer,
                dataHash = 12345
            };

            var state2 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                localToAnchor = Matrix4x4.identity,
                component = m_MeshRenderer,
                dataHash = 12345
            };

            Assert.IsTrue(state1.Equals(state2));
            Assert.IsTrue(state1 == state2);
            Assert.IsFalse(state1 != state2);
        }

        [Test]
        public void Equals_WithDifferentDataHash_ReturnsFalse()
        {
            var state1 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                localToAnchor = Matrix4x4.identity,
                component = m_MeshRenderer,
                dataHash = 12345
            };

            var state2 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                localToAnchor = Matrix4x4.identity,
                component = m_MeshRenderer,
                dataHash = 99999
            };

            Assert.IsFalse(state1.Equals(state2));
            Assert.IsFalse(state1 == state2);
            Assert.IsTrue(state1 != state2);
        }

        [Test]
        public void Equals_WithDifferentComponent_ReturnsFalse()
        {
            var otherObject = new GameObject("OtherObject");
            var otherRenderer = otherObject.AddComponent<MeshRenderer>();

            try
            {
                var state1 = new ComponentState<MeshRenderer>
                {
                    anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                    localToAnchor = Matrix4x4.identity,
                    component = m_MeshRenderer,
                    dataHash = 12345
                };

                var state2 = new ComponentState<MeshRenderer>
                {
                    anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                    localToAnchor = Matrix4x4.identity,
                    component = otherRenderer,
                    dataHash = 12345
                };

                Assert.IsFalse(state1.Equals(state2));
                Assert.IsFalse(state1 == state2);
                Assert.IsTrue(state1 != state2);
            }
            finally
            {
                if (Application.isPlaying) Object.Destroy(otherObject);
                else Object.DestroyImmediate(otherObject);
            }
        }

        [Test]
        public void Equals_WithDifferentMatrix_ReturnsFalse()
        {
            var state1 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                localToAnchor = Matrix4x4.identity,
                component = m_MeshRenderer,
                dataHash = 12345
            };

            var state2 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                localToAnchor = Matrix4x4.Translate(Vector3.one),
                component = m_MeshRenderer,
                dataHash = 12345
            };

            Assert.IsFalse(state1.Equals(state2));
            Assert.IsFalse(state1 == state2);
            Assert.IsTrue(state1 != state2);
        }

        [Test]
        public void Equals_WithDifferentBounds_ReturnsFalse()
        {
            var state1 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                localToAnchor = Matrix4x4.identity,
                component = m_MeshRenderer,
                dataHash = 12345
            };

            var state2 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.one, Vector3.one),
                localToAnchor = Matrix4x4.identity,
                component = m_MeshRenderer,
                dataHash = 12345
            };

            Assert.IsFalse(state1.Equals(state2));
            Assert.IsFalse(state1 == state2);
            Assert.IsTrue(state1 != state2);
        }

        [Test]
        public void Equals_WithObjectType_ReturnsTrueForEqualObject()
        {
            var state1 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                localToAnchor = Matrix4x4.identity,
                component = m_MeshRenderer,
                dataHash = 12345
            };

            object state2 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                localToAnchor = Matrix4x4.identity,
                component = m_MeshRenderer,
                dataHash = 12345
            };

            Assert.IsTrue(state1.Equals(state2));
        }

        [Test]
        public void GetHashCode_EqualStates_ReturnSameHashCode()
        {
            var state1 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                localToAnchor = Matrix4x4.identity,
                component = m_MeshRenderer,
                dataHash = 12345
            };

            var state2 = new ComponentState<MeshRenderer>
            {
                anchorBounds = new Bounds(Vector3.zero, Vector3.one),
                localToAnchor = Matrix4x4.identity,
                component = m_MeshRenderer,
                dataHash = 12345
            };

            Assert.AreEqual(state1.GetHashCode(), state2.GetHashCode());
        }

        #endregion
    }
}
