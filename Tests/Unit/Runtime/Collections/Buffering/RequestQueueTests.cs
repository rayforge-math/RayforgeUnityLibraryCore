using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture]
    public class RequestQueueTests
    {
        #region Constructors

        [Test]
        public void Constructor_Default_InitializesEmpty()
        {
            // Act
            var queue = new RequestQueue<int, float>();

            // Assert
            Assert.AreEqual(0, queue.UpdateCount, "New queue should have no updates.");
            Assert.AreEqual(0, queue.RemovalCount, "New queue should have no removals.");
            Assert.IsFalse(queue.HasRequests, "New queue should not have pending requests.");
        }

        [Test]
        public void Constructor_WithInitialCapacity_InitializesEmpty()
        {
            // Act
            var queue = new RequestQueue<int, float>(100);

            // Assert
            Assert.AreEqual(0, queue.UpdateCount, "Queue with capacity should start empty.");
            Assert.AreEqual(0, queue.RemovalCount, "Queue with capacity should start empty.");
        }

        [Test]
        public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new RequestQueue<int, float>(-1),
                "Constructor should throw when initial capacity is negative.");
        }

        [Test]
        public void Constructor_ZeroCapacity_InitializesSuccessfully()
        {
            // Act
            var queue = new RequestQueue<int, float>(0);

            // Assert
            Assert.IsNotNull(queue, "Queue should handle 0 capacity gracefully.");
        }

        #endregion

        #region Properties

        [Test]
        public void Properties_InitialState_ReturnsZero()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();

            // Assert
            Assert.IsFalse(queue.HasRequests);
            Assert.AreEqual(0, queue.RemovalCount);
            Assert.AreEqual(0, queue.UpdateCount);
        }

        [Test]
        public void Properties_AfterEnqueueUpdate_ReturnsCorrectCounts()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();

            // Act
            queue.EnqueueUpdate(1, 10.0f);

            // Assert
            Assert.IsTrue(queue.HasRequests);
            Assert.AreEqual(1, queue.UpdateCount);
            Assert.AreEqual(0, queue.RemovalCount);
        }

        [Test]
        public void Properties_AfterEnqueueRemoval_ReturnsCorrectCounts()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();

            // Act
            queue.EnqueueRemoval(1);

            // Assert
            Assert.IsTrue(queue.HasRequests);
            Assert.AreEqual(0, queue.UpdateCount);
            Assert.AreEqual(1, queue.RemovalCount);
        }

        [Test]
        public void Properties_AfterClear_ReturnsZero()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueUpdate(1, 10.0f);
            queue.EnqueueRemoval(2);

            // Act
            queue.Clear();

            // Assert
            Assert.IsFalse(queue.HasRequests);
            Assert.AreEqual(0, queue.UpdateCount);
            Assert.AreEqual(0, queue.RemovalCount);
        }

        #endregion

        #region EnqueueUpdate Tests

        [Test]
        public void EnqueueUpdate_NewKey_AddsToUpdates()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            int key = 1;
            float value = 10.0f;

            // Act
            queue.EnqueueUpdate(key, value);

            // Assert
            Assert.AreEqual(1, queue.UpdateCount);
            Assert.AreEqual(0, queue.RemovalCount);

            // Überprüfung des Wertes via Iterator
            var iterator = queue.GetUpdateIterator();
            bool found = false;
            while (iterator.MoveNext())
            {
                if (iterator.Current.Key == key && iterator.Current.Value == value)
                    found = true;
            }
            Assert.IsTrue(found, "The updated key/value pair was not found in the queue.");
        }

        [Test]
        public void EnqueueUpdate_ExistingKey_UpdatesValue()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueUpdate(1, 10.0f);

            // Act
            queue.EnqueueUpdate(1, 20.0f);

            // Assert
            Assert.AreEqual(1, queue.UpdateCount, "Update count should remain 1.");
        }

        [Test]
        public void EnqueueUpdate_WhenKeyIsMarkedForRemoval_CancelsRemoval()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            int key = 1;
            queue.EnqueueRemoval(key); 

            // Act
            queue.EnqueueUpdate(key, 5.0f);

            // Assert
            Assert.AreEqual(0, queue.RemovalCount, "Removal should be cancelled.");
            Assert.AreEqual(1, queue.UpdateCount, "Update should be registered.");
        }

        [Test]
        public void EnqueueUpdate_NullValueHandling_WorksForStructs()
        {
            // Arrange
            var queue = new RequestQueue<int, int>();

            // Act
            queue.EnqueueUpdate(1, 0);

            // Assert
            Assert.AreEqual(1, queue.UpdateCount);
        }

        #endregion

        #region EnqueueRemoval Tests

        [Test]
        public void EnqueueRemoval_NewKey_AddsToRemovals()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            int key = 1;

            // Act
            queue.EnqueueRemoval(key);

            // Assert
            Assert.AreEqual(1, queue.RemovalCount);
            Assert.AreEqual(0, queue.UpdateCount);

            var iterator = queue.GetRemovalIterator();
            bool found = false;
            while (iterator.MoveNext())
            {
                if (iterator.Current == key)
                    found = true;
            }
            Assert.IsTrue(found, "The removed key was not found in the removal queue.");
        }

        [Test]
        public void EnqueueRemoval_WhenKeyHasPendingUpdate_CancelsUpdate()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            int key = 1;
            queue.EnqueueUpdate(key, 10.0f);

            // Act
            queue.EnqueueRemoval(key);

            // Assert
            Assert.AreEqual(0, queue.UpdateCount, "Pending update should be cancelled.");
            Assert.AreEqual(1, queue.RemovalCount, "Removal should be registered.");
        }

        [Test]
        public void EnqueueRemoval_AlreadyMarkedForRemoval_DoesNotDuplicate()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            int key = 1;
            queue.EnqueueRemoval(key);

            // Act
            queue.EnqueueRemoval(key);

            // Assert
            Assert.AreEqual(1, queue.RemovalCount, "Removal queue should handle duplicate removals gracefully (HashSet behavior).");
        }

        [Test]
        public void EnqueueRemoval_AfterClear_IsClean()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueUpdate(1, 10.0f);
            queue.Clear();

            // Act
            queue.EnqueueRemoval(1);

            // Assert
            Assert.AreEqual(1, queue.RemovalCount);
            Assert.AreEqual(0, queue.UpdateCount);
        }

        #endregion

        #region Clear

        [Test]
        public void Clear_WhenHasPendingRequests_ResetsCountsToZero()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueUpdate(1, 10.0f);
            queue.EnqueueUpdate(2, 20.0f);
            queue.EnqueueRemoval(3);

            // Act
            queue.Clear();

            // Assert
            Assert.IsFalse(queue.HasRequests, "Queue should not have requests after clear.");
            Assert.AreEqual(0, queue.UpdateCount, "Update count should be zero after clear.");
            Assert.AreEqual(0, queue.RemovalCount, "Removal count should be zero after clear.");
        }

        [Test]
        public void Clear_WhenEmpty_DoesNothing()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();

            // Act
            queue.Clear();

            // Assert
            Assert.AreEqual(0, queue.UpdateCount);
            Assert.AreEqual(0, queue.RemovalCount);
            Assert.IsFalse(queue.HasRequests);
        }

        [Test]
        public void Clear_AndEnqueueAgain_WorksCorrectly()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueUpdate(1, 10.0f);
            queue.Clear();

            // Act
            queue.EnqueueUpdate(1, 5.0f);

            // Assert
            Assert.AreEqual(1, queue.UpdateCount);

            var iterator = queue.GetUpdateIterator();
            iterator.MoveNext();
            Assert.AreEqual(5.0f, iterator.Current.Value, "After clear and re-enqueue, the new value should be present.");
        }

        #endregion

        #region ForEachRemoval Tests

        [Test]
        public void ForEachRemoval_WhenEmpty_DoesNotExecute()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            var action = new RequestHandler<int>();

            // Act
            queue.ForEachRemoval(ref action);

            // Assert
            Assert.AreEqual(0, action.CallCount, "Action should not be executed when queue is empty.");
        }

        [Test]
        public void ForEachRemoval_WithItems_ExecutesCorrectly()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueRemoval(10);
            queue.EnqueueRemoval(20);

            var action = new RequestHandler<int>();

            // Act
            queue.ForEachRemoval(ref action);

            // Assert
            Assert.AreEqual(2, action.CallCount, "Action should be executed for each pending removal.");
            Assert.Contains(10, action.Elements);
            Assert.Contains(20, action.Elements);
        }

        [Test]
        public void ForEachRemoval_AfterUpdateConflict_ExecutesOnlyRemaining()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueRemoval(1);
            queue.EnqueueRemoval(2);

            queue.EnqueueUpdate(1, 10.0f);

            var action = new RequestHandler<int>();

            // Act
            queue.ForEachRemoval(ref action);

            // Assert
            Assert.AreEqual(1, action.CallCount, "Action should only execute for the non-cancelled removal.");
            Assert.Contains(2, action.Elements, "Key 2 should still be in the removal queue.");
            Assert.IsFalse(action.Elements.Contains(1), "Key 1 should have been removed from the queue due to the update.");
        }

        [Test]
        public void ForEachRemoval_AfterClear_DoesNotExecute()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueRemoval(1);
            queue.Clear();

            var action = new RequestHandler<int>();

            // Act
            queue.ForEachRemoval(ref action);

            // Assert
            Assert.AreEqual(0, action.CallCount, "Action should not be executed after clear.");
        }

        #endregion

        #region ForEachUpdate Tests

        [Test]
        public void ForEachUpdate_WhenEmpty_DoesNotExecute()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            var handler = new RequestHandler<KeyValuePair<int, float>>();

            // Act
            queue.ForEachUpdate(ref handler);

            // Assert
            Assert.AreEqual(0, handler.CallCount, "Action should not be executed when no updates are queued.");
        }

        [Test]
        public void ForEachUpdate_WithItems_ExecutesCorrectly()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueUpdate(1, 10.5f);
            queue.EnqueueUpdate(2, 20.5f);

            var handler = new RequestHandler<KeyValuePair<int, float>>();

            // Act
            queue.ForEachUpdate(ref handler);

            // Assert
            Assert.AreEqual(2, handler.CallCount, "Action should be executed for each pending update.");

            bool found1 = handler.Elements.Any(kvp => kvp.Key == 1 && Math.Abs(kvp.Value - 10.5f) < 0.001f);
            bool found2 = handler.Elements.Any(kvp => kvp.Key == 2 && Math.Abs(kvp.Value - 20.5f) < 0.001f);

            Assert.IsTrue(found1, "Update for key 1 not found.");
            Assert.IsTrue(found2, "Update for key 2 not found.");
        }

        [Test]
        public void ForEachUpdate_AfterRemovalConflict_OnlyExecutesRemaining()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueUpdate(1, 10.0f);
            queue.EnqueueUpdate(2, 20.0f);

            queue.EnqueueRemoval(1);

            var handler = new RequestHandler<KeyValuePair<int, float>>();

            // Act
            queue.ForEachUpdate(ref handler);

            // Assert
            Assert.AreEqual(1, handler.CallCount, "Should only execute for the one non-removed update.");
            Assert.AreEqual(2, handler.Elements[0].Key, "Key 2 should be the only remaining update.");
        }

        [Test]
        public void ForEachUpdate_AfterClear_DoesNotExecute()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueUpdate(1, 10.0f);
            queue.Clear();

            var handler = new RequestHandler<KeyValuePair<int, float>>();

            // Act
            queue.ForEachUpdate(ref handler);

            // Assert
            Assert.AreEqual(0, handler.CallCount, "Action should not be executed after clear.");
        }

        #endregion

        #region GetRemovalIterator Tests

        [Test]
        public void GetRemovalIterator_WhenEmpty_HasNoElements()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();

            // Act
            var iterator = queue.GetRemovalIterator();

            // Assert
            Assert.IsFalse(iterator.MoveNext(), "Iterator should not have elements when queue is empty.");
        }

        [Test]
        public void GetRemovalIterator_WithItems_IteratesCorrectly()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueRemoval(1);
            queue.EnqueueRemoval(2);

            // Act
            var iterator = queue.GetRemovalIterator();
            var foundKeys = new List<int>();
            while (iterator.MoveNext())
            {
                foundKeys.Add(iterator.Current);
            }

            // Assert
            Assert.AreEqual(2, foundKeys.Count);
            Assert.Contains(1, foundKeys);
            Assert.Contains(2, foundKeys);
        }

        [Test]
        public void GetRemovalIterator_AfterClear_IsEmpty()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueRemoval(1);
            queue.Clear();

            // Act
            var iterator = queue.GetRemovalIterator();

            // Assert
            Assert.IsFalse(iterator.MoveNext(), "Iterator should be empty after Clear() is called.");
        }

        #endregion

        #region GetUpdateIterator Tests

        [Test]
        public void GetUpdateIterator_WhenEmpty_HasNoElements()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();

            // Act
            var iterator = queue.GetUpdateIterator();

            // Assert
            Assert.IsFalse(iterator.MoveNext(), "Iterator should not have elements when queue is empty.");
        }

        [Test]
        public void GetUpdateIterator_WithItems_IteratesCorrectly()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueUpdate(1, 10.5f);
            queue.EnqueueUpdate(2, 20.5f);

            // Act
            var iterator = queue.GetUpdateIterator();
            var foundPairs = new List<KeyValuePair<int, float>>();
            while (iterator.MoveNext())
            {
                foundPairs.Add(iterator.Current);
            }

            // Assert
            Assert.AreEqual(2, foundPairs.Count);

            // Check for correct data
            bool found1 = foundPairs.Any(pair => pair.Key == 1 && Math.Abs(pair.Value - 10.5f) < 0.001f);
            bool found2 = foundPairs.Any(pair => pair.Key == 2 && Math.Abs(pair.Value - 20.5f) < 0.001f);

            Assert.IsTrue(found1, "KeyValuePair for key 1 was not found.");
            Assert.IsTrue(found2, "KeyValuePair for key 2 was not found.");
        }

        [Test]
        public void GetUpdateIterator_AfterClear_IsEmpty()
        {
            // Arrange
            var queue = new RequestQueue<int, float>();
            queue.EnqueueUpdate(1, 10.0f);
            queue.Clear();

            // Act
            var iterator = queue.GetUpdateIterator();

            // Assert
            Assert.IsFalse(iterator.MoveNext(), "Iterator should be empty after Clear() is called.");
        }

        #endregion
    }
}
