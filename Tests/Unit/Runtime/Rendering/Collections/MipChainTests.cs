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

        private struct TestCreateHandler : IFunctionHandler<MipCreateContext<TestHandle>, bool>
        {
            public bool ReturnValue;
            public List<int> VisitedMipLevels;
            public List<RenderTextureDescriptor> CapturedDescriptors;

            public bool Execute(MipCreateContext<TestHandle> context)
            {
                VisitedMipLevels?.Add(context.MipLevel);
                CapturedDescriptors?.Add(context.Descriptor);
                context.Handle = new TestHandle { Id = context.MipLevel + 1000 };
                return ReturnValue;
            }
        }

        private struct TestExecutionHandler : IExecutionHandler<TestHandle>
        {
            public List<int> VisitedIds;
            public int ExecutionCount;

            public void Execute(TestHandle handle)
            {
                VisitedIds ??= new List<int>();
                VisitedIds.Add(handle.Id);
                ExecutionCount++;
            }
        }

        private struct SumExecutionHandler : IExecutionHandler<TestHandle>
        {
            public int Sum;

            public void Execute(TestHandle handle)
            {
                Sum += handle.Id;
            }
        }

        private struct TestMipPairExecutionHandler : IExecutionHandler<MipPair<TestHandle>>
        {
            public List<(int higherId, int lowerId)> VisitedPairs;
            public int ExecutionCount;

            public void Execute(MipPair<TestHandle> mipPair)
            {
                VisitedPairs ??= new List<(int, int)>();
                VisitedPairs.Add((mipPair.Source.Id, mipPair.Destination.Id));
                ExecutionCount++;
            }
        }

        private struct SumMipPairExecutionHandler : IExecutionHandler<MipPair<TestHandle>>
        {
            public int CombinedIdSum;

            public void Execute(MipPair<TestHandle> mipPair)
            {
                CombinedIdSum += mipPair.Source.Id + mipPair.Destination.Id;
            }
        }

        #endregion

        #region Properties Tests

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
        public void BaseResolution_WhenUninitialized_ReturnsDefaultValue()
        {
            // Arrange
            var chain = new TestableMipChain();

            // Act & Assert
            Assert.AreEqual(new Vector2Int(-1, -1), chain.BaseResolution);
        }

        [Test]
        public void BaseResolution_AfterCreate_ReturnsCorrectBaseResolution()
        {
            // Arrange
            var chain = new TestableMipChain();
            var descriptorChain = new DescriptorMipChain(1024, 512, 2);
            var handler = new TestCreateHandler { ReturnValue = true };

            // Act
            chain.Create(descriptorChain, ref handler);

            // Assert
            Assert.AreEqual(new Vector2Int(1024, 512), chain.BaseResolution);
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

        [Test]
        public void Create_WithNullDescriptorChain_ThrowsArgumentNullException()
        {
            // Arrange
            var chain = new TestableMipChain();
            var handler = new TestCreateHandler { ReturnValue = true };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => chain.Create<TestCreateHandler>(null, ref handler));
        }

        [Test]
        public void Create_WhenAnyHandleCreated_ReturnsTrueAndPopulatesHandles()
        {
            // Arrange
            var chain = new TestableMipChain();
            var descriptorChain = new DescriptorMipChain(512, 256, 3, null, RenderTextureFormat.ARGB32);
            var handler = new TestCreateHandler
            {
                ReturnValue = true,
                VisitedMipLevels = new List<int>(),
                CapturedDescriptors = new List<RenderTextureDescriptor>()
            };

            // Act
            bool result = chain.Create(descriptorChain, ref handler);

            // Assert
            Assert.IsTrue(result, "Should return true when at least one handle is created.");
            Assert.AreEqual(3, chain.MipCount);
            Assert.AreEqual(1000, chain[0].Id);
            Assert.AreEqual(1001, chain[1].Id);
            Assert.AreEqual(1002, chain[2].Id);
            Assert.AreEqual(new Vector2Int(512, 256), chain.BaseResolution);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, handler.VisitedMipLevels);
            Assert.AreEqual(3, handler.CapturedDescriptors.Count);
            Assert.AreEqual(512, handler.CapturedDescriptors[0].width);
            Assert.AreEqual(256, handler.CapturedDescriptors[0].height);
        }

        [Test]
        public void Create_WhenAllHandlesReused_ReturnsFalse()
        {
            // Arrange
            var chain = new TestableMipChain();
            var descriptorChain = new DescriptorMipChain(128, 128, 2);
            var handler = new TestCreateHandler
            {
                ReturnValue = false,
                VisitedMipLevels = new List<int>(),
                CapturedDescriptors = new List<RenderTextureDescriptor>()
            };

            // Act
            bool result = chain.Create(descriptorChain, ref handler);

            // Assert
            Assert.IsFalse(result, "Should return false when all handles are reused.");
            Assert.AreEqual(2, chain.MipCount);
            CollectionAssert.AreEqual(new[] { 0, 1 }, handler.VisitedMipLevels);
        }

        [Test]
        public void Create_ResizesExistingChainCorrectly()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[] { new TestHandle { Id = 99 } });

            var descriptorChain = new DescriptorMipChain(256, 256, 4);
            var handler = new TestCreateHandler { ReturnValue = true };

            // Act
            chain.Create(descriptorChain, ref handler);

            // Assert
            Assert.AreEqual(4, chain.MipCount);
            Assert.AreEqual(1000, chain[0].Id);
            Assert.AreEqual(1001, chain[1].Id);
            Assert.AreEqual(1002, chain[2].Id);
            Assert.AreEqual(1003, chain[3].Id);
        }

        [Test]
        public void Create_WithSingleDescriptor_DefaultsToOneMipLevel()
        {
            // Arrange
            var chain = new TestableMipChain();
            var descriptor = new RenderTextureDescriptor(512, 512, RenderTextureFormat.ARGB32, 0);
            var handler = new TestCreateHandler
            {
                ReturnValue = true,
                VisitedMipLevels = new List<int>(),
                CapturedDescriptors = new List<RenderTextureDescriptor>()
            };

            // Act
            bool result = chain.Create(descriptor, ref handler);

            // Assert
            Assert.IsTrue(result, "Should return true when handle is created.");
            Assert.AreEqual(1, chain.MipCount);
            Assert.AreEqual(new Vector2Int(512, 512), chain.BaseResolution);
            CollectionAssert.AreEqual(new[] { 0 }, handler.VisitedMipLevels);
            Assert.AreEqual(512, handler.CapturedDescriptors[0].width);
            Assert.AreEqual(512, handler.CapturedDescriptors[0].height);
        }

        [Test]
        public void Create_WithDescriptorAndMipCount_CreatesExpectedMipLevels()
        {
            // Arrange
            var chain = new TestableMipChain();
            var descriptor = new RenderTextureDescriptor(256, 256, RenderTextureFormat.ARGB32, 0);
            var handler = new TestCreateHandler
            {
                ReturnValue = true,
                VisitedMipLevels = new List<int>(),
                CapturedDescriptors = new List<RenderTextureDescriptor>()
            };

            // Act
            bool result = chain.Create(descriptor, 3, ref handler);

            // Assert
            Assert.IsTrue(result, "Should return true when handles are created.");
            Assert.AreEqual(3, chain.MipCount);
            Assert.AreEqual(new Vector2Int(256, 256), chain.BaseResolution);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, handler.VisitedMipLevels);
            Assert.AreEqual(3, handler.CapturedDescriptors.Count);
        }

        [Test]
        public void Create_WithRawDimensionsAndDescriptor_CreatesExpectedMipLevels()
        {
            // Arrange
            var chain = new TestableMipChain();
            var descriptor = new RenderTextureDescriptor(512, 256, RenderTextureFormat.ARGB32, 0);
            var handler = new TestCreateHandler
            {
                ReturnValue = true,
                VisitedMipLevels = new List<int>(),
                CapturedDescriptors = new List<RenderTextureDescriptor>()
            };

            // Act
            bool result = chain.Create(512, 256, descriptor, 2, ref handler);

            // Assert
            Assert.IsTrue(result, "Should return true when handles are created.");
            Assert.AreEqual(2, chain.MipCount);
            Assert.AreEqual(new Vector2Int(512, 256), chain.BaseResolution);
            CollectionAssert.AreEqual(new[] { 0, 1 }, handler.VisitedMipLevels);
            Assert.AreEqual(2, handler.CapturedDescriptors.Count);
        }

        [TestCase(0, 256)]
        [TestCase(-10, 256)]
        [TestCase(512, 0)]
        [TestCase(512, -5)]
        public void Create_WithNonPositiveWidthOrHeight_ThrowsArgumentException(int invalidWidth, int invalidHeight)
        {
            // Arrange
            var chain = new TestableMipChain();
            var descriptor = new RenderTextureDescriptor(256, 256, RenderTextureFormat.ARGB32, 0);
            var handler = new TestCreateHandler { ReturnValue = true };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => chain.Create(invalidWidth, invalidHeight, descriptor, 3, ref handler));
        }

        #endregion

        #region Resize Tests

        [Test]
        public void Resize_FromUninitializedToNewLength_AllocatesEmptyArray()
        {
            // Arrange
            var chain = new TestableMipChain();

            // Act
            chain.Resize(3);

            // Assert
            Assert.AreEqual(3, chain.MipCount);
            Assert.IsNotNull(chain.Handles);
            Assert.AreEqual(0, chain[0].Id);
            Assert.AreEqual(0, chain[1].Id);
            Assert.AreEqual(0, chain[2].Id);
        }

        [Test]
        public void Resize_ToZero_ClearsHandlesAndResetsLength()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 1 },
                new TestHandle { Id = 2 }
            });

            // Act
            chain.Resize(0);

            // Assert
            Assert.AreEqual(0, chain.MipCount);
            Assert.AreEqual(0, chain.Handles.Count);
        }

        [Test]
        public void Resize_ToNegativeLength_TreatsAsZero()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[] { new TestHandle { Id = 1 } });

            // Act
            chain.Resize(-5);

            // Assert
            Assert.AreEqual(0, chain.MipCount);
        }

        [Test]
        public void Resize_WhenLengthIsUnchanged_DoesNothing()
        {
            // Arrange
            var chain = new TestableMipChain();
            var originalArray = new TestHandle[] { new TestHandle { Id = 42 } };
            chain.SetHandlesDirectly(originalArray);

            // Act
            chain.Resize(1);

            // Assert
            Assert.AreEqual(1, chain.MipCount);
            Assert.AreSame(originalArray, chain.Handles);
        }

        [Test]
        public void Resize_ShrinkingArray_DestroysUnpreservedHandles()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 },
                new TestHandle { Id = 20 },
                new TestHandle { Id = 30 }
            });

            // Act
            chain.Resize(1);

            // Assert
            Assert.AreEqual(1, chain.MipCount);
            Assert.AreEqual(10, chain[0].Id);
        }

        [Test]
        public void Resize_WithPreservation_KeepsSpecifiedElementsAndDestroysOthers()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 1 },
                new TestHandle { Id = 2 },
                new TestHandle { Id = 3 },
                new TestHandle { Id = 4 }
            });

            // Act
            chain.Resize(2, preserveIndex: 1, preserveCount: 2);

            // Assert
            Assert.AreEqual(2, chain.MipCount);
            Assert.AreEqual(2, chain[0].Id);
            Assert.AreEqual(3, chain[1].Id);
        }

        [Test]
        public void Resize_GrowingArray_PreservesExistingAndPadsWithDefaults()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
        new TestHandle { Id = 100 }
            });

            // Act
            chain.Resize(3, preserveIndex: 0, preserveCount: 1);

            // Assert
            Assert.AreEqual(3, chain.MipCount);
            Assert.AreEqual(100, chain[0].Id);
            Assert.AreEqual(0, chain[1].Id);
            Assert.AreEqual(0, chain[2].Id);
        }

        #endregion

        #region Span Tests

        [Test]
        public void AsSpan_WithoutArguments_WhenUninitialized_ReturnsEmptySpan()
        {
            // Arrange
            var chain = new TestableMipChain();

            // Act
            ReadOnlySpan<TestHandle> span = chain.AsSpan();

            // Assert
            Assert.IsTrue(span.IsEmpty);
            Assert.AreEqual(0, span.Length);
        }

        [Test]
        public void AsSpan_WithoutArguments_WhenPopulated_ReturnsFullSpan()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 1 },
                new TestHandle { Id = 2 },
                new TestHandle { Id = 3 }
            });

            // Act
            ReadOnlySpan<TestHandle> span = chain.AsSpan();

            // Assert
            Assert.AreEqual(3, span.Length);
            Assert.AreEqual(1, span[0].Id);
            Assert.AreEqual(2, span[1].Id);
            Assert.AreEqual(3, span[2].Id);
        }

        [Test]
        public void AsSpan_WithValidStartAndLength_ReturnsExpectedSubSpan(
            [Values(0, 1, 2)] int start,
            [Values(1, 2)] int length)
        {
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 },
                new TestHandle { Id = 20 },
                new TestHandle { Id = 30 },
                new TestHandle { Id = 40 }
            });

            if (start + length > chain.MipCount) return;

            // Act
            ReadOnlySpan<TestHandle> span = chain.AsSpan(start, length);

            // Assert
            Assert.AreEqual(length, span.Length);
            for (int i = 0; i < length; i++)
            {
                Assert.AreEqual(chain[start + i].Id, span[i].Id);
            }
        }

        [Test]
        public void AsSpan_WithZeroLength_ReturnsEmptySpan()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 },
                new TestHandle { Id = 20 }
            });

            // Act
            ReadOnlySpan<TestHandle> span = chain.AsSpan(1, 0);

            // Assert
            Assert.AreEqual(0, span.Length);
            Assert.IsTrue(span.IsEmpty);
        }

        [Test]
        public void AsSpan_AtBoundary_StartEqualsMipCountAndLengthZero_ReturnsEmptySpan()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 },
                new TestHandle { Id = 20 }
            });

            // Act
            ReadOnlySpan<TestHandle> span = chain.AsSpan(2, 0);

            // Assert
            Assert.AreEqual(0, span.Length);
            Assert.IsTrue(span.IsEmpty);
        }

        [Test]
        public void AsSpan_WithNegativeStart_ThrowsArgumentOutOfRangeException(
            [Values(-1, -5)] int invalidStart)
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[] { new TestHandle { Id = 1 } });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => chain.AsSpan(invalidStart, 1));
        }

        [Test]
        public void AsSpan_WithStartGreaterThanMipCount_ThrowsArgumentOutOfRangeException(
            [Values(3, 5, 10)] int invalidStart)
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 1 },
                new TestHandle { Id = 2 }
            });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => chain.AsSpan(invalidStart, 0));
        }

        [Test]
        public void AsSpan_WithNegativeLength_ThrowsArgumentOutOfRangeException(
            [Values(-1, -10)] int invalidLength)
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[] { new TestHandle { Id = 1 } });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => chain.AsSpan(0, invalidLength));
        }

        [Test]
        public void AsSpan_WithStartPlusLengthExceedingMipCount_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 1 },
                new TestHandle { Id = 2 }
            });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => chain.AsSpan(1, 2));
        }

        #endregion

        #region Copy Tests

        [Test]
        public void CopyFrom_FullChain_CopiesAllHandlesCorrectly()
        {
            // Arrange
            var sourceChain = new TestableMipChain();
            sourceChain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 },
                new TestHandle { Id = 20 },
                new TestHandle { Id = 30 }
            });

            var targetChain = new TestableMipChain();

            // Act
            targetChain.CopyFrom(sourceChain);

            // Assert
            Assert.AreEqual(3, targetChain.MipCount);
            Assert.AreEqual(10, targetChain[0].Id);
            Assert.AreEqual(20, targetChain[1].Id);
            Assert.AreEqual(30, targetChain[2].Id);
        }

        [Test]
        public void CopyFrom_NullChain_ThrowsArgumentNullException()
        {
            // Arrange
            var chain = new TestableMipChain();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => chain.CopyFrom(null));
        }

        [Test]
        public void CopyFrom_Range_CopiesSpecifiedSubRangeCorrectly()
        {
            // Arrange
            var sourceChain = new TestableMipChain();
            sourceChain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 },
                new TestHandle { Id = 20 },
                new TestHandle { Id = 30 },
                new TestHandle { Id = 40 }
            });

            var targetChain = new TestableMipChain();

            // Act
            targetChain.CopyFrom(sourceChain, 1, 2);

            // Assert
            Assert.AreEqual(2, targetChain.MipCount);
            Assert.AreEqual(20, targetChain[0].Id);
            Assert.AreEqual(30, targetChain[1].Id);
        }

        [Test]
        public void CopyFrom_RangeWithNullSource_ThrowsArgumentNullException()
        {
            // Arrange
            var chain = new TestableMipChain();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => chain.CopyFrom(null, 0, 1));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void CopyFrom_RangeWithInvalidStart_ThrowsArgumentOutOfRangeException(int invalidStart)
        {
            // Arrange
            var sourceChain = new TestableMipChain();
            sourceChain.SetHandlesDirectly(new TestHandle[] { new TestHandle { Id = 1 } });

            var targetChain = new TestableMipChain();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => targetChain.CopyFrom(sourceChain, invalidStart, 1));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void CopyFrom_RangeWithInvalidCount_ThrowsArgumentOutOfRangeException(int invalidCount)
        {
            // Arrange
            var sourceChain = new TestableMipChain();
            sourceChain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 1 },
                new TestHandle { Id = 2 }
            });

            var targetChain = new TestableMipChain();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => targetChain.CopyFrom(sourceChain, 0, invalidCount));
        }

        [Test]
        public void CopyFrom_RangeExceedingSourceBounds_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var sourceChain = new TestableMipChain();
            sourceChain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 1 },
                new TestHandle { Id = 2 }
            });

            var targetChain = new TestableMipChain();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => targetChain.CopyFrom(sourceChain, 1, 2));
        }

        [Test]
        public void CopyFrom_SingleHandle_CreatesChainOfLengthOne()
        {
            // Arrange
            var chain = new TestableMipChain();
            var handle = new TestHandle { Id = 999 };

            // Act
            chain.CopyFrom(handle);

            // Assert
            Assert.AreEqual(1, chain.MipCount);
            Assert.AreEqual(999, chain[0].Id);
        }

        #endregion

        #region Iteration Tests

        [Test]
        public void GetIterator_WhenUninitialized_ReturnsNotNullIterator()
        {
            // Arrange
            var chain = new TestableMipChain();

            // Act
            var iterator = chain.GetIterator();

            // Assert
            Assert.IsNotNull(iterator, "GetIterator should never return null.");
        }

        [Test]
        public void GetIterator_WhenPopulated_IteratesThroughAllHandlesInOrder()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 },
                new TestHandle { Id = 20 },
                new TestHandle { Id = 30 }
            });

            // Act
            var iterator = chain.GetIterator();

            // Assert
            Assert.IsNotNull(iterator);

            var collectedIds = new List<int>();

            while (iterator.MoveNext())
            {
                collectedIds.Add(iterator.Current.Id);
            }

            Assert.AreEqual(3, collectedIds.Count);
            Assert.AreEqual(10, collectedIds[0]);
            Assert.AreEqual(20, collectedIds[1]);
            Assert.AreEqual(30, collectedIds[2]);
        }

        [Test]
        public void ForEach_WhenUninitializedOrEmpty_DoesNotExecuteAction()
        {
            // Arrange
            var chain = new TestableMipChain();
            var handler = new TestExecutionHandler();

            // Act
            chain.ForEach(ref handler);

            // Assert
            Assert.AreEqual(0, handler.ExecutionCount);
        }

        [Test]
        public void ForEach_WhenPopulated_ExecutesActionForEachHandleInOrder()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 },
                new TestHandle { Id = 20 },
                new TestHandle { Id = 30 }
            });

            var handler = new TestExecutionHandler();

            // Act
            chain.ForEach(ref handler);

            // Assert
            Assert.AreEqual(3, handler.ExecutionCount);
            CollectionAssert.AreEqual(new[] { 10, 20, 30 }, handler.VisitedIds);
        }

        [Test]
        public void ForEach_ModifiesHandlerStateByRef_ReflectsChangesAfterCall()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 15 },
                new TestHandle { Id = 25 }
            });

            var handler = new SumExecutionHandler { Sum = 10 };

            // Act
            chain.ForEach(ref handler);

            // Assert
            Assert.AreEqual(50, handler.Sum); // 10 + 15 + 25
        }

        [Test]
        public void GetMipPairIterator_WhenUninitialized_ReturnsNotNullIterator()
        {
            // Arrange
            var chain = new TestableMipChain();

            // Act
            var iterator = chain.GetMipPairIterator();

            // Assert
            Assert.IsNotNull(iterator, "GetMipPairIterator should never return null.");
        }

        [Test]
        public void GetMipPairIterator_WhenFewerThanTwoElements_YieldsNoPairs()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 }
            });

            // Act
            var iterator = chain.GetMipPairIterator();

            // Assert
            Assert.IsNotNull(iterator);
            Assert.IsFalse(iterator.MoveNext(), "Should not yield any pairs if there are fewer than 2 elements.");
        }

        [Test]
        public void GetMipPairIterator_WhenPopulatedWithMultipleElements_IteratesConsecutivePairsInOrder()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 },
                new TestHandle { Id = 20 },
                new TestHandle { Id = 30 }
            });

            // Act
            var iterator = chain.GetMipPairIterator();

            // Assert
            Assert.IsNotNull(iterator);

            var pairIds = new List<(int higherId, int lowerId)>();
            while (iterator.MoveNext())
            {
                var pair = iterator.Current;
                pairIds.Add((pair.Source.Id, pair.Destination.Id));
            }

            Assert.AreEqual(2, pairIds.Count);
            Assert.AreEqual((10, 20), pairIds[0]);
            Assert.AreEqual((20, 30), pairIds[1]);
        }

        [Test]
        public void ForEachMipPair_WhenFewerThanTwoElements_DoesNotExecuteAction()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 }
            });

            var handler = new TestMipPairExecutionHandler();

            // Act
            chain.ForEachMipPair(ref handler);

            // Assert
            Assert.AreEqual(0, handler.ExecutionCount);
        }

        [Test]
        public void ForEachMipPair_WhenPopulatedWithMultipleElements_ExecutesActionForEachPairInOrder()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 10 },
                new TestHandle { Id = 20 },
                new TestHandle { Id = 30 }
            });

            var handler = new TestMipPairExecutionHandler();

            // Act
            chain.ForEachMipPair(ref handler);

            // Assert
            Assert.AreEqual(2, handler.ExecutionCount);
            CollectionAssert.AreEqual(new[] { (10, 20), (20, 30) }, handler.VisitedPairs);
        }

        [Test]
        public void ForEachMipPair_ModifiesHandlerStateByRef_ReflectsChangesAfterCall()
        {
            // Arrange
            var chain = new TestableMipChain();
            chain.SetHandlesDirectly(new TestHandle[]
            {
                new TestHandle { Id = 5 },
                new TestHandle { Id = 10 }
            });

            var handler = new SumMipPairExecutionHandler { CombinedIdSum = 0 };

            // Act
            chain.ForEachMipPair(ref handler);

            // Assert
            Assert.AreEqual(15, handler.CombinedIdSum); // 5 + 10
        }

        #endregion
    }
}
