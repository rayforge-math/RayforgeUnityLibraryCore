using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Tests.Collections.Abstractions
{
    [TestFixture]
    public class SyncedBufferSegmentMetaTests
    {
        #region Helpers

        private static BufferSegmentMeta MakeSeg(int start, int count)
            => new BufferSegmentMeta { Source = new int[Math.Max(0, start + count)], Start = start, Count = count };

        private static BufferSegmentMeta EmptySeg()
            => new BufferSegmentMeta();

        #endregion

        #region Default Tests

        [Test]
        public void Default_SegmentA_IsEmpty()
        {
            var seg = new SyncedBufferSegmentMeta();
            Assert.IsTrue(seg.SegmentA.IsEmpty);
        }

        [Test]
        public void Default_SegmentB_IsEmpty()
        {
            var seg = new SyncedBufferSegmentMeta();
            Assert.IsTrue(seg.SegmentB.IsEmpty);
        }

        [Test]
        public void Default_HasWork_IsFalse()
        {
            var seg = new SyncedBufferSegmentMeta();
            Assert.IsFalse(seg.HasWork);
        }

        [Test]
        public void Default_Start_IsZero()
        {
            var seg = new SyncedBufferSegmentMeta();
            Assert.AreEqual(0, seg.Start);
        }

        [Test]
        public void Default_End_IsZero()
        {
            var seg = new SyncedBufferSegmentMeta();
            Assert.AreEqual(0, seg.End);
        }

        [Test]
        public void Default_TotalSpan_IsZero()
        {
            var seg = new SyncedBufferSegmentMeta();
            Assert.AreEqual(0, seg.TotalSpan);
        }

        #endregion

        #region Semantic aliases Tests

        [Test]
        public void Culling_AliasesSegmentA()
        {
            var a = MakeSeg(2, 4);
            var synced = new SyncedBufferSegmentMeta { SegmentA = a };
            Assert.AreEqual(synced.SegmentA.Start, synced.Culling.Start);
            Assert.AreEqual(synced.SegmentA.Count, synced.Culling.Count);
        }

        [Test]
        public void Render_AliasesSegmentB()
        {
            var b = MakeSeg(5, 3);
            var synced = new SyncedBufferSegmentMeta { SegmentB = b };
            Assert.AreEqual(synced.SegmentB.Start, synced.Render.Start);
            Assert.AreEqual(synced.SegmentB.Count, synced.Render.Count);
        }

        #endregion

        #region HasWork Tests

        [Test]
        public void HasWork_BothEmpty_ReturnsFalse()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = EmptySeg(), SegmentB = EmptySeg() };
            Assert.IsFalse(synced.HasWork);
        }

        [Test]
        public void HasWork_OnlySegmentA_ReturnsTrue()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = MakeSeg(0, 3), SegmentB = EmptySeg() };
            Assert.IsTrue(synced.HasWork);
        }

        [Test]
        public void HasWork_OnlySegmentB_ReturnsTrue()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = EmptySeg(), SegmentB = MakeSeg(0, 3) };
            Assert.IsTrue(synced.HasWork);
        }

        [Test]
        public void HasWork_BothPopulated_ReturnsTrue()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = MakeSeg(0, 3), SegmentB = MakeSeg(0, 3) };
            Assert.IsTrue(synced.HasWork);
        }

        #endregion

        #region Start Tests

        [Test]
        public void Start_BothEmpty_ReturnsZero()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = EmptySeg(), SegmentB = EmptySeg() };
            Assert.AreEqual(0, synced.Start);
        }

        [Test]
        public void Start_OnlySegmentA_ReturnsSegmentAStart()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = MakeSeg(4, 3), SegmentB = EmptySeg() };
            Assert.AreEqual(4, synced.Start);
        }

        [Test]
        public void Start_OnlySegmentB_ReturnsSegmentBStart()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = EmptySeg(), SegmentB = MakeSeg(6, 2) };
            Assert.AreEqual(6, synced.Start);
        }

        [Test]
        public void Start_BothPopulated_ReturnsMinimum()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = MakeSeg(5, 3), SegmentB = MakeSeg(2, 4) };
            Assert.AreEqual(2, synced.Start);
        }

        [Test]
        public void Start_BothPopulated_SameStart_ReturnsThatStart()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = MakeSeg(3, 2), SegmentB = MakeSeg(3, 5) };
            Assert.AreEqual(3, synced.Start);
        }

        #endregion

        #region End Tests

        [Test]
        public void End_BothEmpty_ReturnsZero()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = EmptySeg(), SegmentB = EmptySeg() };
            Assert.AreEqual(0, synced.End);
        }

        [Test]
        public void End_OnlySegmentA_ReturnsSegmentAEnd()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = MakeSeg(2, 4), SegmentB = EmptySeg() };
            Assert.AreEqual(6, synced.End);
        }

        [Test]
        public void End_OnlySegmentB_ReturnsSegmentBEnd()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = EmptySeg(), SegmentB = MakeSeg(3, 5) };
            Assert.AreEqual(8, synced.End);
        }

        [Test]
        public void End_BothPopulated_ReturnsMaximum()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = MakeSeg(0, 4), SegmentB = MakeSeg(2, 6) };
            Assert.AreEqual(8, synced.End);
        }

        [Test]
        public void End_BothPopulated_SameEnd_ReturnsThatEnd()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = MakeSeg(0, 5), SegmentB = MakeSeg(2, 3) };
            Assert.AreEqual(5, synced.End);
        }

        #endregion

        #region TotalSpan Tests

        [Test]
        public void TotalSpan_BothEmpty_ReturnsZero()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = EmptySeg(), SegmentB = EmptySeg() };
            Assert.AreEqual(0, synced.TotalSpan);
        }

        [Test]
        public void TotalSpan_OnlySegmentA_ReturnsSegmentACount()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = MakeSeg(2, 4), SegmentB = EmptySeg() };
            Assert.AreEqual(4, synced.TotalSpan);
        }

        [Test]
        public void TotalSpan_OnlySegmentB_ReturnsSegmentBCount()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = EmptySeg(), SegmentB = MakeSeg(3, 5) };
            Assert.AreEqual(5, synced.TotalSpan);
        }

        [Test]
        public void TotalSpan_NonOverlapping_ReturnsFullRange()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = MakeSeg(2, 4), SegmentB = MakeSeg(8, 3) };
            Assert.AreEqual(9, synced.TotalSpan);
        }

        [Test]
        public void TotalSpan_Overlapping_ReturnsUnionRange()
        {
            var synced = new SyncedBufferSegmentMeta { SegmentA = MakeSeg(1, 4), SegmentB = MakeSeg(3, 5) };
            Assert.AreEqual(7, synced.TotalSpan);
        }

        #endregion

        #region Bullshit Scenarios (Negative Values, Reversed Order, Overflows)

        [Test]
        public void Nonsense_NegativeCount_IsIgnoredByIsEmpty()
        {
            // SegmentA: Start 10, Count -5 -> IsEmpty is TRUE because Count <= 0
            // SegmentB: Start 0, Count 2 -> IsEmpty is FALSE
            var synced = new SyncedBufferSegmentMeta
            {
                SegmentA = new BufferSegmentMeta { Source = new int[10], Start = 10, Count = -5 },
                SegmentB = MakeSeg(0, 2)
            };

            // Only SegmentB is processed
            Assert.AreEqual(0, synced.Start);
            Assert.AreEqual(2, synced.End);
            Assert.AreEqual(2, synced.TotalSpan);
        }

        [Test]
        public void Nonsense_BothSegmentsNegativeOffsets_CalculatesCorrectSpan()
        {
            // Even with negative starts, if Count > 0 and Source != null, they are valid
            var synced = new SyncedBufferSegmentMeta
            {
                SegmentA = MakeSeg(-10, 5), // Range [-10, -5)
                SegmentB = MakeSeg(-20, 5)  // Range [-20, -15)
            };

            Assert.AreEqual(-20, synced.Start);
            Assert.AreEqual(-5, synced.End);
            Assert.AreEqual(15, synced.TotalSpan);
        }

        [Test]
        public void Nonsense_IntegerMaxValues_CalculatesWithoutCrash()
        {
            var dummy = new int[1];

            var synced = new SyncedBufferSegmentMeta
            {
                SegmentA = new BufferSegmentMeta
                {
                    Source = dummy,
                    Start = int.MaxValue - 5,
                    Count = 5
                }
            };

            // End should be (int.MaxValue - 5) + 5 = int.MaxValue
            Assert.AreEqual(int.MaxValue, synced.End);
            Assert.AreEqual(5, synced.TotalSpan);
        }

        [Test]
        public void Nonsense_ZeroCount_IsExcludedFromCalculation()
        {
            var synced = new SyncedBufferSegmentMeta
            {
                SegmentA = MakeSeg(-50, 0), // Count 0 -> IsEmpty = True
                SegmentB = MakeSeg(10, 10)  // Range [10, 20)
            };

            // Start should be 10, NOT -50, because SegmentA is empty
            Assert.AreEqual(10, synced.Start);
            Assert.AreEqual(20, synced.End);
            Assert.AreEqual(10, synced.TotalSpan);
        }

        [Test]
        public void Nonsense_NoSource_IsExcludedFromCalculation()
        {
            var synced = new SyncedBufferSegmentMeta
            {
                // Source is null -> IsEmpty = True
                SegmentA = new BufferSegmentMeta { Start = 1, Count = 100 },
                SegmentB = MakeSeg(10, 10)
            };

            Assert.AreEqual(10, synced.Start);
            Assert.AreEqual(10, synced.TotalSpan);
        }

        #endregion
    }
}