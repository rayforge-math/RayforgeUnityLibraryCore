using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Collections.Abstractions.Tests
{
    /// <summary>
    /// Base class for providing test data environments for <see cref="IGpuDataProvider{TKey}"/>.
    /// Handles the generation of sample data and the instantiation of the provider.
    /// </summary>
    public struct GpuDataProviderTestData
    {
        #region Fields

        /// <summary>
        /// The keys array used for testing.
        /// </summary>
        public Vector2[] keys;

        /// <summary>
        /// The <see cref="IGpuDataProvider{TKey}"/> instance used for testing.
        /// </summary>
        public IGpuDataProvider<Vector2> provider;

        /// <summary>
        /// A dictionary mapping keys to their expected sample data arrays.
        /// </summary>
        public Dictionary<Type, Array> expected;

        #endregion
    }
}