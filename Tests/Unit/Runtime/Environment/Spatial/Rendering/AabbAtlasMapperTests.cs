using NUnit.Framework;
using Rayforge.Core.Common.Rendering;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering.Tests
{
    [TestFixture]
    public class AabbAtlasMapperTests
    {
        #region CreateSpatialEntry Tests

        [Test]
        public void AabbAtlasMapper_WhenRequestingTile_CalculatesCorrectSpatialBounds()
        {
            // Arrange
            var mapper = new AabbAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            int key = 42;
            Vector3 worldPos = new Vector3(10f, 20f, 30f);
            float extent = 4f; // Half-extent should be 2f

            // Act
            mapper.RequestTile(key, lodIndex: 0, worldPos, extent);
            mapper.FlushTileRequests();

            // Assert: Verify that the AABB spatial entry was calculated correctly
            bool found = mapper.Registry.TryGetCulling(key, out var spatialData);

            Assert.IsTrue(found, "Spatial entry should exist in the registry for the requested tile.");

            // Expected MinBounds: worldPos - halfExtent (10-2, 20-2, 30-2) = (8, 18, 28)
            Assert.AreEqual(8f, spatialData.MinBounds.x);
            Assert.AreEqual(18f, spatialData.MinBounds.y);
            Assert.AreEqual(28f, spatialData.MinBounds.z);

            // Expected MaxBounds: worldPos + halfExtent (10+2, 20+2, 30+2) = (12, 22, 32)
            Assert.AreEqual(12f, spatialData.MaxBounds.x);
            Assert.AreEqual(22f, spatialData.MaxBounds.y);
            Assert.AreEqual(32f, spatialData.MaxBounds.z);
        }

        [Test]
        public void AabbAtlasMapper_WithZeroExtent_CalculatesExactWorldPositionBounds()
        {
            // Arrange
            var mapper = new AabbAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            int key = 100;
            Vector3 worldPos = new Vector3(5f, 5f, 5f);
            float extent = 0f; // Half-extent should be 0f

            // Act
            mapper.RequestTile(key, lodIndex: 0, worldPos, extent);
            mapper.FlushTileRequests();

            // Assert
            bool found = mapper.Registry.TryGetCulling(key, out var spatialData);

            Assert.IsTrue(found, "Spatial entry should exist in the registry for the requested tile.");
            Assert.AreEqual(worldPos.x, spatialData.MinBounds.x);
            Assert.AreEqual(worldPos.y, spatialData.MinBounds.y);
            Assert.AreEqual(worldPos.z, spatialData.MinBounds.z);
            Assert.AreEqual(worldPos.x, spatialData.MaxBounds.x);
            Assert.AreEqual(worldPos.y, spatialData.MaxBounds.y);
            Assert.AreEqual(worldPos.z, spatialData.MaxBounds.z);
        }

        #endregion
    }
}
