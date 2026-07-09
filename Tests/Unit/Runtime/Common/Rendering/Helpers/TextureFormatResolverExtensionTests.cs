using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Rayforge.Core.Common.Rendering.Helpers.Tests
{
    public class TextureFormatResolverExtensionTests
    {
        #region Members

        private static readonly (RenderTextureFormat RT, TextureFormat Texture)[] _rtToTextureMappings =
        {
            (RenderTextureFormat.R8, TextureFormat.R8),
            (RenderTextureFormat.R16, TextureFormat.R16),
            (RenderTextureFormat.RHalf, TextureFormat.RHalf),
            (RenderTextureFormat.RFloat, TextureFormat.RFloat),
            (RenderTextureFormat.RG16, TextureFormat.RG16),
            (RenderTextureFormat.RGHalf, TextureFormat.RGHalf),
            (RenderTextureFormat.RGFloat, TextureFormat.RGFloat),
            (RenderTextureFormat.ARGB32, TextureFormat.RGBA32),
            (RenderTextureFormat.ARGBHalf, TextureFormat.RGBAHalf),
            (RenderTextureFormat.ARGBFloat, TextureFormat.RGBAFloat),
            (RenderTextureFormat.BGRA32, TextureFormat.BGRA32),
            (RenderTextureFormat.Depth, TextureFormat.R16),
            (RenderTextureFormat.Shadowmap, TextureFormat.R16)
        };

        private static readonly (TextureFormat Texture, RenderTextureFormat RT)[] _textureToRtMappings =
        {
            (TextureFormat.R8, RenderTextureFormat.R8),
            (TextureFormat.R16, RenderTextureFormat.R16),
            (TextureFormat.RHalf, RenderTextureFormat.RHalf),
            (TextureFormat.RFloat, RenderTextureFormat.RFloat),
            (TextureFormat.RG16, RenderTextureFormat.RG16),
            (TextureFormat.RGHalf, RenderTextureFormat.RGHalf),
            (TextureFormat.RGFloat, RenderTextureFormat.RGFloat),
            (TextureFormat.RGBA32, RenderTextureFormat.ARGB32),
            (TextureFormat.ARGB32, RenderTextureFormat.ARGB32),
            (TextureFormat.RGBAHalf, RenderTextureFormat.ARGBHalf),
            (TextureFormat.RGBAFloat, RenderTextureFormat.ARGBFloat),
            (TextureFormat.BGRA32, RenderTextureFormat.BGRA32),
            (TextureFormat.RGB565, RenderTextureFormat.RGB565)
        };

        private static readonly (TextureFormat Format, int Bits)[] _bitsPerPixelMappings =
        {
            (TextureFormat.Alpha8, 8), (TextureFormat.R8, 8),
            (TextureFormat.R16, 16), (TextureFormat.RHalf, 16), (TextureFormat.RG16, 16), (TextureFormat.RGB565, 16), (TextureFormat.ARGB4444, 16), (TextureFormat.RGBA4444, 16),
            (TextureFormat.RGB24, 24),
            (TextureFormat.RFloat, 32), (TextureFormat.RGHalf, 32), (TextureFormat.RGBA32, 32), (TextureFormat.ARGB32, 32), (TextureFormat.BGRA32, 32), (TextureFormat.RG32, 32), (TextureFormat.RGB9e5Float, 32),
            (TextureFormat.RGFloat, 64), (TextureFormat.RGBAHalf, 64), (TextureFormat.RGB48, 64),
            (TextureFormat.RGBA64, 128), (TextureFormat.RGBAFloat, 128)
        };

        private static readonly (TextureFormat Format, int Channels)[] _channelCountMappings =
        {
            (TextureFormat.Alpha8, 1), (TextureFormat.R8, 1), 
            (TextureFormat.R16, 1),
            (TextureFormat.RHalf, 1), (TextureFormat.RFloat, 1),
            (TextureFormat.RG16, 2), (TextureFormat.RG32, 2), 
            (TextureFormat.RGHalf, 2), (TextureFormat.RGFloat, 2),
            (TextureFormat.RGB24, 3), (TextureFormat.RGB565, 3), 
            (TextureFormat.RGB9e5Float, 3), (TextureFormat.RGB48, 3),
            (TextureFormat.RGBA32, 4), (TextureFormat.ARGB32, 4), 
            (TextureFormat.BGRA32, 4), (TextureFormat.RGBA4444, 4), 
            (TextureFormat.ARGB4444, 4), (TextureFormat.RGBAHalf, 4), 
            (TextureFormat.RGBAFloat, 4), (TextureFormat.RGBA64, 4)
        };

        #endregion

        #region ToTextureFormat Tests

        [Test, TestCaseSource(nameof(_rtToTextureMappings))]
        public void ToTextureFormat_MapsCorrectly((RenderTextureFormat RT, TextureFormat Texture) mapping)
        {
            // Act & Assert
            Assert.AreEqual(mapping.Texture, mapping.RT.ToTextureFormat(),
                $"Mapping failed for {mapping.RT}");
        }

        [Test]
        public void ToTextureFormat_ThrowsNotSupportedException_ForInvalidFormat()
        {
            // Arrange
            RenderTextureFormat invalidFormat = (RenderTextureFormat)999;

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => invalidFormat.ToTextureFormat(),
                "Should throw NotSupportedException for unmapped RenderTextureFormat.");
        }

        #endregion

        #region ToGraphicsFormat Tests

        [Test]
        public void ToGraphicsFormat_RenderTextureFormat_ReturnsValidGraphicsFormat()
        {
            // Arrange
            var rtFormat = RenderTextureFormat.ARGB32;

            // Act
            var result = rtFormat.ToGraphicsFormat();

            // Assert
            Assert.AreNotEqual(GraphicsFormat.None, result,
                "ToGraphicsFormat should return a valid GraphicsFormat for ARGB32.");

            Assert.AreEqual(GraphicsFormat.R8G8B8A8_UNorm, result,
                "ARGB32 should map to R8G8B8A8_UNorm in Linear mode.");
        }

        #endregion

        #region ToRenderTextureFormat Tests

        [Test, TestCaseSource(nameof(_textureToRtMappings))]
        public void ToRenderTextureFormat_MapsCorrectly((TextureFormat Texture, RenderTextureFormat RT) mapping)
        {
            // Act & Assert
            Assert.AreEqual(mapping.RT, mapping.Texture.ToRenderTextureFormat(),
                $"Mapping failed for {mapping.Texture}");
        }

        [Test]
        public void ToRenderTextureFormat_ThrowsNotSupportedException_ForInvalidFormat()
        {
            // Arrange
            TextureFormat invalidFormat = TextureFormat.DXT1;

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => invalidFormat.ToRenderTextureFormat(),
                "Should throw NotSupportedException for unmapped TextureFormat.");
        }

        #endregion

        #region ToGraphicsFormat Tests

        [Test]
        public void ToGraphicsFormat_TextureFormat_ReturnsValidGraphicsFormat()
        {
            // Arrange
            var texFormat = TextureFormat.RGBA32;

            // Act
            var result = texFormat.ToGraphicsFormat();

            // Assert
            Assert.AreNotEqual(GraphicsFormat.None, result,
                "ToGraphicsFormat should return a valid GraphicsFormat for RGBA32.");

            Assert.AreEqual(GraphicsFormat.R8G8B8A8_UNorm, result,
                "RGBA32 should map to R8G8B8A8_UNorm.");
        }

        #endregion

        #region ToRenderTextureFormat Tests

        [Test]
        public void ToRenderTextureFormat_FromGraphicsFormat_ReturnsCorrectFormat()
        {
            // Arrange
            var graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;

            // Act
            var result = graphicsFormat.ToRenderTextureFormat();

            // Assert
            Assert.AreEqual(RenderTextureFormat.ARGB32, result,
                "GraphicsFormat.R8G8B8A8_UNorm should resolve back to RenderTextureFormat.ARGB32.");
        }

        #endregion

        #region ToTextureFormat Tests

        [Test]
        public void ToTextureFormat_FromGraphicsFormat_ReturnsCorrectFormat()
        {
            // Arrange
            var graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;

            // Act
            var result = graphicsFormat.ToTextureFormat();

            // Assert
            Assert.AreEqual(TextureFormat.RGBA32, result,
                "GraphicsFormat.R8G8B8A8_UNorm should resolve back to TextureFormat.RGBA32.");
        }

        #endregion

        #region SupportsRandomWrite Tests

        [TestCase(RenderTextureFormat.RHalf, true)]
        [TestCase(RenderTextureFormat.RFloat, true)]
        [TestCase(RenderTextureFormat.RGHalf, true)]
        [TestCase(RenderTextureFormat.RGFloat, true)]
        [TestCase(RenderTextureFormat.ARGBHalf, true)]
        [TestCase(RenderTextureFormat.ARGBFloat, true)]
        [TestCase(RenderTextureFormat.ARGB32, false)]
        [TestCase(RenderTextureFormat.Depth, false)]
        [TestCase(RenderTextureFormat.R8, false)]
        public void SupportsRandomWrite_ReturnsExpectedResult(RenderTextureFormat format, bool expected)
        {
            // Act
            bool result = TextureFormatResolverExtension.SupportsRandomWrite(format);

            // Assert
            Assert.AreEqual(expected, result,
                $"SupportsRandomWrite check failed for {format}. Expected {expected}.");
        }

        #endregion

        #region GetChannelCount Tests

        [TestCase(TextureFormat.Alpha8, 1)]
        [TestCase(TextureFormat.R8, 1)]
        [TestCase(TextureFormat.RHalf, 1)]
        [TestCase(TextureFormat.RG16, 2)]
        [TestCase(TextureFormat.RGHalf, 2)]
        [TestCase(TextureFormat.RGB24, 3)]
        [TestCase(TextureFormat.RGB565, 3)]
        [TestCase(TextureFormat.RGBA32, 4)]
        [TestCase(TextureFormat.ARGB32, 4)]
        [TestCase(TextureFormat.RGBAFloat, 4)]
        public void GetChannelCount_ReturnsCorrectCount(TextureFormat format, int expectedChannels)
        {
            // Act
            int result = TextureFormatResolverExtension.GetChannelCount(format);

            // Assert
            Assert.AreEqual(expectedChannels, result,
                $"Channel count check failed for {format}. Expected {expectedChannels}, but got {result}.");
        }

        #endregion

        #region GetBitsPerPixel

        [TestCase(TextureFormat.Alpha8, 8)]
        [TestCase(TextureFormat.R8, 8)]
        [TestCase(TextureFormat.R16, 16)]
        [TestCase(TextureFormat.RHalf, 16)]
        [TestCase(TextureFormat.RG16, 16)]
        [TestCase(TextureFormat.RGB565, 16)]
        [TestCase(TextureFormat.ARGB4444, 16)]
        [TestCase(TextureFormat.RGBA4444, 16)]
        [TestCase(TextureFormat.RGB24, 24)]
        [TestCase(TextureFormat.RFloat, 32)]
        [TestCase(TextureFormat.RGHalf, 32)]
        [TestCase(TextureFormat.RGBA32, 32)]
        [TestCase(TextureFormat.ARGB32, 32)]
        [TestCase(TextureFormat.BGRA32, 32)]
        [TestCase(TextureFormat.RG32, 32)]
        [TestCase(TextureFormat.RGB9e5Float, 32)]
        [TestCase(TextureFormat.RGFloat, 64)]
        [TestCase(TextureFormat.RGBAHalf, 64)]
        [TestCase(TextureFormat.RGB48, 64)]
        [TestCase(TextureFormat.RGBA64, 128)]
        [TestCase(TextureFormat.RGBAFloat, 128)]
        public void GetBitsPerPixel_ReturnsCorrectValue(TextureFormat format, int expectedBits)
        {
            // Act
            int result = TextureFormatResolverExtension.GetBitsPerPixel(format);

            // Assert
            Assert.AreEqual(expectedBits, result,
                $"Bit depth calculation failed for {format}. Expected {expectedBits}, but got {result}.");
        }

        [Test]
        public void GetBitsPerPixel_ReturnsZero_ForUnknownFormat()
        {
            // Act & Assert
            TextureFormat unknown = TextureFormat.DXT1;

            Assert.AreEqual(0, TextureFormatResolverExtension.GetBitsPerPixel(unknown),
                "Should return 0 for undefined/compressed formats.");
        }

        #endregion
    }
}
