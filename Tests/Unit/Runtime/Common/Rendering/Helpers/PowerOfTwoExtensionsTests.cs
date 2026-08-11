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
            var resolution = PowerOfTwoResolution.Res128;

            // Act & Assert
            Assert.IsTrue(resolution.IsEqual(PowerOfTwoResolution.Res128),
                "IsEqual should return true for identical resolutions.");
        }

        [Test]
        public void IsEqual_ReturnsFalse_WhenResolutionsAreDifferent()
        {
            // Arrange
            var res128 = PowerOfTwoResolution.Res128;
            var res256 = PowerOfTwoResolution.Res256;

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
            var res256 = PowerOfTwoResolution.Res256;
            var res128 = PowerOfTwoResolution.Res128;

            // Act & Assert
            Assert.IsTrue(res256.IsHigher(res128),
                "IsHigher should return true when current is strictly larger.");
        }

        [Test]
        public void IsHigher_ReturnsFalse_WhenCurrentIsEqual()
        {
            // Arrange
            var res128 = PowerOfTwoResolution.Res128;

            // Act & Assert
            Assert.IsFalse(res128.IsHigher(res128),
                "IsHigher should return false when current is equal to other.");
        }

        [Test]
        public void IsHigher_ReturnsFalse_WhenCurrentIsSmaller()
        {
            // Arrange
            var res64 = PowerOfTwoResolution.Res64;
            var res128 = PowerOfTwoResolution.Res128;

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
            var res256 = PowerOfTwoResolution.Res256;
            var res128 = PowerOfTwoResolution.Res128;

            // Act & Assert
            Assert.IsTrue(res256.IsHigherOrEqual(res128),
                "IsHigherOrEqual should return true when current is strictly larger.");
        }

        [Test]
        public void IsHigherOrEqual_ReturnsTrue_WhenCurrentIsEqual()
        {
            // Arrange
            var res128 = PowerOfTwoResolution.Res128;

            // Act & Assert
            Assert.IsTrue(res128.IsHigherOrEqual(res128),
                "IsHigherOrEqual should return true when current is equal to other.");
        }

        [Test]
        public void IsHigherOrEqual_ReturnsFalse_WhenCurrentIsSmaller()
        {
            // Arrange
            var res64 = PowerOfTwoResolution.Res64;
            var res128 = PowerOfTwoResolution.Res128;

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
            var res64 = PowerOfTwoResolution.Res64;
            var res128 = PowerOfTwoResolution.Res128;

            // Act & Assert
            Assert.IsTrue(res64.IsLower(res128),
                "IsLower should return true when current is strictly smaller.");
        }

        [Test]
        public void IsLower_ReturnsFalse_WhenCurrentIsEqual()
        {
            // Arrange
            var res128 = PowerOfTwoResolution.Res128;

            // Act & Assert
            Assert.IsFalse(res128.IsLower(res128),
                "IsLower should return false when current is equal to other.");
        }

        [Test]
        public void IsLower_ReturnsFalse_WhenCurrentIsLarger()
        {
            // Arrange
            var res256 = PowerOfTwoResolution.Res256;
            var res128 = PowerOfTwoResolution.Res128;

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
            var res64 = PowerOfTwoResolution.Res64;
            var res128 = PowerOfTwoResolution.Res128;

            // Act & Assert
            Assert.IsTrue(res64.IsLowerOrEqual(res128),
                "IsLowerOrEqual should return true when current is strictly smaller.");
        }

        [Test]
        public void IsLowerOrEqual_ReturnsTrue_WhenCurrentIsEqual()
        {
            // Arrange
            var res128 = PowerOfTwoResolution.Res128;

            // Act & Assert
            Assert.IsTrue(res128.IsLowerOrEqual(res128),
                "IsLowerOrEqual should return true when current is equal to other.");
        }

        [Test]
        public void IsLowerOrEqual_ReturnsFalse_WhenCurrentIsLarger()
        {
            // Arrange
            var res256 = PowerOfTwoResolution.Res256;
            var res128 = PowerOfTwoResolution.Res128;

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
            var resMin = PowerOfTwoResolution.Res1;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Res1, resMin.Downscale(),
                "Downscale should clamp to Resolution1 when already at the minimum.");
        }

        [Test]
        public void Downscale_ReturnsNextLower_WhenInMiddleRange()
        {
            // Arrange: Test middle range (e.g., 256 down to 128)
            var res256 = PowerOfTwoResolution.Res256;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Res128, res256.Downscale(),
                "Downscale should correctly halve the resolution in the middle range.");
        }

        [Test]
        public void Downscale_ReturnsLowerFromMaximum()
        {
            // Arrange: Test upper boundary
            var resMax = PowerOfTwoResolution.Res8192;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Res4096, resMax.Downscale(),
                "Downscale should correctly halve from the maximum Resolution8192.");
        }

        #endregion

        #region Upscale Tests

        [Test]
        public void Upscale_ReturnsNextHigher_WhenInMiddleRange()
        {
            // Arrange: Test middle range (e.g., 64 up to 128)
            var res64 = PowerOfTwoResolution.Res64;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Res128, res64.Upscale(),
                "Upscale should correctly double the resolution in the middle range.");
        }

        [Test]
        public void Upscale_ReturnsNextHigher_FromMinimum()
        {
            // Arrange: Test lower boundary
            var resMin = PowerOfTwoResolution.Res1;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Res2, resMin.Upscale(),
                "Upscale should correctly double from the minimum Resolution1.");
        }

        [Test]
        public void Upscale_ReturnsMaximum_WhenAtOrAboveMaximum()
        {
            // Arrange: Test upper boundary
            var resMax = PowerOfTwoResolution.Res8192;

            // Act & Assert
            Assert.AreEqual(PowerOfTwoResolution.Res8192, resMax.Upscale(),
                "Upscale should clamp to Resolution8192 when already at the maximum.");
        }

        #endregion

        #region GetPowerOfTwoExponent Tests

        [Test]
        public void GetPowerOfTwoExponent_ReturnsCorrectExponentForMinimum()
        {
            // Arrange
            var res = PowerOfTwoResolution.Res1;

            // Act & Assert
            Assert.AreEqual(0, res.GetPowerOfTwoExponent(), "Log2(1) should be 0.");
        }

        [Test]
        public void GetPowerOfTwoExponent_ReturnsCorrectExponentForMiddleRange()
        {
            // Arrange
            var res = PowerOfTwoResolution.Res256;

            // Act & Assert
            Assert.AreEqual(8, res.GetPowerOfTwoExponent(), "Log2(256) should be 8.");
        }

        [Test]
        public void GetPowerOfTwoExponent_ReturnsCorrectExponentForMaximum()
        {
            // Arrange
            var res = PowerOfTwoResolution.Res8192;

            // Act & Assert
            Assert.AreEqual(13, res.GetPowerOfTwoExponent(), "Log2(8192) should be 13.");
        }

        #endregion
    }
}
