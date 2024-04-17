using System.Collections;
using System.Collections.Generic;

namespace CustomCollections
{
    // https://gmamaladze.wordpress.com/2013/07/25/hashset-that-preserves-insertion-order-or-net-implementation-of-linkedhashset/
    public class OrderedSet<T> : ICollection<T>
    {
        private readonly IDictionary<T, LinkedListNode<T>> _mDictionary;
        private readonly LinkedList<T> _mLinkedList;
     
        public OrderedSet()
            : this(EqualityComparer<T>.Default)
        {
        }
     
        public OrderedSet(IEqualityComparer<T> comparer)
        {
            _mDictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
            _mLinkedList = new LinkedList<T>();
        }
     
        public int Count => _mDictionary.Count;

        public virtual bool IsReadOnly => _mDictionary.IsReadOnly;

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }
     
        public void Clear()
        {
            _mLinkedList.Clear();
            _mDictionary.Clear();
        }
     
        public bool Remove(T item)
        {
            if (item == null) return false;
            var found = _mDictionary.TryGetValue(item, out var node);
            if (!found) return false;
            _mDictionary.Remove(item);
            _mLinkedList.Remove(node);
            return true;
        }
     
        public IEnumerator<T> GetEnumerator()
        {
            return _mLinkedList.GetEnumerator();
        }
     
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
     
        public bool Contains(T item)
        {
            return item != null && _mDictionary.ContainsKey(item);
        }
     
        public void CopyTo(T[] array, int arrayIndex)
        {
            _mLinkedList.CopyTo(array, arrayIndex);
        }
        
        public bool Add(T item)
        {
            if (_mDictionary.ContainsKey(item)) return false;
            var node = _mLinkedList.AddLast(item);
            _mDictionary.Add(item, node);
            return true;
        }
        
        public void UnionWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                Add(item);
            }
        }
        
        public T GetFirst()
        {
            return _mLinkedList.Count == 0 ? default : _mLinkedList.First.Value;
        }
        
        public void RemoveFirst()
        {
            if (_mLinkedList.Count == 0) return;
            _mDictionary.Remove(_mLinkedList.First.Value);
            _mLinkedList.RemoveFirst();
        } 
    }
}