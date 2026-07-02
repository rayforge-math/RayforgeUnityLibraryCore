using UnityEngine;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Holds information about the synchronization of two bit sources at a specific index.
    /// </summary>
    public readonly struct SyncedBitMeta
    {
        public readonly int Index;
        public readonly bool BitA;
        public readonly bool BitB;
        public readonly int ValueA;
        public readonly int ValueB;

        public SyncedBitMeta(int index, bool bitA, bool bitB, int valA, int valB)
        {
            Index = index;
            BitA = bitA;
            BitB = bitB;
            ValueA = valA;
            ValueB = valB;
        }
    }
}
