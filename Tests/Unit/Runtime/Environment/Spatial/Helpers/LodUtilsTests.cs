using NUnit.Framework;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Helpers.Tests
{
    [TestFixture]
    public class LodUtilsTests
    {
        // Thresholds: LOD 0 < 100, LOD 1 < 400, LOD 2 < 900
        private readonly float[] _thresholds = { 100f, 400f, 900f };

        [Test]
        public void CalculateTargetLOD_ReturnsCorrectLevel()
        {
            // Inside LOD 0
            Assert.AreEqual(0, LodUtils.CalculateTargetLOD(50f, _thresholds));

            // Inside LOD 1
            Assert.AreEqual(1, LodUtils.CalculateTargetLOD(200f, _thresholds));

            // Inside LOD 2
            Assert.AreEqual(2, LodUtils.CalculateTargetLOD(800f, _thresholds));
        }

        [Test]
        public void CalculateTargetLOD_ReturnsMinusOne_WhenBeyondMaxRange()
        {
            // Beyond LOD 2
            Assert.AreEqual(-1, LodUtils.CalculateTargetLOD(1000f, _thresholds));
        }

        [Test]
        public void CalculateTargetLOD_HandlesExactThresholds()
        {
            // At threshold 100: Should return next level (LOD 1)
            // Logic: if (dist < threshold) -> 100 is NOT < 100, so it returns 1
            Assert.AreEqual(1, LodUtils.CalculateTargetLOD(100f, _thresholds));

            // At threshold 400: Should return next level (LOD 2)
            Assert.AreEqual(2, LodUtils.CalculateTargetLOD(400f, _thresholds));
        }

        [Test]
        public void CalculateTargetLOD_HandlesEmptyThresholds()
        {
            float[] empty = System.Array.Empty<float>();
            Assert.AreEqual(-1, LodUtils.CalculateTargetLOD(50f, empty));
        }
    }
}
