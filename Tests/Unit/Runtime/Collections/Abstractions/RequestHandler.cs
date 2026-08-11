using Rayforge.Core.Execution.Abstractions;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Abstractions
{
    public struct RequestHandler<T> : IExecutionHandler<T>
    {
        public int CallCount;
        public List<T> Elements;

        public void Execute(T element)
        {
            CallCount++;
            if (Elements == null) Elements = new List<T>();
            Elements.Add(element);
        }
    }
}
