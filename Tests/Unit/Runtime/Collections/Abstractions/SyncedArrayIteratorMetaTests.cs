using NUnit.Framework;
using Rayforge.Core.TestEnv;

namespace Rayforge.Core.Collections.Abstractions.Tests
{
    [TestFixture(typeof(int), typeof(float))]
    [TestFixture(typeof(string), typeof(int))]
    public class SyncedArrayIteratorMetaTests<TValueA, TValueB>
    {
        [Test]
        public void Constructor_SetsAllPropertiesCorrectly()
        {
            // Arrange
            int expectedAbs = 5;
            int expectedRel = 2;

            TValueA valA = TestUtility.CreateSampleItems<TValueA>(1)[0];
            TValueB valB = TestUtility.CreateSampleItems<TValueB>(1)[0];

            // Act
            var meta = new SyncedArrayIteratorMeta<TValueA, TValueB>(expectedAbs, expectedRel, valA, valB);

            // Assert
            Assert.AreEqual(expectedAbs, meta.AbsoluteIndex, "AbsoluteIndex mismatch.");
            Assert.AreEqual(expectedRel, meta.RelativeIndex, "RelativeIndex mismatch.");
            Assert.AreEqual(valA, meta.ValueA, "ValueA mismatch.");
            Assert.AreEqual(valB, meta.ValueB, "ValueB mismatch.");
        }
    }
}
