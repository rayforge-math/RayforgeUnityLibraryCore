using NUnit.Framework;

namespace Rayforge.Core.Collections.Abstractions.Tests
{
    [TestFixture]
    public class BufferSegmentMetaTests
    {
        #region Constructor Tests

        [Test]
        public void Default_SourceIsNull()
        {
            var seg = new BufferSegmentMeta();
            Assert.IsNull(seg.Source);
        }

        [Test]
        public void Default_StartIsZero()
        {
            var seg = new BufferSegmentMeta();
            Assert.AreEqual(0, seg.Start);
        }

        [Test]
        public void Default_CountIsZero()
        {
            var seg = new BufferSegmentMeta();
            Assert.AreEqual(0, seg.Count);
        }

        [Test]
        public void Default_IsEmpty()
        {
            var seg = new BufferSegmentMeta();
            Assert.IsTrue(seg.IsEmpty);
        }

        #endregion

        #region End Tests

        [Test]
        public void End_EqualsStartPlusCount()
        {
            var seg = new BufferSegmentMeta { Start = 3, Count = 5 };
            Assert.AreEqual(8, seg.End);
        }

        [Test]
        public void End_WhenStartIsZero_EqualsCount()
        {
            var seg = new BufferSegmentMeta { Start = 0, Count = 4 };
            Assert.AreEqual(4, seg.End);
        }

        [Test]
        public void End_WhenCountIsZero_EqualsStart()
        {
            var seg = new BufferSegmentMeta { Start = 5, Count = 0 };
            Assert.AreEqual(5, seg.End);
        }

        #endregion

        #region IsEmpty

        [Test]
        public void IsEmpty_WhenSourceIsNull_ReturnsTrue()
        {
            var seg = new BufferSegmentMeta { Source = null, Count = 5 };
            Assert.IsTrue(seg.IsEmpty);
        }

        [Test]
        public void IsEmpty_WhenCountIsZero_ReturnsTrue()
        {
            var seg = new BufferSegmentMeta { Source = new int[5], Count = 0 };
            Assert.IsTrue(seg.IsEmpty);
        }

        [Test]
        public void IsEmpty_WhenCountIsNegative_ReturnsTrue()
        {
            var seg = new BufferSegmentMeta { Source = new int[5], Count = -1 };
            Assert.IsTrue(seg.IsEmpty);
        }

        [Test]
        public void IsEmpty_WhenSourceAndCountValid_ReturnsFalse()
        {
            var seg = new BufferSegmentMeta { Source = new int[5], Start = 0, Count = 3 };
            Assert.IsFalse(seg.IsEmpty);
        }

        #endregion

        #region Contains

        [Test]
        public void Contains_IndexAtStart_ReturnsTrue()
        {
            var seg = new BufferSegmentMeta { Start = 3, Count = 5 };
            Assert.IsTrue(seg.Contains(3));
        }

        [Test]
        public void Contains_IndexAtEndMinusOne_ReturnsTrue()
        {
            var seg = new BufferSegmentMeta { Start = 3, Count = 5 };
            Assert.IsTrue(seg.Contains(7));
        }

        [Test]
        public void Contains_IndexAtEnd_ReturnsFalse()
        {
            var seg = new BufferSegmentMeta { Start = 3, Count = 5 };
            Assert.IsFalse(seg.Contains(8));
        }

        [Test]
        public void Contains_IndexBeforeStart_ReturnsFalse()
        {
            var seg = new BufferSegmentMeta { Start = 3, Count = 5 };
            Assert.IsFalse(seg.Contains(2));
        }

        [Test]
        public void Contains_IndexInMiddle_ReturnsTrue()
        {
            var seg = new BufferSegmentMeta { Start = 3, Count = 5 };
            Assert.IsTrue(seg.Contains(5));
        }

        [Test]
        public void Contains_WhenCountIsZero_ReturnsFalse()
        {
            var seg = new BufferSegmentMeta { Start = 3, Count = 0 };
            Assert.IsFalse(seg.Contains(3));
        }

        [Test]
        public void Contains_NegativeIndex_ReturnsFalse()
        {
            var seg = new BufferSegmentMeta { Start = 0, Count = 5 };
            Assert.IsFalse(seg.Contains(-1));
        }

        [Test]
        public void Contains_SingleElementSegment_ContainsOnlyStart()
        {
            var seg = new BufferSegmentMeta { Start = 4, Count = 1 };
            Assert.IsTrue(seg.Contains(4));
            Assert.IsFalse(seg.Contains(3));
            Assert.IsFalse(seg.Contains(5));
        }

        #endregion
    }
}