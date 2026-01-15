using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PhysicShape_Mesh : MA_PhysicShape
{
    override protected void setRightType()
    {
        m_shapeType = eShapeType.E_MESH;
    }

    private void Awake()
    {
        setRightType();

        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        foreach (Vector3 v in vertices)
            m_pointsArray.Add(v);

        UpdateShapeAABB();
        MA_ColliderAwake?.Invoke(this);
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            UpdateShapeAABB();
        }

        drawForm();
    }

    public void drawForm()
    {
        Vector3[] points = getPointArray();

        for (int i = 0; i < points.Count(); i += 2) 
        {
            if (i > points.Count() - 3)
                break;

            Debug.DrawLine(points[i], points[i + 1]);
            Debug.DrawLine(points[i], points[i + 2]);
            Debug.DrawLine(points[i + 1], points[i + 2]);
        }
    }
}
