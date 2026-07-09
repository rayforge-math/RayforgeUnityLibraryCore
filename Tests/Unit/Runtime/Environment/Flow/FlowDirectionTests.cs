using NUnit.Framework;
using UnityEngine;

namespace Rayforge.Core.Environment.Flow.Tests
{
    [TestFixture]
    public class FlowDirectionTests
    {
        #region Constructor

        [TestCase(0f, 1f, 0f)]     // 0°   -> (1, 0)
        [TestCase(90f, 0f, 1f)]    // 90°  -> (0, 1)
        [TestCase(180f, -1f, 0f)]  // 180° -> (-1, 0)
        [TestCase(270f, 0f, -1f)]  // 270° -> (0, -1)
        [TestCase(360f, 1f, 0f)]   // 360° -> (1, 0) - Loop
        [TestCase(450f, 0f, 1f)]   // 450° -> (0, 1) - Wrap
        [TestCase(-90f, 0f, -1f)]  // -90° -> (0, -1) - Negative
        public void ConstructorAndDegree_CalculatesCorrectVector(float degree, float expectedX, float expectedY)
        {
            var flow = new FlowDirection(degree);

            // Tolerance von 0.001f wegen Floating-Point Berechnungen (Cos/Sin)
            Assert.AreEqual(expectedX, flow.Direction.x, 0.001f, $"X-Komponente bei {degree}° falsch.");
            Assert.AreEqual(expectedY, flow.Direction.y, 0.001f, $"Y-Komponente bei {degree}° falsch.");
        }

        #endregion

        #region Property Tests

        

        [TestCase(1f, 0f, 0f)]     // (1, 0)  -> 0°
        [TestCase(0f, 1f, 90f)]    // (0, 1)  -> 90°
        [TestCase(-1f, 0f, 180f)]  // (-1, 0) -> 180°
        [TestCase(0f, -1f, 270f)]  // (0, -1) -> 270°
        public void Direction_CalculatesCorrectDegree(float x, float y, float expectedDegree)
        {
            var flow = new FlowDirection(new Vector2(x, y));

            Assert.AreEqual(expectedDegree, flow.Degree, 0.001f, $"Winkel bei Vektor ({x}, {y}) falsch.");
        }

        [TestCase(2f, 0f, 0f, 1f, 0f)]      // (2, 0)   -> normalisiert zu (1, 0), 0°
        [TestCase(0f, 5f, 90f, 0f, 1f)]     // (0, 5)   -> normalisiert zu (0, 1), 90°
        [TestCase(-3f, -3f, 225f, -0.7071f, -0.7071f)] // (-3, -3) -> normalisiert zu (-0.7071, -0.7071), 225°
        public void Direction_NormalizesVector(float x, float y, float expectedDegree, float expectedX, float expectedY)
        {
            // Arrange
            var flow = new FlowDirection(new Vector2(x, y));

            // Act
            flow.Direction = new Vector2(x, y);

            // Assert
            Assert.AreEqual(1.0f, flow.Direction.magnitude, 0.001f, "Vector is not normalized.");

            Assert.AreEqual(expectedX, flow.Direction.x, 0.001f);
            Assert.AreEqual(expectedY, flow.Direction.y, 0.001f);

            Assert.AreEqual(expectedDegree, flow.Degree, 0.001f);
        }

        [Test]
        public void Direction_Setter_ThrowsArgumentException_OnZeroVector()
        {
            // Arrange
            var flow = new FlowDirection(45f);

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
            {
                flow.Direction = Vector2.zero;
            }, "Setting Vector2.zero should throw an exception.");
        }

        [TestCase(0f)]
        [TestCase(45.5f)]
        [TestCase(180f)]
        [TestCase(359.9f)]
        public void Degree_Getter_ReturnsSetDegree(float degree)
        {
            // Arrange
            var flow = new FlowDirection();

            // Act
            flow.Degree = degree;

            // Assert
            Assert.AreEqual(degree, flow.Degree, 0.001f, "Getter liefert nicht den Wert zurück, der gesetzt wurde.");
        }

        [TestCase(0f, 0f)]
        [TestCase(360f, 0f)]
        [TestCase(450f, 90f)]
        [TestCase(-90f, 270f)]
        [TestCase(720f, 0f)]
        [TestCase(45f, 45f)]
        public void Degree_Setter_ClampsToCorrectRange(float inputDegree, float expectedDegree)
        {
            // Arrange
            var flow = new FlowDirection();

            // Act
            flow.Degree = inputDegree;

            // Assert
            Assert.AreEqual(expectedDegree, flow.Degree, 0.001f, $"Winkel {inputDegree}° wurde nicht korrekt auf {expectedDegree}° geklammert.");
        }

        [TestCase(0f, 1f, 0f)]      // 0°   -> (1, 0)
        [TestCase(90f, 0f, 1f)]     // 90°  -> (0, 1)
        [TestCase(180f, -1f, 0f)]   // 180° -> (-1, 0)
        [TestCase(270f, 0f, -1f)]   // 270° -> (0, -1)
        [TestCase(45f, 0.7071f, 0.7071f)] // 45° -> (~0.7, ~0.7)
        public void Degree_Setter_SetsCorrectDirectionVector(float degree, float expectedX, float expectedY)
        {
            // Arrange
            var flow = new FlowDirection(90f);

            // Act
            flow.Degree = degree;

            // Assert
            Assert.AreEqual(expectedX, flow.Direction.x, 0.001f, $"X-Komponente bei {degree}° falsch.");
            Assert.AreEqual(expectedY, flow.Direction.y, 0.001f, $"Y-Komponente bei {degree}° falsch.");
        }

        #endregion
    }
}
