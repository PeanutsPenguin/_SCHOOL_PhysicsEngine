using UnityEngine;

public class AABB
{
    private Vector3 m_LowerBound = new Vector3(-0.5f, -0.5f, -0.5f);
    private Vector3 m_UpperBound = new Vector3(0.5f, 0.5f, 0.5f);

    private bool m_ForceUpdate = false;

    public Vector3 LowerBound { get { return m_LowerBound; } set { m_LowerBound = value; } }
    public Vector3 UpperBound { get { return m_UpperBound; } set { m_UpperBound = value; } }
    public Vector3 Position { get { return (m_LowerBound + m_UpperBound) * 0.5f; } }

    public float Diagonal { get { return (m_UpperBound - m_LowerBound).magnitude; } }

    public bool ForceUpdate { get { return m_ForceUpdate; } set { m_ForceUpdate = value; } }

    public AABB() {}

    public AABB(Vector3 _globalLowerBound, Vector3 _globalUpperBound)
    {
        m_LowerBound = _globalLowerBound;
        m_UpperBound = _globalUpperBound;
    }

    public static AABB Union(AABB _a, AABB _b)
    {
        AABB c = new AABB();
        c.m_LowerBound = Vector3.Min(_a.m_LowerBound, _b.m_LowerBound);
        c.m_UpperBound = Vector3.Max(_a.m_UpperBound, _b.m_UpperBound);
        return c;
    }

    public static float Area(AABB _a)
    {
        Vector3 distance = _a.m_UpperBound - _a.m_LowerBound;
        return 2f * (distance.x * distance.y + distance.y * distance.z + distance.z * distance.x);
    }

    public bool Contains(AABB _other)
    {
        return
            m_LowerBound.x <= _other.m_LowerBound.x &&
            m_UpperBound.x >= _other.m_UpperBound.x &&
            m_LowerBound.y <= _other.m_LowerBound.y &&
            m_UpperBound.y >= _other.m_UpperBound.y &&
            m_LowerBound.z <= _other.m_LowerBound.z &&
            m_UpperBound.z >= _other.m_UpperBound.z;
    }

    public bool Collides(AABB _other)
    {
        return
            m_LowerBound.x <= _other.m_UpperBound.x &&
            m_UpperBound.x >= _other.m_LowerBound.x &&
            m_LowerBound.y <= _other.m_UpperBound.y &&
            m_UpperBound.y >= _other.m_LowerBound.y &&
            m_LowerBound.z <= _other.m_UpperBound.z &&
            m_UpperBound.z >= _other.m_LowerBound.z;
    }

    public bool Collides(Vector3 _point)
    {
        return
            m_LowerBound.x <= _point.x && _point.x <= m_UpperBound.x &&
            m_LowerBound.y <= _point.y && _point.y <= m_UpperBound.y &&
            m_LowerBound.z <= _point.z && _point.z <= m_UpperBound.z;
    }

    public static void DrawAABB(AABB _aabb, Color _color)
    {
        Vector3 lb = _aabb.LowerBound;
        Vector3 ub = _aabb.UpperBound;

        Debug.DrawLine(lb, new Vector3(ub.x, lb.y, lb.z), _color);
        Debug.DrawLine(lb, new Vector3(lb.x, ub.y, lb.z), _color);
        Debug.DrawLine(lb, new Vector3(lb.x, lb.y, ub.z), _color);

        Debug.DrawLine(new Vector3(lb.x, ub.y, lb.z), new Vector3(lb.x, ub.y, ub.z), _color);
        Debug.DrawLine(new Vector3(lb.x, ub.y, lb.z), new Vector3(ub.x, ub.y, lb.z), _color);

        Debug.DrawLine(ub, new Vector3(lb.x, ub.y, ub.z), _color);
        Debug.DrawLine(ub, new Vector3(ub.x, lb.y, ub.z), _color);
        Debug.DrawLine(ub, new Vector3(ub.x, ub.y, lb.z), _color);

        Debug.DrawLine(new Vector3(ub.x, lb.y, ub.z), new Vector3(ub.x, lb.y, lb.z), _color);
        Debug.DrawLine(new Vector3(ub.x, lb.y, ub.z), new Vector3(lb.x, lb.y, ub.z), _color);

        Debug.DrawLine(new Vector3(ub.x, lb.y, lb.z), new Vector3(ub.x, ub.y, lb.z), _color);
        Debug.DrawLine(new Vector3(lb.x, ub.y, ub.z), new Vector3(lb.x, lb.y, ub.z), _color);
    }
}
