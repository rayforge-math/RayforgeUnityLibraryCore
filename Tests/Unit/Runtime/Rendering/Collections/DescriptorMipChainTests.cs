using NUnit.Framework;
using Rayforge.Core.Rendering.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Rayforge.Core
{
    [TestFixture]
    public class DescriptorMipChainTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_IntWidthHeight_DefaultParameters_InitializesCorrectly()
        {
            // Act
            var chain = new DescriptorMipChain(512, 256);

            // Assert
            Assert.AreEqual(1, chain.MipCount);
            Assert.AreEqual(new Vector2Int(512, 256), chain.Resolution);
            Assert.AreEqual(RenderTextureFormat.Default, chain.Format);
            Assert.AreEqual(512, chain[0].width);
            Assert.AreEqual(256, chain[0].height);
        }

        [Test]
        public void Constructor_IntWidthHeight_CustomParameters_InitializesCorrectly()
        {
            // Arrange
            MipCreateFunc customFunc = (level, res) => new Vector2Int(res.x >> level, res.y >> level);

            // Act
            var chain = new DescriptorMipChain(1024, 768, 3, customFunc, RenderTextureFormat.ARGB32);

            // Assert
            Assert.AreEqual(3, chain.MipCount);
            Assert.AreEqual(new Vector2Int(1024, 768), chain.Resolution);
            Assert.AreEqual(RenderTextureFormat.ARGB32, chain.Format);
            Assert.AreEqual(1024, chain[0].width);
            Assert.AreEqual(768, chain[0].height);
            Assert.AreEqual(512, chain[1].width);
            Assert.AreEqual(384, chain[1].height);
            Assert.AreEqual(256, chain[2].width);
            Assert.AreEqual(192, chain[2].height);
        }

        [Test]
        public void Constructor_Vector2Int_DefaultParameters_InitializesCorrectly()
        {
            // Arrange
            var baseRes = new Vector2Int(640, 480);

            // Act
            var chain = new DescriptorMipChain(baseRes);

            // Assert
            Assert.AreEqual(1, chain.MipCount);
            Assert.AreEqual(baseRes, chain.Resolution);
            Assert.AreEqual(RenderTextureFormat.Default, chain.Format);
            Assert.AreEqual(640, chain[0].width);
            Assert.AreEqual(480, chain[0].height);
        }

        [Test]
        public void Constructor_Vector2Int_CustomParameters_InitializesCorrectly()
        {
            // Arrange
            var baseRes = new Vector2Int(256, 256);
            MipCreateFunc customFunc = (level, res) => new Vector2Int(res.x + level, res.y + level);

            // Act
            var chain = new DescriptorMipChain(baseRes, 2, customFunc, RenderTextureFormat.RFloat);

            // Assert
            Assert.AreEqual(2, chain.MipCount);
            Assert.AreEqual(baseRes, chain.Resolution);
            Assert.AreEqual(RenderTextureFormat.RFloat, chain.Format);
            Assert.AreEqual(256, chain[0].width);
            Assert.AreEqual(256, chain[0].height);
            Assert.AreEqual(RenderTextureFormat.RFloat, chain[0].colorFormat);
            Assert.AreEqual(257, chain[1].width);
            Assert.AreEqual(257, chain[1].height);
            Assert.AreEqual(RenderTextureFormat.RFloat, chain[1].colorFormat);
        }

        [Test]
        public void Constructor_PrivateLayoutOverload_ViaReflection_InitializesCorrectly()
        {
            // Arrange
            var layout = new MipChainLayout(new Vector2Int(128, 128), 2);
            var constructorInfo = typeof(DescriptorMipChain).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(MipChainLayout), typeof(RenderTextureFormat) },
                null
            );

            Assert.IsNotNull(constructorInfo, "Private constructor with MipChainLayout and RenderTextureFormat must exist.");

            // Act
            var chain = (DescriptorMipChain)constructorInfo.Invoke(new object[] { layout, RenderTextureFormat.Shadowmap });

            // Assert
            Assert.AreEqual(2, chain.MipCount);
            Assert.AreEqual(new Vector2Int(128, 128), chain.Resolution);
            Assert.AreEqual(RenderTextureFormat.Shadowmap, chain.Format);
            Assert.AreEqual(RenderTextureFormat.Shadowmap, chain[0].colorFormat);
        }

        [Test]
        public void Constructor_WithInvalidDimensions_ThrowsArgumentException(
            [Values(0, -1)] int width,
            [Values(0, -10)] int height)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DescriptorMipChain(width, height));
            Assert.Throws<ArgumentException>(() => new DescriptorMipChain(new Vector2Int(width, height)));
        }

        [Test]
        public void Constructor_WithNonPositiveMipCount_ThrowsArgumentException(
            [Values(0, -1, -3)] int mipCount)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DescriptorMipChain(256, 256, mipCount));
            Assert.Throws<ArgumentException>(() => new DescriptorMipChain(new Vector2Int(256, 256), mipCount));
        }

        #endregion

        #region Property Tests

        [Test]
        public void Descriptors_ReturnsReadOnlyListMatchingMipCount()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 3);

            // Act
            IReadOnlyList<RenderTextureDescriptor> descriptors = chain.Descriptors;

            // Assert
            Assert.IsNotNull(descriptors);
            Assert.AreEqual(3, descriptors.Count);
        }

        [Test]
        public void Indexer_ValidIndex_ReturnsExpectedDescriptor()
        {
            // Arrange
            var chain = new DescriptorMipChain(512, 256, 2);

            // Act & Assert
            Assert.AreEqual(512, chain[0].width);
            Assert.AreEqual(256, chain[0].height);
            Assert.AreEqual(256, chain[1].width);
            Assert.AreEqual(128, chain[1].height);
        }

        [Test]
        public void Indexer_WithOutOfBoundsIndex_ThrowsArgumentOutOfRangeException(
            [Values(-1, 2, 5)] int invalidIndex)
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 2); // Valid indices: 0, 1

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var desc = chain[invalidIndex];
            });
        }

        [Test]
        public void MipCount_GetterAndSetter_BehavesCorrectly()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 1);
            Assert.AreEqual(1, chain.MipCount);

            // Act
            chain.MipCount = 4;

            // Assert
            Assert.AreEqual(4, chain.MipCount);
            Assert.AreEqual(4, chain.Descriptors.Count);
            Assert.AreEqual(32, chain[3].width); // 256 -> 128 -> 64 -> 32
        }

        [Test]
        public void Resolution_GetterAndSetter_BehavesCorrectly()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 2);
            Assert.AreEqual(new Vector2Int(256, 256), chain.Resolution);

            // Act
            chain.Resolution = new Vector2Int(512, 1024);

            // Assert
            Assert.AreEqual(new Vector2Int(512, 1024), chain.Resolution);
            Assert.AreEqual(512, chain[0].width);
            Assert.AreEqual(1024, chain[0].height);
            Assert.AreEqual(256, chain[1].width);
            Assert.AreEqual(512, chain[1].height);
        }

        [Test]
        public void Width_GetterAndSetter_UpdatesOnlyWidthDimensionInResolution()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 512, 1);

            // Act
            chain.Width = 1024;

            // Assert
            Assert.AreEqual(1024, chain.Width);
            Assert.AreEqual(512, chain.Height);
            Assert.AreEqual(new Vector2Int(1024, 512), chain.Resolution);
            Assert.AreEqual(1024, chain[0].width);
            Assert.AreEqual(512, chain[0].height);
        }

        [Test]
        public void Height_GetterAndSetter_UpdatesOnlyHeightDimensionInResolution()
        {
            // Arrange
            var chain = new DescriptorMipChain(512, 256, 1);

            // Act
            chain.Height = 1024;

            // Assert
            Assert.AreEqual(512, chain.Width);
            Assert.AreEqual(1024, chain.Height);
            Assert.AreEqual(new Vector2Int(512, 1024), chain.Resolution);
            Assert.AreEqual(512, chain[0].width);
            Assert.AreEqual(1024, chain[0].height);
        }

        [Test]
        public void Format_GetterAndSetter_UpdatesAllDescriptors()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 2, null, RenderTextureFormat.ARGB32);
            Assert.AreEqual(RenderTextureFormat.ARGB32, chain.Format);
            Assert.AreEqual(RenderTextureFormat.ARGB32, chain[0].colorFormat);
            Assert.AreEqual(RenderTextureFormat.ARGB32, chain[1].colorFormat);

            // Act
            chain.Format = RenderTextureFormat.RFloat;

            // Assert
            Assert.AreEqual(RenderTextureFormat.RFloat, chain.Format);
            Assert.AreEqual(RenderTextureFormat.RFloat, chain[0].colorFormat);
            Assert.AreEqual(RenderTextureFormat.RFloat, chain[1].colorFormat);
        }

        #endregion

        #region UpdateBaseResolution Tests

        [Test]
        public void UpdateBaseResolution_WithDifferentResolution_UpdatesResolutionAndRecalculatesDescriptors()
        {
            // Arrange
            var chain = new DescriptorMipChain(512, 512, 2);
            Assert.AreEqual(new Vector2Int(512, 512), chain.Resolution);
            Assert.AreEqual(512, chain[0].width);
            Assert.AreEqual(256, chain[1].width);

            // Act
            chain.UpdateBaseResolution(new Vector2Int(1024, 1024));

            // Assert
            Assert.AreEqual(new Vector2Int(1024, 1024), chain.Resolution);
            Assert.AreEqual(1024, chain[0].width);
            Assert.AreEqual(512, chain[1].width);
        }

        [Test]
        public void UpdateBaseResolution_WithSameResolution_DoesNothing()
        {
            // Arrange
            var chain = new DescriptorMipChain(512, 512, 2);
            var initialDescWidth = chain[0].width;

            // Act
            chain.UpdateBaseResolution(new Vector2Int(512, 512));

            // Assert
            Assert.AreEqual(new Vector2Int(512, 512), chain.Resolution);
            Assert.AreEqual(initialDescWidth, chain[0].width);
        }

        [Test]
        public void UpdateBaseResolution_WithInvalidResolution_ThrowsArgumentException(
            [Values(0, -1)] int width,
            [Values(0, -5)] int height)
        {
            if (width > 0 && height > 0) return;

            // Arrange
            var chain = new DescriptorMipChain(512, 512, 2);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => chain.UpdateBaseResolution(new Vector2Int(width, height)));
        }

        #endregion

        #region UpdateMipCount Tests

        [Test]
        public void UpdateMipCount_WithLargerMipCount_IncreasesSizeAndRecalculatesDescriptors()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 2);
            Assert.AreEqual(2, chain.MipCount);

            // Act
            chain.UpdateMipCount(4);

            // Assert
            Assert.AreEqual(4, chain.MipCount);
            Assert.AreEqual(4, chain.Descriptors.Count);
            Assert.AreEqual(256, chain[0].width);
            Assert.AreEqual(128, chain[1].width);
            Assert.AreEqual(64, chain[2].width);
            Assert.AreEqual(32, chain[3].width);
        }

        [Test]
        public void UpdateMipCount_WithSmallerMipCount_DecreasesSizeAndRecalculatesDescriptors()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 4);
            Assert.AreEqual(4, chain.MipCount);

            // Act
            chain.UpdateMipCount(2);

            // Assert
            Assert.AreEqual(2, chain.MipCount);
            Assert.AreEqual(2, chain.Descriptors.Count);
            Assert.AreEqual(256, chain[0].width);
            Assert.AreEqual(128, chain[1].width);
        }

        [Test]
        public void UpdateMipCount_WithSameMipCount_DoesNothing()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 3);
            var initialDescriptorWidth = chain[2].width;

            // Act
            chain.UpdateMipCount(3);

            // Assert
            Assert.AreEqual(3, chain.MipCount);
            Assert.AreEqual(initialDescriptorWidth, chain[2].width);
        }

        [Test]
        public void UpdateMipCount_WithInvalidMipCount_ThrowsArgumentException(
            [Values(0, -1, -5)] int invalidMipCount)
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 3);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => chain.UpdateMipCount(invalidMipCount));
        }

        #endregion

        #region UpdateFormat Tests

        [Test]
        public void UpdateFormat_WithNewFormat_UpdatesFormatAndAllDescriptors()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 3, null, RenderTextureFormat.ARGB32);
            Assert.AreEqual(RenderTextureFormat.ARGB32, chain.Format);
            Assert.AreEqual(RenderTextureFormat.ARGB32, chain[0].colorFormat);
            Assert.AreEqual(RenderTextureFormat.ARGB32, chain[1].colorFormat);
            Assert.AreEqual(RenderTextureFormat.ARGB32, chain[2].colorFormat);

            var methodInfo = typeof(DescriptorMipChain).GetMethod(
                "UpdateFormat",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(RenderTextureFormat) },
                null
            );

            Assert.IsNotNull(methodInfo, "Private method UpdateFormat must exist.");

            // Act
            methodInfo.Invoke(chain, new object[] { RenderTextureFormat.RFloat });

            // Assert
            Assert.AreEqual(RenderTextureFormat.RFloat, chain.Format);
            Assert.AreEqual(RenderTextureFormat.RFloat, chain[0].colorFormat);
            Assert.AreEqual(RenderTextureFormat.RFloat, chain[1].colorFormat);
            Assert.AreEqual(RenderTextureFormat.RFloat, chain[2].colorFormat);
        }

        [Test]
        public void UpdateFormat_WithSameFormat_DoesNothing()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 2, null, RenderTextureFormat.ARGB32);

            var methodInfo = typeof(DescriptorMipChain).GetMethod(
                "UpdateFormat",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(RenderTextureFormat) },
                null
            );

            // Act
            methodInfo.Invoke(chain, new object[] { RenderTextureFormat.ARGB32 });

            // Assert
            Assert.AreEqual(RenderTextureFormat.ARGB32, chain.Format);
            Assert.AreEqual(RenderTextureFormat.ARGB32, chain[0].colorFormat);
            Assert.AreEqual(RenderTextureFormat.ARGB32, chain[1].colorFormat);
        }

        #endregion

        #region AsSpan Tests

        [Test]
        public void AsSpan_WithoutArguments_ReturnsFullSpan()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 3);

            // Act
            ReadOnlySpan<RenderTextureDescriptor> span = chain.AsSpan();

            // Assert
            Assert.AreEqual(3, span.Length);
            Assert.AreEqual(256, span[0].width);
            Assert.AreEqual(128, span[1].width);
            Assert.AreEqual(64, span[2].width);
        }

        [Test]
        public void AsSpan_WithoutArguments_AfterMipCountChange_ReflectsNewLength()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 2);
            chain.MipCount = 4;

            // Act
            ReadOnlySpan<RenderTextureDescriptor> span = chain.AsSpan();

            // Assert
            Assert.AreEqual(4, span.Length);
            Assert.AreEqual(256, span[0].width);
            Assert.AreEqual(128, span[1].width);
            Assert.AreEqual(64, span[2].width);
            Assert.AreEqual(32, span[3].width);
        }

        [Test]
        public void AsSpan_WithValidStartAndLength_ReturnsExpectedSubSpan(
            [Values(0, 1, 2)] int start,
            [Values(1, 2)] int length)
        {
            var chain = new DescriptorMipChain(256, 256, 4);
            if (start + length > chain.MipCount) return;

            // Act
            ReadOnlySpan<RenderTextureDescriptor> span = chain.AsSpan(start, length);

            // Assert
            Assert.AreEqual(length, span.Length);
            for (int i = 0; i < length; i++)
            {
                Assert.AreEqual(chain[start + i].width, span[i].width);
            }
        }

        [Test]
        public void AsSpan_WithZeroLength_ReturnsEmptySpan()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 3);

            // Act
            ReadOnlySpan<RenderTextureDescriptor> span = chain.AsSpan(1, 0);

            // Assert
            Assert.AreEqual(0, span.Length);
            Assert.IsTrue(span.IsEmpty);
        }

        [Test]
        public void AsSpan_AtBoundary_StartEqualsMipCountAndLengthZero_ReturnsEmptySpan()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 3);

            // Act
            ReadOnlySpan<RenderTextureDescriptor> span = chain.AsSpan(3, 0);

            // Assert
            Assert.AreEqual(0, span.Length);
            Assert.IsTrue(span.IsEmpty);
        }

        [Test]
        public void AsSpan_WithNegativeStart_ThrowsArgumentOutOfRangeException(
            [Values(-1, -5)] int invalidStart)
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => chain.AsSpan(invalidStart, 1));
        }

        [Test]
        public void AsSpan_WithStartGreaterThanMipCount_ThrowsArgumentOutOfRangeException(
            [Values(4, 5, 10)] int invalidStart)
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 3); // MipCount = 3 (valid indices 0,1,2, boundary 3)

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => chain.AsSpan(invalidStart, 0));
        }

        [Test]
        public void AsSpan_WithNegativeLength_ThrowsArgumentOutOfRangeException(
            [Values(-1, -10)] int invalidLength)
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => chain.AsSpan(0, invalidLength));
        }

        [Test]
        public void AsSpan_WithStartPlusLengthExceedingMipCount_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var chain = new DescriptorMipChain(256, 256, 3); // MipCount = 3

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => chain.AsSpan(1, 3)); // 1 + 3 = 4 > 3
        }

        #endregion
    }
}
