using NUnit.Framework;
using System;
using UnityEngine;

namespace Rayforge.Core.Rendering.Collections.Tests
{
    [TestFixture]
    public class MipChainLayoutTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithNonPositiveWidth_ThrowsArgumentException()
        {
            // Arrange
            var invalidRes = new Vector2Int(0, 512);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new MipChainLayout(invalidRes, 3));
            StringAssert.Contains("Base resolution must be greater than 0", ex.Message);
        }

        [Test]
        public void Constructor_WithNonPositiveHeight_ThrowsArgumentException()
        {
            // Arrange
            var invalidRes = new Vector2Int(512, -1);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new MipChainLayout(invalidRes, 3));
            StringAssert.Contains("Base resolution must be greater than 0", ex.Message);
        }

        [Test]
        public void Constructor_WithNonPositiveMipCount_ThrowsArgumentException(
            [Values(0, -1, -5)] int invalidMipCount)
        {
            // Arrange
            var baseRes = new Vector2Int(512, 512);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new MipChainLayout(baseRes, invalidMipCount));
            StringAssert.Contains("Mip count must be greater than 0", ex.Message);
        }

        [Test]
        public void Constructor_WithNullMipFunc_DefaultsToDefaultMipResolution()
        {
            // Arrange
            var baseRes = new Vector2Int(256, 256);

            // Act
            var layout = new MipChainLayout(baseRes, 3, null);

            // Assert
            Assert.AreEqual(baseRes, layout.BaseResolution);
            Assert.AreEqual(3, layout.MipCount);
            Assert.AreEqual(new Vector2Int(256, 256), layout.GetResolution(0));
            Assert.AreEqual(new Vector2Int(128, 128), layout.GetResolution(1));
            Assert.AreEqual(new Vector2Int(64, 64), layout.GetResolution(2));
        }

        [Test]
        public void Constructor_WithCustomMipFunc_AssignsCustomFunction()
        {
            // Arrange
            var baseRes = new Vector2Int(100, 100);
            MipCreateFunc customFunc = (level, res) => new Vector2Int(res.x + level, res.y + level);

            // Act
            var layout = new MipChainLayout(baseRes, 2, customFunc);

            // Assert
            Assert.AreEqual(baseRes, layout.BaseResolution);
            Assert.AreEqual(2, layout.MipCount);
            Assert.AreEqual(new Vector2Int(100, 100), layout.GetResolution(0));
            Assert.AreEqual(new Vector2Int(101, 101), layout.GetResolution(1));
        }

        #endregion

        #region Property Tests

        [Test]
        public void BaseResolution_ReturnsAssignedValue()
        {
            // Arrange
            var expectedRes = new Vector2Int(1920, 1080);
            var layout = new MipChainLayout(expectedRes, 5);

            // Act & Assert
            Assert.AreEqual(expectedRes, layout.BaseResolution);
        }

        [Test]
        public void MipCount_ReturnsAssignedValue()
        {
            // Arrange
            var baseRes = new Vector2Int(512, 512);
            int expectedCount = 4;
            var layout = new MipChainLayout(baseRes, expectedCount);

            // Act & Assert
            Assert.AreEqual(expectedCount, layout.MipCount);
        }

        [Test]
        public void MipFunc_WhenCustomProvided_ReturnsProvidedDelegate()
        {
            // Arrange
            var baseRes = new Vector2Int(256, 256);
            MipCreateFunc expectedFunc = (level, res) => res;
            var layout = new MipChainLayout(baseRes, 3, expectedFunc);

            // Act & Assert
            Assert.AreEqual(expectedFunc, layout.MipFunc);
        }

        [Test]
        public void MipFunc_WhenNullProvided_DefaultsToDefaultMipResolution()
        {
            // Arrange
            var baseRes = new Vector2Int(256, 256);
            var layout = new MipChainLayout(baseRes, 3, null);

            // Act & Assert
            Assert.IsNotNull(layout.MipFunc);

            // Verify it behaves as the default resolution function
            var res = layout.MipFunc(1, baseRes);
            Assert.AreEqual(new Vector2Int(128, 128), res);
        }

        #endregion

        #region GetResolution Tests

        [Test]
        public void GetResolution_WithValidMipLevel_ReturnsCorrectResolution(
            [Values(0, 1, 2)] int mipLevel)
        {
            // Arrange
            var baseRes = new Vector2Int(256, 256);
            var layout = new MipChainLayout(baseRes, 3);

            // Act
            var resolution = layout.GetResolution(mipLevel);

            // Assert
            var expected = new Vector2Int(256 >> mipLevel, 256 >> mipLevel);
            Assert.AreEqual(expected, resolution);
        }

        [Test]
        public void GetResolution_WithCustomMipFunc_UsesCustomFunctionLogic()
        {
            // Arrange
            var baseRes = new Vector2Int(100, 100);
            MipCreateFunc customFunc = (level, res) => new Vector2Int(res.x * (level + 1), res.y * (level + 1));
            var layout = new MipChainLayout(baseRes, 3, customFunc);

            // Act & Assert
            Assert.AreEqual(new Vector2Int(100, 100), layout.GetResolution(0));
            Assert.AreEqual(new Vector2Int(200, 200), layout.GetResolution(1));
            Assert.AreEqual(new Vector2Int(300, 300), layout.GetResolution(2));
        }

        [Test]
        public void GetResolution_WithNegativeMipLevel_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var layout = new MipChainLayout(new Vector2Int(128, 128), 3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetResolution(-1));
        }

        [Test]
        public void GetResolution_WithMipLevelEqualToMipCount_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            int mipCount = 3;
            var layout = new MipChainLayout(new Vector2Int(128, 128), mipCount);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetResolution(mipCount));
        }

        [Test]
        public void GetResolution_WithMipLevelGreaterThanMipCount_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var layout = new MipChainLayout(new Vector2Int(128, 128), 3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetResolution(5));
        }

        #endregion
    }
}
