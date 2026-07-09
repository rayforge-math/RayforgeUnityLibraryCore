using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Rayforge.Core.Environment.Tracking.Tests
{
    public class OriginShiftRelayTests
    {
        #region Properties

        private GameObject _testGo;
        private OriginShiftRelay _relay;
        private Vector3 _receivedDelta;
        private bool _eventFired;

        #endregion

        #region Test Control

        [SetUp]
        public void Setup()
        {
            _testGo = new GameObject("TestGo");
            _relay = _testGo.AddComponent<OriginShiftRelay>();

            // Reset state
            _eventFired = false;
            _receivedDelta = Vector3.zero;

            // Subscribe to the event
            _relay.OnWorldShiftDetected += (delta) =>
            {
                _eventFired = true;
                _receivedDelta = delta;
            };
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(_testGo);
        }

        #endregion

        #region Init Tests

        [Test]
        public void Properties_InitializeCorrectly_WithSquaredThreshold()
        {
            // Arrange
            var go = new GameObject("TestGo");
            var relay = go.AddComponent<OriginShiftRelay>();

            float expectedThreshold = 100f;
            float expectedSqr = expectedThreshold * expectedThreshold;

            // Assert
            Assert.AreEqual(expectedThreshold, relay.ShiftThreshold, "ShiftThreshold should match the default value.");
            Assert.AreEqual(expectedSqr, relay.SqrThreshold, 0.001f, "SqrThreshold should be the square of ShiftThreshold.");

            Object.Destroy(go);
        }

        [Test]
        public void LastStablePosition_MatchesInitialTransformPosition()
        {
            // Arrange
            var go = new GameObject("TestGo");

            // Setting position away from origin to verify dynamic initialization
            var startPosition = new Vector3(10f, 20f, 30f);
            go.transform.position = startPosition;

            // Act
            var relay = go.AddComponent<OriginShiftRelay>();

            // Assert
            Assert.AreEqual(startPosition, relay.LastStablePosition, "LastStablePosition must exactly match transform.position on Awake.");

            Object.Destroy(go);
        }

        #endregion

        #region UpdateThreshold Tests

        [Test]
        public void UpdateThreshold_UpdatesValuesCorrectly_WhenInputIsValid()
        {
            // Arrange
            var go = new GameObject("TestGo");
            var relay = go.AddComponent<OriginShiftRelay>();
            float newThreshold = 250f;
            float expectedSqr = newThreshold * newThreshold;

            // Act
            relay.UpdateThreshold(newThreshold);

            // Assert
            Assert.AreEqual(newThreshold, relay.ShiftThreshold, "ShiftThreshold should be updated to the new value.");
            Assert.AreEqual(expectedSqr, relay.SqrThreshold, 0.001f, "SqrThreshold should be updated to the square of the new threshold.");

            Object.Destroy(go);
        }

        [Test]
        public void UpdateThreshold_ThrowsArgumentException_WhenThresholdIsZeroOrNegative()
        {
            // Arrange
            var go = new GameObject("TestGo");
            var relay = go.AddComponent<OriginShiftRelay>();

            // Assert & Act
            Assert.Throws<System.ArgumentException>(() => relay.UpdateThreshold(0f), "Should throw ArgumentException for 0.");
            Assert.Throws<System.ArgumentException>(() => relay.UpdateThreshold(-10f), "Should throw ArgumentException for negative values.");

            Object.Destroy(go);
        }

        #endregion

        #region ResetOrigin Tests

        [Test]
        public void ResetOrigin_UpdatesLastStablePositionToCurrentPosition()
        {
            // Arrange
            var go = new GameObject("TestGo");
            var relay = go.AddComponent<OriginShiftRelay>();

            // Move the object away from the initial position
            var newPosition = new Vector3(50f, 50f, 50f);
            go.transform.position = newPosition;

            // Act
            relay.ResetOrigin();

            // Assert
            Assert.AreEqual(newPosition, relay.LastStablePosition, "LastStablePosition should be updated to the current transform.position after ResetOrigin is called.");

            Object.Destroy(go);
        }

        #endregion

        #region OnWorldShiftDetected Tests

        [UnityTest]
        public IEnumerator OnWorldShiftDetected_FiresWhenThresholdIsExceeded()
        {
            // Act: Move beyond the default 100f threshold
            _testGo.transform.position = new Vector3(150f, 0f, 0f);

            // Wait for LateUpdate to execute
            yield return new WaitForEndOfFrame();

            // Assert
            Assert.IsTrue(_eventFired, "The event should have been fired.");
            Assert.AreEqual(new Vector3(150f, 0f, 0f), _receivedDelta, "The delta vector passed to the event is incorrect.");
            Assert.AreEqual(_testGo.transform.position, _relay.LastStablePosition, "LastStablePosition should have been updated after the event.");
        }

        [UnityTest]
        public IEnumerator OnWorldShiftDetected_DoesNotFire_WhenThresholdNotReached()
        {
            // Act: Move within the 100f threshold
            _testGo.transform.position = new Vector3(50f, 0f, 0f);

            yield return new WaitForEndOfFrame();

            // Assert
            Assert.IsFalse(_eventFired, "The event should not have fired yet.");
        }

        [UnityTest]
        public IEnumerator OnWorldShiftDetected_TriggersMultipleTimes_AfterResets()
        {
            // Arrange: Move past first threshold (100f)
            // Position 150f (Delta 150f)
            _testGo.transform.position = new Vector3(150f, 0f, 0f);
            yield return new WaitForEndOfFrame();

            Assert.IsTrue(_eventFired, "First event should have fired.");

            // Reset local tracking for the second phase
            _eventFired = false;
            _receivedDelta = Vector3.zero;

            // Act: Move another 150f from the new position
            // Total position is now 300f. Delta should be 150f.
            _testGo.transform.position = new Vector3(300f, 0f, 0f);
            yield return new WaitForEndOfFrame();

            // Assert
            Assert.IsTrue(_eventFired, "Second event should have fired after crossing the threshold again.");
            Assert.AreEqual(new Vector3(150f, 0f, 0f), _receivedDelta, "The second delta vector should be calculated relative to the new origin.");
            Assert.AreEqual(new Vector3(300f, 0f, 0f), _relay.LastStablePosition, "LastStablePosition should have updated to the position of the second shift.");
        }

        #endregion
    }
}
