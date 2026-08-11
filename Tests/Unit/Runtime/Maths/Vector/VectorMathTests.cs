using NUnit.Framework;
using UnityEngine;

namespace Rayforge.Core.Maths.Vector.Tests
{
    [TestFixture]
    public class VectorMathTests
    {
        [TestCase(0f, 1f, 0f)]      // 0°   -> (1, 0)
        [TestCase(90f, 0f, 1f)]     // 90°  -> (0, 1)
        [TestCase(180f, -1f, 0f)]   // 180° -> (-1, 0)
        [TestCase(270f, 0f, -1f)]   // 270° -> (0, -1)
        [TestCase(45f, 0.7071f, 0.7071f)] // 45°  -> (~0.7, ~0.7)
        public void DegreeToVector_ReturnsCorrectDirection(float degree, float expectedX, float expectedY)
        {
            // Act
            Vector2 result = VectorMath.DegreeToVector(degree);

            // Assert
            Assert.AreEqual(expectedX, result.x, 0.001f);
            Assert.AreEqual(expectedY, result.y, 0.001f);
        }

        [TestCase(1f, 0f, 0f)]      // (1, 0)   -> 0°
        [TestCase(0f, 1f, 90f)]     // (0, 1)   -> 90°
        [TestCase(-1f, 0f, 180f)]   // (-1, 0)  -> 180°
        [TestCase(0f, -1f, 270f)]   // (0, -1)  -> 270°
        [TestCase(0.7071f, 0.7071f, 45f)] // (~0.7, ~0.7) -> 45°
        public void VectorToDegree_ReturnsCorrectAngle(float x, float y, float expectedDegree)
        {
            // Act
            float result = VectorMath.VectorToDegree(new Vector2(x, y));

            // Assert
            Assert.AreEqual(expectedDegree, result, 0.01f);
        }
    }
}
