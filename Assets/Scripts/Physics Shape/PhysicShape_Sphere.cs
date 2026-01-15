using UnityEngine;

public class DebugSphere : MA_PhysicShape
{
    public float radius;

    private void Awake()
    {
        setRightType();
        UpdateShapeAABB();
        MA_ColliderAwake?.Invoke(this);
    }

    protected override void setRightType()
    {
        m_shapeType = eShapeType.E_SPHERE;
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            UpdateShapeAABB();
        }
    }

    public override void UpdateShapeAABB()
    {
        float baseDiagonal = p_broadCollider.Diagonal;

        p_broadCollider.LowerBound = transform.position - new Vector3(radius, radius, radius);
        p_broadCollider.UpperBound = transform.position + new Vector3(radius, radius, radius);

        if (p_broadCollider.Diagonal < baseDiagonal)
            p_broadCollider.ForceUpdate = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
