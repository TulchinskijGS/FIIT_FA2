using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys
    {
        get
        {
            var keys = new List<TKey>();
            foreach (var entry in InOrder())
                keys.Add(entry.Key);
            return keys;
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            var values = new List<TValue>();
            foreach (var entry in InOrder())
                values.Add(entry.Value);
            return values;
        }
    }
    
    public virtual void Add(TKey key, TValue value)
    {
        if (Root == null)
        {
            Root = CreateNode(key, value);
            Root.Parent = null;
            Root.Height = 1;
            Count++;
            OnNodeAdded(Root);
            return;
        }
        TNode current = Root;
        TNode? parent = null;
        int cmp = 0;
        while (true)
        {
            parent = current;
            cmp = Comparer.Compare(key, current.Key);
            if (cmp > 0)
            {
                if (current.Right == null)
                    break;

                current = current.Right;
            }
            else if (cmp < 0)
            {
                if (current.Left == null)
                    break;
                current = current.Left;
            }
            else
            {
                current.Value = value;
                return;
            }
        }
        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;
        newNode.Left = null;
        newNode.Right = null;
        newNode.Height = 1;
        if (cmp < 0)
            parent.Left = newNode;
        else
            parent.Right = newNode;
        Count++;
        UpdateHeightsUpwards(parent);
        OnNodeAdded(newNode);
    }

    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }
        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    protected virtual void RemoveNode(TNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (node.Left == null)
        {
            TNode? parent = node.Parent;
            TNode? replacement = node.Right;
            Transplant(node, replacement);
            if (replacement != null)
                UpdateHeightsUpwards(replacement);
            else
                UpdateHeightsUpwards(parent);
            node.Left = null;
            node.Right = null;
            node.Parent = null;
            node.Height = 1;
            OnNodeRemoved(parent, replacement);
            return;
        }
        if (node.Right == null)
        {
            TNode? parent = node.Parent;
            TNode? replacement = node.Left;
            Transplant(node, replacement);
            if (replacement != null)
                UpdateHeightsUpwards(replacement);
            else
                UpdateHeightsUpwards(parent);
            node.Left = null;
            node.Right = null;
            node.Parent = null;
            node.Height = 1;
            OnNodeRemoved(parent, replacement);
            return;
        }
        TNode successor = node.Right;
        while (successor.Left != null)
            successor = successor.Left;
        TNode? oldSuccessorParent = successor.Parent;
        if (successor.Parent != node)
        {
            Transplant(successor, successor.Right);
            successor.Right = node.Right;
            successor.Right.Parent = successor;
        }
        Transplant(node, successor);
        successor.Left = node.Left;
        successor.Left.Parent = successor;
        UpdateHeight(successor);
        if (oldSuccessorParent != null && oldSuccessorParent != node)
            UpdateHeightsUpwards(oldSuccessorParent);
        UpdateHeightsUpwards(successor);
        TNode? parentForHook = successor.Parent;
        node.Left = null;
        node.Right = null;
        node.Parent = null;
        node.Height = 1;
        OnNodeRemoved(parentForHook, successor);
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }

    protected static int GetHeight(TNode? node)
    {
        return node?.Height ?? 0; 
    } 

    protected static void UpdateHeight(TNode node) 
    { 
        node.Height = 1 + Math.Max( GetHeight(node.Left), GetHeight(node.Right));
    }

    protected static void UpdateHeightsUpwards(TNode? node)
    {
        TNode? current = node;

        while (current != null)
        {
            UpdateHeight(current);
            current = current.Parent;
        }
    }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

     protected void RotateLeft(TNode x)
    {
        if (x == null)
            throw new ArgumentNullException(nameof(x));
        TNode? y = x.Right;
        if (y == null)
            throw new InvalidOperationException(
                "Cannot rotate left without right child.");
        TNode? parent = x.Parent;
        x.Right = y.Left;
        if (y.Left != null)
            y.Left.Parent = x;
        y.Parent = parent;
        if (parent == null)
        {
            Root = y;
        }
        else if (x.IsLeftChild)
        {
            parent.Left = y;
        }
        else
        {
            parent.Right = y;
        }
        y.Left = x;
        x.Parent = y;
        UpdateHeight(x);
        UpdateHeight(y);
    }

    protected void RotateRight(TNode y)
    {
        if (y == null)
            throw new ArgumentNullException(nameof(y));
        TNode? x = y.Left;
        if (x == null)
            throw new InvalidOperationException(
                "Cannot rotate right without left child.");
        TNode? parent = y.Parent;
        y.Left = x.Right;
        if (x.Right != null)
            x.Right.Parent = y;
        x.Parent = parent;
        if (parent == null)
        {
            Root = x;
        }
        else if (y.IsLeftChild)
        {
            parent.Left = x;
        }
        else
        {
            parent.Right = x;
        }
        x.Right = y;
        y.Parent = x;
        UpdateHeight(y);
        UpdateHeight(x);
    }

    protected void RotateDoubleLeft(TNode x)
    {
        if (x.Right == null)
            throw new InvalidOperationException("Cannot perform double left rotation without right child.");
        RotateRight(x.Right);
        RotateLeft(x);
    }

    protected void RotateDoubleRight(TNode y)
    {
        if (y.Left == null)
            throw new InvalidOperationException("Cannot perform double right rotation without left child.");
        RotateLeft(y.Left);
        RotateRight(y);
    }

    protected void RotateBigLeft(TNode x)
    {
        if (x.Right?.Left == null)
            RotateLeft(x);
        else
            RotateDoubleLeft(x);
    }

    protected void RotateBigRight(TNode y)
    {
        if (y.Left?.Right == null)
            RotateRight(y);
        else
            RotateDoubleRight(y);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null) {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }

    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder() => 
        new TreeIterator(Root, TraversalStrategy.InOrder);
    

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() =>
        new TreeIterator(Root, TraversalStrategy.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() =>
        new TreeIterator(Root, TraversalStrategy.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() =>
        new TreeIterator(Root, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() =>
        new TreeIterator(Root, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() =>
        new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy;
        private bool _started = false;
        private TNode? _currentNode; 
        private bool _disposed;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _currentNode = null;
            _disposed = false;
            Reset();
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        public TreeEntry<TKey, TValue> Current
        {
            get
            {
                if (_currentNode == null)
                    throw new InvalidOperationException("Enumeration not started or has finished.");
                return new TreeEntry<TKey, TValue>(
                    _currentNode.Key,
                    _currentNode.Value,
                    _currentNode.Height);
            }
        }

        object IEnumerator.Current => Current;
        
        public bool MoveNext()
        {
            return _strategy switch
            {
                TraversalStrategy.PreOrder =>
                    MoveNextPreOrder(),

                TraversalStrategy.InOrder =>
                    MoveNextInOrder(),

                TraversalStrategy.PostOrder =>
                    MoveNextPostOrder(),

                TraversalStrategy.PreOrderReverse =>
                    MoveNextPreOrderReverse(),

                TraversalStrategy.InOrderReverse =>
                    MoveNextInOrderReverse(),

                TraversalStrategy.PostOrderReverse =>
                    MoveNextPostOrderReverse(),

                _ => throw new NotSupportedException(
                    $"Strategy {_strategy} is not supported.")
            };
        }

        private bool MoveNextPreOrder()
        {
            if (!_started)
            {
                _started = true;
                if (_root == null)
                    return false;
                _currentNode = _root;
                return true;
            }
            if (_currentNode == null)
                return false;
            if (_currentNode.Left != null)
            {
                _currentNode = _currentNode.Left;
                return true;
            }
            if (_currentNode.Right != null)
            {
                _currentNode = _currentNode.Right;
                return true;
            }
            TNode node = _currentNode;
            while (node.Parent != null)
            {
                TNode parent = node.Parent;
                if (node == parent.Left && parent.Right != null)
                {
                    _currentNode = parent.Right;
                    return true;
                }
                node = parent;
            }
            _currentNode = null;
            return false;
        }

        private bool MoveNextInOrder()
        {
            if (!_started)
            {
                _started = true;
                if (_root == null)
                    return false;
                _currentNode = _root;
                while (_currentNode.Left != null)
                    _currentNode = _currentNode.Left;
                return true;
            }
            if (_currentNode == null)
                return false;
            if (_currentNode.Right != null)
            {
                _currentNode = _currentNode.Right;
                while (_currentNode.Left != null)
                    _currentNode = _currentNode.Left;
                return true;
            }
            TNode node = _currentNode;
            while (node.Parent != null && node == node.Parent.Right)
                node = node.Parent;
            _currentNode = node.Parent;
            return _currentNode != null;
        }

        private bool MoveNextPostOrder()
        {
            if (!_started)
            {
                _started = true;
                if (_root == null)
                    return false;
                _currentNode = GetFirstPostOrderNode(_root);
                return true;
            }
            if (_currentNode == null)
                return false;
            TNode node = _currentNode;
            if (node.Parent == null)
            {
                _currentNode = null;
                return false;
            }
            TNode parent = node.Parent;
            if (node == parent.Left && parent.Right != null)
            {
                _currentNode = GetFirstPostOrderNode(parent.Right);
                return true;
            }
            _currentNode = parent;
            return true;
        }

        private TNode GetFirstPostOrderNode(TNode node)
        {
            while (true)
            {
                if (node.Left != null)
                {
                    node = node.Left;
                }
                else if (node.Right != null)
                {
                    node = node.Right;
                }
                else
                {
                    return node;
                }
            }
        }

        private bool MoveNextPreOrderReverse()
        {
            if (!_started)
            {
                _started = true;
                if (_root == null)
                    return false;
                _currentNode = _root;
                return true;
            }
            if (_currentNode == null)
                return false;
            if (_currentNode.Left != null)
            {
                _currentNode = _currentNode.Left;
                return true;
            }
            if (_currentNode.Right != null)
            {
                _currentNode = _currentNode.Right;
                return true;
            }
            TNode node = _currentNode;
            while (node.Parent != null)
            {
                TNode parent = node.Parent;
                if (node == parent.Left && parent.Right != null)
                {
                    _currentNode = parent.Right;
                    return true;
                }
                node = parent;
            }
            _currentNode = null;
            return false;
        }

        private bool MoveNextInOrderReverse()
        {
            if (!_started)
            {
                _started = true;
                if (_root == null)
                    return false;
                _currentNode = _root;
                while (_currentNode.Right != null)
                    _currentNode = _currentNode.Right;
                return true;
            }
            if (_currentNode == null)
                return false;
            if (_currentNode.Left != null)
            {
                _currentNode = _currentNode.Left;
                while (_currentNode.Right != null)
                    _currentNode = _currentNode.Right;
                return true;
            }
            TNode node = _currentNode;
            while (node.Parent != null && node == node.Parent.Left)
                node = node.Parent;
            _currentNode = node.Parent;
            return _currentNode != null;
        }

        private bool MoveNextPostOrderReverse()
        {
            if (!_started)
            {
                _started = true;
                if (_root == null)
                    return false;
                _currentNode = GetFirstPostOrderReverseNode(_root);
                return true;
            }
            if (_currentNode == null)
                return false;
            TNode node = _currentNode;
            if (node.Parent == null)
            {
                _currentNode = null;
                return false;
            }
            TNode parent = node.Parent;
            if (node == parent.Right && parent.Left != null)
            {
                _currentNode = GetFirstPostOrderReverseNode(parent.Left);
                return true;
            }
            _currentNode = parent;
            return true;
        }

        private TNode GetFirstPostOrderReverseNode(TNode node)
        {
            while (true)
            {
                if (node.Right != null)
                {
                    node = node.Right;
                }
                else if (node.Left != null)
                {
                    node = node.Left;
                }
                else
                {
                    return node;
                }
            }
        }

        public void Reset()
        {
            _started = false;
            _currentNode = null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _started = false;
                _currentNode = null;
                _disposed = true;
            }
        }
    }
        
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        var list = new List<KeyValuePair<TKey,TValue>>();
        foreach (var entry in InOrder())
            list.Add(new KeyValuePair<TKey,TValue>(
                entry.Key,
                entry.Value));
        return list.GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (array.Length - arrayIndex < Count) 
            throw new ArgumentException("Array is too small.");

        foreach (var entry in InOrder())
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}