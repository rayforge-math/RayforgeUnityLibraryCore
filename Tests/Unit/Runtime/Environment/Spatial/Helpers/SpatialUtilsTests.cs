using NUnit.Framework;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Environment.Spatial.Helpers;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Helpers.Tests
{
    [TestFixture]
    public class SpatialUtilsTests
    {
        #region Setup

        private const float GridSize = 10.0f;
        private const float Epsilon = 0.001f;

        #endregion

        #region 1D CORE LOGIC Tests

        [TestCase(0.0f, 0.0f, 0)]      // Lower boundary (inclusive)
        [TestCase(9.9f, 0.0f, 0)]      // Just before grid boundary
        [TestCase(10.0f, 0.0f, 1)]     // Upper boundary (inclusive)
        [TestCase(-0.1f, 0.0f, -1)]    // Negative boundary
        [TestCase(-10.0f, 0.0f, -1)]   // Exact negative boundary
        [TestCase(-10.1f, 0.0f, -2)]   // Beyond negative boundary
        public void PositionToKey1D_CorrectKeyMapping(float pos, float anchor, int expectedKey)
        {
            int actualKey = SpatialUtils.PositionToKey1D(pos, GridSize, anchor);
            Assert.AreEqual(expectedKey, actualKey, $"Position {pos} with anchor {anchor} should map to key {expectedKey}");
        }

        [TestCase(0, 0.0f, false, 0.0f)]   // Start of the cell
        [TestCase(0, 0.0f, true, 5.0f)]    // Center of the cell
        [TestCase(1, 0.0f, false, 10.0f)]  // Start of next cell
        [TestCase(-1, 0.0f, false, -10.0f)]// Negative cell coordinate
        [TestCase(0, 5.0f, false, 5.0f)]   // Offset by anchor
        [TestCase(0, 5.0f, true, 10.0f)]   // Offset by anchor and centered
        public void KeyToPosition1D_CorrectCoordinateMapping(int key, float anchor, bool centered, float expectedPos)
        {
            float actualPos = SpatialUtils.KeyToPosition1D(key, GridSize, anchor, centered);
            Assert.AreEqual(expectedPos, actualPos, 0.001f, $"Key {key} with anchor {anchor} should map to position {expectedPos}");
        }

        [Test]
        public void RoundTrip_Consistency()
        {
            float originalPos = 123.45f;
            float anchor = 2.5f;

            int key = SpatialUtils.PositionToKey1D(originalPos, GridSize, anchor);
            float reconstructedPos = SpatialUtils.KeyToPosition1D(key, GridSize, anchor, centered: false);

            // Should be the start of the cell (122.5)
            Assert.AreEqual(122.5f, reconstructedPos, 0.001f);
        }

        #endregion

        #region 2D CONVERSIONS Tests

        [TestCase(0f, 0f, 0f, 0, 0)]      // Center/Origin
        [TestCase(5f, 99f, 5f, 0, 0)]     // Inside first cell (Y is ignored)
        [TestCase(15f, 0f, -5f, 1, -1)]   // Crossing X and Z boundaries
        public void PositionToKey2D_Vector3_XZProjection(float x, float y, float z, int expectedX, int expectedZ)
        {
            Vector3 pos = new Vector3(x, y, z);
            Vector2Int expected = new Vector2Int(expectedX, expectedZ);

            Vector2Int result = SpatialUtils.PositionToKey2D(pos, GridSize);

            Assert.AreEqual(expected, result, $"3D position {pos} should project to 2D key {expected}");
        }

        [TestCase(5f, 5f, 0, 0)]          // First cell
        [TestCase(-5f, 15f, -1, 1)]       // Negative X, positive Y
        [TestCase(25f, -25f, 2, -3)]      // Positive X, negative Y
        public void PositionToKey2D_Vector2_Mapping(float x, float y, int expectedX, int expectedY)
        {
            Vector2 pos = new Vector2(x, y);
            Vector2Int expected = new Vector2Int(expectedX, expectedY);

            Vector2Int result = SpatialUtils.PositionToKey2D(pos, GridSize);

            Assert.AreEqual(expected, result, $"2D position {pos} should map to key {expected}");
        }

        [Test]
        public void PositionToKey2D_WithAnchor_ShiftsCorrectly()
        {
            Vector3 pos = new Vector3(5f, 0f, 5f);
            Vector3 anchor = new Vector3(10f, 0f, 10f);

            // (5 - 10) / 10 = -0.5 -> floor is -1
            Vector2Int expected = new Vector2Int(-1, -1);
            Vector2Int result = SpatialUtils.PositionToKey2D(pos, GridSize, anchor);

            Assert.AreEqual(expected, result);
        }

        #endregion

        #region 3D CONVERSIONS Tests

        [TestCase(5f, 5f, 5f, 0, 0, 0)]          // Origin cell
        [TestCase(-5f, 15f, -25f, -1, 1, -3)]    // Mixed signs
        [TestCase(0.1f, 0.1f, 0.1f, 0, 0, 0)]    // Small offset
        [TestCase(10f, 20f, 30f, 1, 2, 3)]       // Exact boundaries
        public void PositionToKey3D_MapsCorrectly(float x, float y, float z, int ex, int ey, int ez)
        {
            Vector3 pos = new Vector3(x, y, z);
            Vector3Int expected = new Vector3Int(ex, ey, ez);

            Vector3Int result = SpatialUtils.PositionToKey3D(pos, GridSize);

            Assert.AreEqual(expected, result, $"Position {pos} should map to key {expected}");
        }

        [TestCase(0, 0, 0, false, 0f, 0f, 0f)]      // Origin corner
        [TestCase(0, 0, 0, true, 5f, 5f, 5f)]       // Origin center
        [TestCase(1, -2, 3, false, 10f, -20f, 30f)] // Offset corner
        [TestCase(1, -2, 3, true, 15f, -15f, 35f)]  // Offset center
        public void KeyToPosition3D_MapsCorrectly(int kx, int ky, int kz, bool centered, float ex, float ey, float ez)
        {
            Vector3Int key = new Vector3Int(kx, ky, kz);
            Vector3 expected = new Vector3(ex, ey, ez);

            Vector3 result = SpatialUtils.KeyToPosition3D(key, GridSize, Vector3.zero, centered);

            Assert.AreEqual(expected, result, $"Key {key} should map to position {expected}");
        }

        [Test]
        public void PositionToKey3D_WithAnchor_ShiftsCorrectly()
        {
            Vector3 pos = new Vector3(5f, 5f, 5f);
            Vector3 anchor = new Vector3(10f, 10f, 10f);

            // (5-10)/10 = -0.5 -> floor is -1
            Vector3Int expected = new Vector3Int(-1, -1, -1);
            Vector3Int result = SpatialUtils.PositionToKey3D(pos, GridSize, anchor);

            Assert.AreEqual(expected, result);
        }

        #endregion

        #region GetSqrDistance Tests

        [TestCase(5f, 2f, 9f)]    // (5-2)^2 = 9
        [TestCase(2f, 5f, 9f)]    // (2-5)^2 = 9
        [TestCase(-2f, 2f, 16f)]  // (-2-2)^2 = 16
        [TestCase(0f, 0f, 0f)]    // Distance 0
        public void GetSqrDistance1D_CalculatesCorrectly(float a, float b, float expected)
        {
            float result = SpatialUtils.GetSqrDistance1D(a, b);
            Assert.AreEqual(expected, result, Epsilon);
        }

        [Test]
        public void GetSqrDistance2D_CalculatesCorrectly()
        {
            Vector2 a = new Vector2(0f, 0f);
            Vector2 b = new Vector2(3f, 4f);

            // (3-0)^2 + (4-0)^2 = 9 + 16 = 25
            float result = SpatialUtils.GetSqrDistance2D(a, b);
            Assert.AreEqual(25f, result, Epsilon);
        }

        [Test]
        public void GetSqrDistance3D_CalculatesCorrectly()
        {
            Vector3 a = new Vector3(1f, 1f, 1f);
            Vector3 b = new Vector3(4f, 5f, 1f);

            // (4-1)^2 + (5-1)^2 + (1-1)^2 = 3^2 + 4^2 + 0^2 = 9 + 16 = 25
            float result = SpatialUtils.GetSqrDistance3D(a, b);
            Assert.AreEqual(25f, result, Epsilon);
        }

        #endregion

        #region GetSqrDistanceToClosestEdge Tests

        [TestCase(5f, 5f, 2f, 0f)]    // Exactly at center (inside)
        [TestCase(6f, 5f, 2f, 0f)]    // Inside (boundary 3 to 7)
        [TestCase(8f, 5f, 2f, 1f)]    // Outside (8 - 7 = 1, 1^2 = 1)
        [TestCase(2f, 5f, 2f, 1f)]    // Outside (3 - 2 = 1, 1^2 = 1)
        public void GetSqrDistanceToClosestEdge1D_CalculatesCorrectly(float pos, float center, float halfSize, float expected)
        {
            float result = SpatialUtils.GetSqrDistanceToClosestEdge1D(pos, center, halfSize);
            Assert.AreEqual(expected, result, Epsilon);
        }

        [Test]
        public void GetSqrDistanceToClosestEdge2D_InsideReturnsZero()
        {
            Vector2 center = Vector2.zero;
            Vector2 halfExtents = new Vector2(2f, 2f); // Box from -2 to +2
            Vector2 inside = new Vector2(1f, -1f);

            float result = SpatialUtils.GetSqrDistanceToClosestEdge2D(inside, center, halfExtents);
            Assert.AreEqual(0f, result, Epsilon);
        }

        [Test]
        public void GetSqrDistanceToClosestEdge2D_OutsideReturnsCorrectDistance()
        {
            Vector2 center = Vector2.zero;
            Vector2 halfExtents = new Vector2(2f, 2f); // Box from -2 to +2
                                                       // Outside: X=3 (dist 1), Y=4 (dist 2) -> 1^2 + 2^2 = 5
            Vector2 outside = new Vector2(3f, 4f);

            float result = SpatialUtils.GetSqrDistanceToClosestEdge2D(outside, center, halfExtents);
            Assert.AreEqual(5f, result, Epsilon);
        }

        [Test]
        public void GetSqrDistanceToClosestEdge3D_InsideReturnsZero()
        {
            Vector3 center = Vector3.zero;
            Vector3 halfExtents = new Vector3(2f, 2f, 2f); // Box from -2 to +2
            Vector3 inside = new Vector3(0f, 1.5f, -2f);

            float result = SpatialUtils.GetSqrDistanceToClosestEdge3D(inside, center, halfExtents);
            Assert.AreEqual(0f, result, Epsilon);
        }

        [Test]
        public void GetSqrDistanceToClosestEdge3D_OutsideReturnsCorrectDistance()
        {
            Vector3 center = Vector3.zero;
            Vector3 halfExtents = new Vector3(2f, 2f, 2f); // Box from -2 to +2
                                                           // Outside: X=3 (dist 1), Y=4 (dist 2), Z=0 (inside) -> 1^2 + 2^2 + 0 = 5
            Vector3 outside = new Vector3(3f, 4f, 0f);

            float result = SpatialUtils.GetSqrDistanceToClosestEdge3D(outside, center, halfExtents);
            Assert.AreEqual(5f, result, Epsilon);
        }

        #endregion
    }
}
