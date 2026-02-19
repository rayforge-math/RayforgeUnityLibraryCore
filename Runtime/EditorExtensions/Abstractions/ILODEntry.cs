using Rayforge.Core.Common.Rendering;

namespace Rayforge.Core.EditorExtensions.Abstractions
{
    /// <summary>
    /// Defines a type-safe contract for LOD entries.
    /// Uses <typeparamref name="TSelf"/> to ensure logical comparisons occur 
    /// between identical data structures.
    /// </summary>
    /// <typeparam name="TSelf">The implementing type itself.</typeparam>
    public interface ILodEntry<TSelf>
    {
        /// <summary>
        /// The maximum distance for this LOD. 
        /// Managed by the <see cref="LodTable{TEntry}"/>.
        /// </summary>
        float DistanceThreshold { get; set; }

        /// <summary>
        /// Checks if this entry represents a valid quality reduction 
        /// compared to the <paramref name="predecessor"/>.
        /// </summary>
        bool IsLogicalSuccessor(TSelf predecessor);

        /// <summary>
        /// Adjusts this entry's internal data to ensure it is a valid 
        /// successor to the <paramref name="predecessor"/>.
        /// </summary>
        void MakeValidSuccessor(TSelf predecessor);
    }
}