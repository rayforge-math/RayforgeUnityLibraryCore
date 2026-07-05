using NUnit.Framework;
using Rayforge.Core.TestEnv;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.Port;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture]
    public class MetadataStoreTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_ShouldNotHavePublicDefaultConstructor()
        {
            var type = typeof(MetadataStore<>);

            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            var hasDefaultConstructor = constructors.Any(c => c.GetParameters().Length == 0);

            Assert.IsFalse(hasDefaultConstructor, "MetadataStore should not have a public default constructor.");
        }

        [Test]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Arrange
            int capacity = 100;
            int batchSize = 10;

            // Act
            var store = new MetadataStore<int>(capacity, batchSize);

            // Assert
            Assert.AreEqual(capacity, store.Capacity, "Capacity should match requested size.");
            Assert.AreEqual(batchSize, store.BatchSize, "BatchSize should match requested size.");
            Assert.IsFalse(store.AnyDirty, "Store should not be dirty upon initialization.");
            Assert.AreEqual(capacity, store.TypedBuffer.Length, "Internal buffer length should match capacity.");
        }

        [Test]
        public void Constructor_ZeroBatchSize_DefaultsToMaxBatchSize()
        {
            // Act
            var store = new MetadataStore<int>(100, 0);

            // Assert
            Assert.AreEqual(100, store.BatchSize, "BatchSize 0 should be normalized to 1.");
        }

        [Test]
        public void Constructor_NegativeCapacity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new MetadataStore<int>(-1, 10));
        }

        [Test]
        public void Constructor_ZeroCapacity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new MetadataStore<int>(0, 10));
        }

        [Test]
        public void Constructor_NegativeBatchSize_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new MetadataStore<int>(100, -5));
        }

        #endregion

        #region Properties

        [Test]
        public void Capacity_ReturnsInitializedLength()
        {
            int capacity = 42;
            var store = new MetadataStore<int>(capacity, 1);

            // Ensure the capacity property correctly exposes the array length
            Assert.AreEqual(capacity, store.Capacity);
        }

        [Test]
        public void Stride_ReturnsSizeOfUnmanagedType()
        {
            // MetadataStore<float> should have a stride of 4 bytes
            var floatStore = new MetadataStore<float>(1, 1);
            Assert.AreEqual(4, floatStore.Stride);

            // MetadataStore<long> should have a stride of 8 bytes
            var longStore = new MetadataStore<long>(1, 1);
            Assert.AreEqual(8, longStore.Stride);
        }

        [Test]
        public void BatchSize_ReturnsConfiguredValue()
        {
            int batchSize = 7;
            var store = new MetadataStore<int>(100, batchSize);

            // Validate that the returned batch size matches the constructor input
            Assert.AreEqual(batchSize, store.BatchSize);
        }

        [Test]
        [TestCase(10, 3, 4)]    // 3 + 3 + 3 + 1 = 4
        [TestCase(10, 5, 2)]    // Exact fit: 5 + 5 = 2
        [TestCase(10, 10, 1)]   // Full array as one batch
        [TestCase(10, 11, 1)]   // Batch size larger than capacity = 1 batch
        [TestCase(5, 2, 3)]     // 2 + 2 + 1 = 3
        [TestCase(1, 1, 1)]     // Single element
        public void TotalBatchCount_CalculatesCorrectly(int capacity, int batchSize, int expectedBatches)
        {
            // Act
            var store = new MetadataStore<int>(capacity, batchSize);

            // Assert
            Assert.AreEqual(expectedBatches, store.TotalBatchCount,
                $"Failed for Capacity: {capacity}, BatchSize: {batchSize}. Expected {expectedBatches} batches.");
        }

        [Test]
        [TestCase(10, 0, 1)]    // 0 defaults to full capacity (10), so 1 batch
        [TestCase(100, 0, 1)]   // 0 defaults to full capacity (100), so 1 batch
        public void TotalBatchCount_WithBatchSizeZero_DefaultsToCapacity(int capacity, int batchSize, int expectedBatches)
        {
            // Arrange & Act
            var store = new MetadataStore<int>(capacity, batchSize);

            // Assert
            Assert.AreEqual(1, store.TotalBatchCount, $"With batchSize 0, capacity {capacity} should default to 1 batch.");
            Assert.AreEqual(capacity, store.BatchSize, $"With batchSize 0, BatchSize property should be equal to Capacity ({capacity}).");
        }

        [Test]
        public void AnyDirty_StartsAsFalse()
        {
            var store = new MetadataStore<int>(10, 1);

            // Newly initialized store must not be dirty
            Assert.IsFalse(store.AnyDirty);
        }

        [Test]
        public void DirtyBits_ProvidesReferenceToBitArray()
        {
            var store = new MetadataStore<int>(10, 2);

            // Ensure the property returns the BitArray instance and it has the correct length
            Assert.IsNotNull(store.DirtyBits);
            Assert.AreEqual(store.TotalBatchCount, store.DirtyBits.Length);
        }

        [Test]
        public void UntypedBuffer_ExposesInternalArray()
        {
            var store = new MetadataStore<int>(5, 1);

            // Validate that the returned Array is not null and has correct length
            Assert.IsInstanceOf<int[]>(store.UntypedBuffer);
            Assert.AreEqual(5, store.UntypedBuffer.Length);
        }

        [Test]
        public void TypedBuffer_ExposesCorrectGenericTypeArray()
        {
            var store = new MetadataStore<double>(5, 1);

            // Ensure the typed buffer is of the correct generic type
            Assert.IsInstanceOf<double[]>(store.TypedBuffer);
            Assert.AreEqual(5, store.TypedBuffer.Length);
        }

        [Test]
        public void AsSpan_ProvidesCorrectWindowToData()
        {
            var store = new MetadataStore<int>(5, 1);
            store.Set(2, 999);

            ReadOnlySpan<int> span = store.AsSpan();

            // Ensure the span contains the correct data at the expected index
            Assert.AreEqual(5, span.Length);
            Assert.AreEqual(999, span[2]);
        }

        #endregion

        #region Resize Tests

        [Test]
        [TestCase(1)]       // Smallest positive
        [TestCase(100)]     // Standard
        [TestCase(10000)]   // Large
        public void Resize_PositiveCapacity_UpdatesSuccessfully(int newCapacity)
        {
            // Arrange
            var store = new MetadataStore<int>(10, 2);

            // Act
            store.Resize(newCapacity);

            // Assert
            Assert.AreEqual(newCapacity, store.Capacity);
        }

        [Test]
        public void Resize_ZeroCapacity_ThrowsArgumentException()
        {
            // Arrange
            var store = new MetadataStore<int>(10, 2);

            // Assert
            Assert.Throws<ArgumentException>(() => store.Resize(0),
                "Resize with capacity 0 should throw ArgumentException.");
        }

        [Test]
        public void Resize_NegativeCapacity_ThrowsArgumentException()
        {
            // Arrange
            var store = new MetadataStore<int>(10, 2);

            // Assert
            Assert.Throws<ArgumentException>(() => store.Resize(-1),
                "Resize with negative capacity should throw ArgumentException.");
        }

        [Test]
        [TestCase(10, 5, 2)]    // Exact fit: 10 / 5 = 2
        [TestCase(11, 5, 3)]    // Remainder: 11 / 5 = 2.2 -> 3 batches
        [TestCase(1, 5, 1)]     // Smaller than batch: 1 / 5 = 0.2 -> 1 batch
        [TestCase(10, 10, 1)]   // Exact capacity match
        [TestCase(10, 11, 1)]   // Batch larger than capacity
        public void Resize_UpdatesTotalBatches_CorrectlyCalculated(int capacity, int batchSize, int expectedBatches)
        {
            // Arrange
            var store = new MetadataStore<int>(100, 1); // Initial state doesn't matter

            // Use reflection or a temporary update to set the batch size 
            // to match the scenario (assuming UpdateBatchSize exists)
            store.UpdateBatchSize(batchSize);

            // Act
            store.Resize(capacity);

            // Assert
            Assert.AreEqual(expectedBatches, store.TotalBatchCount,
                $"With capacity {capacity} and batchSize {batchSize}, expected {expectedBatches} batches.");
        }

        [Test]
        [TestCase(10)] // Test same capacity
        [TestCase(20)] // Test different capacity
        public void Resize_AlwaysClearsExistingData(int targetCapacity)
        {
            // Arrange
            var store = new MetadataStore<int>(10, 1);
            store.Set(0, 42);

            // Act
            store.Resize(targetCapacity);

            // Assert
            Assert.AreEqual(0, store.Get(0), $"Data must be cleared when resizing to {targetCapacity}.");
        }

        [Test]
        [TestCase(10)] // Test same capacity
        [TestCase(20)] // Test different capacity
        public void Resize_AlwaysResetsDirtyState(int targetCapacity)
        {
            // Arrange
            var store = new MetadataStore<int>(10, 1);
            store.Set(0, 1);
            Assert.IsTrue(store.AnyDirty);
            Assert.IsTrue(store.DirtyBits.Get(0));

            // Act
            store.Resize(targetCapacity);

            // Assert
            Assert.IsFalse(store.AnyDirty, $"AnyDirty flag must be false when resizing to {targetCapacity}.");

            // We check the first bit; if the BitArray was re-initialized, this will be false.
            Assert.IsFalse(store.DirtyBits.Get(0), $"Dirty bit for index 0 must be false when resizing to {targetCapacity}.");
        }

        [Test]
        public void Resize_InvalidCapacity_ThrowsArgumentException()
        {
            var store = new MetadataStore<int>(10, 1);

            // Assert
            Assert.Throws<ArgumentException>(() => store.Resize(0), "Resize should throw for 0 capacity.");
            Assert.Throws<ArgumentException>(() => store.Resize(-10), "Resize should throw for negative capacity.");
        }

        #endregion

        #region UpdateBatchSize Tests

        [Test]
        public void UpdateBatchSize_NegativeValue_ThrowsArgumentException()
        {
            var store = new MetadataStore<int>(10, 2);

            Assert.Throws<ArgumentException>(() => store.UpdateBatchSize(-1),
                "Negative batch size should throw ArgumentException.");
        }

        [Test]
        public void UpdateBatchSize_ZeroValue_DefaultsToCapacity()
        {
            int capacity = 100;
            var store = new MetadataStore<int>(capacity, 5);

            // Act
            store.UpdateBatchSize(0);

            // Assert
            Assert.AreEqual(capacity, store.BatchSize, "Batch size 0 should default to full capacity.");
            Assert.AreEqual(1, store.TotalBatchCount, "Total batches should be 1 when batch size equals capacity.");
        }

        [Test]
        public void UpdateBatchSize_SameBatchSize_PreservesDirtyState()
        {
            // Arrange
            var store = new MetadataStore<int>(10, 2);
            store.Set(0, 42); // Mark dirty
            Assert.IsTrue(store.AnyDirty, "Store should be dirty before UpdateBatchSize(same).");

            // Act: Update to same batch size (2)
            store.UpdateBatchSize(2);

            // Assert
            Assert.IsTrue(store.AnyDirty, "AnyDirty should be true after UpdateBatchSize(same).");
            Assert.IsTrue(store.DirtyBits.Get(0), "Dirty bits should be preserved after UpdateBatchSize(same).");
        }

        [Test]
        public void UpdateBatchSize_SameBatchSize_PreservesData()
        {
            // Arrange
            var store = new MetadataStore<int>(10, 2);
            store.Set(0, 42);

            // Act
            store.UpdateBatchSize(2);

            // Assert
            Assert.AreEqual(42, store.Get(0), "Data should be preserved when updating to same batch size.");
        }

        [Test]
        public void UpdateBatchSize_UpdatesBatchSizeProperty()
        {
            var store = new MetadataStore<int>(100, 10);
            store.UpdateBatchSize(16);

            // Verify only the BatchSize property
            Assert.AreEqual(16, store.BatchSize, "BatchSize property was not updated correctly.");
        }

        [Test]
        public void UpdateBatchSize_UpdatesTotalBatchCount()
        {
            var store = new MetadataStore<int>(100, 10);
            store.UpdateBatchSize(16);

            // Verify only the batch count calculation
            // 100 / 16 = 6.25 -> 7 batches
            Assert.AreEqual(7, store.TotalBatchCount, "TotalBatchCount was not calculated correctly for the new batch size.");
        }

        [Test]
        public void UpdateBatchSize_ResizesBitArrayLength()
        {
            var store = new MetadataStore<int>(100, 10);
            store.UpdateBatchSize(16);

            // Verify only the BitArray length matches the new batch count
            Assert.AreEqual(7, store.DirtyBits.Length, "The DirtyBits BitArray length must match the new TotalBatchCount.");
        }

        [Test]
        public void UpdateBatchSize_MaintainsFalseState_WhenNoBitsAreDirty()
        {
            var store = new MetadataStore<int>(100, 10);

            store.UpdateBatchSize(20);

            Assert.IsFalse(store.AnyDirty);
            for (int i = 0; i < store.DirtyBits.Length; i++)
            {
                Assert.IsFalse(store.DirtyBits.Get(i), $"Bit {i} should be false.");
            }
        }

        [Test]
        public void UpdateBatchSize_IncreaseSize_MergingDirtyBatches()
        {
            // 100 Capacity, initial 10 per batch (10 batches total)
            var store = new MetadataStore<int>(100, 10);

            // Mark batch 0 and 1 (Elements 0-19) as dirty
            store.MarkDirtyBatch(0);
            store.MarkDirtyBatch(1);

            // Resize to 20: 
            // Old 0 (0-9) + Old 1 (10-19) -> New Batch 0 (0-19)
            store.UpdateBatchSize(20);

            Assert.IsTrue(store.DirtyBits.Get(0), "New batch 0 should merge dirtiness.");
            Assert.IsFalse(store.DirtyBits.Get(1), "New batch 1 should be clean.");
        }

        [Test]
        public void UpdateBatchSize_PrimeBatchSize_CorrectMapping()
        {
            // 100 Capacity, initial 25 per batch (4 batches: 0, 1, 2, 3)
            var store = new MetadataStore<int>(100, 25);

            // Mark batch 3 (Elements 75-99) as dirty
            store.MarkDirtyBatch(3);

            // Re-batch to 33 (100 / 33 = 4 batches: 0, 1, 2, 3)
            // Element 75 is in new batch 2 (75/33 = 2)
            // Element 99 is in new batch 3 (99/33 = 3)
            store.UpdateBatchSize(33);

            Assert.IsTrue(store.DirtyBits.Get(2), "Batch 2 should be dirty due to overlap.");
            Assert.IsTrue(store.DirtyBits.Get(3), "Batch 3 should be dirty due to overlap.");
        }

        [Test]
        public void UpdateBatchSize_ToSingleElementBatches()
        {
            var store = new MetadataStore<int>(10, 5); // 2 batches
            store.MarkDirtyBatch(1); // Elements 5-9 are dirty

            // Change to 10 batches (one element per batch)
            store.UpdateBatchSize(1);

            for (int i = 5; i < 10; i++)
            {
                Assert.IsTrue(store.DirtyBits.Get(i), $"Batch {i} should be dirty.");
            }
        }

        [Test]
        public void UpdateBatchSize_FullDirtyStore_RemainsFullDirty()
        {
            var store = new MetadataStore<int>(100, 20);
            for (int i = 0; i < 5; i++) store.MarkDirtyBatch(i);

            // Resize to an awkward size
            store.UpdateBatchSize(13);

            int newTotalBatches = (int)Math.Ceiling(100.0 / 13);
            for (int i = 0; i < newTotalBatches; i++)
            {
                Assert.IsTrue(store.DirtyBits.Get(i), $"Batch {i} should be dirty.");
            }
        }

        [Test]
        public void UpdateBatchSize_ExactCapacityDivisions()
        {
            // Capacity 10, Batch size 3: Batches 0(0-2), 1(3-5), 2(6-8), 3(9)
            var store = new MetadataStore<int>(10, 3);
            store.MarkDirtyBatch(3); // Element 9 is dirty

            // Change to size 5: Batches 0(0-4), 1(5-9)
            store.UpdateBatchSize(5);

            Assert.IsTrue(store.DirtyBits.Get(1), "Batch 1 should be dirty as it contains element 9.");
            Assert.IsFalse(store.DirtyBits.Get(0), "Batch 0 should be clean.");
        }

        [Test]
        public void UpdateBatchSize_ComplexRebatching()
        {
            // 100 elements. Init: 7 per batch (15 batches total)
            // 100/7 = 14 full batches, 1 batch with 2 elements (0-99)
            var store = new MetadataStore<int>(100, 7);

            // Mark the last batch (the remainder batch containing 98-99)
            store.MarkDirtyBatch(14);

            // Update to 11
            store.UpdateBatchSize(11);

            // Element 98 is in new batch 8, element 99 in new batch 9.
            Assert.IsTrue(store.DirtyBits.Get(8) || store.DirtyBits.Get(9), "Dirty bits must have migrated to the new batch indices.");
        }

        #endregion

        #region MarkDirty Tests

        [Test]
        public void MarkDirty_FirstIndex_MarksBatchZero()
        {
            var store = new MetadataStore<int>(100, 10);

            store.MarkDirty(0);

            Assert.IsTrue(store.AnyDirty, "Store should report as dirty.");
            Assert.IsTrue(store.DirtyBits.Get(0), "Batch 0 should be marked dirty.");
        }

        [Test]
        public void MarkDirty_LastIndex_MarksCorrectBatch()
        {
            int capacity = 100;
            int batchSize = 10;
            var store = new MetadataStore<int>(capacity, batchSize);

            // Index 99 is the last element (Batch 9)
            store.MarkDirty(99);

            Assert.IsTrue(store.DirtyBits.Get(9), "Last batch should be marked dirty.");
        }

        [Test]
        public void MarkDirty_MultipleIndicesInSameBatch_OnlyAffectsOneBatch()
        {
            var store = new MetadataStore<int>(100, 10);

            store.MarkDirty(11); // Batch 1
            store.MarkDirty(15); // Batch 1
            store.MarkDirty(19); // Batch 1

            Assert.IsTrue(store.DirtyBits.Get(1), "Batch 1 should be dirty.");

            // Ensure no other batches were accidentally marked
            for (int i = 0; i < 10; i++)
            {
                if (i != 1) Assert.IsFalse(store.DirtyBits.Get(i), $"Batch {i} should remain clean.");
            }
        }

        [Test]
        public void MarkDirty_DifferentBatches_MarksAllRelevantBatches()
        {
            var store = new MetadataStore<int>(100, 10);

            store.MarkDirty(5);  // Batch 0
            store.MarkDirty(25); // Batch 2
            store.MarkDirty(85); // Batch 8

            Assert.IsTrue(store.DirtyBits.Get(0), "Batch 0 should be dirty.");
            Assert.IsTrue(store.DirtyBits.Get(2), "Batch 2 should be dirty.");
            Assert.IsTrue(store.DirtyBits.Get(8), "Batch 8 should be dirty.");
        }

        [Test]
        public void MarkDirty_BoundaryConditions_MarksBothBatches()
        {
            var store = new MetadataStore<int>(100, 10);

            store.MarkDirty(9);  // Last index of Batch 0
            store.MarkDirty(10); // First index of Batch 1

            Assert.IsTrue(store.DirtyBits.Get(0), "Batch 0 should be dirty.");
            Assert.IsTrue(store.DirtyBits.Get(1), "Batch 1 should be dirty.");
        }

        [Test]
        public void MarkDirty_OutOfBounds_ThrowsException()
        {
            var store = new MetadataStore<int>(100, 10);

            Assert.Throws<ArgumentOutOfRangeException>(() => store.MarkDirty(100));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.MarkDirty(-1));
        }

        #endregion

        #region MarkDirtyBatch Tests

        [Test]
        public void MarkDirtyBatch_FirstBatch_MarksCorrectly()
        {
            // Capacity 100, BatchSize 20 = 5 batches (0-4)
            var store = new MetadataStore<int>(100, 20);

            store.MarkDirtyBatch(0);

            Assert.IsTrue(store.AnyDirty, "Store should report as dirty.");
            Assert.IsTrue(store.DirtyBits.Get(0), "Batch 0 should be marked dirty.");
        }

        [Test]
        public void MarkDirtyBatch_LastBatch_MarksCorrectly()
        {
            var store = new MetadataStore<int>(100, 20);

            store.MarkDirtyBatch(4);

            Assert.IsTrue(store.DirtyBits.Get(4), "Last batch (4) should be marked dirty.");
        }

        [Test]
        public void MarkDirtyBatch_MultipleBatches_MarksAllIndividually()
        {
            var store = new MetadataStore<int>(100, 20);

            store.MarkDirtyBatch(1);
            store.MarkDirtyBatch(3);

            Assert.IsTrue(store.DirtyBits.Get(1), "Batch 1 should be dirty.");
            Assert.IsTrue(store.DirtyBits.Get(3), "Batch 3 should be dirty.");
            Assert.IsFalse(store.DirtyBits.Get(0), "Batch 0 should remain clean.");
            Assert.IsFalse(store.DirtyBits.Get(2), "Batch 2 should remain clean.");
            Assert.IsFalse(store.DirtyBits.Get(4), "Batch 4 should remain clean.");
        }

        [Test]
        public void MarkDirtyBatch_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            var store = new MetadataStore<int>(100, 20);

            Assert.Throws<ArgumentOutOfRangeException>(() => store.MarkDirtyBatch(-1));
        }

        [Test]
        public void MarkDirtyBatch_IndexEqualsTotalBatches_ThrowsArgumentOutOfRangeException()
        {
            var store = new MetadataStore<int>(100, 20); // 5 batches (0, 1, 2, 3, 4)

            Assert.Throws<ArgumentOutOfRangeException>(() => store.MarkDirtyBatch(5));
        }

        [Test]
        public void MarkDirtyBatch_IndexLargerThanTotalBatches_ThrowsArgumentOutOfRangeException()
        {
            var store = new MetadataStore<int>(50, 10); // 5 batches

            Assert.Throws<ArgumentOutOfRangeException>(() => store.MarkDirtyBatch(10));
        }

        #endregion

        #region MarkAllDirty Tests

        [Test]
        public void MarkAllDirty_FromCleanState_MarksAllBatches()
        {
            // 100 Capacity, 20 per batch = 5 batches
            var store = new MetadataStore<int>(100, 20);

            store.MarkAllDirty();

            Assert.IsTrue(store.AnyDirty, "AnyDirty should be true.");
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(store.DirtyBits.Get(i), $"Batch {i} should be dirty.");
            }
        }

        [Test]
        public void MarkAllDirty_FromPartiallyDirtyState_MarksAllBatches()
        {
            var store = new MetadataStore<int>(100, 20);
            store.MarkDirtyBatch(1); // Only batch 1 is dirty

            store.MarkAllDirty();

            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(store.DirtyBits.Get(i), $"Batch {i} should be dirty even if it was clean before.");
            }
        }

        [Test]
        public void MarkAllDirty_SingleBatchStore_WorksCorrectly()
        {
            var store = new MetadataStore<int>(10, 10);

            store.MarkAllDirty();

            Assert.IsTrue(store.DirtyBits.Get(0), "The single batch should be dirty.");
        }

        [Test]
        public void MarkAllDirty_AfterMixedOperations_EverythingRemainsDirty()
        {
            var store = new MetadataStore<int>(60, 20); // 3 batches: 0, 1, 2

            store.MarkDirtyBatch(0);
            store.MarkDirtyBatch(2);
            // Batch 1 is clean

            store.MarkAllDirty();

            Assert.IsTrue(store.DirtyBits.Get(1), "Batch 1 must become dirty.");
            Assert.IsTrue(store.DirtyBits.Get(0) && store.DirtyBits.Get(2), "Batches 0 and 2 must remain dirty.");
        }

        #endregion

        #region ClearDirty Tests

        [Test]
        public void ClearDirty_FromFullDirtyState_ClearsAllBits()
        {
            // 100 Capacity, 20 per batch = 5 batches
            var store = new MetadataStore<int>(100, 20);
            store.MarkAllDirty();

            store.ClearDirty();

            Assert.IsFalse(store.AnyDirty, "AnyDirty should be false.");
            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(store.DirtyBits.Get(i), $"Batch {i} should be clean.");
            }
        }

        [Test]
        public void ClearDirty_FromPartiallyDirtyState_ClearsOnlyDirtyBits()
        {
            var store = new MetadataStore<int>(100, 20);
            store.MarkDirtyBatch(1);
            store.MarkDirtyBatch(3);

            store.ClearDirty();

            Assert.IsFalse(store.AnyDirty, "AnyDirty should be false.");
            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(store.DirtyBits.Get(i), $"Batch {i} should be clean.");
            }
        }

        [Test]
        public void ClearDirty_FromCleanState_DoesNothing()
        {
            var store = new MetadataStore<int>(100, 20);

            // Assert state is initially clean
            Assert.IsFalse(store.AnyDirty);

            store.ClearDirty();

            Assert.IsFalse(store.AnyDirty, "Store should remain clean.");
            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(store.DirtyBits.Get(i), $"Batch {i} should remain clean.");
            }
        }

        [Test]
        public void ClearDirty_FollowedByMarkDirty_WorksCorrectly()
        {
            var store = new MetadataStore<int>(100, 20);

            store.MarkAllDirty();
            store.ClearDirty();

            // After clearing, mark just one batch
            store.MarkDirtyBatch(2);

            Assert.IsTrue(store.AnyDirty, "Store should be dirty again.");
            Assert.IsTrue(store.DirtyBits.Get(2), "Batch 2 should be dirty.");
            Assert.IsFalse(store.DirtyBits.Get(0), "Batch 0 should be clean.");
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ResetsCpuDataToDefaults()
        {
            var store = new MetadataStore<int>(100, 20);
            // Simulate data in the array
            for (int i = 0; i < 100; i++) store.TypedBuffer[i] = 42;

            store.Clear();

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(0, store.TypedBuffer[i], $"Index {i} should be reset to 0.");
            }
        }

        [Test]
        public void Clear_ResetsDirtyTracking()
        {
            var store = new MetadataStore<int>(100, 20);
            store.MarkAllDirty();

            store.Clear();

            Assert.IsFalse(store.AnyDirty, "AnyDirty should be reset to false.");
            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(store.DirtyBits.Get(i), $"Batch {i} should be clean.");
            }
        }

        [Test]
        public void Clear_HandlesPartialStateCorrectly()
        {
            var store = new MetadataStore<int>(100, 20);
            store.TypedBuffer[50] = 99; // Set a value
            store.MarkDirtyBatch(2); // Set a dirty bit

            store.Clear();

            Assert.AreEqual(0, store.TypedBuffer[50], "Data should be wiped.");
            Assert.IsFalse(store.DirtyBits.Get(2), "Dirty bit should be wiped.");
        }

        [Test]
        public void Clear_MultipleTimes_IsSafe()
        {
            var store = new MetadataStore<int>(100, 20);
            store.TypedBuffer[0] = 1;
            store.MarkDirtyBatch(0);

            store.Clear();
            store.Clear(); // Calling again (idempotency)

            Assert.AreEqual(0, store.TypedBuffer[0]);
            Assert.IsFalse(store.AnyDirty);
        }

        #endregion

        #region Set Tests

        [Test]
        public void Set_UpdatesDataAndMarksBatchDirty()
        {
            var store = new MetadataStore<int>(100, 20);
            int index = 25; // Inside batch 1 (20-39)
            int value = 42;

            store.Set(index, value);

            // Assert data update
            Assert.AreEqual(value, store.TypedBuffer[index], "The value should be stored correctly in CPU buffer.");

            // Assert dirty tracking
            Assert.IsTrue(store.AnyDirty, "Store should report as dirty.");
            Assert.IsTrue(store.DirtyBits.Get(1), "Batch 1 should be marked dirty.");
        }

        [Test]
        public void Set_OnCleanIndex_MarksBatchDirty()
        {
            var store = new MetadataStore<int>(100, 20);

            store.Set(0, 100);

            Assert.IsTrue(store.DirtyBits.Get(0), "Batch 0 should be dirty.");
        }

        [Test]
        public void Set_MultipleTimesInSameBatch_StateConsistent()
        {
            var store = new MetadataStore<int>(100, 20);

            store.Set(21, 10);
            store.Set(22, 20);

            Assert.AreEqual(20, store.TypedBuffer[22], "Data at index 22 should be updated.");
            Assert.IsTrue(store.DirtyBits.Get(1), "Batch 1 should be dirty.");
        }

        [Test]
        public void Set_BoundaryIndices_MarksCorrectBatches()
        {
            var store = new MetadataStore<int>(100, 20);

            store.Set(0, 1);   // Batch 0
            store.Set(99, 1);  // Batch 4

            Assert.IsTrue(store.DirtyBits.Get(0), "Batch 0 should be dirty.");
            Assert.IsTrue(store.DirtyBits.Get(4), "Batch 4 should be dirty.");
        }

        [Test]
        public void Set_OutOfBounds_ThrowsException()
        {
            var store = new MetadataStore<int>(100, 20);

            Assert.Throws<ArgumentOutOfRangeException>(() => store.Set(100, 42));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Set(-1, 42));
        }

        [Test]
        public void SetRange_AtStartOfStore_CopiesCorrectly()
        {
            var store = new MetadataStore<int>(100, 20);
            int[] source = { 1, 2, 3 };

            store.SetRange(0, source, 0, 3);

            Assert.AreEqual(1, store.Get(0));
            Assert.AreEqual(3, store.Get(2));
        }

        [Test]
        public void SetRange_AtEndOfStore_CopiesCorrectly()
        {
            var store = new MetadataStore<int>(100, 20);
            int[] source = { 99, 100 };

            // Copy 2 elements to the last 2 slots (98, 99)
            store.SetRange(98, source, 0, 2);

            Assert.AreEqual(99, store.Get(98));
            Assert.AreEqual(100, store.Get(99));
        }



        #endregion

        #region Get Tests

        [Test]
        public void Get_ReturnsCorrectValue_AfterSet()
        {
            var store = new MetadataStore<int>(100, 20);
            int index = 50;
            int value = 123;

            store.Set(index, value);

            Assert.AreEqual(value, store.Get(index), "The retrieved value should match the set value.");
        }

        [Test]
        public void Get_UninitializedIndex_ReturnsDefault()
        {
            var store = new MetadataStore<int>(100, 20);

            // Default for int is 0
            Assert.AreEqual(0, store.Get(10), "Uninitialized slots should return the type default.");
        }

        [Test]
        public void Get_BoundaryIndices_ReturnsValues()
        {
            var store = new MetadataStore<int>(100, 20);
            store.Set(0, 1);
            store.Set(99, 999);

            Assert.AreEqual(1, store.Get(0), "First index should be accessible.");
            Assert.AreEqual(999, store.Get(99), "Last index should be accessible.");
        }

        [Test]
        public void Get_OutOfBounds_ThrowsException()
        {
            var store = new MetadataStore<int>(100, 20);

            Assert.Throws<ArgumentOutOfRangeException>(() => store.Get(100));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Get(-1));
        }

        #endregion

        #region SetRange Tests

        [Test]
        public void SetRange_UpdatesDataAndMarksBatchesDirty()
        {
            var store = new MetadataStore<int>(100, 20);
            int[] source = { 1, 2, 3, 4, 5 };

            // Copy 5 elements into index 20 (start of batch 1)
            store.SetRange(20, source, 0, 5);

            // Assert data integrity
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(source[i], store.Get(20 + i), $"Data at index {20 + i} should match.");
            }

            // Assert dirty tracking: Batch 1 should be dirty, 0 and 2 should be clean
            Assert.IsTrue(store.DirtyBits.Get(1), "Batch 1 should be dirty.");
            Assert.IsFalse(store.DirtyBits.Get(0), "Batch 0 should be clean.");
            Assert.IsFalse(store.DirtyBits.Get(2), "Batch 2 should be clean.");
        }

        [Test]
        public void SetRange_SpanningMultipleBatches_MarksAllDirty()
        {
            var store = new MetadataStore<int>(100, 20);
            int[] source = new int[50]; // Large array

            // Copy into index 15. Covers indices 15-64.
            // Batch 0 (15-19), 1 (20-39), 2 (40-59), 3 (60-64)
            store.SetRange(15, source, 0, 50);

            Assert.IsTrue(store.DirtyBits.Get(0), "Batch 0 should be dirty.");
            Assert.IsTrue(store.DirtyBits.Get(1), "Batch 1 should be dirty.");
            Assert.IsTrue(store.DirtyBits.Get(2), "Batch 2 should be dirty.");
            Assert.IsTrue(store.DirtyBits.Get(3), "Batch 3 should be dirty.");
            Assert.IsFalse(store.DirtyBits.Get(4), "Batch 4 should remain clean.");
        }

        [Test]
        public void SetRange_NullSource_ThrowsArgumentNullException()
        {
            var store = new MetadataStore<int>(100, 20);
            Assert.Throws<ArgumentNullException>(() => store.SetRange(0, null, 0, 10));
        }

        [Test]
        public void SetRange_TargetOutOfBounds_ThrowsArgumentOutOfRangeException()
        {
            var store = new MetadataStore<int>(100, 20);
            int[] source = new int[10];

            // Start index valid, but length causes overflow
            Assert.Throws<ArgumentOutOfRangeException>(() => store.SetRange(95, source, 0, 10));

            // Negative start index
            Assert.Throws<ArgumentOutOfRangeException>(() => store.SetRange(-1, source, 0, 5));
        }

        [Test]
        public void SetRange_SourceOutOfBounds_ThrowsArgumentOutOfRangeException()
        {
            var store = new MetadataStore<int>(100, 20);
            int[] source = new int[10];

            // Source index out of bounds
            Assert.Throws<ArgumentOutOfRangeException>(() => store.SetRange(0, source, 5, 6));

            // Negative source index
            Assert.Throws<ArgumentOutOfRangeException>(() => store.SetRange(0, source, -1, 5));
        }

        [Test]
        public void SetRange_FullSourceArray_CopiesCorrectly()
        {
            var store = new MetadataStore<int>(10, 2);
            int[] source = { 1, 2, 3, 4 };

            store.SetRange(0, source, 0, 4);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(i + 1, store.Get(i));
            }
        }

        #endregion

        #region GetRange Tests

        [Test]
        public void GetRange_CopiesDataCorrectly()
        {
            var store = new MetadataStore<int>(100, 20);
            // Setup specific data
            store.Set(20, 10);
            store.Set(21, 20);
            store.Set(22, 30);

            int[] destination = new int[3];
            store.GetRange(20, destination, 0, 3);

            Assert.AreEqual(10, destination[0]);
            Assert.AreEqual(20, destination[1]);
            Assert.AreEqual(30, destination[2]);
        }

        [Test]
        public void GetRange_WithDestinationOffset_CopiesCorrectly()
        {
            var store = new MetadataStore<int>(100, 20);
            store.Set(0, 500);

            int[] destination = new int[5]; // Destination array size 5
            store.GetRange(0, destination, 4, 1); // Copy 1 element to index 4

            Assert.AreEqual(500, destination[4]);
            Assert.AreEqual(0, destination[0], "Other indices in destination should be untouched.");
        }

        [Test]
        public void GetRange_NullDestination_ThrowsArgumentNullException()
        {
            var store = new MetadataStore<int>(100, 20);
            Assert.Throws<ArgumentNullException>(() => store.GetRange(0, null, 0, 10));
        }

        [Test]
        public void GetRange_SourceOutOfBounds_ThrowsArgumentOutOfRangeException()
        {
            var store = new MetadataStore<int>(100, 20);
            int[] dest = new int[10];

            // Start index + length exceeds store capacity
            Assert.Throws<ArgumentOutOfRangeException>(() => store.GetRange(95, dest, 0, 10));

            // Negative start index
            Assert.Throws<ArgumentOutOfRangeException>(() => store.GetRange(-1, dest, 0, 5));
        }

        [Test]
        public void GetRange_DestinationOutOfBounds_ThrowsArgumentOutOfRangeException()
        {
            var store = new MetadataStore<int>(100, 20);
            int[] dest = new int[5];

            // Destination index + length exceeds destination capacity
            Assert.Throws<ArgumentOutOfRangeException>(() => store.GetRange(0, dest, 2, 4));

            // Negative destination index
            Assert.Throws<ArgumentOutOfRangeException>(() => store.GetRange(0, dest, -1, 1));
        }

        [Test]
        public void GetRange_FromStartOfStore_CopiesCorrectly()
        {
            var store = new MetadataStore<int>(100, 20);
            store.Set(0, 50);
            store.Set(1, 60);

            int[] destination = new int[2];
            store.GetRange(0, destination, 0, 2);

            Assert.AreEqual(50, destination[0]);
            Assert.AreEqual(60, destination[1]);
        }

        [Test]
        public void GetRange_FromEndOfStore_CopiesCorrectly()
        {
            var store = new MetadataStore<int>(100, 20);
            store.Set(98, 980);
            store.Set(99, 990);

            int[] destination = new int[2];
            store.GetRange(98, destination, 0, 2);

            Assert.AreEqual(980, destination[0]);
            Assert.AreEqual(990, destination[1]);
        }

        [Test]
        public void GetRange_FillFullDestinationArray_CopiesCorrectly()
        {
            var store = new MetadataStore<int>(10, 2);
            store.Set(5, 50);
            store.Set(6, 60);

            int[] destination = new int[2];
            store.GetRange(5, destination, 0, 2);

            Assert.AreEqual(50, destination[0]);
            Assert.AreEqual(60, destination[1]);
        }

        #endregion

        #region ForEach Tests

        [Test]
        public void ForEach_Int_IteratesAndSums()
        {
            var store = new MetadataStore<int>(3, 10);
            store.SetRange(0, new[] { 10, 20, 30 }, 0, 3);

            var action = new TestAction<int>();
            store.ForEach(ref action);

            Assert.AreEqual(3, action.CallCount);
            Assert.AreEqual(60.0, action.Sum);
        }

        [Test]
        public void ForEach_Vector3_IteratesAndSumsMagnitude()
        {
            var store = new MetadataStore<Vector3>(2, 10);
            var data = new[] { new Vector3(1, 0, 0), new Vector3(0, 1, 0) };
            store.SetRange(0, data, 0, 2);

            var action = new TestAction<Vector3>();
            store.ForEach(ref action);

            Assert.AreEqual(2, action.CallCount);
            Assert.AreEqual(2.0, action.Sum, 0.001);
        }

        [Test]
        public void ForEach_SingleElement_IteratesCorrectly()
        {
            var store = new MetadataStore<int>(1, 10);
            store.Set(0, 1337);
            var action = new TestAction<int>();

            store.ForEach(ref action);

            Assert.AreEqual(1, action.CallCount);
            Assert.AreEqual(1337, action.Sum);
        }

        #endregion

        #region GetIterator Tests

        [Test]
        public void GetIterator_Int_IteratesAndSums()
        {
            var store = new MetadataStore<int>(3, 10);
            store.SetRange(0, new[] { 10, 20, 30 }, 0, 3);

            double sum = 0;
            int count = 0;
            var it = store.GetIterator();

            while (it.MoveNext())
            {
                count++;
                sum += it.Current;
            }

            Assert.AreEqual(3, count);
            Assert.AreEqual(60.0, sum);
        }

        [Test]
        public void GetIterator_Vector3_IteratesAndSumsMagnitude()
        {
            var store = new MetadataStore<Vector3>(2, 10);
            var data = new[] { new Vector3(1, 0, 0), new Vector3(0, 1, 0) };
            store.SetRange(0, data, 0, 2);

            double sum = 0;
            int count = 0;
            var it = store.GetIterator();

            while (it.MoveNext())
            {
                count++;
                sum += it.Current.magnitude;
            }

            Assert.AreEqual(2, count);
            Assert.AreEqual(2.0, sum, 0.001);
        }

        [Test]
        public void GetIterator_SingleElement_IteratesCorrectly()
        {
            var store = new MetadataStore<int>(1, 10);
            store.Set(0, 1337);

            int count = 0;
            int value = 0;
            var it = store.GetIterator();

            while (it.MoveNext())
            {
                count++;
                value = it.Current;
            }

            Assert.AreEqual(1, count);
            Assert.AreEqual(1337, value);
        }

        #endregion

        #region ForEachDirtySegment Tests

        [Test]
        public void ForEachDirtySegment_WhenClean_DoesNothing()
        {
            var store = new MetadataStore<int>(10, 5);
            var action = new DirtyAction<int>();

            store.ForEachDirtySegment(ref action);

            // Verify that the action is not called when no data is marked as dirty
            Assert.AreEqual(0, action.CallCount, "Should not execute any action on a clean store.");
        }

        [Test]
        public void ForEachDirtySegment_WithDirtyData_ProcessesCorrectly()
        {
            var store = new MetadataStore<int>(10, 5);
            // Mark index 0 as dirty to trigger segment processing
            store.Set(0, 42);

            var action = new DirtyAction<int>();
            store.ForEachDirtySegment(ref action);

            // Ensure exactly one segment was identified and processed
            Assert.AreEqual(1, action.CallCount);
            Assert.AreEqual(5, action.TotalLength);
        }

        [Test]
        public void ForEachDirtySegment_MergeContiguous_HandlesSplitsCorrectly()
        {
            // Setup store with capacity 20 and batch size 5
            var store = new MetadataStore<int>(20, 5);

            // Dirty data in Batch 0 (Index 0) and Batch 2 (Index 10)
            // These are distinct ranges that should not be merged
            store.Set(0, 10);
            store.Set(10, 20);

            var action = new DirtyAction<int>();
            store.ForEachDirtySegment(ref action, mergeContiguous: true);

            // Verify they remain as 2 separate segments
            Assert.AreEqual(2, action.CallCount, "Should be 2 distinct segments.");
        }

        [Test]
        public void ForEachDirtySegment_MergeContiguous_MergesCorrectly()
        {
            var store = new MetadataStore<int>(20, 1);

            // Dirty data in index 0 and 1 (same batch)
            // These should be merged into a single segment
            store.Set(0, 10);
            store.Set(1, 20);

            var action = new DirtyAction<int>();
            store.ForEachDirtySegment(ref action, mergeContiguous: true);

            // Verify that contiguous dirty elements are combined
            Assert.AreEqual(1, action.CallCount, "Adjacent dirty elements should be merged into one segment.");
            Assert.AreEqual(2, action.TotalLength);
        }

        [Test]
        public void ForEachDirtySegment_MultipleBatches_ProcessesAll()
        {
            var store = new MetadataStore<int>(20, 5);

            // Dirty data in Batch 0 and Batch 1
            store.Set(0, 1);
            store.Set(6, 1);

            var action = new DirtyAction<int>();
            // Disable merge to process each batch independently
            store.ForEachDirtySegment(ref action, mergeContiguous: false);

            // Verify that both batches were processed
            Assert.AreEqual(2, action.CallCount);
        }

        #endregion

        #region ForEachDirtyIndex Tests

        [Test]
        public void ForEachDirtyIndex_WhenClean_DoesNothing()
        {
            // Store with enough capacity for 10 batches
            var store = new MetadataStore<int>(50, 5);
            var action = new IndexAction();

            store.ForEachDirtyIndex(ref action);

            // Verify no indices are processed if nothing is marked dirty
            Assert.AreEqual(0, action.CallCount, "Should not process any indices on a clean store.");
        }

        [Test]
        public void ForEachDirtyIndex_SingleDirtyBatch_ProcessesCorrectly()
        {
            var store = new MetadataStore<int>(50, 5);
            // Mark index 0 as dirty (Batch index 0)
            store.Set(0, 100);

            var action = new IndexAction();
            store.ForEachDirtyIndex(ref action);

            // Verify that batch index 0 was identified
            Assert.AreEqual(1, action.CallCount);
            Assert.Contains(0, action.Indices);
        }

        [Test]
        public void ForEachDirtyIndex_MultipleDirtyBatches_ProcessesAll()
        {
            var store = new MetadataStore<int>(50, 5);

            // Mark elements in Batch 0 and Batch 2 as dirty
            store.Set(0, 10);  // Batch 0
            store.Set(10, 20); // Batch 2 (indices 10-14)

            var action = new IndexAction();
            store.ForEachDirtyIndex(ref action);

            // Verify that both batch indices were found
            Assert.AreEqual(2, action.CallCount);
            Assert.Contains(0, action.Indices);
            Assert.Contains(2, action.Indices);
        }

        [Test]
        public void ForEachDirtyIndex_AllBatchesDirty_ProcessesAll()
        {
            var store = new MetadataStore<int>(10, 5);

            // Mark items in both Batch 0 and Batch 1
            store.Set(0, 1);
            store.Set(5, 1);

            var action = new IndexAction();
            store.ForEachDirtyIndex(ref action);

            // Verify all batches (0 and 1) are reported
            Assert.AreEqual(2, action.CallCount);
            Assert.Contains(0, action.Indices);
            Assert.Contains(1, action.Indices);
        }

        #endregion
    }
}
