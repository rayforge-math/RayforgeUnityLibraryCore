using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Represents a mutable spatial collection that allows structural modifications 
    /// and clearing operations on tracked spatial cells.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells (must be an equatable struct).</typeparam>
    public interface ISpatialCollection<TKey> : IReadOnlySpatialCollection<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        #region Mutation & Management

        /// <summary>
        /// Clears all entries and resets all spatial tracking data.
        /// </summary>
        void Clear();

        #endregion
    }
}
