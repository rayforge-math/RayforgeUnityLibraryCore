using NUnit.Framework;
using Rayforge.Core.Common.Rendering;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering.Tests
{
    [TestFixture]
    public class SphereAtlasMapperTests
    {
        #region CreateSpatialEntry Tests

        [Test]
        public void SphereAtlasMapper_WhenRequestingTile_CalculatesCorrectSpatialBounds()
        {
            // Arrange
            var mapper = new SphereAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            int key = 42;
            Vector3 worldPos = new Vector3(10f, 20f, 30f);
            float extent = 4f; // Half-extent should be 2f, radius = 2 * sqrt(3)

            // Act
            mapper.RequestTile(key, lodIndex: 0, worldPos, extent);
            mapper.FlushTileRequests();

            // Assert: Verify that the sphere spatial entry was calculated correctly
            bool found = mapper.Registry.TryGetCulling(key, out var spatialData);

            Assert.IsTrue(found, "Spatial entry should exist in the registry for the requested tile.");

            // Expected Position: worldPos (10, 20, 30)
            Assert.AreEqual(worldPos.x, spatialData.Position.x);
            Assert.AreEqual(worldPos.y, spatialData.Position.y);
            Assert.AreEqual(worldPos.z, spatialData.Position.z);

            // Expected Radius: halfExtent * Sqrt(3) -> 2 * Sqrt(3)
            float expectedRadius = (extent * 0.5f) * Mathf.Sqrt(3f);
            Assert.AreEqual(expectedRadius, spatialData.Radius, 0.0001f);
        }

        [Test]
        public void SphereAtlasMapper_WithZeroExtent_CalculatesZeroRadius()
        {
            // Arrange
            var mapper = new SphereAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            int key = 100;
            Vector3 worldPos = new Vector3(5f, 5f, 5f);
            float extent = 0f; // Half-extent should be 0f, radius should be 0f

            // Act
            mapper.RequestTile(key, lodIndex: 0, worldPos, extent);
            mapper.FlushTileRequests();

            // Assert
            bool found = mapper.Registry.TryGetCulling(key, out var spatialData);

            Assert.IsTrue(found, "Spatial entry should exist in the registry for the requested tile.");
            Assert.AreEqual(worldPos.x, spatialData.Position.x);
            Assert.AreEqual(worldPos.y, spatialData.Position.y);
            Assert.AreEqual(worldPos.z, spatialData.Position.z);
            Assert.AreEqual(0f, spatialData.Radius, 0.0001f);
        }

        #endregion
    }
}
