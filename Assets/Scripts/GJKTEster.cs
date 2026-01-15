using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class GJKTEster : MonoBehaviour
{

    public MA_PhysicShape a;
    public MA_PhysicShape b;

    CollisionPoints m_points;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MathFunctions.GJK(a, b, out m_points);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(m_points.contactPoint, .2f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(m_points.pointA, .2f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(m_points.pointB, .2f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(m_points.contactPoint, m_points.contactPoint + m_points.normal);
    }
}