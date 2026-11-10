using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
{
    public RedBlackTree() : base() { }

    public RedBlackTree(IComparer<TKey>? comparer) : base(comparer) { }

    #region Вспомогательные методы

    private static RbColor ColorOf(RbNode<TKey, TValue>? node)
        => node == null ? RbColor.Black : node.Color;

    private static bool IsRed(RbNode<TKey, TValue>? node)
        => node != null && node.Color == RbColor.Red;

    private static bool IsBlack(RbNode<TKey, TValue>? node)
        => ColorOf(node) == RbColor.Black;

    private RbNode<TKey, TValue>? Minimum(RbNode<TKey, TValue> node)
    {
        while (node.Left != null)
            node = node.Left;
        return node;
    }

    #endregion

    #region Балансировка вставки

    private void FixInsert(RbNode<TKey, TValue> node)
    {
        while (node != Root && IsRed(node.Parent))
        {
            var parent = node.Parent!;
            var grandParent = parent.Parent!;

            if (parent == grandParent.Left)
            {
                var uncle = grandParent.Right;
                if (IsRed(uncle))
                {
                    parent.Color = RbColor.Black;
                    uncle!.Color = RbColor.Black;
                    grandParent.Color = RbColor.Red;
                    node = grandParent;
                }
                else
                {
                    if (node == parent.Right)
                    {
                        node = parent;
                        RotateLeft(node);
                        parent = node.Parent!;
                        grandParent = parent.Parent!;
                    }
                    parent.Color = RbColor.Black;
                    grandParent.Color = RbColor.Red;
                    RotateRight(grandParent);
                }
            }
            else
            {
                var uncle = grandParent.Left;
                if (IsRed(uncle))
                {
                    parent.Color = RbColor.Black;
                    uncle!.Color = RbColor.Black;
                    grandParent.Color = RbColor.Red;
                    node = grandParent;
                }
                else
                {
                    if (node == parent.Left)
                    {
                        node = parent;
                        RotateRight(node);
                        parent = node.Parent!;
                        grandParent = parent.Parent!;
                    }
                    parent.Color = RbColor.Black;
                    grandParent.Color = RbColor.Red;
                    RotateLeft(grandParent);
                }
            }
        }
        Root!.Color = RbColor.Black;
    }

    #endregion

    #region Балансировка удаления

    private void FixDelete(
        RbNode<TKey, TValue>? x,
        RbNode<TKey, TValue>? parent) {
        while (x != Root && IsBlack(x))
        {
            if (parent == null)
                break;
            if (x == parent.Left) 
            {
                RbNode<TKey, TValue>? sibling = parent.Right;
                if (IsRed(sibling)) {
                    sibling!.Color = RbColor.Black;
                    parent.Color = RbColor.Red;
                    RotateLeft(parent);
                    sibling = parent.Right;
                }
                if (sibling == null) 
                {
                    x = parent;
                    parent = x.Parent;
                    continue;
                }
                if (IsBlack(sibling.Left) && IsBlack(sibling.Right)) 
                {
                    sibling.Color = RbColor.Red;
                    x = parent;
                    parent = x.Parent;
                }
                else 
                {
                    if (IsBlack(sibling.Right)) 
                    {
                        if (sibling.Left != null)
                            sibling.Left.Color = RbColor.Black;
                        sibling.Color = RbColor.Red;
                        RotateRight(sibling);
                        sibling = parent.Right;
                    }
                    if (sibling != null) 
                    {
                        sibling.Color = parent.Color;
                        if (sibling.Right != null)
                            sibling.Right.Color = RbColor.Black;
                    }
                    parent.Color = RbColor.Black;
                    RotateLeft(parent);
                    x = Root;
                    parent = null;
                }
            }
            else 
            {
                RbNode<TKey, TValue>? sibling = parent.Left;
                if (IsRed(sibling)) 
                {
                    sibling!.Color = RbColor.Black;
                    parent.Color = RbColor.Red;
                    RotateRight(parent);
                    sibling = parent.Left;
                }
                if (sibling == null) 
                {
                    x = parent;
                    parent = x.Parent;
                    continue;
                }
                if (IsBlack(sibling.Left) && IsBlack(sibling.Right)) 
                {
                    sibling.Color = RbColor.Red;

                    x = parent;
                    parent = x.Parent;
                }
                else 
                {
                    if (IsBlack(sibling.Left)) 
                    {
                        if (sibling.Right != null)
                            sibling.Right.Color = RbColor.Black;
                        sibling.Color = RbColor.Red;
                        RotateLeft(sibling);
                        sibling = parent.Left;
                    }
                    if (sibling != null) 
                    {
                        sibling.Color = parent.Color;
                        if (sibling.Left != null)
                            sibling.Left.Color = RbColor.Black;
                    }
                    parent.Color = RbColor.Black;
                    RotateRight(parent);
                    x = Root;
                    parent = null;
                }
            }
        }
        if (x != null)
            x.Color = RbColor.Black;
    }

    #endregion

    #region Переопределение шаблонных методов

    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        FixInsert(newNode);
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child) {}

    protected override void RemoveNode(RbNode<TKey, TValue> node)
    {
        var y = node;
        var yOriginalColor = y.Color;
        RbNode<TKey, TValue>? x;
        if (node.Left == null)
        {
            x = node.Right;
            Transplant(node, node.Right);
        }
        else if (node.Right == null)
        {
            x = node.Left;
            Transplant(node, node.Left);
        }
        else
        {
            y = Minimum(node.Right)!;
            yOriginalColor = y.Color;
            x = y.Right;
            if (y.Parent == node)
            {
                if (x != null)
                    x.Parent = y;
            }
            else
            {
                Transplant(y, y.Right);
                y.Right = node.Right;
                y.Right!.Parent = y;
            }
            Transplant(node, y);
            y.Left = node.Left;
            y.Left!.Parent = y;
            y.Color = node.Color;
        }
        if (yOriginalColor == RbColor.Black)
            FixDelete(x, x?.Parent ?? y.Parent);
    }

    #endregion
}