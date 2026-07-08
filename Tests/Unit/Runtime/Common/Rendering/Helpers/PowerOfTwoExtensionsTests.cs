using NUnit.Framework;
using UnityEngine;

namespace Rayforge.Core.Common.Rendering.Helpers.Tests
{
    public class PowerOfTwoExtensionsTests
    {
        #region IsEqual Tests

        [Test]
        public void IsEqual_ReturnsTrue_WhenResolutionsAreSame()
        {
            // Arrange
            var resolution = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsTrue(resolution.IsEqual(PowerOfTwoResolution.Resolution128),
                "IsEqual should return true for identical resolutions.");
        }

        [Test]
        public void IsEqual_ReturnsFalse_WhenResolutionsAreDifferent()
        {
            // Arrange
            var res128 = PowerOfTwoResolution.Resolution128;
            var res256 = PowerOfTwoResolution.Resolution256;

            // Act & Assert
            Assert.IsFalse(res128.IsEqual(res256),
                "IsEqual should return false for different resolutions.");
        }

        #endregion

        #region IsHigher Tests

        [Test]
        public void IsHigher_ReturnsTrue_WhenCurrentIsLarger()
        {
            // Arrange
            var res256 = PowerOfTwoResolution.Resolution256;
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsTrue(res256.IsHigher(res128),
                "IsHigher should return true when current is strictly larger.");
        }

        [Test]
        public void IsHigher_ReturnsFalse_WhenCurrentIsEqual()
        {
            // Arrange
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsFalse(res128.IsHigher(res128),
                "IsHigher should return false when current is equal to other.");
        }

        [Test]
        public void IsHigher_ReturnsFalse_WhenCurrentIsSmaller()
        {
            // Arrange
            var res64 = PowerOfTwoResolution.Resolution64;
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsFalse(res64.IsHigher(res128),
                "IsHigher should return false when current is strictly smaller.");
        }

        #endregion

        #region IsHigherOrEqual

        [Test]
        public void IsHigherOrEqual_ReturnsTrue_WhenCurrentIsLarger()
        {
            // Arrange
            var res256 = PowerOfTwoResolution.Resolution256;
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsTrue(res256.IsHigherOrEqual(res128),
                "IsHigherOrEqual should return true when current is strictly larger.");
        }

        [Test]
        public void IsHigherOrEqual_ReturnsTrue_WhenCurrentIsEqual()
        {
            // Arrange
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsTrue(res128.IsHigherOrEqual(res128),
                "IsHigherOrEqual should return true when current is equal to other.");
        }

        [Test]
        public void IsHigherOrEqual_ReturnsFalse_WhenCurrentIsSmaller()
        {
            // Arrange
            var res64 = PowerOfTwoResolution.Resolution64;
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsFalse(res64.IsHigherOrEqual(res128),
                "IsHigherOrEqual should return false when current is strictly smaller.");
        }

        #endregion

        #region IsLower

        [Test]
        public void IsLower_ReturnsTrue_WhenCurrentIsSmaller()
        {
            // Arrange
            var res64 = PowerOfTwoResolution.Resolution64;
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsTrue(res64.IsLower(res128),
                "IsLower should return true when current is strictly smaller.");
        }

        [Test]
        public void IsLower_ReturnsFalse_WhenCurrentIsEqual()
        {
            // Arrange
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsFalse(res128.IsLower(res128),
                "IsLower should return false when current is equal to other.");
        }

        [Test]
        public void IsLower_ReturnsFalse_WhenCurrentIsLarger()
        {
            // Arrange
            var res256 = PowerOfTwoResolution.Resolution256;
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsFalse(res256.IsLower(res128),
                "IsLower should return false when current is strictly larger.");
        }

        #endregion

        #region IsLowerOrEqual Tests

        [Test]
        public void IsLowerOrEqual_ReturnsTrue_WhenCurrentIsSmaller()
        {
            // Arrange
            var res64 = PowerOfTwoResolution.Resolution64;
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsTrue(res64.IsLowerOrEqual(res128),
                "IsLowerOrEqual should return true when current is strictly smaller.");
        }

        [Test]
        public void IsLowerOrEqual_ReturnsTrue_WhenCurrentIsEqual()
        {
            // Arrange
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsTrue(res128.IsLowerOrEqual(res128),
                "IsLowerOrEqual should return true when current is equal to other.");
        }

        [Test]
        public void IsLowerOrEqual_ReturnsFalse_WhenCurrentIsLarger()
        {
            // Arrange
            var res256 = PowerOfTwoResolution.Resolution256;
            var res128 = PowerOfTwoResolution.Resolution128;

            // Act & Assert
            Assert.IsFalse(res256.IsLowerOrEqual(res128),
                "IsLowerOrEqual should return false when current is strictly larger.");
        }

        #endregion

        #region Downscale Tests

        [Test]
        public void Downscale_ReturnsMinimum_WhenAtOrBelowMinimum()
        {
            // Arrange: Test boundary at Resolution1
            var resMin = PowerOfTwoResolution.Resolution1;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Resolution1, resMin.Downscale(),
                "Downscale should clamp to Resolution1 when already at the minimum.");
        }

        [Test]
        public void Downscale_ReturnsNextLower_WhenInMiddleRange()
        {
            // Arrange: Test middle range (e.g., 256 down to 128)
            var res256 = PowerOfTwoResolution.Resolution256;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Resolution128, res256.Downscale(),
                "Downscale should correctly halve the resolution in the middle range.");
        }

        [Test]
        public void Downscale_ReturnsLowerFromMaximum()
        {
            // Arrange: Test upper boundary
            var resMax = PowerOfTwoResolution.Resolution8192;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Resolution4096, resMax.Downscale(),
                "Downscale should correctly halve from the maximum Resolution8192.");
        }

        #endregion

        #region Upscale Tests

        [Test]
        public void Upscale_ReturnsNextHigher_WhenInMiddleRange()
        {
            // Arrange: Test middle range (e.g., 64 up to 128)
            var res64 = PowerOfTwoResolution.Resolution64;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Resolution128, res64.Upscale(),
                "Upscale should correctly double the resolution in the middle range.");
        }

        [Test]
        public void Upscale_ReturnsNextHigher_FromMinimum()
        {
            // Arrange: Test lower boundary
            var resMin = PowerOfTwoResolution.Resolution1;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Resolution2, resMin.Upscale(),
                "Upscale should correctly double from the minimum Resolution1.");
        }

        [Test]
        public void Upscale_ReturnsMaximum_WhenAtOrAboveMaximum()
        {
            // Arrange: Test upper boundary
            var resMax = PowerOfTwoResolution.Resolution8192;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Resolution8192, resMax.Upscale(),
                "Upscale should clamp to Resolution8192 when already at the maximum.");
        }

        #endregion

        #region GetPowerOfTwoExponent Tests

        [Test]
        public void GetPowerOfTwoExponent_ReturnsCorrectExponentForMinimum()
        {
            // Arrange
            var res = PowerOfTwoResolution.Resolution1;

            // Act & Assert
            Assert.AreEqual(0, res.GetPowerOfTwoExponent(), "Log2(1) should be 0.");
        }

        [Test]
        public void GetPowerOfTwoExponent_ReturnsCorrectExponentForMiddleRange()
        {
            // Arrange
            var res = PowerOfTwoResolution.Resolution256;

            // Act & Assert
            Assert.AreEqual(8, res.GetPowerOfTwoExponent(), "Log2(256) should be 8.");
        }

        [Test]
        public void GetPowerOfTwoExponent_ReturnsCorrectExponentForMaximum()
        {
            // Arrange
            var res = PowerOfTwoResolution.Resolution8192;

            // Act & Assert
            Assert.AreEqual(13, res.GetPowerOfTwoExponent(), "Log2(8192) should be 13.");
        }

        #endregion

        #region ToSlotCountPerDim Tests

        [Test]
        public void ToSlotCountPerDim_ReturnsOne_WhenResolutionsAreEqual()
        {
            // Arrange
            var res = PowerOfTwoResolution.Resolution1024;

            // Act & Assert
            Assert.AreEqual(1, res.ToSlotCountPerDim(res),
                "A resolution fitting into itself should return 1 slot per dimension.");
        }

        [Test]
        public void ToSlotCountPerDim_ReturnsCorrectRatio_InStandardScenario()
        {
            // Arrange
            var tile = PowerOfTwoResolution.Resolution512;
            var container = PowerOfTwoResolution.Resolution2048;

            // Act & Assert (2048 / 512 = 4)
            Assert.AreEqual(4, tile.ToSlotCountPerDim(container),
                "Should return correct ratio for standard atlas grid calculation.");
        }

        [Test]
        public void ToSlotCountPerDim_ReturnsMaxRatio_WhenTileIsMinimumAndContainerIsMaximum()
        {
            // Arrange
            var tile = PowerOfTwoResolution.Resolution1;
            var container = PowerOfTwoResolution.Resolution8192;

            // Act & Assert (8192 / 1 = 8192)
            Assert.AreEqual(8192, tile.ToSlotCountPerDim(container),
                "Should return the full range ratio when tile is 1 and container is 8192.");
        }

        [Test]
        public void ToSlotCountPerDim_ThrowsArgumentException_WhenTileIsLargerThanBase()
        {
            // Arrange
            var tile = PowerOfTwoResolution.Resolution1024;
            var container = PowerOfTwoResolution.Resolution512;

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => tile.ToSlotCountPerDim(container),
                "ToSlotCountPerDim should throw an exception if the tile is larger than the container.");
        }

        #endregion

        #region ToSlotCount Tests

        [Test]
        public void ToSlotCount_ReturnsOne_WhenResolutionsAreEqual()
        {
            // Arrange
            var res = PowerOfTwoResolution.Resolution1024;

            // Act & Assert
            Assert.AreEqual(1, res.ToSlotCount(res),
                "If tile size equals base size, capacity should be 1.");
        }

        [Test]
        public void ToSlotCount_ReturnsCorrectCapacity_InStandardScenario()
        {
            // Arrange
            var tile = PowerOfTwoResolution.Resolution512;
            var container = PowerOfTwoResolution.Resolution2048;

            // Act & Assert (4 slots per dim * 4 slots per dim = 16)
            Assert.AreEqual(16, tile.ToSlotCount(container),
                "A 4x4 grid should result in 16 total slots.");
        }

        [Test]
        public void ToSlotCount_ReturnsMaximumCapacity_AtExtremeDifference()
        {
            // Arrange
            var tile = PowerOfTwoResolution.Resolution1;
            var container = PowerOfTwoResolution.Resolution8192;

            // Act & Assert (8192 * 8192 = 67,108,864)
            Assert.AreEqual(67108864, tile.ToSlotCount(container),
                "Should correctly calculate the large total area for small tiles in a large container.");
        }

        [Test]
        public void ToSlotCount_ThrowsArgumentException_WhenTileIsLargerThanBase()
        {
            // Arrange
            var tile = PowerOfTwoResolution.Resolution1024;
            var container = PowerOfTwoResolution.Resolution512;

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => tile.ToSlotCount(container),
                "ToSlotCount should throw an exception if the tile is larger than the container.");
        }

        #endregion
    }
}
