using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using UnityEngine;

namespace Rayforge.Core
{
    public struct SyncedSegmentAction<T1, T2> : IExecutionHandler<SyncedSegmentMeta<T1, T2>>
    {
        public int SegmentCount;
        public int TotalSegmentsExpected;

        public void Execute(SyncedSegmentMeta<T1, T2> segment)
        {
            SegmentCount++;
        }
    }
}
