using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekat2
{
    class CacheAsync//ne koristim ovo jer nema poboljsanja u performansama a komplikuje kod 
    {
        private readonly Dictionary<string, LinkedListNode<string>> _cache;
        private readonly LinkedList<string> _currentList;
        private readonly ReaderWriterLockSlim _lock;
        private readonly int _maxSize;

        public CacheAsync(int maxSize = 32)
        {
            _cache = new Dictionary<string, LinkedListNode<string>>();
            _currentList = new LinkedList<string>();
            _lock = new ReaderWriterLockSlim();
            _maxSize = maxSize;
        }

        public async Task AddAsync(string key)
        {
            await Task.Run(() =>
            {
                _lock.EnterWriteLock();
                try
                {
                    if (_cache.ContainsKey(key))
                    {
                        _currentList.Remove(_cache[key]);
                    }
                    else if (_cache.Count >= _maxSize)
                    {
                        string toRemove = _currentList.Last.Value;
                        _currentList.RemoveLast();
                        _cache.Remove(toRemove);
                    }

                    LinkedListNode<string> newNode = _currentList.AddFirst(key);
                    _cache[key] = newNode;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            });
        }

        public async Task<bool> ContainsAsync(string key)
        {
            return await Task.Run(() =>
            {
                _lock.EnterUpgradeableReadLock();
                try
                {
                    if (_cache.TryGetValue(key, out LinkedListNode<string> node))
                    {
                        _lock.EnterWriteLock();
                        try
                        {
                            _currentList.Remove(node);
                            _currentList.AddFirst(node);
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                        return true;
                    }

                    return false;
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                }
            });
        }

        public async Task ClearAsync()
        {
            await Task.Run(() =>
            {
                _lock.EnterWriteLock();
                try
                {
                    _cache.Clear();
                    _currentList.Clear();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            });
        }
    }
}
