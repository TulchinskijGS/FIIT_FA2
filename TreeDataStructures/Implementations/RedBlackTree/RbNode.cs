using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public enum RbColor : byte
{
    Red = 0,
    Black = 1
}

public class RbNode<TKey, TValue> : Node<TKey, TValue, RbNode<TKey, TValue>>
{
    public RbColor Color { get; set; } = RbColor.Red;

    public RbNode(TKey key, TValue value) : base(key, value) { }

    public void RecalculateHeight() { }
}