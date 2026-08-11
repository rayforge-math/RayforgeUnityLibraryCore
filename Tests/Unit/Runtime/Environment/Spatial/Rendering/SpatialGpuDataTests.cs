using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering.Tests
{
    public class SpatialGpuDataTests
    {
        #region SphereSpatialData Tests

        [Test]
        public void SphereSpatialData_Validity_ChecksRadius()
        {
            // Arrange & Act
            var validSphere = new SphereSpatialData { Position = Vector3.zero, Radius = 5f };
            var zeroSphere = new SphereSpatialData { Position = Vector3.zero, Radius = 0f };
            var negativeSphere = new SphereSpatialData { Position = Vector3.zero, Radius = -1f };

            // Assert
            Assert.IsTrue(validSphere.IsValid);
            Assert.IsFalse(zeroSphere.IsValid);
            Assert.IsFalse(negativeSphere.IsValid);
        }

        [Test]
        public void SphereSpatialData_InvalidData_ResetsCorrectly()
        {
            // Arrange
            var sphere = new SphereSpatialData { Position = new Vector3(10, 20, 30), Radius = 5f };

            // Act
            var invalid = sphere.InvalidData();

            // Assert
            Assert.AreEqual(Vector3.zero, invalid.Position);
            Assert.AreEqual(0f, invalid.Radius);
            Assert.IsFalse(invalid.IsValid);
        }

        [Test]
        public void SphereSpatialData_MemoryLayout_Is16Bytes()
        {
            // Assert: 12 Bytes (Vector3) + 4 Bytes (float) = 16 Bytes (1x float4)
            Assert.AreEqual(16, Marshal.SizeOf<SphereSpatialData>());
        }

        #endregion

        #region AabbSpatialData Tests

        [Test]
        public void AabbSpatialData_Validity_ChecksActiveFlagBitwise()
        {
            // Arrange
            var validAabb = new AabbSpatialData
            {
                MinBounds = Vector3.zero,
                MaxBounds = Vector3.one,
                ActiveFlag = BitConverter.Int32BitsToSingle(0x1) // Non-zero bits
            };

            var invalidAabb = new AabbSpatialData
            {
                MinBounds = Vector3.zero,
                MaxBounds = Vector3.one,
                ActiveFlag = BitConverter.Int32BitsToSingle(0x0) // All bits zero
            };

            // Assert
            Assert.IsTrue(validAabb.IsValid);
            Assert.IsFalse(invalidAabb.IsValid);
        }

        [Test]
        public void AabbSpatialData_InvalidData_ResetsCorrectly()
        {
            // Arrange
            var aabb = new AabbSpatialData
            {
                MinBounds = Vector3.one,
                MaxBounds = Vector3.one * 10f,
                LayerMask = 1f,
                ActiveFlag = BitConverter.Int32BitsToSingle(0x1)
            };

            // Act
            var invalid = aabb.InvalidData();

            // Assert
            Assert.AreEqual(Vector3.zero, invalid.MinBounds);
            Assert.AreEqual(Vector3.zero, invalid.MaxBounds);
            Assert.IsFalse(invalid.IsValid);
        }

        [Test]
        public void AabbSpatialData_MemoryLayout_Is32Bytes()
        {
            // Assert: 12 + 4 + 12 + 4 = 32 Bytes (2x float4)
            Assert.AreEqual(32, Marshal.SizeOf<AabbSpatialData>());
        }

        #endregion

        #region MatrixSpatialData Tests

        [Test]
        public void MatrixSpatialData_Validity_ChecksMatrixNonZero()
        {
            // Arrange
            var validMatrix = new MatrixSpatialData { LocalToWorld = Matrix4x4.identity };
            var zeroMatrix = new MatrixSpatialData { LocalToWorld = Matrix4x4.zero };

            // Assert
            Assert.IsTrue(validMatrix.IsValid);
            Assert.IsFalse(zeroMatrix.IsValid);
        }

        [Test]
        public void MatrixSpatialData_InvalidData_ResetsCorrectly()
        {
            // Arrange
            var matrix = new MatrixSpatialData { LocalToWorld = Matrix4x4.TRS(Vector3.one, Quaternion.identity, Vector3.one) };

            // Act
            var invalid = matrix.InvalidData();

            // Assert
            Assert.AreEqual(Matrix4x4.zero, invalid.LocalToWorld);
            Assert.IsFalse(invalid.IsValid);
        }

        [Test]
        public void MatrixSpatialData_MemoryLayout_Is64Bytes()
        {
            // Assert: Matrix4x4 = 64 Bytes (4x float4)
            Assert.AreEqual(64, Marshal.SizeOf<MatrixSpatialData>());
        }

        #endregion
    }
}