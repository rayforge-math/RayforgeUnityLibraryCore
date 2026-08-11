using NUnit.Framework;
using System;
using System.Threading;

namespace Rayforge.Core.Common.Cache.Tests
{
    [TestFixture(typeof(int))]
    [TestFixture(typeof(string))]
    public class StaticArrayPoolTests<T>
    {
        #region Property Tests

        [Test]
        public void MaxPoolSize_ReturnsExpectedConfiguredValue()
        {
            // Assert: Verify that the public property matches the internal constant 
            // to ensure the pool's capacity is correctly exposed.
            Assert.AreEqual(1024, StaticArrayPool<T>.MaxPoolSize,
                "MaxPoolSize should match the defined capacity of the pool.");
        }

        #endregion

        #region Get Tests

        [Test]
        public void Get_ValidCount_ReturnsArrayOfCorrectLength()
        {
            var result = StaticArrayPool<T>.Get(10);
            Assert.AreEqual(10, result.Length);
        }

        [Test]
        public void Get_CountWithinPool_ReturnsCachedInstance()
        {
            // Erster Aufruf initialisiert das Array im Pool
            var first = StaticArrayPool<T>.Get(50);
            // Zweiter Aufruf muss dasselbe Objekt zurückgeben
            var second = StaticArrayPool<T>.Get(50);

            Assert.AreSame(first, second, "Get should return the same cached instance for the same count.");
        }

        [Test]
        public void Get_CountExceedingPool_ReturnsNewInstance()
        {
            int count = StaticArrayPool<T>.MaxPoolSize + 1;
            var first = StaticArrayPool<T>.Get(count);
            var second = StaticArrayPool<T>.Get(count);

            Assert.AreNotSame(first, second, "Requests exceeding MaxPoolSize should result in unique allocations.");
        }

        [Test]
        public void Get_CountLessThanOne_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StaticArrayPool<T>.Get(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => StaticArrayPool<T>.Get(-1));
        }

        [Test]
        public void Get_CalledFromBackgroundThread_ThrowsInvalidOperationException()
        {
            bool exceptionThrown = false;
            var thread = new Thread(() =>
            {
                try
                {
                    StaticArrayPool<T>.Get(10);
                }
                catch (InvalidOperationException)
                {
                    exceptionThrown = true;
                }
            });

            thread.Start();
            thread.Join();

            Assert.IsTrue(exceptionThrown, "Get should throw InvalidOperationException when called from a background thread.");
        }

        #endregion
    }
}
