using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using UnityEngine;

namespace Rayforge.Core
{
    public struct SyncedDirtySegmentAction<T1, T2> : IExecutionHandler<SyncedSegmentMeta<T1, T2>>
    {
        public int CallCount;
        public int TotalLength;

        public void Execute(SyncedSegmentMeta<T1, T2> segment)
        {
            CallCount++;
            TotalLength += segment.TotalSpan;
        }
    }
}
