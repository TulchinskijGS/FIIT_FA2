using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlNode<TKey, TValue> : Node<TKey, TValue, AvlNode<TKey, TValue>>
{

    public AvlNode(TKey key, TValue value) : base(key, value) { }
}