using NUnit.Framework;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Rendering.Collections.Tests
{
    [TestFixture]
    public class MipChainTests
    {
        #region Test Env

        public struct TestHandle
        {
            public int Id;
        }

        private class TestableMipChain : MipChain<TestHandle>
        {
            protected override void DestroyHandle(ref TestHandle handle)
            {
                handle = default;
            }

            public void SetHandlesDirectly(TestHandle[] handles)
            {
                m_Handles = handles;
            }
        }

        private struct TestCreateHandler : IFunctionHandler<MipCreateContext<int>, bool>
        {
            public bool ReturnValue;
            public List<int> VisitedMipLevels;
            public List<RenderTextureDescriptor> CapturedDescriptors;

            public bool Execute(MipCreateContext<int> context)
            {
                VisitedMipLevels?.Add(context.MipLevel);
                CapturedDescriptors?.Add(context.Descriptor);
                context.Handle = context.MipLevel + 1000;
                return ReturnValue;
            }
        }

        #endregion

        #region Properties

        [Test]
        public void MipCount_WhenUninitialized_ReturnsZero()
        {
            // Arrange
            var chain = new TestableMipChain();

            // Act & Assert
            Assert.AreEqual(0, chain.MipCount);
        }

        [Test]
        public void MipCount_WhenPopulated_ReturnsCorrectLength()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 1 },
                new TestHandle { Id = 2 },
                new TestHandle { Id = 3 }
            });

            // Act & Assert
            Assert.AreEqual(3, chain.MipCount);
        }

        [Test]
        public void Handles_WhenUninitialized_ReturnsEmptyCollectionNotNull()
        {
            // Arrange
            var chain = new TestableMipChain();

            // Act
            IReadOnlyList<TestHandle> handles = chain.Handles;

            // Assert
            Assert.IsNotNull(handles, "Handles should never return null even when uninitialized.");
            Assert.AreEqual(0, handles.Count, "Handles collection should be empty.");
        }

        [Test]
        public void Handles_WhenPopulated_ReturnsUnderlyingElements()
        {
            // Arrange
            var chain = new TestableMipChain();
            var expectedHandles = new TestHandle[]
            {
                new TestHandle { Id = 10 },
                new TestHandle { Id = 20 }
            };
            chain.SetHandlesDirectly(expectedHandles);

            // Act
            IReadOnlyList<TestHandle> handles = chain.Handles;

            // Assert
            Assert.IsNotNull(handles);
            Assert.AreEqual(2, handles.Count);
            Assert.AreEqual(10, handles[0].Id);
            Assert.AreEqual(20, handles[1].Id);
        }

        [Test]
        public void Indexer_WithValidIndex_ReturnsCorrectHandle()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 100 },
                new TestHandle { Id = 200 }
            });

            // Act & Assert
            Assert.AreEqual(100, chain[0].Id);
            Assert.AreEqual(200, chain[1].Id);
        }

        [TestCase(-1)]
        [TestCase(2)]
        [TestCase(5)]
        public void Indexer_WithInvalidIndex_ThrowsArgumentOutOfRangeException(int invalidIndex)
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 1 },
                new TestHandle { Id = 2 }
            });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var handle = chain[invalidIndex];
            });
        }

        #endregion

        #region Create Tests



        #endregion
    }
}
