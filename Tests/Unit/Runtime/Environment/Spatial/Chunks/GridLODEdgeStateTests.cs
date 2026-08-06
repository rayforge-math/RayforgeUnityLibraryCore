using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions.Tests;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks.Tests
{
    [TestFixture]
    public class GridLODEdgeStateTests : IIterationLogicTests<Vector3Int, GridLODEdgeState>
    {
        #region Create Test Env

        protected override IterationTestData<Vector3Int, GridLODEdgeState> CreateLogic(int count)
        {
            var keys = new Vector3Int[count];

            for (int i = 0; i < count; ++i)
            {
                keys[i] = new Vector3Int(i, 0, 0);
            }

            var localCentre = Vector3.zero;
            var gridSize = new Vector3(1, 1, 1);
            var axes = SpatialAxes.Voxel;

            GridLODEdgeState logic;
            if (count == 0)
            {
                logic = new GridLODEdgeState(
                    Vector3Int.zero,
                    Vector3Int.zero,
                    localCentre,
                    0f, 0f,
                    gridSize,
                    axes);
            }
            else
            {
                logic = new GridLODEdgeState(
                    keys[0],
                    keys[keys.Length - 1],
                    localCentre,
                    0f, float.MaxValue,
                    gridSize,
                    axes);
            }

            return new IterationTestData<Vector3Int, GridLODEdgeState>
            {
                expected = keys,
                logic = logic
            };
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ValidParameters_SuccessfullyInitializes()
        {
            // Arrange
            Vector3Int min = new Vector3Int(-2, -2, -2);
            Vector3Int max = new Vector3Int(2, 2, 2);
            Vector3 worldCenter = new Vector3(10f, 0f, -5f);
            float minSqrRadius = 25f;
            float maxSqrRadius = 100f;
            Vector3 gridSize = new Vector3(10f, 10f, 10f);
            SpatialAxes activeAxes = SpatialAxes.X | SpatialAxes.Z;

            // Act & Assert
            Assert.DoesNotThrow(() => new GridLODEdgeState(
                min, max,
                worldCenter,
                minSqrRadius, maxSqrRadius,
                gridSize,
                activeAxes
            ), "Valid constructor arguments must not throw any exception.");
        }

        [Test]
        [TestCase(0f, 10f, 10f)]
        [TestCase(-1f, 10f, 10f)]
        [TestCase(10f, 0f, 10f)]
        [TestCase(10f, -5f, 10f)]
        [TestCase(10f, 10f, 0f)]
        [TestCase(10f, 10f, -0.1f)]
        public void Constructor_InvalidGridSize_ThrowsArgumentOutOfRangeException(float x, float y, float z)
        {
            // Arrange
            Vector3 invalidGridSize = new Vector3(x, y, z);

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                0f, 100f,
                invalidGridSize,
                SpatialAxes.Voxel
            ), "Grid size with any axis <= 0 must throw ArgumentOutOfRangeException.");

            Assert.AreEqual("gridSize", ex.ParamName);
        }

        [Test]
        [TestCase(-0.01f, 100f)]
        [TestCase(-100f, 100f)]
        public void Constructor_NegativeMinSqrRadius_ThrowsArgumentOutOfRangeException(float minSqr, float maxSqr)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                minSqr, maxSqr,
                new Vector3(10f, 10f, 10f),
                SpatialAxes.Voxel
            ), "Negative minSqrRadius must throw ArgumentOutOfRangeException.");

            Assert.AreEqual("minSqrRadius", ex.ParamName);
        }

        [Test]
        [TestCase(0f, -0.01f)]
        [TestCase(0f, -50f)]
        public void Constructor_NegativeMaxSqrRadius_ThrowsArgumentOutOfRangeException(float minSqr, float maxSqr)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                minSqr, maxSqr,
                new Vector3(10f, 10f, 10f),
                SpatialAxes.Voxel
            ), "Negative maxSqrRadius must throw ArgumentOutOfRangeException.");

            Assert.AreEqual("maxSqrRadius", ex.ParamName);
        }

        [Test]
        public void Constructor_MinSqrRadiusGreaterThanMaxSqrRadius_ThrowsArgumentException()
        {
            // Arrange
            float minSqrRadius = 100.01f;
            float maxSqrRadius = 100.00f;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                minSqrRadius, maxSqrRadius,
                new Vector3(10f, 10f, 10f),
                SpatialAxes.Voxel
            ), "minSqrRadius > maxSqrRadius must throw ArgumentException.");

            Assert.AreEqual("minSqrRadius", ex.ParamName);
        }

        [Test]
        public void Constructor_EqualMinAndMaxSqrRadius_DoesNotThrow()
        {
            // Arrange - Edge case where interval is [R, R)
            float radius = 50f;

            // Act & Assert
            Assert.DoesNotThrow(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                radius, radius,
                new Vector3(10f, 10f, 10f),
                SpatialAxes.Voxel
            ), "Equal minSqrRadius and maxSqrRadius is valid and should not throw.");
        }

        [Test]
        public void Constructor_SpatialAxesNone_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                0f, 100f,
                new Vector3(10f, 10f, 10f),
                SpatialAxes.None
            ), "SpatialAxes.None must throw ArgumentException.");

            Assert.AreEqual("activeAxes", ex.ParamName);
        }

        [Test]
        [TestCase(0, 0)] // Single LOD 0
        [TestCase(0, 3)] // Full range [0..3]
        [TestCase(1, 2)] // Inner range [1..2]
        [TestCase(3, 3)] // Single highest LOD
        public void Constructor_ValidLodIndices_DoesNotThrow(int minLod, int maxLod)
        {
            // Arrange
            float[] distances = new float[] { 100f, 400f, 900f, 1600f };

            // Act & Assert
            Assert.DoesNotThrow(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                minLod, maxLod,
                distances,
                new Vector3(10f, 10f, 10f),
                SpatialAxes.Voxel
            ), $"Valid LOD range [{minLod}..{maxLod}] should initialize without throwing.");
        }

        [Test]
        [TestCase(-1)]
        [TestCase(-10)]
        public void Constructor_NegativeMinLodIndex_ThrowsArgumentOutOfRangeException(int invalidMinLod)
        {
            // Arrange
            float[] distances = new float[] { 100f, 400f, 900f, 1600f };

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                invalidMinLod, 2,
                distances,
                new Vector3(10f, 10f, 10f),
                SpatialAxes.Voxel
            ), "Negative minLodIndex must throw ArgumentOutOfRangeException.");

            Assert.AreEqual("minLodIndex", ex.ParamName);
        }

        [Test]
        [TestCase(4)]
        [TestCase(10)]
        public void Constructor_MinLodIndexGreaterOrEqualToSpanLength_ThrowsArgumentOutOfRangeException(int invalidMinLod)
        {
            // Arrange
            float[] distances = new float[] { 100f, 400f, 900f, 1600f };

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                invalidMinLod, invalidMinLod,
                distances,
                new Vector3(10f, 10f, 10f),
                SpatialAxes.Voxel
            ), "minLodIndex >= span length must throw ArgumentOutOfRangeException.");

            Assert.AreEqual("minLodIndex", ex.ParamName);
        }

        [Test]
        [TestCase(2, 1)]
        [TestCase(3, 0)]
        public void Constructor_MaxLodIndexLessThanMinLodIndex_ThrowsArgumentOutOfRangeException(int minLod, int invalidMaxLod)
        {
            // Arrange
            float[] distances = new float[] { 100f, 400f, 900f, 1600f };

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                minLod, invalidMaxLod,
                distances,
                new Vector3(10f, 10f, 10f),
                SpatialAxes.Voxel
            ), "maxLodIndex < minLodIndex must throw ArgumentOutOfRangeException.");

            Assert.AreEqual("maxLodIndex", ex.ParamName);
        }

        [Test]
        [TestCase(0, 4)]
        [TestCase(1, 10)]
        public void Constructor_MaxLodIndexGreaterOrEqualToSpanLength_ThrowsArgumentOutOfRangeException(int minLod, int invalidMaxLod)
        {
            // Arrange
            float[] distances = new float[] { 100f, 400f, 900f, 1600f };

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                minLod, invalidMaxLod,
                distances,
                new Vector3(10f, 10f, 10f),
                SpatialAxes.Voxel
            ), "maxLodIndex >= span length must throw ArgumentOutOfRangeException.");

            Assert.AreEqual("maxLodIndex", ex.ParamName);
        }

        [Test]
        public void Constructor_InvalidGridSize_DelegatesArgumentOutOfRangeException()
        {
            // Arrange
            float[] distances = new float[] { 100f, 400f, 900f, 1600f };

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                0, 1,
                distances,
                new Vector3(0f, 10f, 10f), // Invalid grid size X <= 0
                SpatialAxes.Voxel
            ), "Delegated constructor validation for gridSize must be triggered.");

            Assert.AreEqual("gridSize", ex.ParamName);
        }

        [Test]
        public void Constructor_SpatialAxesNone_DelegatesArgumentException()
        {
            // Arrange
            float[] distances = new float[] { 100f, 400f, 900f, 1600f };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new GridLODEdgeState(
                new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1),
                Vector3.zero,
                0, 1,
                distances,
                new Vector3(10f, 10f, 10f),
                SpatialAxes.None // Invalid axes
            ), "Delegated constructor validation for SpatialAxes.None must be triggered.");

            Assert.AreEqual("activeAxes", ex.ParamName);
        }

        #endregion
    }
}
