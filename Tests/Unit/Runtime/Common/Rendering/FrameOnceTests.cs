using NUnit.Framework;
using UnityEngine;

namespace Rayforge.Core.Common.Rendering.Tests
{
    [TestFixture]
    public class FrameOnceTests
    {
        #region Setup

        private int _mockCurrentFrame;

        private int GetMockFrame() => _mockCurrentFrame;

        [SetUp]
        public void Setup()
        {
            _mockCurrentFrame = 1;
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ShouldStartAt_NegativeOne()
        {
            var guard = new FrameOnce();

            Assert.Equals(-1, guard.LastFrame);
        }

        [Test]
        public void Constructor_WithLambda_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new FrameOnce(GetMockFrame),
                "Should not throw when valid labda expression is passed.");
        }

        #endregion

        #region TryBegin Tests

        [Test]
        public void TryBegin_ReturnsTrue_OnFirstCall()
        {
            // Arrange
            var guard = new FrameOnce(GetMockFrame);

            // Act
            bool result = guard.TryBegin();

            // Assert
            Assert.IsTrue(result, "Der erste Aufruf im Frame sollte erfolgreich sein.");
        }

        [Test]
        public void TryBegin_ReturnsFalse_OnSubsequentCallsInSameFrame()
        {
            // Arrange
            var guard = new FrameOnce(GetMockFrame);
            guard.TryBegin();

            // Act
            bool result = guard.TryBegin();

            // Assert
            Assert.IsFalse(result, "Der zweite Aufruf im selben Frame muss false zurückgeben.");
        }

        [Test]
        public void TryBegin_ReturnsTrue_AfterFrameChange()
        {
            // Arrange
            var guard = new FrameOnce(GetMockFrame);
            guard.TryBegin();

            // Act
            _mockCurrentFrame = 2;
            bool result = guard.TryBegin();

            // Assert
            Assert.IsTrue(result, "Nach einem Frame-Wechsel sollte TryBegin wieder true zurückgeben.");
        }

        #endregion
    }
}
