using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
{
    public SplayTree() : base() { }

    public SplayTree(IComparer<TKey>? comparer) : base(comparer) { }

    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent != null)
        {
            var parent = node.Parent;
            var grandParent = parent.Parent;
            if (grandParent == null)
            {
                if (node == parent.Left)
                    RotateRight(parent);
                else
                    RotateLeft(parent);
            }
            else if (node == parent.Left && parent == grandParent.Left)
            {
                RotateRight(grandParent);
                RotateRight(parent);
            }
            else if (node == parent.Right && parent == grandParent.Right)
            {
                RotateLeft(grandParent);
                RotateLeft(parent);
            }
            else if (node == parent.Right && parent == grandParent.Left)
            {
                RotateLeft(parent);
                RotateRight(grandParent);
            }
            else
            {
                RotateRight(parent);
                RotateLeft(grandParent);
            }
        }
        Root = node;
    }

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }

    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent != null)
        {
            Splay(parent);
        }
        else if (child != null)
        {
            Splay(child);
        }
    }

    public override bool ContainsKey(TKey key)
    {
        var node = FindNode(key);
        if (node == null)
            return false;
        Splay(node);
        return true;
    }

    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = FindNode(key);
        if (node == null)
        {
            value = default;
            return false;
        }
        Splay(node);
        value = node.Value;
        return true;
    }
}