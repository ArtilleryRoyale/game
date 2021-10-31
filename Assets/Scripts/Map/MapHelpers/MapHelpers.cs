using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;
using Jrmgx.Helpers;
using CC;
using PathBerserker2d;
using System;

public static class MapHelpers
{
    public static void Smoothen(SpriteShapeController sc, int pointIndex)
    {
        var pointCount = sc.spline.GetPointCount();
        Vector3 position = sc.spline.GetPosition(pointIndex);
        Vector3 positionNext = sc.spline.GetPosition(SplineUtility.NextIndex(pointIndex, pointCount));
        Vector3 positionPrev = sc.spline.GetPosition(SplineUtility.PreviousIndex(pointIndex, pointCount));
        Vector3 forward = Vector3.forward;

        float scale = Mathf.Min((positionNext - position).magnitude, (positionPrev - position).magnitude) * 0.33f;

        sc.spline.SetTangentMode(pointIndex, ShapeTangentMode.Broken);
        SplineUtility.CalculateTangents(position, positionPrev, positionNext, forward, scale, out Vector3 rightTangent, out Vector3 leftTangent);

        sc.spline.SetLeftTangent(pointIndex, leftTangent);
        sc.spline.SetRightTangent(pointIndex, rightTangent);
    }

    public static float SmallestX(List<Vector2> points)
    {
        float result = float.MaxValue;
        foreach (var p in points) {
            if (p.x < result) {
                result = p.x;
            }
        }
        return result;
    }

    public static float BiggestX(List<Vector2> points)
    {
        float result = float.MinValue;
        foreach (var p in points) {
            if (p.x > result) {
                result = p.x;
            }
        }
        return result;
    }

    public static float SmallestY(List<Vector2> points)
    {
        float result = float.MaxValue;
        foreach (var p in points) {
            if (p.y < result) {
                result = p.y;
            }
        }
        return result;
    }

    public static float BiggestY(List<Vector2> points)
    {
        float result = float.MinValue;
        foreach (var p in points) {
            if (p.y > result) {
                result = p.y;
            }
        }
        return result;
    }

    public static float SurfacePolygon(List<Vector2> points)
    {
        float temp = 0;
        int i = 0 ;
        for(; i < points.Count ; i++) {
            if (i != points.Count - 1) {
                float mulA = points[i].x * points[i + 1].y;
                float mulB = points[i + 1].x * points[i].y;
                temp = temp + ( mulA - mulB );
            } else {
                float mulA = points[i].x * points[0].y;
                float mulB = points[0].x * points[i].y;
                temp = temp + ( mulA - mulB );
            }
        }
        temp *= 0.5f;
        return Mathf.Abs(temp);
    }

    public static List<Vector2> SimplifyPolygon(Vector2[] points)
    {
        var paths = ClipperLib.Clipper.SimplifyPolygon(Path.GetPath(points));
        if (paths.Count == 0) {
            Log.Critical("MapHelper", "SimplifyPolygon() did not return any path for: " + Debugging.IEnumerableToString(points, inline: true));
            return new List<Vector2>();
        }
        return paths[0].ToListVector2();
    }

    /// <summary>
    /// Return the points of those colliders translated and scaled
    /// </summary>
    public static List<Vector2> PointsFromColliders(List<PolygonCollider2D> colliders)
    {
        var points = new List<Vector2>();
        foreach (PolygonCollider2D collider in colliders) {
            try {
                foreach (var polygon in ColliderConverter.Convert(collider)) {
                    points.AddRange(polygon.Hull.Verts);
                }
            } catch (Exception e) {
                Log.Critical("MapHelper", "Error in PointsFromColliders: " + e);
            }
        }
        return points;
    }
}
