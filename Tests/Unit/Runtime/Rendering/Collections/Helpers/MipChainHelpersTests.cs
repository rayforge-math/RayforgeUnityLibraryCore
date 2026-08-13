using NUnit.Framework;
using UnityEngine;

namespace Rayforge.Core.Rendering.Collections.Helpers
{
    [TestFixture]
    public class MipChainHelpersTests
    {
        #region MipChainHelpers Tests

        [TestCase(1024, 512, 0, 1024, 512)]
        [TestCase(1024, 512, 1, 512, 256)]
        [TestCase(1024, 512, 2, 256, 128)]
        [TestCase(100, 50, 1, 50, 25)]
        [TestCase(1024, 512, 12, 1, 1)] // Clamped to 1 when shift exceeds dimensions
        [TestCase(2, 2, 3, 1, 1)]       // Clamped to 1
        public void DefaultMipResolution_ReturnsExpectedResolution(
            int baseX, int baseY, int mipLevel, int expectedX, int expectedY)
        {
            // Arrange
            var baseRes = new Vector2Int(baseX, baseY);

            // Act
            var result = MipChainHelpers.DefaultMipResolution(mipLevel, baseRes);

            // Assert
            Assert.AreEqual(new Vector2Int(expectedX, expectedY), result);
        }

        #endregion 
    }
}
