using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// State that bundles multiple Iterators and treats them as a single stream.
    /// Compatible with the universal Iterator struct.
    /// </summary>
    public struct MultiCompositeState<T> : IIterationLogic<T, MultiCompositeState<T>>
    {
        private readonly IIterator<T>[] _sources;
        private int _index;

        public MultiCompositeState(params IIterator<T>[] sources)
        {
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            _index = 0;
        }

        public bool MoveNext(ref MultiCompositeState<T> self, out T result)
        {
            while (self._index < self._sources.Length)
            {
                if (self._sources[self._index].MoveNext())
                {
                    result = self._sources[self._index].Current;
                    return true;
                }

                self._sources[self._index].Dispose();
                self._index++;
            }

            result = default;
            return false;
        }
    }
}