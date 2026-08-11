using NUnit.Framework;
using Rayforge.Core.Environment.Spatial.Chunks.Tests;
using Rayforge.Core.Rendering.Abstractions;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering.Tests
{
    [TestFixture]
    public class TextureChunkTests : LODChunkTests<TextureChunk>
    {
        #region TextureMappingData Tests

        [Test]
        public void SetTextureMapping_AssignsMappingAndUpdatesHasMapping()
        {
            // Arrange
            var chunk = _container.AddComponent<TextureChunk>();
            var mappingData = new TextureMappingData { SliceIndex = 1, RelativeScale = 1.0f };

            // Act
            chunk.SetTextureMapping(mappingData);

            // Assert
            Assert.IsTrue(chunk.HasMapping, "HasMapping should be true when SliceIndex is valid.");
            Assert.AreEqual(1, chunk.Mapping.SliceIndex, "SliceIndex should match the assigned data.");
            Assert.AreEqual(1.0f, chunk.Mapping.RelativeScale, 0.001f, "RelativeScale should match the assigned data.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void ClearMapping_ResetsMappingAndHasMappingToFalse()
        {
            // Arrange
            var chunk = _container.AddComponent<TextureChunk>();
            var mappingData = new TextureMappingData { SliceIndex = 0 };
            chunk.SetTextureMapping(mappingData);

            Assert.IsTrue(chunk.HasMapping, "Precondition: Mapping should be active.");

            // Act
            chunk.ClearMapping();

            // Assert
            Assert.IsFalse(chunk.HasMapping, "HasMapping should be false after clearing the mapping.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion
    }
}
