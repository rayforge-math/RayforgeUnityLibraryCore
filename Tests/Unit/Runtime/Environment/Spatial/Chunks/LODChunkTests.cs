using NUnit.Framework;
using Rayforge.Core.Environment.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks.Tests
{
    public abstract class LODChunkTests<T> : ChunkTests<T>
        where T : LODChunk<T>
    {
        #region LODState Tests

        [Test]
        public void CurrentLOD_InitializesToNegativeTwo()
        {
            var chunk = _container.AddComponent<T>();
            Assert.AreEqual(-2, chunk.CurrentLOD, "Initial CurrentLOD should be -2.");
            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void IsVisible_InitializesToFalse()
        {
            var chunk = _container.AddComponent<T>();
            Assert.IsFalse(chunk.IsVisible, "Initial visibility should be false.");
            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void MaxLOD_InitializesToZero()
        {
            var chunk = _container.AddComponent<T>();
            Assert.AreEqual(0, chunk.MaxLOD, "Initial MaxLOD should be 0.");
            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void IsVisible_MatchesInternalState()
        {
            var chunk = _container.AddComponent<T>();

            ((ILODReceiver)chunk).SetVisibility(true, false);
            Assert.IsTrue(chunk.IsVisible);

            ((ILODReceiver)chunk).SetVisibility(false, false);
            Assert.IsFalse(chunk.IsVisible);

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnLODChanged_FiresEvent_WithCorrectParameters()
        {
            var chunk = _container.AddComponent<T>();
            ((ILODReceiver)chunk).ConfigureLODRange(5);

            ILODState capturedState = null;
            int capturedOldLod = -99;
            int capturedNewLod = -99;

            chunk.OnLODChanged += (state, oldLod, newLod) =>
            {
                capturedState = state;
                capturedOldLod = oldLod;
                capturedNewLod = newLod;
            };

            // Act
            ((ILODReceiver)chunk).UpdateLOD(3, false);

            // Assert
            Assert.AreEqual(chunk, capturedState);
            Assert.AreEqual(-2, capturedOldLod);
            Assert.AreEqual(3, capturedNewLod);

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void OnVisibilityChanged_FiresEvent_WhenVisibilityToggles()
        {
            var chunk = _container.AddComponent<T>();
            bool receivedVisibility = false;
            bool eventFired = false;

            chunk.OnVisibilityChanged += (state, isVisible) =>
            {
                eventFired = true;
                receivedVisibility = isVisible;
            };

            // Act
            ((ILODReceiver)chunk).SetVisibility(true, false);

            // Assert
            Assert.IsTrue(eventFired, "OnVisibilityChanged event should fire on change.");
            Assert.IsTrue(receivedVisibility, "Event should report true visibility.");
            Assert.IsTrue(chunk.IsVisible, "Property IsVisible should reflect the change.");

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region UpdateLOD Tests

        [Test]
        public void UpdateLOD_ReturnsTrue_WhenLodChanges()
        {
            var chunk = _container.AddComponent<T>();
            ((ILODReceiver)chunk).ConfigureLODRange(5);

            // Act
            bool result = ((ILODReceiver)chunk).UpdateLOD(1, false);

            // Assert
            Assert.IsTrue(result, "UpdateLOD should return true when LOD changes.");
            Assert.AreEqual(1, chunk.CurrentLOD);

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void UpdateLOD_ReturnsFalse_WhenLodIsSame()
        {
            var chunk = _container.AddComponent<T>();
            ((ILODReceiver)chunk).ConfigureLODRange(5);
            ((ILODReceiver)chunk).UpdateLOD(2, false);

            // Act
            bool result = ((ILODReceiver)chunk).UpdateLOD(2, false);

            // Assert
            Assert.IsFalse(result, "UpdateLOD should return false when setting same LOD.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void UpdateLOD_ThrowsException_WhenLodIsOutOfRange()
        {
            var chunk = _container.AddComponent<T>();
            ((ILODReceiver)chunk).ConfigureLODRange(3);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ((ILODReceiver)chunk).UpdateLOD(99, false));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ((ILODReceiver)chunk).UpdateLOD(-5, false));

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void UpdateLOD_UpdatesVisibility_BasedOnLod()
        {
            var chunk = _container.AddComponent<T>();
            ((ILODReceiver)chunk).ConfigureLODRange(3);

            // Act: LOD >= 0 
            ((ILODReceiver)chunk).UpdateLOD(0, false);
            Assert.IsTrue(chunk.IsVisible);

            // Act: LOD -1
            ((ILODReceiver)chunk).UpdateLOD(-1, false);
            Assert.IsFalse(chunk.IsVisible);

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region SetVisibility Tests

        [Test]
        public void SetVisibility_UpdatesInternalState_AndFiresEvent()
        {
            var chunk = _container.AddComponent<T>();
            bool capturedVisibility = false;
            bool eventFired = false;
            chunk.OnVisibilityChanged += (state, visible) => { eventFired = true; capturedVisibility = visible; };

            // Act
            ((ILODReceiver)chunk).SetVisibility(true, false);

            // Assert
            Assert.IsTrue(chunk.IsVisible);
            Assert.IsTrue(eventFired);
            Assert.IsTrue(capturedVisibility);

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void SetVisibility_DoesNotFireEvent_IfStateUnchanged()
        {
            var chunk = _container.AddComponent<T>();

            bool eventFired = false;
            chunk.OnVisibilityChanged += (state, visible) => eventFired = true;

            // Act
            ((ILODReceiver)chunk).SetVisibility(false, false);

            // Assert
            Assert.IsFalse(eventFired, "Event should not fire if visibility and active state haven't changed.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void SetVisibility_HardDeactivation_DeactivatesGameObject()
        {
            var chunk = _container.AddComponent<T>();

            // Act
            ((ILODReceiver)chunk).SetVisibility(false, true);

            // Assert
            Assert.IsFalse(chunk.gameObject.activeSelf, "GameObject should be inactive when hard deactivated.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void SetVisibility_NoHardDeactivation_KeepsGameObjectActive()
        {
            var chunk = _container.AddComponent<T>();

            // Act:
            ((ILODReceiver)chunk).SetVisibility(false, false);

            // Assert
            Assert.IsTrue(chunk.gameObject.activeSelf, "GameObject should remain active if hard deactivation is disabled.");

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void SetVisibility_TargetActiveState_CalculatedCorrectly()
        {
            var chunk = _container.AddComponent<T>();

            ((ILODReceiver)chunk).SetVisibility(true, true);
            Assert.IsTrue(chunk.gameObject.activeSelf);

            ((ILODReceiver)chunk).SetVisibility(false, false);
            Assert.IsTrue(chunk.gameObject.activeSelf);

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion

        #region ConfigureLODRange Tests

        [Test]
        public void ConfigureLODRange_SetsMaxLODCorrectly()
        {
            var chunk = _container.AddComponent<T>();
            int expectedMaxLod = 5;

            // Act
            ((ILODReceiver)chunk).ConfigureLODRange(expectedMaxLod);

            // Assert
            Assert.AreEqual(expectedMaxLod, chunk.MaxLOD);

            UnityEngine.Object.Destroy(chunk);
        }

        [Test]
        public void ConfigureLODRange_ThrowsArgumentException_OnNegativeValue()
        {
            var chunk = _container.AddComponent<T>();

            // Assert
            Assert.Throws<ArgumentException>(() =>
                ((ILODReceiver)chunk).ConfigureLODRange(-1));

            UnityEngine.Object.Destroy(chunk);
        }

        #endregion
    }
}
