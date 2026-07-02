using UnityEngine;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Holds information about the synchronization of two bit sources at a specific index.
    /// </summary>
    public struct SyncedBitIteratorMeta
    {
        public int Index;
        public bool BitA;
        public bool BitB;

        public SyncedBitIteratorMeta(int index, bool bitA, bool bitB)
        {
            Index = index;
            BitA = bitA;
            BitB = bitB;
        }
    }
}
