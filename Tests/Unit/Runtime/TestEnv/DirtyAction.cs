using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.TestEnv
{
    public struct DirtyAction<T> : IExecutionHandler<BufferSegmentMeta<T>>
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
