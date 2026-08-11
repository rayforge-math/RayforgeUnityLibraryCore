using NUnit.Framework;
using Rayforge.Core.Collections.Helpers;
using System;
using Rayforge.Core.TestEnv;

namespace Rayforge.Core.Caching.Containers.Tests
{
    public class SnapshotCache
    {
        #region Constructors

        [Test]
        public void Constructor_Default_CountIsZero()
        {
            var cache = new SnapshotCache<int>();
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void Constructor_Default_CurrentIsEmpty()
        {
            var cache = new SnapshotCache<int>();
            Assert.AreEqual(0, cache.Current.Length);
        }

        [Test]
        public void Constructor_WithData_CountMatches()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            Assert.AreEqual(3, cache.Count);
        }

        [Test]
        public void Constructor_WithData_CurrentMatchesInput()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            CollectionAssert.AreEqual(samples, cache.Current.ToArray());
        }

        [Test]
        public void Constructor_WithEmptySpan_CountIsZero()
        {
            var cache = new SnapshotCache<int>(ReadOnlySpan<int>.Empty);
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void Constructor_WithEmptySpan_CurrentIsEmpty()
        {
            var cache = new SnapshotCache<int>(ReadOnlySpan<int>.Empty);
            Assert.AreEqual(0, cache.Current.Length);
        }

        [Test]
        public void Constructor_WithCapacity_CountMatches()
        {
            var cache = new SnapshotCache<int>(4);
            Assert.AreEqual(4, cache.Count);
        }

        [Test]
        public void Constructor_WithCapacity_CurrentIsDefaultValues()
        {
            var cache = new SnapshotCache<int>(3);
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, cache.Current.ToArray());
        }

        [Test]
        public void Constructor_WithZeroCapacity_CountIsZero()
        {
            var cache = new SnapshotCache<int>(0);
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void Constructor_WithNegativeCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SnapshotCache<int>(-1));
        }

        #endregion

        #region Apply(ReadOnlySpan<T>)

        [Test]
        public void Apply_Span_EmptyToEmpty_ReturnsFalse()
        {
            var cache = new SnapshotCache<int>();
            Assert.IsFalse(cache.Apply(ReadOnlySpan<int>.Empty));
        }

        [Test]
        public void Apply_Span_EmptyToData_ReturnsTrue()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>();
            Assert.IsTrue(cache.Apply(samples));
        }

        [Test]
        public void Apply_Span_EmptyToData_UpdatesCache()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>();
            cache.Apply(samples);
            CollectionAssert.AreEqual(samples, cache.Current.ToArray());
        }

        [Test]
        public void Apply_Span_SameData_ReturnsFalse()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            Assert.IsFalse(cache.Apply(samples));
        }

        [Test]
        public void Apply_Span_SameData_CacheUnchanged()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            cache.Apply(samples);
            CollectionAssert.AreEqual(samples, cache.Current.ToArray());
        }

        [Test]
        public void Apply_Span_DifferentValues_ReturnsTrue()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2, 3 });
            Assert.IsTrue(cache.Apply(new[] { 1, 2, 99 }));
        }

        [Test]
        public void Apply_Span_DifferentValues_UpdatesCache()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2, 3 });
            cache.Apply(new[] { 1, 2, 99 });
            CollectionAssert.AreEqual(new[] { 1, 2, 99 }, cache.Current.ToArray());
        }

        [Test]
        public void Apply_Span_ShorterData_ReturnsTrue()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2, 3 });
            Assert.IsTrue(cache.Apply(new[] { 1, 2 }));
        }

        [Test]
        public void Apply_Span_ShorterData_UpdatesCache()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2, 3 });
            cache.Apply(new[] { 1, 2 });
            CollectionAssert.AreEqual(new[] { 1, 2 }, cache.Current.ToArray());
        }

        [Test]
        public void Apply_Span_LongerData_ReturnsTrue()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2 });
            Assert.IsTrue(cache.Apply(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Apply_Span_LongerData_UpdatesCache()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2 });
            cache.Apply(new[] { 1, 2, 3 });
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, cache.Current.ToArray());
        }

        [Test]
        public void Apply_Span_DataToEmpty_ReturnsTrue()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            Assert.IsTrue(cache.Apply(ReadOnlySpan<int>.Empty));
        }

        [Test]
        public void Apply_Span_DataToEmpty_ClearsCache()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            cache.Apply(ReadOnlySpan<int>.Empty);
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void Apply_Span_SingleElement_SameValue_ReturnsFalse()
        {
            var cache = new SnapshotCache<int>(new[] { 42 });
            Assert.IsFalse(cache.Apply(new[] { 42 }));
        }

        [Test]
        public void Apply_Span_SingleElement_DifferentValue_ReturnsTrue()
        {
            var cache = new SnapshotCache<int>(new[] { 42 });
            Assert.IsTrue(cache.Apply(new[] { 43 }));
        }

        [Test]
        public void Apply_Span_CountUpdatesAfterChange()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2, 3 });
            cache.Apply(new[] { 1, 2 });
            Assert.AreEqual(2, cache.Count);
        }

        [Test]
        public void Apply_Span_CountUnchangedWhenNoChange()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            cache.Apply(samples);
            Assert.AreEqual(3, cache.Count);
        }

        [Test]
        public void Apply_Span_MultipleSequentialChanges_TracksCorrectly()
        {
            var cache = new SnapshotCache<int>();
            cache.Apply(new[] { 1 });
            cache.Apply(new[] { 1, 2 });
            cache.Apply(new[] { 1, 2, 3 });
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, cache.Current.ToArray());
        }

        #endregion

        #region Apply(TIterator) via Array.ToIterator()

        [Test]
        public void Apply_ArrayIterator_EmptyToEmpty_ReturnsFalse()
        {
            var cache = new SnapshotCache<int>();
            Assert.IsFalse(cache.Apply(Array.Empty<int>().ToIterator()));
        }

        [Test]
        public void Apply_ArrayIterator_EmptyToData_ReturnsTrue()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>();
            Assert.IsTrue(cache.Apply(samples.ToIterator()));
        }

        [Test]
        public void Apply_ArrayIterator_EmptyToData_UpdatesCache()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>();
            cache.Apply(samples.ToIterator());
            CollectionAssert.AreEqual(samples, cache.Current.ToArray());
        }

        [Test]
        public void Apply_ArrayIterator_SameData_ReturnsFalse()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            Assert.IsFalse(cache.Apply(samples.ToIterator()));
        }

        [Test]
        public void Apply_ArrayIterator_SameData_CacheUnchanged()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            cache.Apply(samples.ToIterator());
            CollectionAssert.AreEqual(samples, cache.Current.ToArray());
        }

        [Test]
        public void Apply_ArrayIterator_DifferentValues_ReturnsTrue()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2, 3 });
            Assert.IsTrue(cache.Apply(new[] { 1, 2, 99 }.ToIterator()));
        }

        [Test]
        public void Apply_ArrayIterator_DifferentValues_UpdatesCache()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2, 3 });
            cache.Apply(new[] { 1, 2, 99 }.ToIterator());
            CollectionAssert.AreEqual(new[] { 1, 2, 99 }, cache.Current.ToArray());
        }

        [Test]
        public void Apply_ArrayIterator_ShorterData_ReturnsTrue()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2, 3 });
            Assert.IsTrue(cache.Apply(new[] { 1, 2 }.ToIterator()));
        }

        [Test]
        public void Apply_ArrayIterator_ShorterData_UpdatesCache()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2, 3 });
            cache.Apply(new[] { 1, 2 }.ToIterator());
            CollectionAssert.AreEqual(new[] { 1, 2 }, cache.Current.ToArray());
        }

        [Test]
        public void Apply_ArrayIterator_ShorterData_CountUpdates()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2, 3 });
            cache.Apply(new[] { 1, 2 }.ToIterator());
            Assert.AreEqual(2, cache.Count);
        }

        [Test]
        public void Apply_ArrayIterator_LongerData_ReturnsTrue()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2 });
            Assert.IsTrue(cache.Apply(new[] { 1, 2, 3 }.ToIterator()));
        }

        [Test]
        public void Apply_ArrayIterator_LongerData_UpdatesCache()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2 });
            cache.Apply(new[] { 1, 2, 3 }.ToIterator());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, cache.Current.ToArray());
        }

        [Test]
        public void Apply_ArrayIterator_LongerData_CountUpdates()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2 });
            cache.Apply(new[] { 1, 2, 3 }.ToIterator());
            Assert.AreEqual(3, cache.Count);
        }

        [Test]
        public void Apply_ArrayIterator_DataToEmpty_ReturnsTrue()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            Assert.IsTrue(cache.Apply(Array.Empty<int>().ToIterator()));
        }

        [Test]
        public void Apply_ArrayIterator_DataToEmpty_CountIsZero()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            cache.Apply(Array.Empty<int>().ToIterator());
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void Apply_ArrayIterator_DataToEmpty_CurrentIsEmpty()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            cache.Apply(Array.Empty<int>().ToIterator());
            Assert.AreEqual(0, cache.Current.Length);
        }

        [Test]
        public void Apply_ArrayIterator_SingleElement_SameValue_ReturnsFalse()
        {
            var cache = new SnapshotCache<int>(new[] { 42 });
            Assert.IsFalse(cache.Apply(new[] { 42 }.ToIterator()));
        }

        [Test]
        public void Apply_ArrayIterator_SingleElement_DifferentValue_ReturnsTrue()
        {
            var cache = new SnapshotCache<int>(new[] { 42 });
            Assert.IsTrue(cache.Apply(new[] { 43 }.ToIterator()));
        }

        [Test]
        public void Apply_ArrayIterator_CountUpdatesAfterChange()
        {
            var cache = new SnapshotCache<int>(new[] { 1, 2, 3 });
            cache.Apply(new[] { 1, 2 }.ToIterator());
            Assert.AreEqual(2, cache.Count);
        }

        [Test]
        public void Apply_ArrayIterator_CountUnchangedWhenNoChange()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            cache.Apply(samples.ToIterator());
            Assert.AreEqual(3, cache.Count);
        }

        [Test]
        public void Apply_ArrayIterator_MultipleSequentialChanges_TracksCorrectly()
        {
            var cache = new SnapshotCache<int>();
            cache.Apply(new[] { 1 }.ToIterator());
            cache.Apply(new[] { 1, 2 }.ToIterator());
            cache.Apply(new[] { 1, 2, 3 }.ToIterator());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, cache.Current.ToArray());
        }

        [Test]
        public void Apply_ArrayIterator_NeverShrinksBuffer_AfterShrink()
        {
            var samples = TestUtility.CreateSampleItems<int>(5);
            var smallerSamples = samples.AsSpan(0, 2).ToArray();

            var cache = new SnapshotCache<int>(samples);
            cache.Apply(smallerSamples.ToIterator());
            bool changed = cache.Apply(samples.ToIterator());
            Assert.IsTrue(changed);
            CollectionAssert.AreEqual(samples, cache.Current.ToArray());
        }

        #endregion

        #region Span vs Iterator consistency

        [Test]
        public void Apply_SpanThenIterator_SameData_ReturnsFalse()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>(samples);
            Assert.IsFalse(cache.Apply(samples.ToIterator()));
        }

        [Test]
        public void Apply_IteratorThenSpan_SameData_ReturnsFalse()
        {
            var samples = TestUtility.CreateSampleItems<int>(3);

            var cache = new SnapshotCache<int>();
            cache.Apply(samples.ToIterator());
            Assert.IsFalse(cache.Apply(samples));
        }

        #endregion
    }
}
