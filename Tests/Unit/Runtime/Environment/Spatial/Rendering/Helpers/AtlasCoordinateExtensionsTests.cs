using NUnit.Framework;
using Rayforge.Core.Rendering.Abstractions;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering.Helpers.Tests
{
    [TestFixture]
    public class AtlasCoordinateExtensionsTests
    {
        [Test]
        public void ToViewportRect_WithStandardValues_CalculatesExpectedRect()
        {
            // Arrange
            var mapping = new TextureMappingData
            {
                SliceIndex = 2,
                RelativeScale = 0.5f,
                RelativeOffset = new Vector2(0.25f, 0.1f)
            };
            int atlasResolution = 1024;

            // Act
            Rect result = mapping.ToViewportRect(atlasResolution);

            // Assert
            float expectedX = 0.25f * 1024f; // 256f
            float expectedY = 0.1f * 1024f;  // 102.4f
            float expectedSize = 1024f * 0.5f; // 512f

            Assert.AreEqual(expectedX, result.x, 0.001f, "Viewport Rect X coordinate is incorrect.");
            Assert.AreEqual(expectedY, result.y, 0.001f, "Viewport Rect Y coordinate is incorrect.");
            Assert.AreEqual(expectedSize, result.width, 0.001f, "Viewport Rect width is incorrect.");
            Assert.AreEqual(expectedSize, result.height, 0.001f, "Viewport Rect height is incorrect.");
        }

        [Test]
        public void ToViewportRect_WithFullScaleAndZeroOffset_MatchesResolution()
        {
            // Arrange
            var mapping = new TextureMappingData
            {
                SliceIndex = 0,
                RelativeScale = 1.0f,
                RelativeOffset = Vector2.zero
            };
            int atlasResolution = 2048;

            // Act
            Rect result = mapping.ToViewportRect(atlasResolution);

            // Assert
            Assert.AreEqual(0f, result.x, "X should be zero.");
            Assert.AreEqual(0f, result.y, "Y should be zero.");
            Assert.AreEqual(2048f, result.width, "Width should match full atlas resolution.");
            Assert.AreEqual(2048f, result.height, "Height should match full atlas resolution.");
        }

        [Test]
        public void ToSlotView_MapsSliceIndexAndViewportRectCorrectly()
        {
            // Arrange
            var mapping = new TextureMappingData
            {
                SliceIndex = 5,
                RelativeScale = 0.25f,
                RelativeOffset = new Vector2(0.5f, 0.5f)
            };
            int atlasResolution = 512;

            // Act
            AtlasSlotView slotView = mapping.ToSlotView(atlasResolution);

            // Assert
            Assert.AreEqual(5, slotView.SliceIndex, "SliceIndex was not mapped correctly.");

            float expectedX = 0.5f * 512f; // 256f
            float expectedY = 0.5f * 512f; // 256f
            float expectedSize = 512f * 0.25f; // 128f

            Assert.AreEqual(expectedX, slotView.ViewportRect.x, 0.001f, "SlotView ViewportRect X is incorrect.");
            Assert.AreEqual(expectedY, slotView.ViewportRect.y, 0.001f, "SlotView ViewportRect Y is incorrect.");
            Assert.AreEqual(expectedSize, slotView.ViewportRect.width, 0.001f, "SlotView ViewportRect width is incorrect.");
            Assert.AreEqual(expectedSize, slotView.ViewportRect.height, 0.001f, "SlotView ViewportRect height is incorrect.");
        }
    }
}
