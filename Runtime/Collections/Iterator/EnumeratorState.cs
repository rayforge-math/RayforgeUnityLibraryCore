using Rayforge.Core.Collections.Abstractions;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// Wraps a standard HashSet enumerator to make it compatible with the universal Iterator.
    /// </summary>
    public struct EnumeratorState<T, TEnumerator> : IIterationLogic<T, EnumeratorState<T, TEnumerator>>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;

        public EnumeratorState(TEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public bool MoveNext(ref EnumeratorState<T, TEnumerator> self, out T result)
        {
            if (self._enumerator.MoveNext())
            {
                result = self._enumerator.Current;
                return true;
            }

            result = default;
            return false;
        }
    }
}