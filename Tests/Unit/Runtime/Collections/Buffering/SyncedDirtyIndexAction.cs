using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System.Collections.Generic;

namespace Rayforge.Core
{
    public struct SyncedDirtyIndexAction<T1, T2> : IExecutionHandler<SyncedBitIteratorMeta>
    {
        public int CallCount;
        public Dictionary<int, bool[]> IndexList;

        public void Execute(SyncedBitIteratorMeta meta)
        {
            CallCount++;
            IndexList[meta.Index] = new bool[] { meta.BitA, meta.BitB };
        }
    }
}
