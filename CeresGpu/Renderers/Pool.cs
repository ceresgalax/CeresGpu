using System;
using System.Collections.Generic;

namespace Metalancer.Renderers
{
    public sealed class Pool<T> : IDisposable
    {
        public interface IFactory
        {
            T Make();
            void DisposeOf(T item);
        }

        private readonly IFactory _factory;
        private readonly List<T> _resources = new();
        private int _nextFree;
        
        public Pool(IFactory factory)
        {
            _factory = factory;
        }

        public T Get()
        {
            T item;
            if (_nextFree >= _resources.Count) {
                item = _factory.Make();
                _resources.Add(item);
            } else {
                item = _resources[_nextFree];
            }
            ++_nextFree;
            return item;
        }

        public void Reset()
        {
            _nextFree = 0;
        }

        public void Dispose()
        {
            foreach (T t in _resources) {
                _factory.DisposeOf(t);
            }
            _resources.Clear();
        }
    }
}