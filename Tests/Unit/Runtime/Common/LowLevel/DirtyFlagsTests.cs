using NUnit.Framework;

namespace Rayforge.Core.Common.LowLevel.Tests
{
    [TestFixture]
    public class DirtyFlagsTests
    {
        #region Setup

        private DirtyFlags _dirtyFlags;
        private const uint TransformDirty = 1 << 0;
        private const uint MaterialDirty = 1 << 1;
        private const uint MeshDirty = 1 << 2;

        [SetUp]
        public void Setup() => _dirtyFlags = new DirtyFlags();

        #endregion

        #region Init Tests

        [Test]
        public void NewDirtyFlags_Value_ShouldBeZero()
        {
            // Arrange: Create a new instance
            var dirtyFlags = new DirtyFlags();

            // Assert: Verify that the internal bit storage is initialized to 0
            Assert.AreEqual(0u, dirtyFlags.Value, "A new DirtyFlags instance must have an internal value of 0.");
        }

        #endregion

        #region Property Tests

        [Test]
        public void Any_ShouldReflectInternalBitState()
        {
            // Arrange
            var dirtyFlags = new DirtyFlags();
            Assert.IsFalse(dirtyFlags.Any, "Initially, Any should be false.");

            // Act: Mark as dirty
            dirtyFlags.MarkDirty(0b0001);

            // Assert: Check that Any correctly reports the dirty state
            Assert.IsTrue(dirtyFlags.Any, "Any should be true after marking a flag as dirty.");

            // Act: Clear all flags
            dirtyFlags.ClearAll();

            // Assert: Verify state reset
            Assert.IsFalse(dirtyFlags.Any, "Any should be false after clearing all flags.");
        }

        #endregion

        #region MarkDirty Tests

        [Test]
        public void MarkDirty_ShouldSetInternalValue()
        {
            // Arrange
            var dirtyFlags = new DirtyFlags();
            uint mask = 0b0101; // Bit 0 and Bit 2

            // Act
            dirtyFlags.MarkDirty(mask);

            // Assert
            Assert.AreEqual(mask, dirtyFlags.Value, "Internal value should match the applied mask.");
            Assert.IsTrue(dirtyFlags.Any, "Any should be true after marking dirty flags.");
        }

        [Test]
        public void MarkDirty_ShouldBeCumulative()
        {
            // Arrange
            var dirtyFlags = new DirtyFlags();

            // Act
            dirtyFlags.MarkDirty(0b0001); // Set Bit 0
            dirtyFlags.MarkDirty(0b0010); // Set Bit 1

            // Assert
            uint expected = 0b0011;
            Assert.AreEqual(expected, dirtyFlags.Value, "MarkDirty should be cumulative (using OR logic).");
        }

        #endregion

        #region MarkAllDirty Tests

        [Test]
        public void MarkAllDirty_ShouldSetAllBitsToTrue()
        {
            // Arrange
            var dirtyFlags = new DirtyFlags();

            // Act
            dirtyFlags.MarkAllDirty();

            // Assert
            // 0xFFFFFFFF represents all 32 bits being set
            Assert.AreEqual(0xFFFFFFFFu, dirtyFlags.Value, "MarkAllDirty should set all 32 bits.");
            Assert.IsTrue(dirtyFlags.Any, "Any should be true after marking all flags dirty.");
        }

        [Test]
        public void MarkAllDirty_OverwritesPartialState()
        {
            // Arrange
            var dirtyFlags = new DirtyFlags();
            dirtyFlags.MarkDirty(0b0001); // Partially dirty

            // Act
            dirtyFlags.MarkAllDirty();

            // Assert
            Assert.AreEqual(0xFFFFFFFFu, dirtyFlags.Value, "MarkAllDirty should override any partial state.");
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ShouldOnlyRemoveSpecifiedFlags()
        {
            // Arrange: Mark multiple flags as dirty
            var dirtyFlags = new DirtyFlags();
            uint initialMask = 0b0111; // Bits 0, 1, 2
            dirtyFlags.MarkDirty(initialMask);

            // Act: Clear only Bit 1 (0010)
            dirtyFlags.Clear(0b0010);

            // Assert: Only Bit 1 should be gone, 0101 should remain
            uint expected = 0b0101;
            Assert.AreEqual(expected, dirtyFlags.Value, "Clear did not clear the correct bits or affected others.");
        }

        [Test]
        public void Clear_WhenBitNotDirty_ShouldRemainUnchanged()
        {
            // Arrange: Mark Bit 0 as dirty
            var dirtyFlags = new DirtyFlags();
            dirtyFlags.MarkDirty(0b0001);

            // Act: Try to clear Bit 2 (which is not dirty)
            dirtyFlags.Clear(0b0100);

            // Assert: State must be unchanged
            Assert.AreEqual(0b0001u, dirtyFlags.Value, "Clear should not affect bits that were not dirty.");
        }

        [Test]
        public void Clear_UntilAllEmpty_ShouldResetAnyState()
        {
            // Arrange
            var dirtyFlags = new DirtyFlags();
            dirtyFlags.MarkDirty(0b1111);

            // Act
            dirtyFlags.Clear(0b1111);

            // Assert
            Assert.AreEqual(0u, dirtyFlags.Value);
            Assert.IsFalse(dirtyFlags.Any, "Any should be false after all flags are cleared.");
        }

        #endregion

        #region ClearAll Tests

        [Test]
        public void ClearAll_ShouldResetToCleanState()
        {
            // Arrange: Set multiple flags
            var dirtyFlags = new DirtyFlags();
            dirtyFlags.MarkDirty(0b1010_1010);

            // Act
            dirtyFlags.ClearAll();

            // Assert
            Assert.AreEqual(0u, dirtyFlags.Value, "ClearAll must reset the value to 0.");
            Assert.IsFalse(dirtyFlags.Any, "After ClearAll, Any must be false.");
        }

        [Test]
        public void ClearAll_WhenAlreadyEmpty_ShouldRemainEmpty()
        {
            // Arrange
            var dirtyFlags = new DirtyFlags();

            // Act
            dirtyFlags.ClearAll();

            // Assert
            Assert.AreEqual(0u, dirtyFlags.Value, "ClearAll on an empty instance should not change anything.");
        }

        #endregion

        #region IsDirtyAny Tests

        [Test]
        public void IsDirtyAny_ShouldReturnTrue_WhenAtLeastOneFlagMatches()
        {
            // Arrange: Only Mark Dirty Flag 0 and 2
            var dirtyFlags = new DirtyFlags();
            dirtyFlags.MarkDirty(0b0101); // Bits 0 and 2

            // Act & Assert
            // Check with mask that contains Bit 0 -> Should be true
            Assert.IsTrue(dirtyFlags.IsDirtyAny(0b0001), "Should be true if bit 0 overlaps.");
            // Check with mask that contains Bit 1 and 2 -> Should be true (due to bit 2)
            Assert.IsTrue(dirtyFlags.IsDirtyAny(0b0110), "Should be true if bit 2 overlaps.");
        }

        [Test]
        public void IsDirtyAny_ShouldReturnFalse_WhenNoFlagsMatch()
        {
            // Arrange: Mark only Bit 0
            var dirtyFlags = new DirtyFlags();
            dirtyFlags.MarkDirty(0b0001);

            // Act & Assert
            // Check against Bit 1 and 2 -> No overlap
            Assert.IsFalse(dirtyFlags.IsDirtyAny(0b0110), "Should be false if none of the flags match.");
        }

        #endregion

        #region IsDirty Tests

        [Test]
        public void IsDirty_ShouldReturnTrue_WhenAllSpecifiedFlagsAreDirty()
        {
            // Arrange: Mark bits 0, 1, and 2 as dirty
            var dirtyFlags = new DirtyFlags();
            dirtyFlags.MarkDirty(0b0111);

            // Act & Assert
            // Check for exact match
            Assert.IsTrue(dirtyFlags.IsDirty(0b0111), "Should be true if all bits match.");
            // Check for subset (only bits 0 and 1)
            Assert.IsTrue(dirtyFlags.IsDirty(0b0011), "Should be true if mask is a subset of dirty flags.");
        }

        [Test]
        public void IsDirty_ShouldReturnFalse_WhenAnySpecifiedFlagIsMissing()
        {
            // Arrange: Only mark bit 0 as dirty
            var dirtyFlags = new DirtyFlags();
            dirtyFlags.MarkDirty(0b0001);

            // Act & Assert
            // Check against 0011 (bits 0 and 1) -> bit 1 is missing, so false
            Assert.IsFalse(dirtyFlags.IsDirty(0b0011), "Should be false if at least one bit in the mask is missing.");
        }

        [Test]
        public void IsDirty_WithZeroMask_ShouldReturnTrue()
        {
            // Arrange
            var dirtyFlags = new DirtyFlags();

            // Act & Assert
            // Consistent with the BitField logic: 0 is always contained in any state.
            Assert.IsTrue(dirtyFlags.IsDirty(0), "IsDirty with mask 0 should return true.");
        }

        #endregion
    }
}
