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

        #endregion
    }
}
