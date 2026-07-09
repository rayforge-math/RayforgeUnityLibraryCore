using NUnit.Framework;
using UnityEngine;

namespace Rayforge.Core.Environment.Flow.Tests
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

            // Acts
            state.Update(velocity, flowZ, deltaTime);

            // Assert
            Assert.AreEqual(1.0f, state.Offset.x, 0.001f);
            Assert.AreEqual(0.0f, state.Offset.y, 0.001f);
            Assert.AreEqual(0.5f, state.Offset.z, 0.001f);
        }

        [Test]
        public void Update_HandlesNegativeVelocityAndFlow()
        {
            // Arrange
            var state = new FlowAdvectionState();
            // Negative Geschwindigkeit und negativer Z-Flow
            var velocity = new Vector2(-2.0f, -0.5f);
            float deltaTime = 1.0f;
            float flowZ = -1.0f;

            // Act
            state.Update(velocity, flowZ, deltaTime);

            // Assert:
            Assert.AreEqual(1022f, state.Offset.x, 0.001f);
            Assert.AreEqual(1023.5f, state.Offset.y, 0.001f);
            Assert.AreEqual(1023f, state.Offset.z, 0.001f);
        }

        [Test]
        public void Update_WrapsOffsetCorrectly()
        {
            // Arrange
            var state = new FlowAdvectionState();
            float wrap = 100.0f;

            // Act 1
            state.Update(new Vector2(105.0f, 0f), 0f, 1.0f, wrap);

            // Assert 1
            Assert.AreEqual(5.0f, state.Offset.x, 0.001f);

            // Act 2
            var state2 = new FlowAdvectionState();
            state2.Update(new Vector2(-5.0f, 0f), 0f, 1.0f, wrap);

            // Assert 2
            Assert.AreEqual(95.0f, state2.Offset.x, 0.001f);
        }

        #endregion
    }
}
