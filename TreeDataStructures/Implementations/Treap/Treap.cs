using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue>: BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>{
    protected virtual (
        TreapNode<TKey, TValue>? Left,
        TreapNode<TKey, TValue>? Right
    ) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null)
            return (null, null);
        int cmp = Comparer.Compare(root.Key, key);
        if (cmp <= 0)
        {
            var (leftPart, rightPart) = Split(root.Right, key);
            root.Right = leftPart;
            if (leftPart != null)
                leftPart.Parent = root;
            root.Parent = null;
            UpdateHeight(root);
            if (rightPart != null)
                rightPart.Parent = null;
            return (root, rightPart);
        }
        else
        {
            var (leftPart, rightPart) = Split(root.Left, key);
            root.Left = rightPart;
            if (rightPart != null)
                rightPart.Parent = root;
            root.Parent = null;
            UpdateHeight(root);
            if (leftPart != null)
                leftPart.Parent = null;
            return (leftPart, root);
        }
    }

    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null)
        {
            if (right != null)
                right.Parent = null;

            return right;
        }
        if (right == null)
        {
            left.Parent = null;
            return left;
        }
        if (left.Priority >= right.Priority)
        {
            left.Right = Merge(left.Right, right);
            if (left.Right != null)
                left.Right.Parent = left;
            left.Parent = null;
            UpdateHeight(left);
            return left;
        }
        else
        {
            right.Left = Merge(left, right.Left);
            if (right.Left != null)
                right.Left.Parent = right;
            right.Parent = null;
            UpdateHeight(right);
            return right;
        }
    }

    private (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) SplitLess(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null)
            return (null, null);
        int cmp = Comparer.Compare(root.Key, key);
        if (cmp < 0)
        {
            var (leftPart, rightPart) = SplitLess(root.Right, key);
            root.Right = leftPart;
            if (leftPart != null)
                leftPart.Parent = root;
            root.Parent = null;
            UpdateHeight(root);
            if (rightPart != null)
                rightPart.Parent = null;
            return (root, rightPart);
        }
        else
        {
            var (leftPart, rightPart) = SplitLess(root.Left, key);
            root.Left = rightPart;
            if (rightPart != null)
                rightPart.Parent = root;
            root.Parent = null;
            UpdateHeight(root);
            if (leftPart != null)
                leftPart.Parent = null;
            return (leftPart, root);
        }
    }

    public override void Add(TKey key, TValue value)
    {
        var existing = FindNode(key);
        if (existing != null)
        {
            existing.Value = value;
            return;
        }
        TreapNode<TKey, TValue> newNode = CreateNode(key, value);
        var (left, right) = Split(Root, key);
        var mergedLeft = Merge(left, newNode);
        Root = Merge(mergedLeft, right);
        if (Root != null)
            Root.Parent = null;
        Count++;
        OnNodeAdded(newNode);
    }

    public override bool Remove(TKey key)
    {
        if (Root == null)
            return false;
        var (less, greaterOrEqual) = SplitLess(Root, key);
        var (equal, greater) = Split(greaterOrEqual, key);
        if (equal == null)
        {
            Root = Merge(less, greater);
            if (Root != null)
                Root.Parent = null;
            return false;
        }
        equal.Left = null;
        equal.Right = null;
        equal.Parent = null;
        equal.Height = 1;
        Root = Merge(less, greater);
        if (Root != null)
            Root.Parent = null;
        Count--;
        OnNodeRemoved(
            Root?.Parent,
            Root
        );
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }
}