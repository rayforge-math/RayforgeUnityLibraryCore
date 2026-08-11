using Rayforge.Core.Execution.Abstractions;
using UnityEngine;

namespace Rayforge.Core.TestEnv
{
    public struct TestAction<T> : IExecutionHandler<T> 
        where T : struct
    {
        public int CallCount;
        public double Sum;

        public void Execute(T element)
        {
            CallCount++;

            if (element is int i) Sum += i;
            else if (element is float f) Sum += f;
            else if (element is double d) Sum += d;
            else if (element is long l) Sum += l;
            else if (element is Vector2 v2) Sum += v2.magnitude;
            else if (element is Vector3 v3) Sum += v3.magnitude;
            else if (element is Vector4 v4) Sum += v4.magnitude;
        }
    }
}
