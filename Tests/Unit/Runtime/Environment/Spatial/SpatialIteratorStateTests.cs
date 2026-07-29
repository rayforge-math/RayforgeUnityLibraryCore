using NUnit.Framework;
using NUnit.Framework.Internal;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.Collections.Iterator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Tests
{
    public class MeshRenderer_SpatialIteratorStateTests : SpatialIteratorStateTests<MeshRenderer>
    {
        #region Create Test Object

        protected override MeshRenderer CreateEntity()
        {
            return new MeshRenderer();
        }

        #endregion

        #region Stress Test

        [Test]
        public void SpatialIteratorState_HandlesMissingEntriesAndComplexBucketsCorrectly()
        {
            // Arrange
            var registry = new Dictionary<int, SpatialState<MeshRenderer>>();

            var rendererA = new MeshRenderer();
            var rendererB = new MeshRenderer();
            var rendererC = new MeshRenderer();

            registry[10] = new SpatialState<MeshRenderer> { component = rendererA };
            registry[20] = new SpatialState<MeshRenderer> { component = rendererB };
            registry[30] = new SpatialState<MeshRenderer> { component = rendererC };

            var bucket = new HashSet<int> { 5, 10, 99, 30, 20, 40 };

            // Direct instantiation of the raw state struct
            var state = new SpatialIteratorState<int, MeshRenderer, HashSet<int>.Enumerator>(
                bucket.GetEnumerator(),
                registry
            );

            // Act
            var results = new List<MeshRenderer>();

            // Iterating directly using the struct methods
            while (state.MoveNext(ref state, out var value))
            {
                results.Add(value);
            }

            // Assert
            Assert.That(results, Is.EquivalentTo(new[] { rendererA, rendererB, rendererC }));
        }

        #endregion
    }

    public abstract class SpatialIteratorStateTests<T> : IIterationLogicTests<T, SpatialIteratorState<int, T, HashSet<int>.Enumerator>>
    {
        #region IIterationLogicTests

        protected override IterationTestData<T, SpatialIteratorState<int, T, HashSet<int>.Enumerator>> CreateLogic(int count)
        {
            var bucket = new HashSet<int>();
            var expectedValues = new List<T>(count);
            var registry = new Dictionary<int, SpatialState<T>>();

            for (int i = 0; i < count; i++)
            {
                int id = i;
                bucket.Add(id);

                T val = CreateEntity();
                expectedValues.Add(val);

                registry[id] = new SpatialState<T> { component = val };
            }

            var state = new SpatialIteratorState<int, T, HashSet<int>.Enumerator>(
                bucket.GetEnumerator(),
                registry
            );

            return new IterationTestData<T, SpatialIteratorState<int, T, HashSet<int>.Enumerator>>
            {
                expected = expectedValues.ToArray(),
                logic = state
            };
        }

        protected abstract T CreateEntity();

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ThrowsOnNullStorage()
        {
            // Arrange
            var emptySet = new HashSet<int>();
            var enumerator = emptySet.GetEnumerator();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                new SpatialIteratorState<int, string, HashSet<int>.Enumerator>(enumerator, null);
            });
        }

        #endregion
    }
}
