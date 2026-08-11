using NUnit.Framework;
using System;

namespace Rayforge.Core.Collections.Abstractions.Tests
{
    [TestFixture(typeof(int), typeof(int))]
    [TestFixture(typeof(float), typeof(float))]
    [TestFixture(typeof(int), typeof(float))]
    public class SyncedBufferSegmentMetaTests<T1, T2>
        where T1 : unmanaged
        where T2 : unmanaged
    {
        #region Helpers

        private static BufferSegmentMeta<T> MakeSeg<T>(int start, int count)
            where T : unmanaged
            => new BufferSegmentMeta<T>
            {
                Source = new T[Math.Max(0, start + count)],
                Start = start,
                Count = count
            };

        private static BufferSegmentMeta<T> MakeEmpty<T>()
            where T : unmanaged
            => new BufferSegmentMeta<T>();

        #endregion

        #region Default Tests

        [Test]
        public void Default_SegmentA_IsEmpty()
        {
            var seg = new SyncedSegmentMeta<T1, T2>();
            Assert.IsTrue(seg.SegmentA.IsEmpty);
        }

        [Test]
        public void Default_SegmentB_IsEmpty()
        {
            var seg = new SyncedSegmentMeta<T1, T2>();
            Assert.IsTrue(seg.SegmentB.IsEmpty);
        }

        [Test]
        public void Default_HasWork_IsFalse()
        {
            var seg = new SyncedSegmentMeta<T1, T2>();
            Assert.IsFalse(seg.HasWork);
        }

        [Test]
        public void Default_Start_IsZero()
        {
            var seg = new SyncedSegmentMeta<T1, T2>();
            Assert.AreEqual(0, seg.Start);
        }

        [Test]
        public void Default_End_IsZero()
        {
            var seg = new SyncedSegmentMeta<T1, T2>();
            Assert.AreEqual(0, seg.End);
        }

        [Test]
        public void Default_TotalSpan_IsZero()
        {
            var seg = new SyncedSegmentMeta<T1, T2>();
            Assert.AreEqual(0, seg.TotalSpan);
        }

        #endregion
        
        #region HasWork Tests

        [Test]
        public void HasWork_BothEmpty_ReturnsFalse()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeEmpty<T1>(), SegmentB = MakeEmpty<T2>() };
            Assert.IsFalse(synced.HasWork);
        }

        [Test]
        public void HasWork_OnlySegmentA_ReturnsTrue()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeSeg<T1>(0, 3), SegmentB = MakeEmpty<T2>() };
            Assert.IsTrue(synced.HasWork);
        }

        [Test]
        public void HasWork_OnlySegmentB_ReturnsTrue()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeEmpty<T1>(), SegmentB = MakeSeg<T2>(0, 3) };
            Assert.IsTrue(synced.HasWork);
        }

        [Test]
        public void HasWork_BothPopulated_ReturnsTrue()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeSeg<T1>(0, 3), SegmentB = MakeSeg<T2>(0, 3) };
            Assert.IsTrue(synced.HasWork);
        }

        #endregion
        
        #region Start Tests

        [Test]
        public void Start_BothEmpty_ReturnsZero()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeEmpty<T1>(), SegmentB = MakeEmpty<T2>() };
            Assert.AreEqual(0, synced.Start);
        }

        [Test]
        public void Start_OnlySegmentA_ReturnsSegmentAStart()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeSeg<T1>(4, 3), SegmentB = MakeEmpty<T2>() };
            Assert.AreEqual(4, synced.Start);
        }

        [Test]
        public void Start_OnlySegmentB_ReturnsSegmentBStart()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeEmpty <T1>(), SegmentB = MakeSeg<T2>(6, 2) };
            Assert.AreEqual(6, synced.Start);
        }

        [Test]
        public void Start_BothPopulated_ReturnsMinimum()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeSeg<T1>(5, 3), SegmentB = MakeSeg<T2>(2, 4) };
            Assert.AreEqual(2, synced.Start);
        }

        [Test]
        public void Start_BothPopulated_SameStart_ReturnsThatStart()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeSeg<T1>(3, 2), SegmentB = MakeSeg<T2>(3, 5) };
            Assert.AreEqual(3, synced.Start);
        }

        #endregion
        
        #region End Tests

        [Test]
        public void End_BothEmpty_ReturnsZero()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeEmpty<T1>(), SegmentB = MakeEmpty<T2>() };
            Assert.AreEqual(0, synced.End);
        }

        [Test]
        public void End_OnlySegmentA_ReturnsSegmentAEnd()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeSeg<T1>(2, 4), SegmentB = MakeEmpty<T2>() };
            Assert.AreEqual(6, synced.End);
        }

        [Test]
        public void End_OnlySegmentB_ReturnsSegmentBEnd()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeEmpty<T1>(), SegmentB = MakeSeg<T2>(3, 5) };
            Assert.AreEqual(8, synced.End);
        }

        [Test]
        public void End_BothPopulated_ReturnsMaximum()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeSeg<T1>(0, 4), SegmentB = MakeSeg<T2>(2, 6) };
            Assert.AreEqual(8, synced.End);
        }

        [Test]
        public void End_BothPopulated_SameEnd_ReturnsThatEnd()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeSeg<T1>(0, 5), SegmentB = MakeSeg<T2>(2, 3) };
            Assert.AreEqual(5, synced.End);
        }

        #endregion
        
        #region TotalSpan Tests

        [Test]
        public void TotalSpan_BothEmpty_ReturnsZero()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeEmpty<T1>(), SegmentB = MakeEmpty<T2>() };
            Assert.AreEqual(0, synced.TotalSpan);
        }

        [Test]
        public void TotalSpan_OnlySegmentA_ReturnsSegmentACount()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeSeg<T1>(2, 4), SegmentB = MakeEmpty<T2>() };
            Assert.AreEqual(4, synced.TotalSpan);
        }

        [Test]
        public void TotalSpan_OnlySegmentB_ReturnsSegmentBCount()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeEmpty<T1>(), SegmentB = MakeSeg<T2>(3, 5) };
            Assert.AreEqual(5, synced.TotalSpan);
        }

        [Test]
        public void TotalSpan_NonOverlapping_ReturnsFullRange()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeSeg<T1>(2, 4), SegmentB = MakeSeg<T2>(8, 3) };
            Assert.AreEqual(9, synced.TotalSpan);
        }

        [Test]
        public void TotalSpan_Overlapping_ReturnsUnionRange()
        {
            var synced = new SyncedSegmentMeta<T1, T2> { SegmentA = MakeSeg<T1>(1, 4), SegmentB = MakeSeg<T2>(3, 5) };
            Assert.AreEqual(7, synced.TotalSpan);
        }

        #endregion
        
        #region Bullshit Scenarios (Negative Values, Reversed Order, Overflows)

        [Test]
        public void Nonsense_NegativeCount_IsIgnoredByIsEmpty()
        {
            // SegmentA: Start 10, Count -5 -> IsEmpty is TRUE because Count <= 0
            // SegmentB: Start 0, Count 2 -> IsEmpty is FALSE
            var synced = new SyncedSegmentMeta<T1, T2>
            {
                SegmentA = new BufferSegmentMeta<T1> { Source = new T1[10], Start = 10, Count = -5 },
                SegmentB = MakeSeg<T2>(0, 2)
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
            var synced = new SyncedSegmentMeta<T1, T2>
            {
                SegmentA = MakeSeg<T1>(-10, 5), // Range [-10, -5)
                SegmentB = MakeSeg<T2>(-20, 5)  // Range [-20, -15)
            };

            Assert.AreEqual(-20, synced.Start);
            Assert.AreEqual(-5, synced.End);
            Assert.AreEqual(15, synced.TotalSpan);
        }

        [Test]
        public void Nonsense_IntegerMaxValues_CalculatesWithoutCrash()
        {
            var dummy = new T1[1];

            var synced = new SyncedSegmentMeta<T1, T2>
            {
                SegmentA = new BufferSegmentMeta<T1>
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
            var synced = new SyncedSegmentMeta<T1, T2>
            {
                SegmentA = MakeSeg<T1>(-50, 0), // Count 0 -> IsEmpty = True
                SegmentB = MakeSeg<T2>(10, 10)  // Range [10, 20)
            };

            // Start should be 10, NOT -50, because SegmentA is empty
            Assert.AreEqual(10, synced.Start);
            Assert.AreEqual(20, synced.End);
            Assert.AreEqual(10, synced.TotalSpan);
        }

        [Test]
        public void Nonsense_NoSource_IsExcludedFromCalculation()
        {
            var synced = new SyncedSegmentMeta<T1, T2>
            {
                // Source is null -> IsEmpty = True
                SegmentA = new BufferSegmentMeta<T1> { Start = 1, Count = 100 },
                SegmentB = MakeSeg<T2>(10, 10)
            };

            Assert.AreEqual(10, synced.Start);
            Assert.AreEqual(10, synced.TotalSpan);
        }

        #endregion
    }
}