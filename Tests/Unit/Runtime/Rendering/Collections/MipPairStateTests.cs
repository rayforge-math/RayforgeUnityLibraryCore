using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions.Tests;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Core.Rendering.Collections.Tests
{
    [TestFixture]
    public class MipPairStateTests : IIterationLogicTests<MipPair<RTHandle>, MipPairState<RTHandle>>
    {
        #region Test Env

        private List<RTHandle> m_AllocatedHandles;

        private RTHandle CreateManagedHandle(int width, int height)
        {
            var handle = RTHandles.Alloc(width, height, colorFormat: GraphicsFormat.R8G8B8A8_UNorm);
            m_AllocatedHandles.Add(handle);
            return handle;
        }

        protected override IterationTestData<MipPair<RTHandle>, MipPairState<RTHandle>> CreateLogic(int count)
        {
            int handleCount = Math.Max(0, ++count);
            var handles = new RTHandle[handleCount];

            for (int i = 0; i < handleCount; i++)
            {
                handles[i] = CreateManagedHandle(1024 >> i / 2, 1024 >> i / 2);
            }

            int pairCount = Math.Max(0, handleCount - 1);
            var expectedPairs = new MipPair<RTHandle>[pairCount];
            for (int i = 0; i < pairCount; i++)
            {
                expectedPairs[i] = new MipPair<RTHandle>(handles[i], handles[i + 1], i + 1);
            }

            return new IterationTestData<MipPair<RTHandle>, MipPairState<RTHandle>>
            {
                expected = expectedPairs,
                logic = new MipPairState<RTHandle>(handles)
            };
        }

        [SetUp]
        public void SetUp()
        {
            m_AllocatedHandles = new List<RTHandle>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var handle in m_AllocatedHandles)
            {
                handle?.Release();
            }
            m_AllocatedHandles.Clear();
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_WithNullHandlesArray_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MipPairState<RTHandle>(null));
        }

        #endregion
    }
}
