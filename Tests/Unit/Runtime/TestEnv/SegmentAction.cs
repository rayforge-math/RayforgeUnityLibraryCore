using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using UnityEngine;

namespace Rayforge.Core.TestEnv
{
    public struct SegmentAction<T> : IExecutionHandler<BufferSegmentMeta<T>> where T : struct
    {
        public int SegmentCount;
        public int TotalSegmentsExpected;

        public void Execute(BufferSegmentMeta<T> segment)
        {
            SegmentCount++;
        }
    }
}
