using NUnit.Framework;

namespace Rayforge.Core.Environment.Spatial.Tests
{
    public class SpatialAxesTests
    {
        [Test]
        public void SpatialAxes_BasicValues_AreCorrect()
        {
            Assert.AreEqual(0, (int)SpatialAxes.None);
            Assert.AreEqual(1, (int)SpatialAxes.X);
            Assert.AreEqual(2, (int)SpatialAxes.Y);
            Assert.AreEqual(4, (int)SpatialAxes.Z);
            Assert.AreEqual(8, (int)SpatialAxes.W);
        }

        [Test]
        public void SpatialAxes_Combinations_MatchBitwiseOr()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(SpatialAxes.X | SpatialAxes.Y, SpatialAxes.XY);
            Assert.AreEqual(SpatialAxes.X | SpatialAxes.Z, SpatialAxes.XZ);
            Assert.AreEqual(SpatialAxes.Y | SpatialAxes.Z, SpatialAxes.YZ);

            // Aliases
            Assert.AreEqual(SpatialAxes.XZ, SpatialAxes.Surface);
            Assert.AreEqual(SpatialAxes.X | SpatialAxes.Y | SpatialAxes.Z, SpatialAxes.Voxel);
            Assert.AreEqual(SpatialAxes.X | SpatialAxes.Y | SpatialAxes.Z | SpatialAxes.W, SpatialAxes.Full);
        }

        [TestCase(SpatialAxes.Surface, SpatialAxes.X, true)]
        [TestCase(SpatialAxes.Surface, SpatialAxes.Y, false)]
        [TestCase(SpatialAxes.Surface, SpatialAxes.Z, true)]
        [TestCase(SpatialAxes.Voxel, SpatialAxes.W, false)]
        [TestCase(SpatialAxes.Full, SpatialAxes.W, true)]
        [TestCase(SpatialAxes.None, SpatialAxes.X, false)]
        public void SpatialAxes_HasFlag_EvaluatesCorrectly(SpatialAxes axes, SpatialAxes flagToCheck, bool expectedResult)
        {
            // Act
            bool result = axes.HasFlag(flagToCheck);

            // Assert
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void SpatialAxes_BitwiseOperations_CombineAndModifyCorrectly()
        {
            // Start with None, add X and W
            SpatialAxes axes = SpatialAxes.None;
            axes |= SpatialAxes.X;
            axes |= SpatialAxes.W;

            Assert.IsTrue(axes.HasFlag(SpatialAxes.X));
            Assert.IsFalse(axes.HasFlag(SpatialAxes.Y));
            Assert.IsTrue(axes.HasFlag(SpatialAxes.W));

            // Remove X again
            axes &= ~SpatialAxes.X;
            Assert.IsFalse(axes.HasFlag(SpatialAxes.X));
            Assert.IsTrue(axes.HasFlag(SpatialAxes.W));
        }
    }
}