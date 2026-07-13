using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.Environment.Spatial.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    public class GridRadiusEdgeStateTests : IIterationLogicTests<Vector3Int, GridRadiusEdgeState>
    {
        #region Create Test Env

        private readonly Vector3Int _min = Vector3Int.zero;
        private readonly Vector3Int _max = Vector3Int.one;
        private readonly Vector3 _center = Vector3.zero;
        private const float ValidRadius = 5f;
        private readonly Vector3 _validGridSize = new Vector3(10f, 10f, 10f);
        private const SpatialAxes ValidAxes = SpatialAxes.X | SpatialAxes.Y | SpatialAxes.Z;

        protected override IterationTestData<Vector3Int, GridRadiusEdgeState> CreateLogic(int count)
        {
            var keys = new Vector3Int[count];

            for (int i = 0; i < count; ++i)
            {
                keys[i] = new Vector3Int(i, 0, 0);
            }

            var localCentre = new Vector3Int(0, 0, 0);
            var radius = count;
            var gridSize = new Vector3(1, 1, 1);
            var axes = SpatialAxes.Voxel;

            GridRadiusEdgeState logic;
            if (count == 0)
            {
                logic = new GridRadiusEdgeState(
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(-1, -1, -1),
                    localCentre,
                    radius,
                    gridSize,
                    axes);
            }
            else
            {
                logic = new GridRadiusEdgeState(
                    keys[0],
                    keys[keys.Length - 1],
                    localCentre,
                    radius,
                    gridSize,
                    axes);
            }

            return new IterationTestData<Vector3Int, GridRadiusEdgeState>
            {
                expected = keys,
                logic = logic
            };
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_InvalidGridSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new GridRadiusEdgeState(_min, _max, _center, ValidRadius, new Vector3(0, 10, 10), ValidAxes));

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new GridRadiusEdgeState(_min, _max, _center, ValidRadius, new Vector3(10, -1, 10), ValidAxes));
        }

        [Test]
        public void Constructor_NegativeRadius_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new GridRadiusEdgeState(_min, _max, _center, -1f, _validGridSize, ValidAxes));
        }

        [Test]
        public void Constructor_NoActiveAxes_ThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new GridRadiusEdgeState(_min, _max, _center, ValidRadius, _validGridSize, SpatialAxes.None));
        }

        [Test]
        public void Constructor_ValidParameters_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                new GridRadiusEdgeState(_min, _max, _center, ValidRadius, _validGridSize, ValidAxes));
        }

        #endregion

        #region Iteration Tests

        [TestCase(15f, 10f, SpatialAxes.X | SpatialAxes.Y | SpatialAxes.Z)]
        [TestCase(5f, 10f, SpatialAxes.X)]
        [TestCase(20f, 5f, SpatialAxes.Y | SpatialAxes.Z)]
        [TestCase(100f, 50f, SpatialAxes.X | SpatialAxes.Y)]
        public void Iterator_MatchesBruteForceCalculation_ForAllCases(float radius, float gridSize, SpatialAxes axes)
        {
            Vector3Int min = new Vector3Int(-5, -5, -5);
            Vector3Int max = new Vector3Int(5, 5, 5);
            Vector3 center = new Vector3(2.5f, -1.2f, 3.8f);

            var expectedKeys = CalculateExpectedKeys(min, max, center, radius, gridSize, axes);

            var state = new GridRadiusEdgeState(min, max, center, radius, gridSize, axes);
            var actualKeys = new HashSet<Vector3Int>();

            while (state.MoveNext(ref state, out Vector3Int result))
            {
                actualKeys.Add(result);
            }

            Assert.AreEqual(expectedKeys.Count, actualKeys.Count, $"Mismatch count for Radius {radius}, Grid {gridSize}, Axes {axes}");
            Assert.IsTrue(expectedKeys.SetEquals(actualKeys), "The sets of found keys do not match.");
        }

        [Test]
        public void Iterator_HandlesLargeCenterOffsetCorrectly()
        {
            Vector3 center = new Vector3(1234.5f, -987.6f, 555.5f);
            float radius = 25f;
            float gridSize = 10f;
            SpatialAxes axes = SpatialAxes.X | SpatialAxes.Y | SpatialAxes.Z;

            Vector3Int min = new Vector3Int(120, -100, 50);
            Vector3Int max = new Vector3Int(125, -95, 60);

            var expectedKeys = CalculateExpectedKeys(min, max, center, radius, gridSize, axes);

            var state = new GridRadiusEdgeState(min, max, center, radius, gridSize, axes);
            var actualKeys = new HashSet<Vector3Int>();

            while (state.MoveNext(ref state, out Vector3Int result))
            {
                actualKeys.Add(result);
            }

            Assert.Greater(expectedKeys.Count, 0, "Range should contain valid points.");
            Assert.AreEqual(expectedKeys.Count, actualKeys.Count, "Key count mismatch for large offset.");
            Assert.IsTrue(expectedKeys.SetEquals(actualKeys), "The found keys do not match the expected set for large offset.");
        }

        #endregion

        #region Test Helpers

        private HashSet<Vector3Int> CalculateExpectedKeys(Vector3Int min, Vector3Int max, Vector3 center, float radius, float gridSize, SpatialAxes axes)
        {
            var keys = new HashSet<Vector3Int>();
            float sqrRadius = radius * radius;
            Vector3 halfSizes = new Vector3(gridSize, gridSize, gridSize) * 0.5f;

            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int z = min.z; z <= max.z; z++)
                    {
                        Vector3Int candidate = new Vector3Int(x, y, z);
                        Vector3 cellPos = new Vector3(
                            gridSize * x + halfSizes.x,
                            gridSize * y + halfSizes.y,
                            gridSize * z + halfSizes.z
                        );

                        if (SpatialUtils.GetSqrDistanceEdge(center, cellPos, halfSizes, axes) <= sqrRadius + 0.0001f)
                        {
                            keys.Add(candidate);
                        }
                    }
                }
            }
            return keys;
        }

        #endregion
    }
}