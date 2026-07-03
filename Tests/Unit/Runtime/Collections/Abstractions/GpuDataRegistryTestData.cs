using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.Collections.Buffering;
using System.Collections.Generic;

namespace Rayforge.Core
{
    public class GpuDataRegistryTestData : GpuDataProviderTestData
    {
        #region MetadataProviderTestData Impl

        /// <summary>
        /// Creates and populates a <see cref="GpuDataRegistry{T}"/> instance with the provided data.
        /// </summary>
        /// <param name="data">A dictionary where the key is the index and the value is an array of metadata values to register.</param>
        /// <returns>A configured <see cref="GpuDataRegistry{T}"/> instance.</returns>
        public override IGpuDataProvider<int> CreateProvider(Dictionary<int, int[]> data)
        {
            var registry = new GpuDataRegistry<int>(data.Count, 1);

            foreach (var entry in data)
            {
                int key = entry.Key;
                int[] values = entry.Value;

                foreach (var value in values)
                {
                    registry.Set(key, value);
                }
            }

            return registry;
        }

        #endregion
    }
}
