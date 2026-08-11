using NUnit.Framework;

namespace Rayforge.Core.Common.LowLevel.Tests
{
    [TestFixture]
    public class BitFieldTests
    {
        #region Init Tests

        [Test]
        public void NewBitField_Value_ShouldBeZero()
        {
            // Arrange: Create a new BitField instance
            var bitField = new BitField();

            // Assert: Verify that the internal value is exactly 0
            Assert.AreEqual(0u, bitField.Value, "A new BitField must be initialized with a value of 0.");
        }

        [Test]
        public void NewBitField_Any_ShouldBeFalse()
        {
            // Arrange
            var bitField = new BitField();

            // Assert
            Assert.IsFalse(bitField.Any, "A new BitField must report 'Any' as false.");
        }

        #endregion

        #region Set Tests

        [Test]
        public void Set_ShouldCorrectlyUpdateValueAndAnyState()
        {
            // Arrange
            var bitField = new BitField();
            uint mask = 0b0000_1010; // Bit 1 and Bit 3

            // Act & Assert (Initial check)
            Assert.AreEqual(0u, bitField.Value);
            Assert.IsFalse(bitField.Any);

            // Act: Set bits
            bitField.Set(mask);

            // Assert: Check Value and Any
            Assert.AreEqual(mask, bitField.Value, "Value should match the mask after setting bits.");
            Assert.IsTrue(bitField.Any, "Any should be true after setting bits.");

            // Act: Set an additional bit
            uint additionalMask = 0b0000_0001; // Bit 0
            bitField.Set(additionalMask);

            // Assert: Check that previous bits are preserved (cumulative)
            Assert.AreEqual(mask | additionalMask, bitField.Value, "Setting bits should be cumulative.");
            Assert.IsTrue(bitField.Any);
        }

        [Test]
        public void Set_WithZeroMask_ShouldNotChangeState()
        {
            // Arrange
            var bitField = new BitField();
            bitField.Set(0b0000_0010); // Set Bit 1

            // Act
            bitField.Set(0); // Set "nothing"

            // Assert
            Assert.AreEqual(0b0000_0010u, bitField.Value, "Setting a 0 mask should not alter the state.");
        }

        [Test]
        public void Set_UsingNamedArgument_ShouldOverwrite()
        {
            // Arrange
            var bitField = new BitField();
            bitField.Set(0b1111);

            // Act
            bitField.Set(0b0001, overwrite: true);

            // Assert
            Assert.AreEqual(0b0001u, bitField.Value);
        }

        [Test]
        public void Set_WithoutOverwriteParameter_ShouldDefaultToCumulative()
        {
            // Arrange
            var bitField = new BitField();
            bitField.Set(0b0001);

            // Act
            bitField.Set(0b0010);

            // Assert
            Assert.AreEqual(0b0011u, bitField.Value, "Should default to cumulative (OR) behavior.");
        }

        #endregion

        #region Unset Tests

        [Test]
        public void Unset_ShouldRemoveOnlySpecifiedBits()
        {
            // Arrange: Set multiple bits (0111)
            var bitField = new BitField();
            uint initialMask = 0b0111;
            bitField.Set(initialMask);

            // Act: Clear specific bits (0010)
            uint unsetMask = 0b0010;
            bitField.Unset(unsetMask);

            // Assert: Only 0010 should be gone, 0101 should remain
            uint expected = 0b0101;
            Assert.AreEqual(expected, bitField.Value, "Unset did not clear the correct bits or affected others.");
        }

        [Test]
        public void Unset_WhenAllBitsAreSet_ShouldClearCorrectly()
        {
            // Arrange: Set all bits
            var bitField = new BitField();
            bitField.SetAll();

            // Act: Clear a subset
            uint mask = 0b1111;
            bitField.Unset(mask);

            // Assert: Ensure the remaining bits are still set
            Assert.AreEqual(~mask, bitField.Value, "Unset failed to clear the mask from a fully set field.");
        }

        [Test]
        public void Unset_WhenBitNotSet_ShouldRemainUnchanged()
        {
            // Arrange: Set bit 0 (0001)
            var bitField = new BitField();
            bitField.Set(0b0001);

            // Act: Try to unset bit 2 (0100) which is not set
            bitField.Unset(0b0100);

            // Assert: State should be unchanged
            Assert.AreEqual(0b0001u, bitField.Value, "Unset changed state even though bit was not set.");
            Assert.IsTrue(bitField.Any);
        }

        [Test]
        public void Unset_UntilEmpty_ShouldUpdateAnyState()
        {
            // Arrange
            var bitField = new BitField();
            bitField.Set(0b1111);

            // Act
            bitField.Unset(0b1111);

            // Assert
            Assert.AreEqual(0u, bitField.Value);
            Assert.IsFalse(bitField.Any, "Any should be false after all bits are unset.");
        }

        #endregion

        #region SetAll Tests

        [Test]
        public void SetAll_SetsAllBitsToTrue()
        {
            // Arrange
            var bitField = new BitField();

            // Act
            bitField.SetAll();

            // Assert
            Assert.AreEqual(0xFFFFFFFFu, bitField.Value, "SetAll should set all 32 bits to 1.");
            Assert.IsTrue(bitField.Any, "Any should be true after SetAll.");
        }

        [Test]
        public void SetAll_IsIdempotent()
        {
            // Arrange
            var bitField = new BitField();
            bitField.SetAll();

            // Act: SetAll again
            bitField.SetAll();

            // Assert: State should remain unchanged
            Assert.AreEqual(0xFFFFFFFFu, bitField.Value);
        }

        [Test]
        public void SetAll_FollowedByReset_ReturnsToZero()
        {
            // Arrange
            var bitField = new BitField();
            bitField.SetAll();

            // Act
            bitField.Reset();

            // Assert
            Assert.AreEqual(0u, bitField.Value, "Reset after SetAll should return to 0.");
            Assert.IsFalse(bitField.Any);
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_ShouldClearAllBitsRegardlessOfPreviousState()
        {
            // Arrange: Start with a partially filled field
            var bitField = new BitField();
            bitField.Set(0b10101010);

            // Act
            bitField.Reset();

            // Assert
            Assert.AreEqual(0u, bitField.Value, "Reset should clear all bits.");
            Assert.IsFalse(bitField.Any, "Any should be false after Reset.");
        }

        [Test]
        public void Reset_AfterSetAll_ShouldBeZero()
        {
            // Arrange
            var bitField = new BitField();
            bitField.SetAll();

            // Act
            bitField.Reset();

            // Assert
            Assert.AreEqual(0u, bitField.Value, "Reset should clear a fully filled field.");
        }

        [Test]
        public void Reset_OnAlreadyEmptyField_ShouldRemainEmpty()
        {
            // Arrange
            var bitField = new BitField();

            // Act
            bitField.Reset();

            // Assert
            Assert.AreEqual(0u, bitField.Value, "Reset on an empty field should do nothing.");
        }

        #endregion

        #region IsSetAny Tests

        [Test]
        public void IsSetAny_ShouldReturnTrue_WhenAtLeastOneBitMatches()
        {
            // Arrange: Set bits 0 and 2 (0101)
            var bitField = new BitField();
            bitField.Set(0b0101);

            // Act & Assert
            // Bit 0 (0001)
            Assert.IsTrue(bitField.IsSetAny(0b0001), "Should be true if one bit overlaps.");
            // Bit 1 and 2 (0110) -> Bit 2
            Assert.IsTrue(bitField.IsSetAny(0b0110), "Should be true if at least one bit overlaps.");
        }

        [Test]
        public void IsSetAny_ShouldReturnFalse_WhenNoBitsMatch()
        {
            // Arrange: Set bit 0 (0001)
            var bitField = new BitField();
            bitField.Set(0b0001);

            // Act & Assert
            // Bit 1 and 2 (0110)
            Assert.IsFalse(bitField.IsSetAny(0b0110), "Should be false if no bits overlap.");
        }

        [Test]
        public void IsSetAny_WithZeroMask_ShouldReturnFalse()
        {
            // Arrange
            var bitField = new BitField();
            bitField.SetAll();

            // Act & Assert
            Assert.IsFalse(bitField.IsSetAny(0), "IsSetAny with mask 0 must always be false.");
        }

        #endregion

        #region ContainsAny Tests

        [Test]
        public void ContainsAny_ShouldBeEquivalentToIsSetAny()
        {
            // Arrange
            var bitField = new BitField();
            bitField.Set(0b0101);

            // Act & Assert
            bool expectedResult = bitField.IsSetAny(0b0001);
            Assert.AreEqual(expectedResult, bitField.ContainsAny(0b0001), "ContainsAny must return the same result as IsSetAny.");
        }

        [Test]
        public void ContainsAny_ShouldReturnTrue_WhenAtLeastOneBitMatches()
        {
            // Arrange
            var bitField = new BitField();
            bitField.Set(0b0010);

            // Act & Assert
            Assert.IsTrue(bitField.ContainsAny(0b0011), "ContainsAny should return true if at least one bit (Bit 1) matches.");
        }

        [Test]
        public void ContainsAny_ShouldReturnFalse_WhenNoBitsMatch()
        {
            // Arrange
            var bitField = new BitField();
            bitField.Set(0b0001);

            // Act & Assert
            Assert.IsFalse(bitField.ContainsAny(0b0110), "ContainsAny should return false if no bits match.");
        }

        #endregion

        #region IsSet Tests

        [Test]
        public void IsSet_ShouldReturnTrue_WhenAllBitsInMaskAreSet()
        {
            // Arrange: Set bits 0 and 1 (0011)
            var bitField = new BitField();
            bitField.Set(0b0011);

            // Act & Assert
            // Check for subset (0001) - Should be true
            Assert.IsTrue(bitField.IsSet(0b0001), "IsSet should return true if the mask is a subset of the set bits.");
            // Check for exact match (0011) - Should be true
            Assert.IsTrue(bitField.IsSet(0b0011), "IsSet should return true for exact match.");
        }

        [Test]
        public void IsSet_ShouldReturnFalse_WhenAtLeastOneBitIsMissing()
        {
            // Arrange: Set bit 0 (0001)
            var bitField = new BitField();
            bitField.Set(0b0001);

            // Act & Assert
            Assert.IsFalse(bitField.IsSet(0b0011), "IsSet should return false if even one bit of the mask is missing.");
        }

        [Test]
        public void IsSet_WithZeroMask_ShouldReturnTrue()
        {
            // Arrange
            var bitField = new BitField();

            // Act & Assert
            Assert.IsTrue(bitField.IsSet(0), "IsSet with mask 0 should technically return true as 0 is a subset of any state.");
        }

        #endregion

        #region Contains

        [Test]
        public void Contains_ShouldReturnTrue_WhenAllBitsInMaskAreSet()
        {
            // Arrange: Set bits 0 and 1 (0011)
            var bitField = new BitField();
            bitField.Set(0b0011);

            // Act & Assert
            // Check for subset (0001) - Should be true
            Assert.IsTrue(bitField.Contains(0b0001), "Contains should return true if the mask is a subset of the set bits.");
            // Check for exact match (0011) - Should be true
            Assert.IsTrue(bitField.Contains(0b0011), "Contains should return true for exact match.");
        }

        [Test]
        public void Contains_ShouldReturnFalse_WhenAtLeastOneBitIsMissing()
        {
            // Arrange: Set bit 0 (0001)
            var bitField = new BitField();
            bitField.Set(0b0001);

            // Act & Assert
            Assert.IsFalse(bitField.Contains(0b0011), "Contains should return false if even one bit of the mask is missing.");
        }

        [Test]
        public void Contains_WithZeroMask_ShouldReturnTrue()
        {
            // Arrange
            var bitField = new BitField();

            // Act & Assert
            Assert.IsTrue(bitField.Contains(0), "Contains with mask 0 should technically return true as 0 is a subset of any state.");
        }

        #endregion
    }
}
