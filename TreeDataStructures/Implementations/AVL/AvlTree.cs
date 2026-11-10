using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue>: BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
{
    public AvlTree() : base() { }

    public AvlTree(IComparer<TKey>? comparer) : base(comparer) { }

    #region Вспомогательные методы

    private static int Height(AvlNode<TKey, TValue>? node)
        => node?.Height ?? 0;

    private static int GetBalanceFactor(AvlNode<TKey, TValue>? node)
    {
        if (node == null)
            return 0;
        return Height(node.Right) - Height(node.Left);
    }

    private AvlNode<TKey, TValue> Balance(AvlNode<TKey, TValue> node)
    {
        UpdateHeight(node);
        int balanceFactor = GetBalanceFactor(node);
        if (balanceFactor > 1)
        {
            AvlNode<TKey, TValue>? right = node.Right;
            if (right != null && GetBalanceFactor(right) < 0)
            {
                RotateDoubleLeft(node);
            }
            else
            {
                RotateLeft(node);
            }
            return node.Parent ?? Root!;
        }
        if (balanceFactor < -1)
        {
            AvlNode<TKey, TValue>? left = node.Left;
            if (left != null && GetBalanceFactor(left) > 0)
            {
                RotateDoubleRight(node);
            }
            else
            {
                RotateRight(node);
            }
            return node.Parent ?? Root!;
        }
        return node;
    }

    #endregion

    #region Переопределение хуков

    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        AvlNode<TKey, TValue>? current = newNode.Parent;
        while (current != null)
        {
            AvlNode<TKey, TValue> subtreeRoot = Balance(current);
            current = subtreeRoot.Parent;
        }
    }

    protected override void OnNodeRemoved(
        AvlNode<TKey, TValue>? parent,
        AvlNode<TKey, TValue>? child)
    {
        AvlNode<TKey, TValue>? current = parent ?? child;
        while (current != null)
        {
            AvlNode<TKey, TValue> subtreeRoot = Balance(current);
            current = subtreeRoot.Parent;
        }
    }
    #endregion
}