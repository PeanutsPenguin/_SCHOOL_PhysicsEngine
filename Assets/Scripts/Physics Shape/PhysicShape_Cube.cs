using UnityEngine;

public class DebugCube : MA_PhysicShape
{
    public float m_width;
    public float m_height;
    public float m_depth;

    private void Awake()
    {
        setRightType();
        UpdatePosition();
        UpdateShapeAABB();
        MA_ColliderAwake?.Invoke(this);
    }

    protected override void setRightType()
    {
        m_shapeType = eShapeType.E_CUBE;
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            UpdateShapeAABB();
        }
    }

    private void UpdatePosition()
    {
        m_pointsArray.Clear();

        //Right Face
        //Bottom
        m_pointsArray.Add(new Vector3(m_width, -m_height, m_depth));
        m_pointsArray.Add(new Vector3(m_width, -m_height, -m_depth));
        //Top
        m_pointsArray.Add(new Vector3(m_width, m_height, -m_depth));
        m_pointsArray.Add(new Vector3(m_width, m_height, m_depth));

        //Left Face
        //Bottom
        m_pointsArray.Add(new Vector3(-m_width, -m_height, m_depth));
        m_pointsArray.Add(new Vector3(-m_width, -m_height, -m_depth));
        //Top
        m_pointsArray.Add(new Vector3(-m_width, m_height, -m_depth));
        m_pointsArray.Add(new Vector3(-m_width, m_height, m_depth));
    }
}
