using System;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer.Internal.Converters;
using UnityEngine;
using UnityEngine.UIElements;

public class Simplex
{
    private List<SupportPoint> points;

    public List<SupportPoint> Vertices
    {
        get { return points; }
    }

    public Simplex()
    {
        points = new List<SupportPoint>();
    }

    public void AddSupportPoint(SupportPoint supportPoint)
    {
        points.Insert(0, supportPoint);
    }

    public bool ContainsOrigin(ref Vector3 direction)
    {
        if (points.Count == 4)
        {
            return ContainsOriginTetrahedron(ref direction);
        }
        else if (points.Count == 3)
        {
            return ContainsOriginTriangle(ref direction);
        }
        else if (points.Count == 2)
        {
            return ContainsOriginLine(ref direction);
        }

        return false;
    }

    public void DrawSimplex()
    {
        if (points.Count > 2) 
        {
            Debug.DrawLine(points[0].Point, points[1].Point);
            Debug.DrawLine(points[0].Point, points[2].Point);
            Debug.DrawLine(points[0].Point, points[3].Point);

            Debug.DrawLine(points[1].Point, points[2].Point);
            Debug.DrawLine(points[1].Point, points[3].Point);

            Debug.DrawLine(points[2].Point, points[3].Point);
        }
    }

    private bool ContainsOriginTetrahedron(ref Vector3 direction)
    {
        SupportPoint a = points[0];
        SupportPoint b = points[1];
        SupportPoint c = points[2];
        SupportPoint d = points[3];

        Vector3 ab = b.Point - a.Point;
        Vector3 ac = c.Point - a.Point;
        Vector3 ad = d.Point - a.Point;
        Vector3 ao = -a.Point;

        Vector3 abc = Vector3.Cross(ab, ac);
        Vector3 acd = Vector3.Cross(ac, ad);
        Vector3 adb = Vector3.Cross(ad, ab);


       

        if (Vector3.Dot(abc, ao) > 0)
        {
            points.Clear();
            points.Add(a);
            points.Add(b);
            points.Add(c);
            //return Triangle(points = { a, b, c }, direction);
            return ContainsOriginTriangle(ref direction);
        }

        if (Vector3.Dot(acd, ao) > 0)
        {
            points.Clear();
            points.Add(a);
            points.Add(c);
            points.Add(d);
            //return Triangle(points = { a, c, d }, direction);
            return ContainsOriginTriangle(ref direction);
        }

        if (Vector3.Dot(adb, ao) > 0)
        {
            points.Clear();
            points.Add(a);
            points.Add(d);
            points.Add(b);
            //return Triangle(points = { a, d, b }, direction);
            return ContainsOriginTriangle(ref direction);
        }

        return true;
    }

    private bool ContainsOriginTriangle(ref Vector3 direction)
    {
        //The order of the points is such that a is always the last point added.
        SupportPoint a = points[0];
        SupportPoint b = points[1];
        SupportPoint c = points[2];

        Vector3 ab = b.Point - a.Point;
        Vector3 ac = c.Point - a.Point;
        Vector3 ao = -a.Point;

        Vector3 abc = Vector3.Cross(ab, ac);

        if (Vector3.Dot(Vector3.Cross(abc, ac), ao) > 0)
        {
            if (Vector3.Dot(ac, ao) > 0)
            {
                points.Clear();
                points.Add(a);
                points.Add(c);
                //points = { a, c };
                direction = Vector3.Cross(Vector3.Cross(ac, ao), ac);
            }
            else
            {
                points.Clear();
                points.Add(a);
                points.Add(b);
                //points = { a, b };
                return ContainsOriginLine(ref direction);
            }
        }

        else
        {
            if (Vector3.Dot(Vector3.Cross(ab, abc), ao) > 0)
            {
                points.Clear();
                points.Add(a);
                points.Add(b);
                //points = { a, b }
                return ContainsOriginLine(ref direction);
            }

            else
            {
                if (Vector3.Dot(abc, ao) > 0)
                {
                    direction = abc;
                }

                else
                {
                    points.Clear();
                    points.Add(a);
                    points.Add(c);
                    points.Add(b);
                    //points = { a, c, b };
                    direction = -abc;
                }
            }
        }

        return false;
    }

    private bool ContainsOriginLine(ref Vector3 direction)
    {
        // The order of points is such that 'a' is always the newly added point
        SupportPoint a = points[0];
        SupportPoint b = points[1];

        Vector3 ab = b.Point - a.Point;

        Vector3 ao = -a.Point;

        if (Vector3.Dot(ab, ao) > 0)
            // Construct a perpendicular to the line in the direction of the origin
            direction = Vector3.Cross(Vector3.Cross(ab, ao), ab);
        else
        {
            points.Clear();
            points.Add(a);

            direction = ao;
        }

        return false;
    }

}
