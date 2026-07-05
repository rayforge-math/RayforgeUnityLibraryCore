using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;

namespace Rayforge.Core.TestEnv
{
    public struct DirtyAction<T> : IExecutionHandler<BufferSegmentMeta<T>> where T : struct
    {
        public int CallCount;
        public int TotalLength;

        public void Execute(BufferSegmentMeta<T> segment)
        {
            CallCount++;
            TotalLength += segment.Count;
        }
    }
}
