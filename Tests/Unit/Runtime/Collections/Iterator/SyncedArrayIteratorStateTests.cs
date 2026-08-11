using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.TestEnv;
using System;

namespace Rayforge.Core.Collections.Iterator.Tests
{
    [TestFixture(typeof(int), typeof(float))]
    [TestFixture(typeof(long), typeof(string))]
    public class SyncedArrayIteratorStateTests<T1, T2> : IIterationLogicTests<SyncedArrayIteratorMeta<T1, T2>, SyncedArrayIteratorState<T1, T2>>
    {
        #region Create Test Env

        protected override IterationTestData<SyncedArrayIteratorMeta<T1, T2>, SyncedArrayIteratorState<T1, T2>> CreateLogic(int count)
        {
            var data1 = TestUtility.CreateSampleItems<T1>(count);
            var data2 = TestUtility.CreateSampleItems<T2>(count);

            var expected = new SyncedArrayIteratorMeta<T1, T2>[count];
            for (int i = 0; i < count; ++i)
            {
                expected[i] = new SyncedArrayIteratorMeta<T1, T2>
                {
                    AbsoluteIndex = i,
                    RelativeIndex = i,
                    ValueA = data1[i],
                    ValueB = data2[i]
                };
            }

            var state = new SyncedArrayIteratorState<T1, T2>(
                data1,
                data2,
                0,
                count);

            return new IterationTestData<SyncedArrayIteratorMeta<T1, T2>, SyncedArrayIteratorState<T1, T2>>
            {
                expected = expected,
                logic = state
            };
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ValidInput_InitializesCorrectly()
        {
            int[] array = TestUtility.CreateSampleItems<int>(10);

            Assert.DoesNotThrow(() => new ArrayIteratorState<int>(array, 5, 5));
        }

        [Test]
        public void Constructor_NullArray_ThrowsArgumentNullException()
        {
            int[]? array = null;
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new ArrayIteratorState<int>(array!, 0, 1));

            Assert.AreEqual("array", ex.ParamName);
        }

        [Test]
        public void Constructor_NegativeStart_ThrowsArgumentOutOfRangeException()
        {
            int[] array = TestUtility.CreateSampleItems<int>(10);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArrayIteratorState<int>(array, -1, 1));

            Assert.AreEqual("start", ex.ParamName);
        }

        [Test]
        public void Constructor_StartExceedsLength_ThrowsArgumentOutOfRangeException()
        {
            int[] array = TestUtility.CreateSampleItems<int>(10);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArrayIteratorState<int>(array, 11, 1));

            Assert.AreEqual("start", ex.ParamName);
        }

        [Test]
        public void Constructor_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            int[] array = TestUtility.CreateSampleItems<int>(10);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArrayIteratorState<int>(array, 0, -1));

            Assert.AreEqual("count", ex.ParamName);
        }

        [Test]
        public void Constructor_RangeExceedsArrayLength_ThrowsArgumentOutOfRangeException()
        {
            int[] array = TestUtility.CreateSampleItems<int>(10);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ArrayIteratorState<int>(array, 5, 6)); // 5 + 6 = 11 > 10

            Assert.AreEqual("count", ex.ParamName);
        }

        #endregion
    }
}
