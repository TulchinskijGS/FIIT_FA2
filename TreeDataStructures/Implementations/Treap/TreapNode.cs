using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class TreapNode<TKey, TValue> : Node<TKey, TValue, TreapNode<TKey, TValue>>
{
    public int Priority { get; set; } = Random.Shared.Next();

    public TreapNode(TKey key, TValue value) : base(key, value) { }

    public void RecalculateHeight()
    {
        int leftHeight = Left?.Height ?? -1;
        int rightHeight = Right?.Height ?? -1;
        Height = Math.Max(leftHeight, rightHeight) + 1;
    }
}