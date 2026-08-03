using NUnit.Framework;
using UnityEngine;

namespace Rayforge.Core.Environment.Tracking.Tests
{
    [TestFixture]
    public class FlowAdvectionStateTests
    {
        #region Init Tests

        [Test]
        public void InitialState_IsZero()
        {
            // Arrange & Act
            var state = new FlowAdvectionState();

            // Assert
            Assert.AreEqual(Vector3.zero, state.Offset, "Initial offset should be (0, 0, 0).");
        }

        #endregion

        #region Update Tests

        [Test]
        public void Update_AccumulatesVelocityCorrectly()
        {
            // Arrange
            var state = new FlowAdvectionState();
            var velocity = new Vector2(2.0f, 0f);
            float deltaTime = 0.5f;
            float flowZ = 1.0f;

            // Act
            state.Update(velocity, flowZ, deltaTime);

            // Assert
            Assert.AreEqual(1.0f, state.Offset.x, 0.001f);
            Assert.AreEqual(0.0f, state.Offset.y, 0.001f);
            Assert.AreEqual(0.5f, state.Offset.z, 0.001f);
        }

        [Test]
        public void Update_HandlesNegativeVelocityAndFlow()
        {
            // Arrange (Default wrap value is 1024.0f)
            var state = new FlowAdvectionState();
            var velocity = new Vector2(-2.0f, -0.5f);
            float deltaTime = 1.0f;
            float flowZ = -1.0f;

            // Act
            state.Update(velocity, flowZ, deltaTime);

            // Assert: Mathf.Repeat(-2, 1024) -> 1022, etc.
            Assert.AreEqual(1022f, state.Offset.x, 0.001f);
            Assert.AreEqual(1023.5f, state.Offset.y, 0.001f);
            Assert.AreEqual(1023f, state.Offset.z, 0.001f);
        }

        [Test]
        public void Update_WrapsOffsetCorrectly_WithCustomWrapValue()
        {
            // Arrange mit benutzerdefiniertem Wrap-Wert im Konstruktor
            float wrap = 100.0f;
            var state = new FlowAdvectionState(wrap);

            // Act 1
            state.Update(new Vector2(105.0f, 0f), 0f, 1.0f);

            // Assert 1
            Assert.AreEqual(5.0f, state.Offset.x, 0.001f);

            // Act 2 (Negative Werte mit individuellem Wrap)
            var state2 = new FlowAdvectionState(wrap);
            state2.Update(new Vector2(-5.0f, 0f), 0f, 1.0f);

            // Assert 2
            Assert.AreEqual(95.0f, state2.Offset.x, 0.001f);
        }

        #endregion
    }
}