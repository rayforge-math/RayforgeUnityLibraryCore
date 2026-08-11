using NUnit.Framework;

namespace Rayforge.Core.Collections.Abstractions.Tests
{
    [TestFixture]
    public class SyncedBitIteratorMetaTests
    {
        [Test]
        public void Constructor_SetsAllPropertiesCorrectly()
        {
            // Arrange
            int expectedIndex = 123;
            bool expectedBitA = true;
            bool expectedBitB = false;

            // Act
            var meta = new SyncedBitIteratorMeta(expectedIndex, expectedBitA, expectedBitB);

            // Assert
            Assert.AreEqual(expectedIndex, meta.Index, "Index mismatch.");
            Assert.AreEqual(expectedBitA, meta.BitA, "BitA mismatch.");
            Assert.AreEqual(expectedBitB, meta.BitB, "BitB mismatch.");
        }
    }
}