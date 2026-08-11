using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using System.Linq;

namespace Rayforge.Core.Collections.Iterator.Tests
{
    [TestFixture]
    public class EmptyStateTests
    {
        #region Self Tests

        [Test]
        public void Self_IsDefaultIterator()
        {
            var empty = EmptyState<int>.Self;
            Assert.IsNotNull(empty);
        }

        [Test]
        public void Self_ReturnsSameInstance()
        {
            var a = EmptyState<int>.Self;
            var b = EmptyState<int>.Self;
            Assert.AreEqual(a, b);
        }

        [Test]
        public void Self_ProducesNoElements()
        {
            int count = 0;
            foreach (var _ in EmptyState<int>.Self)
                count++;
            Assert.AreEqual(0, count);
        }

        [Test]
        public void Self_IsSeparatePerType()
        {
            // Each type parameter produces its own static instance
            var intEmpty = EmptyState<int>.Self;
            var stringEmpty = EmptyState<string>.Self;
            Assert.AreEqual(0, intEmpty.Count());
            Assert.AreEqual(0, stringEmpty.Count());
        }

        #endregion

        #region HasNext Tests

        [Test]
        public void HasNext_AlwaysReturnsFalse()
        {
            var state = new EmptyState<int>();
            bool result = state.HasNext(ref state);
            Assert.IsFalse(result);
        }

        [Test]
        public void HasNext_CalledMultipleTimes_AlwaysReturnsFalse()
        {
            var state = new EmptyState<int>();
            Assert.IsFalse(state.HasNext(ref state));
            Assert.IsFalse(state.HasNext(ref state));
            Assert.IsFalse(state.HasNext(ref state));
        }

        #endregion

        #region TryPeekNext Tests

        [Test]
        public void TryPeekNext_ReturnsFalse()
        {
            var state = new EmptyState<int>();
            bool result = state.TryPeekNext(ref state, out _);
            Assert.IsFalse(result);
        }

        [Test]
        public void TryPeekNext_OutputIsDefault()
        {
            var state = new EmptyState<int>();
            state.TryPeekNext(ref state, out int result);
            Assert.AreEqual(default(int), result);
        }

        [Test]
        public void TryPeekNext_OutputIsDefault_ReferenceType()
        {
            var state = new EmptyState<string>();
            state.TryPeekNext(ref state, out string result);
            Assert.IsNull(result);
        }

        [Test]
        public void TryPeekNext_CalledMultipleTimes_AlwaysReturnsFalse()
        {
            var state = new EmptyState<int>();
            Assert.IsFalse(state.TryPeekNext(ref state, out _));
            Assert.IsFalse(state.TryPeekNext(ref state, out _));
            Assert.IsFalse(state.TryPeekNext(ref state, out _));
        }

        #endregion

        #region MoveNext Tests

        [Test]
        public void MoveNext_ReturnsFalse()
        {
            var state = new EmptyState<int>();
            bool result = state.MoveNext(ref state, out _);
            Assert.IsFalse(result);
        }

        [Test]
        public void MoveNext_OutputIsDefault()
        {
            var state = new EmptyState<int>();
            state.MoveNext(ref state, out int result);
            Assert.AreEqual(default(int), result);
        }

        [Test]
        public void MoveNext_OutputIsDefault_ReferenceType()
        {
            var state = new EmptyState<string>();
            state.MoveNext(ref state, out string result);
            Assert.IsNull(result);
        }

        [Test]
        public void MoveNext_CalledMultipleTimes_AlwaysReturnsFalse()
        {
            var state = new EmptyState<int>();
            Assert.IsFalse(state.MoveNext(ref state, out _));
            Assert.IsFalse(state.MoveNext(ref state, out _));
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        #endregion
    }
}