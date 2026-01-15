using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct CollisionPoints
{
    public Vector3 pointA;
    public Vector3 pointB;
    public Vector3 normal;
    public Vector3 contactPoint;
    public float depth;
    public bool hasCollision;
}

public struct SupportPoint
{
    public Vector3 Point; 
    public int Index1;   
    public int Index2; 

    public SupportPoint(Vector3 _point, int _index1, int _index2)
    {
        Point = _point;
        Index1 = _index1;
        Index2 = _index2;
    }
}

public static class MathFunctions
{

    #region GJK
    public static bool GJK(MA_PhysicShape _shapeA, MA_PhysicShape _shapeB, out CollisionPoints _col)
    {
        _col = new CollisionPoints();

        Simplex simplex = new Simplex();
        Vector3 direction = Vector3.right;

        if (_shapeA.transform.position.y == _shapeB.transform.position.y && _shapeA.transform.position.z == _shapeB.transform.position.z)
            direction = Vector3.up;
        

        SupportPoint support = CalculateSupportPoint(_shapeA, _shapeB, direction);

        simplex.AddSupportPoint(support);

        direction = -support.Point;

        int gjk_cnt = 0;

        while (gjk_cnt < 200) // 200 Hardcoded again ik
        {
            support = CalculateSupportPoint(_shapeA, _shapeB, direction);

            if (Vector3.Dot(support.Point, direction) <= 0)
            {
                return false;
            }

            simplex.AddSupportPoint(support);

            if (simplex.ContainsOrigin(ref direction))
            {
                simplex.DrawSimplex();

                _col.hasCollision = false;

                int epa_cnt = EPA(_shapeA, _shapeB, simplex, ref _col);

                return true;
            }

            gjk_cnt++;
        }
        return false;
    }
    #endregion

    #region EPA
    private static int EPA(MA_PhysicShape _shapeA, MA_PhysicShape _shapeB, Simplex _simplex, ref CollisionPoints _col)
    {
        int epa_cnt = 0;

        List<SupportPoint> polytope = _simplex.Vertices;

        List<int> faces = new List<int>{
            0, 1, 2,
            0, 3, 1,
            0, 2, 3,
            1, 3, 2
        };

        var (normals, minFace) = GetFaceNormals(polytope, faces);

        Vector3 minNormal = V4to3(normals[minFace]);
        float minDistance = float.MaxValue;

        while ((minDistance == float.MaxValue) && (epa_cnt < 200))  //200 hardcoded ik
        {
            if(minFace <= normals.Count)
            {
                minNormal = V4to3(normals[minFace]);
                minDistance = normals[minFace].w;
            }  

            SupportPoint support = CalculateSupportPoint(_shapeA, _shapeB, minNormal);
            float sDistance = Vector3.Dot(minNormal, support.Point);

            if (Math.Abs(sDistance - minDistance) > 0.001f)
            {
                minDistance = float.MaxValue;

                List<(int, int)> uniqueEdges = new List<(int, int)>();

                for (int i = 0; i < normals.Count; i++)
                {
                    if ((Vector3.Dot(V4to3(normals[i]), support.Point) - normals[i].w) > 0)
                    {
                        int f = i * 3;
                        AddIfUniqueEdge(ref uniqueEdges, faces, f, f + 1);
                        AddIfUniqueEdge(ref uniqueEdges, faces, f + 1, f + 2);
                        AddIfUniqueEdge(ref uniqueEdges, faces, f + 2, f);
                        faces[f + 2] = faces.Last<int>(); faces.RemoveAt(faces.Count - 1);
                        faces[f + 1] = faces.Last<int>(); faces.RemoveAt(faces.Count - 1);
                        faces[f] = faces.Last<int>(); faces.RemoveAt(faces.Count - 1);

                        normals[i] = normals.Last<Vector4>(); normals.RemoveAt(normals.Count - 1);

                        i--;
                    }
                }

                List<int> newFaces = new List<int>();
                for (int i = 0; i < uniqueEdges.Count; i++)
                {
                    (int, int) f = uniqueEdges[i];
                    newFaces.Add(f.Item1);
                    newFaces.Add(f.Item2);
                    newFaces.Add(polytope.Count);
                }

                polytope.Add(support);

                var (newNormals, newMinFace) = GetFaceNormals(polytope, newFaces);

                float oldMinDistance = float.MaxValue;
                for (int i = 0; i < normals.Count; i++)
                {
                    if (normals[i].w < oldMinDistance)
                    {
                        oldMinDistance = normals[i].w;
                        minFace = i;
                    }
                }

                if(newNormals.Count > 0)
                {
                    if (newNormals[newMinFace].w < oldMinDistance)
                    {
                        minFace = newMinFace + normals.Count;
                    }

                    faces.AddRange(newFaces);
                    normals.AddRange(newNormals);
                }
            }
            epa_cnt++;
        }

        if (faces.Count <= 0)
            return epa_cnt;
             
        SupportPoint a = polytope[faces[minFace * 3]];
        SupportPoint b = polytope[faces[minFace * 3 + 1]];
        SupportPoint c = polytope[faces[minFace * 3 + 2]];

        float distance = Vector3.Dot(a.Point, minNormal);   
        Vector3 projectedPoint = -distance * minNormal;

        (float u, float v, float w) = GetBarycentricCoordinates(projectedPoint, a.Point, b.Point, c.Point);

        if (_shapeA.getShapeType() == eShapeType.E_SPHERE)
        {
            int index = 0;
            _col.pointA = GetSupportPoint(_shapeA, minNormal, out index);
        }
        else
        {
            Vector3 a1 = _shapeA.getPointArray()[a.Index1];
            Vector3 b1 = _shapeA.getPointArray()[b.Index1];
            Vector3 c1 = _shapeA.getPointArray()[c.Index1];

            _col.pointA = u * a1 + v * b1 + w * c1;
        }

        if (_shapeB.getShapeType() == eShapeType.E_SPHERE)
        {
            int index = 0;
            _col.pointB = GetSupportPoint(_shapeB, -minNormal, out index);
        }
        else
        {
            Vector3 a2 = _shapeB.getPointArray()[a.Index2];
            Vector3 b2 = _shapeB.getPointArray()[b.Index2];
            Vector3 c2 = _shapeB.getPointArray()[c.Index2];


            _col.pointB = u * a2 + v * b2 + w * c2;
        }

        _col.contactPoint = (_col.pointA + _col.pointB) / 2; 
        _col.normal = Vector3.Normalize(minNormal);
        _col.depth = minDistance + 0.001f;
        _col.hasCollision = true;

        return epa_cnt;
    }

    #endregion

    #region UTILITIES
    private static SupportPoint CalculateSupportPoint(MA_PhysicShape __shapeA, MA_PhysicShape __shapeB, Vector3 _direction)
    {
        int index1, index2;
        Vector3 support1 = GetSupportPoint(__shapeA, _direction.normalized, out index1);
        Vector3 support2 = GetSupportPoint(__shapeB, -_direction.normalized, out index2);

        return new SupportPoint(support1 - support2, index1, index2);
    }

    private static Vector3 GetSupportPoint(MA_PhysicShape _shape, Vector3 _direction, out int _index)
    {
        double maxDot = double.MinValue;
        Vector3 support = new Vector3(0, 0, 0);
        _index = -1;

        if (_shape.getShapeType() == eShapeType.E_SPHERE)
        {
            support = _shape.gameObject.transform.position + _direction * ((DebugSphere)_shape).radius;
            return support;
        }

        Vector3[] polyhedron = _shape.getPointArray();

        for (int i = 0; i < polyhedron.Length; i++)
        {
            double dot = Vector3.Dot(polyhedron[i], _direction);
            if (dot > maxDot)
            {
                maxDot = dot;
                support = polyhedron[i];
                _index = i;
            }
        }

        return support;
    }

    private static (List<Vector4>, int) GetFaceNormals(List<SupportPoint> polytope, List<int> faces)
    {
        List<Vector4> normals = new List<Vector4>();
        int minTriangle = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < faces.Count; i += 3)
        {
            Vector3 a = polytope[faces[i]].Point;
            Vector3 b = polytope[faces[i + 1]].Point;
            Vector3 c = polytope[faces[i + 2]].Point;

            Vector3 normal = Vector3.Cross(b - a, c - a);

            float l = normal.magnitude; 

            float distance = float.MaxValue;

            if (l < 0.001f)
            {
                normal = Vector3.zero;
                distance = float.MaxValue;
            }
            else
            {
                normal = normal / l;
                distance = Vector3.Dot(normal, a);
            }

            if (distance < 0)
            {
                normal *= -1;
                distance *= -1;
            }

            normals.Add(new Vector4(normal.x, normal.y, normal.z, distance));

            if (distance < minDistance)
            {
                minTriangle = i / 3;
                minDistance = distance;
            }
        }

        return (normals, minTriangle);
    }

    public static void AddIfUniqueEdge(ref List<(int, int)> edges, List<int> faces, int a, int b)
    {
        (int, int) reverse = (0, 0);
        bool found = false;
        foreach ((int, int) f in edges)
        {
            if ((f.Item1 == faces[b]) && (f.Item2 == faces[a]))
            {
                reverse = f;
                found = true;
                break;
            }
        }

        if (found)
        {
            edges.Remove(reverse);
        }
        else
        {
            edges.Add((faces[a], faces[b]));
        }
    }

    public static Vector3 V4to3(Vector4 v)
    {
        return new Vector3(v.x, v.y, v.z);
    }

    //from : https://github.com/exatb/GJKEPA/blob/main/gjkepa.cs
    public static (float u, float v, float w) GetBarycentricCoordinates(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        // Vectors from vertex A to vertices B and C
        Vector3 v0 = b - a, v1 = c - a, v2 = p - a;

        // Compute dot products
        float d00 = Vector3.Dot(v0, v0); // Same as length squared V0
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1); // Same as length squared V1
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;

        float u = 0;
        float v = 0;
        float w = 0;

        // Check for a zero denominator before division = check for degenerate triangle (area is zero)
        if (Math.Abs(denom) <= 0.001f)
        {
            // The triangle is degenerate

            // Check if all vertices coincide (triangle collapses to a point)
            if (d00 <= 0.001f && d11 <= 0.001f)
            {
                // All edges are degenerate (vertices coincide at a point)
                // Return barycentric coordinates corresponding to vertex a
                u = 1;
                v = 0;
                w = 0;
            }
            else
            {
                // Seems triangle collapses to a line (vertices are colinear)
                // We can check it:
                // Vector3 cross = Vector3.Cross(v0, v1);
                // if (Vector3.Dot(cross, cross) <= Epsilon).... 
                // But if the triangle area is close to zero and the triangle has not colapsed to a point then it has colapsed to a line
                // Use edge AB if it's not degenerate
                if (d00 > 0.001f)
                {
                    // Compute parameter t for projection of point p onto line AB
                    float t = Vector3.Dot(v2, v0) / d00;
                    // if |t|>1 then p lies in AC but we can use u,v,w calculated to AB with a small error    
                    // Barycentric coordinates for edge AB
                    u = 1.0f - t;   // weight for vertex a
                    v = t;          // weight for vertex b
                    w = 0.0f;       // vertex c does not contribute
                }
                // Else, use edge AC 
                else if (d11 > 0.001f)
                {
                    // Compute parameter t for projection of point p onto line AC
                    float t = Vector3.Dot(v2, v1) / d11;
                    // Barycentric coordinates for edge AC
                    u = 1.0f - t; // weight for vertex a
                    v = 0.0f;     // vertex b does not contribute
                    w = t;        // weight for vertex c
                }
                else
                {
                    // The triangle is degenerate in an unexpected way
                    // Return barycentric coordinates corresponding to vertex a                    
                    u = 1;
                    v = 0;
                    w = 0;
                }
            }

        }
        else
        {
            // Compute barycentric coordinates
            v = (d11 * d20 - d01 * d21) / denom;
            w = (d00 * d21 - d01 * d20) / denom;
            u = 1.0f - v - w;
        }

        return (u, v, w);
    }
    #endregion
}
