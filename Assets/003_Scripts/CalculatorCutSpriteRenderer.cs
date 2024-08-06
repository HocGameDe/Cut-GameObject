using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CalculatorCutSpriteRenderer
{
    //LineEquation: Ax+By+C=0
    // A = y2-y1
    // B = x1-x2
    // C = x2*y1 - x1*y2
    public static Vector3 LinearEquations(Vector3 point1, Vector3 point2)
    {
        Vector3 result = new Vector3();
        result.x = point2.y - point1.y;
        result.y = point1.x - point2.x;
        result.z = point2.x * point1.y - point1.x * point2.y;
        return result;
    }
    public static bool IsValueOnLeftLine(Vector2 pointHead, Vector2 pointTail, Vector2 point)
    {
        Vector3 linearEquation = LinearEquations(pointHead, pointTail);
        return (linearEquation.x * point.x + linearEquation.y * point.y + linearEquation.z) > 0;
    }
    public static Vector2 GetIntersectionPointBetweenTwoLines(Vector3 point1Line1, Vector3 point2Line1, Vector3 point1Line2, Vector3 point2Line2)
    {
        //coefficient Line
        Vector3 cLine1 = LinearEquations(point1Line1, point2Line1);
        Vector3 cLine2 = LinearEquations(point1Line2, point2Line2);
        Vector2 result = new Vector2();
        result.y = (cLine2.x * cLine1.z - cLine1.x * cLine2.z) / (cLine2.y * cLine1.x - cLine2.x * cLine1.y);
        result.x = -cLine1.z / cLine1.x - cLine1.y * result.y / cLine1.x;
        return result;
    }
    
    public static bool IsNotClockwise(List<Vector2> points)
    {
        Vector2 p1;
        Vector2 p2;
        float sumS;
        sumS = 0;
        for (int i = 0; i < points.Count; i++)
        {
            p1 = points[i];
            p2 = points[(i + 1) % points.Count];
            sumS += (p2.x - p1.x) * (p2.y + p1.y);
        }
        return sumS < 0;
    }
    public static bool IsAnyPointsInTriangle(Vector2 A, Vector2 B, Vector2 C, List<Vector2> points)
    {
        foreach (var point in points)
        {
            if (point != A && point != B && point != C && IsPointInTriangle(A, B, C, point)) return true;
        }
        return false;
    }
    public static bool IsPointInTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
    {
        float areaABC = Area(A, B, C);
        float areaABD = Area(A, B, D);
        float areaBCD = Area(B, C, D);
        float areaCAD = Area(C, A, D);
        return Mathf.Abs(areaABC - (areaABD + areaBCD + areaCAD)) < 0.001f;
    }
    public static bool CanFormTriangle(Vector2 A, Vector2 B, Vector2 C)
    {
        float area = Area(A, B, C);
        return area >= 0.005f;
    }
    public static float Area(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return Mathf.Abs(p1.x * (p2.y - p3.y) + p2.x * (p3.y - p1.y) + p3.x * (p1.y - p2.y)) / 2.0f;
    }
}
