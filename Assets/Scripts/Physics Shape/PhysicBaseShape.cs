using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void PhysicShapeEvent(MA_PhysicShape _shape);
public enum eShapeType
{
    E_POINTS, 
    E_CUBE, 
    E_SPHERE, 
    E_MESH
}

public delegate void ShapeEvent(MA_PhysicShape _shape);

public class MA_PhysicShape : MonoBehaviour
{
    protected List<Vector3> m_pointsArray = new List<Vector3>();
    protected eShapeType m_shapeType;

    protected AABB p_broadCollider = new AABB();

    public AABB AABB { get { return p_broadCollider; } }

    public static PhysicShapeEvent MA_ColliderAwake;
    public static PhysicShapeEvent MA_ColliderDestroy;

    void Awake()
    {
        UpdateShapeAABB();
        MA_ColliderAwake?.Invoke(this);
    }

    private void OnDestroy()
    {
        MA_ColliderDestroy?.Invoke(this);
    }

    virtual protected void setRightType()
    {
        m_shapeType = eShapeType.E_POINTS;
    }

    virtual public void DrawForm()
    {
        for (int i = 0; i < m_pointsArray.Count; i++)
        {
            if(i == m_pointsArray.Count - 1)
            {
                Debug.DrawLine(m_pointsArray[i], m_pointsArray[0]);
                break;
            }

            Debug.DrawLine(m_pointsArray[i], m_pointsArray[i + 1]);
        }
    }

    public Vector3[] getPointArray()
    {
        List<Vector3> points = new List<Vector3>();

        Matrix4x4 baseMatrix = transform.localToWorldMatrix;

        for (int i = 0; i < m_pointsArray.Count; i++)
        {
            points.Add(baseMatrix.MultiplyPoint3x4(m_pointsArray[i]));
        }

        return points.ToArray();
    }

    virtual public (Vector3, Vector3) getMinMax()
    {
        Vector3[] points = getPointArray();

        (Vector3, Vector3) minMax = (Vector3.zero, Vector3.zero);

        if (points.Count() <= 0)
            return minMax;

        minMax = (points[0], points[0]);

        foreach(Vector3 point in points)
        {
            minMax.Item1 = Vector3.Min(minMax.Item1, point);
            minMax.Item2 = Vector3.Max(minMax.Item2, point); 
        }

        return minMax;
    }

    public eShapeType getShapeType()
    {
        return m_shapeType;
    }

    public virtual void UpdateShapeAABB()
    {
        float baseDiagonal = p_broadCollider.Diagonal;

        (Vector3, Vector3) minMax = getMinMax();

        p_broadCollider.LowerBound = minMax.Item1;
        p_broadCollider.UpperBound = minMax.Item2;

        if (p_broadCollider.Diagonal < baseDiagonal)
            p_broadCollider.ForceUpdate = true;
    }

    public void OnMACollisionEnter(GameObject _collider)
    {
    }

    public void OnMACollisionStay(GameObject _collider)
    {
    }

    public void OnMACollisionExit(GameObject _collider)
    {
    }

    private void Update()
    {
        if(transform.hasChanged)
        {
            UpdateShapeAABB();
        }
    }
}