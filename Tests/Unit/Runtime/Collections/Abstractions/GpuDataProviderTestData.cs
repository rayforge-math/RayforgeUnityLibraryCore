using Rayforge.Core.TestEnv;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Abstractions.Tests
{
    /// <summary>
    /// Base class for providing test data environments for <see cref="IMetadataProvider{TKey}"/>.
    /// Handles the generation of sample data and the instantiation of the provider.
    /// </summary>
    public abstract class GpuDataProviderTestData
    {
        #region Fields

        /// <summary>
        /// The <see cref="IMetadataProvider{TKey}"/> instance used for testing.
        /// </summary>
        public IGpuDataProvider<int> Registry { get; private set; }

        /// <summary>
        /// A dictionary mapping keys to their expected sample data arrays.
        /// </summary>
        public Dictionary<int, int[]> Expected { get; private set; } = new Dictionary<int, int[]>();

        #endregion

        /// <summary>
        /// Creates a provider instance populated with the provided test data.
        /// </summary>
        /// <param name="data">The dictionary containing keys and their associated data arrays.</param>
        /// <returns>A configured <see cref="IMetadataProvider{TKey}"/> instance.</returns>
        public abstract IGpuDataProvider<int> CreateProvider(Dictionary<int, int[]> data);

        #region Constructor & Init

        /// <summary>
        /// Initializes a new instance of the <see cref="GpuDataProviderTestData"/> class.
        /// </summary>
        public GpuDataProviderTestData() { }

        /// <summary>
        /// Initializes the test environment by generating sample data and creating the registry.
        /// </summary>
        /// <param name="storeCount">The number of stores or items to generate.</param>
        /// <param name="length">The number of elements per sample array.</param>
        public void Initialize(int storeCount, int length)
        {
            for (int i = 0; i < storeCount; ++i)
            {
                Expected[i * 10] = TestUtility.CreateSampleItems<int>(length);
            }

            Registry = CreateProvider(Expected);
        }

        #endregion
    }
}