using System;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class Node
{
    #region Variables
    public AABB m_BoundingBox = new AABB();
    public AABB m_LargeBoundingBox = new AABB();

    private Node m_Parent;

    public Node m_LeftChild;
    public Node m_RightChild;
    public bool m_ChildrenCrossed;

    public Node Parent { get { return m_Parent; } set { m_Parent = value; } }
    public Node LeftChild { get { return m_LeftChild; } set { m_LeftChild = value; } }
    public Node RightChild { get { return m_RightChild; } set { m_RightChild = value; } }
    public bool IsLeaf { get { return (m_RightChild == null && m_LeftChild == null); } }
    public AABB AABB { get { return m_BoundingBox; } set { m_BoundingBox = AABB; } }
    public AABB LargeAABB { get { return m_LargeBoundingBox; } }
    public Node Sibling { get { return this == m_Parent.m_LeftChild ? m_Parent.RightChild : m_Parent.m_LeftChild; } }
    public bool ChildrenCrossed { get { return m_ChildrenCrossed; } set { m_ChildrenCrossed = value; } }

    /// <summary>
    /// Default node constructor
    /// </summary>
    public Node() { }

    /// <summary>
    /// Node copy constructor
    /// </summary>
    /// <param name="_o">: Node to copy</param>
    public Node(Node _o)
    {
        m_BoundingBox = _o.m_BoundingBox;
        m_LargeBoundingBox = _o.m_LargeBoundingBox;
        m_Parent = _o.m_Parent;
        m_LeftChild = _o.m_LeftChild;
        m_RightChild = _o.m_RightChild;
    }

    /// <summary>
    /// Set a node as a branch
    /// </summary>
    /// <param name="_firstNode">: First child of the node</param>
    /// <param name="_secondNode">: Second child of the node</param>
    public void SetBranch(Node _firstNode, Node _secondNode)
    {
        m_BoundingBox = null;

        _firstNode.m_Parent = this;
        _secondNode.m_Parent = this;

        m_LeftChild = _firstNode;
        m_RightChild = _secondNode;
    }

    /// <summary>
    /// Set a node as a leaf
    /// </summary>
    public void SetLeaf(AABB _aabb)
    {
        m_BoundingBox = _aabb;

        m_LeftChild = null;
        m_RightChild = null;
    }

    /// <summary>
    /// Update the large AABB of each nodes / leafs
    /// </summary>
    /// <param name="_margin">: </param>
    public void UpdateAABB(float _margin)
    {
        if (IsLeaf)
        {
            Vector3 margin = new Vector3(_margin, _margin, _margin);

            Vector3 lb = m_BoundingBox.LowerBound;
            Vector3 ub = m_BoundingBox.UpperBound;

            m_LargeBoundingBox = new AABB(lb - margin, ub + margin);
        }
        else
        {
            m_LargeBoundingBox = AABB.Union(m_LeftChild.LargeAABB, m_RightChild.LargeAABB);
        }
    }

    #endregion
}

public class Tree
{
    #region Variables
    private Node m_Root;
    private List<Node> m_InvalidNodes = new List<Node>();

    private float m_Margin = 0.5f;

    // 1 = based on volume // 0 = based on distance
    private float m_A = 0.5f;

    public Dictionary<AABB, MA_PhysicShape> AABBShapes = new Dictionary<AABB, MA_PhysicShape>();

    public Node Root { get { return m_Root; } }

    private bool m_IsColliderUpdateRecursive = false;
    private bool m_IsUpdateRecursive = false;

    #endregion

    #region Methods
    public void Add(MA_PhysicShape _shape)
    {
        AABBShapes.Add(_shape.AABB, _shape);

        if (m_Root != null)
        {
            Node node = new Node();
            node.SetLeaf(_shape.AABB);
            node.UpdateAABB(m_Margin);
            InsertNode(node);
        }
        else
        {
            m_Root = new Node();
            m_Root.SetLeaf(_shape.AABB);
            m_Root.UpdateAABB(m_Margin);
        }
    }

    public void Remove(MA_PhysicShape _shape)
    {
        AABBShapes.Remove(_shape.AABB);
        RemoveNode(SearchNode(_shape.AABB));
    }

    private void RemoveNode(Node _node)
    {
        Node parent = _node.Parent;

        if (parent != null)
        {
            Node sibling = _node.Sibling;
            if (parent.Parent != null)
            {
                sibling.Parent = parent.Parent;

                if (parent.Parent != null)
                {
                    if (parent == parent.Parent.LeftChild)
                        parent.Parent.LeftChild = sibling;
                    else
                        parent.Parent.RightChild = sibling;

                    sibling.Parent = parent.Parent;
                }
            }
            else
            {
                m_Root = sibling;
                sibling.Parent = null;
            }
        }
        else
        {
            m_Root = null;
            _node = null;
        }
    }

    public Node SearchNode(AABB _nodeValue)
    {
        return Search(m_Root, _nodeValue);
    }

    private Node Search(Node _node, AABB _nodeValue)
    {
        if (_node == null)
            return null;

        if (_node.AABB == _nodeValue)
            return _node;

        Node resultNode = null;

        if (_node.RightChild != null)
        {
            resultNode = Search(_node.RightChild, _nodeValue);

            if (resultNode != null)
                return resultNode;
        }

        if (_node.LeftChild != null)
        {
            resultNode = Search(_node.LeftChild, _nodeValue);

            if (resultNode != null)
                return resultNode;
        }

        return null;
    }

    private void InsertNode(Node _newNode)
    {
        Node bestSibling = PickBestSiblingIterative(_newNode);

        Node bestParent = bestSibling.Parent;

        Node newBranch = new Node();

        if (bestParent == null)
        {
            m_Root = newBranch;
        }
        else
        {
            if (bestParent.LeftChild == bestSibling)
            {
                bestParent.LeftChild = newBranch;
            }
            else
            {
                bestParent.RightChild = newBranch;
            }
        }

        newBranch.Parent = bestParent;
        newBranch.SetBranch(bestSibling, _newNode);

        bestSibling.UpdateAABB(m_Margin);
        _newNode.UpdateAABB(m_Margin);

        UpdateParentIterative(_newNode);
    }

    #region Draw Methods

    public void DrawTree()
    {
        DrawTree(m_Root);
    }

    private void DrawTree(Node _node)
    {
        if (!_node.IsLeaf)
        {
            AABB.DrawAABB(_node.LargeAABB, Color.yellow);
        }
        else if (_node.IsLeaf)
        {
            AABB.DrawAABB(_node.LargeAABB, Color.cyan);
            AABB.DrawAABB(_node.AABB, Color.green);
        }

        if (_node.RightChild != null)
            DrawTree(_node.RightChild);

        if (_node.LeftChild != null)
            DrawTree(_node.LeftChild);
    }
    #endregion

    #region Update Methods

    public void UpdateTree()
    {
        if (m_Root == null)
            return;

        if (m_Root.IsLeaf)
        {
            m_Root.UpdateAABB(m_Margin);
        }
        else
        {
            m_InvalidNodes.Clear();

            if (m_IsUpdateRecursive)
                UpdateNodeHelper(m_Root, m_InvalidNodes);
            else
                UpdateNodeIterative(m_InvalidNodes);

            foreach (Node invalidNode in m_InvalidNodes)
            {
                Node node = invalidNode;
                Node parent = node.Parent;
                Node sibling = node.Sibling;

                if (parent.Parent != null)
                {
                    if (parent == parent.Parent.LeftChild)
                        parent.Parent.LeftChild = sibling;
                    else
                        parent.Parent.RightChild = sibling;

                    sibling.Parent = parent.Parent;
                }
                else
                {
                    m_Root = sibling;
                    sibling.Parent = null;
                }

                parent.LeftChild = null;
                parent.RightChild = null;
                parent.Parent = null;

                node.UpdateAABB(m_Margin);
                InsertNode(node);

                if (m_IsUpdateRecursive)
                    UpdateParent(node);
                else
                    UpdateParentIterative(node);
            }
        }

        m_InvalidNodes.Clear();
    }

    #region Iterative Methods

    private void UpdateNodeIterative(List<Node> _invalidNodes)
    {
        Stack<Node> nodeStack = new Stack<Node>();

        nodeStack.Push(m_Root);

        while (nodeStack.Count != 0)
        {
            Node currentNode = nodeStack.Pop();

            if (currentNode.IsLeaf)
            {
                if (!currentNode.LargeAABB.Contains(currentNode.AABB) || currentNode.AABB.ForceUpdate)
                {
                    m_InvalidNodes.Add(currentNode);
                    currentNode.AABB.ForceUpdate = false;
                }
            }
            else
            {
                if (currentNode.LeftChild != null)
                    nodeStack.Push(currentNode.LeftChild);

                if (currentNode.RightChild != null)
                    nodeStack.Push(currentNode.RightChild);
            }
        }
    }

    private void UpdateParentIterative(Node _parentNode)
    {
        Stack<Node> nodeStack = new Stack<Node>();

        nodeStack.Push(_parentNode);

        while (nodeStack.Count != 0)
        {
            Node currentNode = nodeStack.Pop();

            TryTreeRotation(currentNode);
            currentNode.UpdateAABB(m_Margin);

            if (currentNode.Parent == null)
                return;

            nodeStack.Push(currentNode.Parent);
        }
    }

    private Node PickBestSiblingIterative(Node _newNode)
    {
        Stack<Node> nodeStack = new Stack<Node>();
        nodeStack.Push(m_Root);

        float bestCost = float.MaxValue;
        Node bestNode = null;

        while (nodeStack.Count != 0)
        {
            Node currentNode = nodeStack.Pop();

            float cost = AABB.Area(AABB.Union(currentNode.LargeAABB, _newNode.LargeAABB));

            Node parent = currentNode.Parent;

            while (parent != null)
            {
                cost += (AABB.Area(AABB.Union(parent.LargeAABB, _newNode.LargeAABB)) - AABB.Area(parent.LargeAABB));
                parent = parent.Parent;
            }

            cost = m_A * cost + (1f - m_A) * (currentNode.LargeAABB.Position - _newNode.LargeAABB.Position).magnitude;

            if (cost < bestCost)
            {
                bestCost = cost;
                bestNode = currentNode;
            }

            if (currentNode.IsLeaf)
            {
                continue;
            }

            float leftCost = float.MaxValue;
            float rightCost = float.MaxValue;

            if (currentNode.LeftChild != null)
                leftCost = AABB.Area(AABB.Union(currentNode.LeftChild.LargeAABB, _newNode.LargeAABB));

            if (currentNode.RightChild != null)
                rightCost = AABB.Area(AABB.Union(currentNode.RightChild.LargeAABB, _newNode.LargeAABB));

            if (leftCost < bestCost)
                nodeStack.Push(currentNode.LeftChild);
            if (rightCost < bestCost)
                nodeStack.Push(currentNode.RightChild);
        }

        return bestNode;
    }

    #endregion

    #region Recursive Methods

    private void UpdateNodeHelper(Node _node, List<Node> _invalidNodes)
    {
        if (_node.IsLeaf)
        {
            if (!_node.LargeAABB.Contains(_node.AABB) || _node.AABB.ForceUpdate)
            {
                m_InvalidNodes.Add(_node);
                _node.AABB.ForceUpdate = false;
            }
        }
        else
        {
            UpdateNodeHelper(_node.RightChild, _invalidNodes);
            UpdateNodeHelper(_node.LeftChild, _invalidNodes);
        }
    }

    private void UpdateParent(Node _parentNode)
    {
        TryTreeRotation(_parentNode);
        _parentNode.UpdateAABB(m_Margin);

        if (_parentNode.Parent == null)
            return;
        else
            UpdateParent(_parentNode.Parent);
    }

    private Node PickBestSibling(Node _currentNode)
    {
        return new Node();
    }

    #endregion

    #endregion

    #region Pairs Computing Methods
    public Stack<(AABB, AABB)> ComputePairs()
    {
        Stack<(AABB, AABB)> colliderPairsStack = new Stack<(AABB, AABB)>();

        if (m_Root == null || m_Root.IsLeaf)
            return colliderPairsStack;

        if (m_IsColliderUpdateRecursive)
        {
            ClearChildrenCrossFlag(m_Root);
            ComputePairsHelper(m_Root.LeftChild, m_Root.RightChild, ref colliderPairsStack);
        }
        else
        {
            ClearChildrenCrossFlagIterative();
            ComputePairsIterative(ref colliderPairsStack);
        }

        return colliderPairsStack;
    }

    #region Iteratives Methods
    private void ComputePairsIterative(ref Stack<(AABB, AABB)> _colliderPairsStack)
    {
        Stack<(Node, Node)> nodeStack = new Stack<(Node, Node)>();

        nodeStack.Push((m_Root.LeftChild, m_Root.RightChild));

        while (nodeStack.Count != 0)
        {
            var (a, b) = nodeStack.Pop();

            if (a.IsLeaf)
            {
                if (b.IsLeaf)
                {
                    if (a.AABB.Collides(b.AABB))
                    {
                        _colliderPairsStack.Push((a.AABB, b.AABB));
                    }
                }
                else
                {
                    if (!b.ChildrenCrossed)
                    {
                        b.ChildrenCrossed = true;
                        nodeStack.Push((b.LeftChild, b.RightChild));
                    }

                    nodeStack.Push((a, b.LeftChild));
                    nodeStack.Push((a, b.RightChild));
                }
            }
            else
            {
                if (b.IsLeaf)
                {
                    if (!a.ChildrenCrossed)
                    {
                        a.ChildrenCrossed = true;
                        nodeStack.Push((a.LeftChild, a.RightChild));
                    }

                    nodeStack.Push((a.LeftChild, b));
                    nodeStack.Push((a.RightChild, b));
                }
                else
                {
                    if (!a.ChildrenCrossed)
                    {
                        a.ChildrenCrossed = true;
                        nodeStack.Push((a.LeftChild, a.RightChild));
                    }

                    if (!b.ChildrenCrossed)
                    {
                        b.ChildrenCrossed = true;
                        nodeStack.Push((b.LeftChild, b.RightChild));
                    }

                    nodeStack.Push((a.LeftChild, b.LeftChild));
                    nodeStack.Push((a.LeftChild, b.RightChild));
                    nodeStack.Push((a.RightChild, b.LeftChild));
                    nodeStack.Push((a.RightChild, b.RightChild));
                }
            }
        }
    }

    private void ClearChildrenCrossFlagIterative()
    {
        Stack<Node> nodeStack = new Stack<Node>();

        nodeStack.Push(m_Root);

        while (nodeStack.Count != 0)
        {
            Node currentNode = nodeStack.Pop();

            currentNode.ChildrenCrossed = false;

            if (!currentNode.IsLeaf)
            {
                if (currentNode.LeftChild != null)
                    nodeStack.Push(currentNode.LeftChild);

                if (currentNode.RightChild != null)
                    nodeStack.Push(currentNode.RightChild);
            }
        }
    }
    #endregion

    #region Recursive Methods
    private void ComputePairsHelper(Node _firstNode, Node _secondNode, ref Stack<(AABB, AABB)> _colliderPairsStack)
    {
        if (_firstNode.IsLeaf)
        {
            if (_secondNode.IsLeaf)
            {
                if (_firstNode.AABB.Collides(_secondNode.AABB))
                {
                    _colliderPairsStack.Push((_firstNode.AABB, _secondNode.AABB));

                }
            }
            else
            {
                CrossChildren(_secondNode, ref _colliderPairsStack);
                ComputePairsHelper(_firstNode, _secondNode.LeftChild, ref _colliderPairsStack);
                ComputePairsHelper(_firstNode, _secondNode.RightChild, ref _colliderPairsStack);
            }
        }
        else
        {
            if (_secondNode.IsLeaf)
            {
                CrossChildren(_firstNode, ref _colliderPairsStack);
                ComputePairsHelper(_firstNode.LeftChild, _secondNode, ref _colliderPairsStack);
                ComputePairsHelper(_firstNode.RightChild, _secondNode, ref _colliderPairsStack);
            }
            else
            {
                CrossChildren(_firstNode, ref _colliderPairsStack);
                CrossChildren(_secondNode, ref _colliderPairsStack);
                ComputePairsHelper(_firstNode.LeftChild, _secondNode.LeftChild, ref _colliderPairsStack);
                ComputePairsHelper(_firstNode.LeftChild, _secondNode.RightChild, ref _colliderPairsStack);
                ComputePairsHelper(_firstNode.RightChild, _secondNode.LeftChild, ref _colliderPairsStack);
                ComputePairsHelper(_firstNode.RightChild, _secondNode.RightChild, ref _colliderPairsStack);
            }
        }
    }

    private void CrossChildren(Node _node, ref Stack<(AABB, AABB)> _colliderPairsStack)
    {
        if (_node.ChildrenCrossed)
            return;

        ComputePairsHelper(_node.LeftChild, _node.RightChild, ref _colliderPairsStack);

        _node.ChildrenCrossed = true;
    }

    private void ClearChildrenCrossFlag(Node _node)
    {
        _node.ChildrenCrossed = false;

        if (!_node.IsLeaf)
        {
            ClearChildrenCrossFlag(_node.LeftChild);
            ClearChildrenCrossFlag(_node.RightChild);
        }
    }
    #endregion

    #region Tree rotation

    enum ETreeRotationType
    {
        ROTATION_NONE,
        ROTATION_BF,
        ROTATION_BG,
        ROTATION_CD,
        ROTATION_CE
    }

    void TryTreeRotation(Node _rootNode)
    {
        if (_rootNode == null)
            return;

        Node B = _rootNode.LeftChild;
        Node C = _rootNode.RightChild;

        if (B == null || C == null)
            return;

        if (B.IsLeaf)
        {
            if (C.IsLeaf)
                return;

            Node F = C.LeftChild;
            Node G = C.RightChild;

            if (F == null || G == null)
                return;

            float baseCost = AABB.Area(C.LargeAABB);

            AABB BG = AABB.Union(B.LargeAABB, G.LargeAABB);
            float costBG = AABB.Area(BG);

            AABB BF = AABB.Union(B.LargeAABB, F.LargeAABB);
            float costBF = AABB.Area(BF);

            if (baseCost < costBF && baseCost < costBF)
                return;

            if (costBF < costBG)
            {
                _rootNode.SetBranch(C, F);
                C.SetBranch(B, G);

                C.UpdateAABB(m_Margin);
                _rootNode.UpdateAABB(m_Margin);
            }
            else
            {
                _rootNode.SetBranch(G, C);
                C.SetBranch(F, B);

                C.UpdateAABB(m_Margin);
                _rootNode.UpdateAABB(m_Margin);
            }
        }
        else if (C.IsLeaf)
        {
            if (B.IsLeaf)
                return;

            Node D = B.LeftChild;
            Node E = B.RightChild;

            if (D == null || E == null)
                return;

            float baseCost = AABB.Area(B.LargeAABB);

            AABB CE = AABB.Union(C.LargeAABB, E.LargeAABB);
            float costCE = AABB.Area(CE);

            AABB CD = AABB.Union(C.LargeAABB, D.LargeAABB);
            float costCD = AABB.Area(CD);

            if (baseCost < costCD && baseCost < costCE)
                return;

            if (costCD < costCE)
            {
                _rootNode.SetBranch(D, B);
                B.SetBranch(C, E);
                
                B.UpdateAABB(m_Margin);
                _rootNode.UpdateAABB(m_Margin);
            }
            else
            {
                _rootNode.SetBranch(E, B);
                B.SetBranch(C, D);

                B.UpdateAABB(m_Margin);
                _rootNode.UpdateAABB(m_Margin);
            }
        }
        else
        {
            Node D = B.LeftChild;
            Node E = B.RightChild;
            Node F = C.LeftChild;
            Node G = C.RightChild;

            if (D == null || E == null || F == null || G == null)
                return;

            float areaB = AABB.Area(B.LargeAABB);
            float areaC = AABB.Area(C.LargeAABB);

            float baseCost = areaB + areaC;
            ETreeRotationType bestRotation = ETreeRotationType.ROTATION_NONE;
            float bestCost = baseCost;

            float costBF = areaB + AABB.Area(AABB.Union(B.LargeAABB, G.LargeAABB));
            if (costBF < bestCost)
            {
                bestRotation = ETreeRotationType.ROTATION_BF;
                bestCost = costBF;
            }

            float costBG = areaB + AABB.Area(AABB.Union(B.LargeAABB, F.LargeAABB));
            if (costBG < bestCost)
            {
                bestRotation = ETreeRotationType.ROTATION_BG;
                bestCost = costBG;
            }

            float costCD = areaB + AABB.Area(AABB.Union(C.LargeAABB, E.LargeAABB));
            if (costCD < bestCost)
            {
                bestRotation = ETreeRotationType.ROTATION_CD;
                bestCost = costCD;
            }

            float costCE = areaB + AABB.Area(AABB.Union(C.LargeAABB, D.LargeAABB));
            if (costCE < bestCost)
            {
                bestRotation = ETreeRotationType.ROTATION_CE;
                bestCost = costCE;
            }

            switch (bestRotation)
            {
                case ETreeRotationType.ROTATION_BF:
                    _rootNode.SetBranch(C, F);
                    C.SetBranch(B, G);

                    C.UpdateAABB(m_Margin);
                    _rootNode.UpdateAABB(m_Margin);
                    break;
                case ETreeRotationType.ROTATION_BG:
                    _rootNode.SetBranch(G, C);
                    C.SetBranch(F, B);

                    C.UpdateAABB(m_Margin);
                    _rootNode.UpdateAABB(m_Margin);
                    break;
                case ETreeRotationType.ROTATION_CD:
                    _rootNode.SetBranch(D, B);
                    B.SetBranch(C, E);

                    B.UpdateAABB(m_Margin);
                    _rootNode.UpdateAABB(m_Margin);
                    break;
                case ETreeRotationType.ROTATION_CE:
                    _rootNode.SetBranch(E, B);
                    B.SetBranch(C, D);

                    B.UpdateAABB(m_Margin);
                    _rootNode.UpdateAABB(m_Margin);
                    break;
                case ETreeRotationType.ROTATION_NONE:
                default:
                    return;
            }
        }
    }
    #endregion
    #endregion
    #endregion
}
