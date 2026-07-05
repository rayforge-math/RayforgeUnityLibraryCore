using Rayforge.Core.Execution.Abstractions;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.TestEnv
{
    public struct IndexAction : IExecutionHandler<int>
    {
        public int CallCount;
        public List<int> Indices;

        public void Execute(int element)
        {
            CallCount++;
            if (Indices == null) Indices = new List<int>();
            Indices.Add(element);
        }
    }
}
