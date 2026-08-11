using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions.Tests
{
    /// <param name="TKey">The unique identifier type.</param>
    /// <param name="TCulling">The culling struct type.</param>
    /// <param name="TRender">The render struct type.</param>
    /// <typeparam name="TProvider">The concrete type of the spatial metadata provider being tested.</typeparam>
    public abstract class ISpatialMetadataProviderTests<TKey, TCulling, TRender, TProvider>
        where TKey : struct, IEquatable<TKey>
        where TCulling : unmanaged, IGpuData<TCulling>
        where TRender : unmanaged, IGpuData<TRender>
        where TProvider : ISpatialMetadataProvider<TKey, TCulling, TRender>
    {
        #region TestEnv

        protected abstract TProvider CreateProvider(int capacity, int batchSize);
        protected abstract TCulling GetDefaultCulling();
        protected abstract TRender GetDefaultRender();

        #endregion

        #region Properties

        [Test]
        public void CullingTypedBuffer_NotNull()
        {
            var provider = CreateProvider(16, 4);
            Assert.That(provider.CullingTypedBuffer, Is.Not.Null);
        }

        [Test]
        public void CullingUntypedBuffer_NotNull()
        {
            var provider = CreateProvider(16, 4);
            Assert.That(provider.CullingUntypedBuffer, Is.Not.Null);
        }

        [Test]
        public void CullingUntypedBuffer_IsOfTypeTCullingArray()
        {
            var provider = CreateProvider(16, 4);
            Assert.That(provider.CullingUntypedBuffer, Is.InstanceOf<TCulling[]>());
        }

        [Test]
        public void CullingAsSpan_NotNullOrEmpty()
        {
            var provider = CreateProvider(16, 4);
            ReadOnlySpan<TCulling> span = provider.CullingAsSpan();
            Assert.That(span.IsEmpty, Is.False);
        }

        [Test]
        public void CullingAsSpan_LengthMatchesTypedBuffer()
        {
            var provider = CreateProvider(16, 4);
            ReadOnlySpan<TCulling> span = provider.CullingAsSpan();
            Assert.That(span.Length, Is.EqualTo(provider.CullingTypedBuffer.Length));
        }

        [Test]
        public void CullingStride_MatchesStructSize()
        {
            var provider = CreateProvider(16, 4);
            Assert.That(provider.CullingStride, Is.EqualTo(4));
        }

        [Test]
        public void RenderTypedBuffer_NotNull()
        {
            var provider = CreateProvider(16, 4);
            Assert.That(provider.RenderTypedBuffer, Is.Not.Null);
        }

        [Test]
        public void RenderUntypedBuffer_NotNull()
        {
            var provider = CreateProvider(16, 4);
            Assert.That(provider.RenderUntypedBuffer, Is.Not.Null);
        }

        [Test]
        public void RenderUntypedBuffer_IsOfTypeTRenderArray()
        {
            var provider = CreateProvider(16, 4);
            Assert.That(provider.RenderUntypedBuffer, Is.InstanceOf<TRender[]>());
        }

        [Test]
        public void RenderAsSpan_NotNullOrEmpty()
        {
            var provider = CreateProvider(16, 4);
            ReadOnlySpan<TRender> span = provider.RenderAsSpan();
            Assert.That(span.IsEmpty, Is.False);
        }

        [Test]
        public void RenderAsSpan_LengthMatchesTypedBuffer()
        {
            var provider = CreateProvider(16, 4);
            ReadOnlySpan<TRender> span = provider.RenderAsSpan();
            Assert.That(span.Length, Is.EqualTo(provider.RenderTypedBuffer.Length));
        }

        [Test]
        public void RenderStride_MatchesStructSize()
        {
            var provider = CreateProvider(16, 4);
            Assert.That(provider.RenderStride, Is.EqualTo(4));
        }

        [Test]
        public void Capacity_MatchesExpected()
        {
            int expectedCapacity = 16;
            var provider = CreateProvider(expectedCapacity, 4);
            Assert.That(provider.Capacity, Is.EqualTo(expectedCapacity));
        }

        [Test]
        public void BatchSize_MatchesExpected()
        {
            int expectedBatchSize = 8;
            var provider = CreateProvider(16, expectedBatchSize);
            Assert.That(provider.BatchSize, Is.EqualTo(expectedBatchSize));
        }

        [Test]
        public void HighestIndex_InitialStateIsMinusOneOrValid()
        {
            var provider = CreateProvider(16, 4);
            Assert.That(provider.HighestIndex, Is.EqualTo(-1));
        }

        #endregion

        #region SetMetaData Tests

        [Test]
        public void SetMetadata_WithNewKey_AllocatesIndexAndStoresData()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;
            TCulling culling = GetDefaultCulling();
            TRender render = GetDefaultRender();

            int index = provider.SetMetadata(key, culling, render);

            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            Assert.That(provider.Contains(key), Is.True);

            Assert.That(provider.TryGetMetadata(key, out var retrievedCulling, out var retrievedRender), Is.True);
            Assert.That(retrievedCulling, Is.EqualTo(culling));
            Assert.That(retrievedRender, Is.EqualTo(render));
        }

        [Test]
        public void SetMetadata_WithExistingKey_UpdatesDataWithoutAllocatingNewIndex()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;

            int initialIndex = provider.SetMetadata(key, GetDefaultCulling(), GetDefaultRender());

            TCulling newCulling = GetDefaultCulling();
            TRender newRender = GetDefaultRender();

            int subsequentIndex = provider.SetMetadata(key, newCulling, newRender);

            Assert.That(subsequentIndex, Is.EqualTo(initialIndex));

            provider.TryGetMetadata(key, out var retrievedCulling, out var retrievedRender);
            Assert.That(retrievedCulling, Is.EqualTo(newCulling));
            Assert.That(retrievedRender, Is.EqualTo(newRender));
        }

        #endregion

        #region SetCulling Tests

        [Test]
        public void SetCulling_WithNewKey_AllocatesIndexAndStoresCulling()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;
            TCulling culling = GetDefaultCulling();

            int index = provider.SetCulling(key, culling);

            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            Assert.That(provider.Contains(key), Is.True);

            Assert.That(provider.TryGetCulling(key, out var retrievedCulling), Is.True);
            Assert.That(retrievedCulling, Is.EqualTo(culling));
        }

        [Test]
        public void SetCulling_WithExistingKey_UpdatesCullingWithoutChangingIndex()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;

            int initialIndex = provider.SetMetadata(key, GetDefaultCulling(), GetDefaultRender());

            TCulling newCulling = GetDefaultCulling();
            int subsequentIndex = provider.SetCulling(key, newCulling);

            Assert.That(subsequentIndex, Is.EqualTo(initialIndex));

            provider.TryGetCulling(key, out var retrievedCulling);
            Assert.That(retrievedCulling, Is.EqualTo(newCulling));
        }

        #endregion

        #region SetCulling Tests

        [Test]
        public void SetRender_WithNewKey_AllocatesIndexAndStoresRender()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;
            TRender render = GetDefaultRender();

            int index = provider.SetRender(key, render);

            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            Assert.That(provider.Contains(key), Is.True);

            Assert.That(provider.TryGetRender(key, out var retrievedRender), Is.True);
            Assert.That(retrievedRender, Is.EqualTo(render));
        }

        [Test]
        public void SetRender_WithExistingKey_UpdatesRenderWithoutChangingIndex()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;

            int initialIndex = provider.SetMetadata(key, GetDefaultCulling(), GetDefaultRender());

            TRender newRender = GetDefaultRender();
            int subsequentIndex = provider.SetRender(key, newRender);

            Assert.That(subsequentIndex, Is.EqualTo(initialIndex));

            provider.TryGetRender(key, out var retrievedRender);
            Assert.That(retrievedRender, Is.EqualTo(newRender));
        }

        #endregion

        #region ReleaseAndKill Tests

        [Test]
        public void ReleaseAndKill_WithExistingKey_ReturnsValidIndexAndRemovesKey()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;

            int originalIndex = provider.SetMetadata(key, GetDefaultCulling(), GetDefaultRender());

            int releasedIndex = provider.ReleaseAndKill(key);

            Assert.That(releasedIndex, Is.EqualTo(originalIndex));
            Assert.That(provider.Contains(key), Is.False);
        }

        [Test]
        public void ReleaseAndKill_WithNonExistingKey_ReturnsNegativeIndex()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;

            int releasedIndex = provider.ReleaseAndKill(key);

            Assert.That(releasedIndex, Is.LessThan(0));
        }

        #endregion

        #region TryGetMetadata Tests

        [Test]
        public void TryGetMetadata_WithExistingKey_ReturnsTrueAndCorrectData()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;
            TCulling expectedCulling = GetDefaultCulling();
            TRender expectedRender = GetDefaultRender();

            provider.SetMetadata(key, expectedCulling, expectedRender);

            bool success = provider.TryGetMetadata(key, out var actualCulling, out var actualRender);

            Assert.That(success, Is.True);
            Assert.That(actualCulling, Is.EqualTo(expectedCulling));
            Assert.That(actualRender, Is.EqualTo(expectedRender));
        }

        [Test]
        public void TryGetMetadata_WithNonExistingKey_ReturnsFalse()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;

            bool success = provider.TryGetMetadata(key, out _, out _);

            Assert.That(success, Is.False);
        }

        #endregion

        #region TryGetCulling Tests

        [Test]
        public void TryGetCulling_WithExistingKey_ReturnsTrueAndCorrectCullingData()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;
            TCulling expectedCulling = GetDefaultCulling();
            TRender expectedRender = GetDefaultRender();

            provider.SetMetadata(key, expectedCulling, expectedRender);

            bool success = provider.TryGetCulling(key, out var actualCulling);

            Assert.That(success, Is.True);
            Assert.That(actualCulling, Is.EqualTo(expectedCulling));
        }

        [Test]
        public void TryGetCulling_WithNonExistingKey_ReturnsFalse()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;

            bool success = provider.TryGetCulling(key, out _);

            Assert.That(success, Is.False);
        }

        #endregion

        #region TryGetRender Tests

        [Test]
        public void TryGetRender_WithExistingKey_ReturnsTrueAndCorrectRenderData()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;
            TCulling expectedCulling = GetDefaultCulling();
            TRender expectedRender = GetDefaultRender();

            provider.SetMetadata(key, expectedCulling, expectedRender);

            bool success = provider.TryGetRender(key, out var actualRender);

            Assert.That(success, Is.True);
            Assert.That(actualRender, Is.EqualTo(expectedRender));
        }

        [Test]
        public void TryGetRender_WithNonExistingKey_ReturnsFalse()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;

            bool success = provider.TryGetRender(key, out _);

            Assert.That(success, Is.False);
        }

        #endregion

        #region Contains Tests

        [Test]
        public void Contains_WithExistingKey_ReturnsTrue()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;

            provider.SetMetadata(key, GetDefaultCulling(), GetDefaultRender());

            Assert.That(provider.Contains(key), Is.True);
        }

        [Test]
        public void Contains_WithNonExistingKey_ReturnsFalse()
        {
            var provider = CreateProvider(16, 4);
            TKey key = default;

            Assert.That(provider.Contains(key), Is.False);
        }

        #endregion
    }
}