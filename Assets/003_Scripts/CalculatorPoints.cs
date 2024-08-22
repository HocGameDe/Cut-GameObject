using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CalculatorPoints
{
    //LineEquation: Ax+By+C=0
    // A = y2-y1
    // B = x1-x2
    // C = x2*y1 - x1*y2
    public static Vector3 LinearEquations(Vector2 point1, Vector2 point2)
    {
        float a = point2.y - point1.y;
        float b = point1.x - point2.x;
        float c = -a * point1.x - b * point1.y;
        return new Vector3(a, b, c);
    }
    public static Vector3 LinearEquations(Vector2 perpendicularLineThroughPoint, Vector2 A, Vector2 B)
    {
        // Hệ số của đường thẳng ban đầu ax + by + c = 0
        Vector3 line = LinearEquations(A, B);
        float a = line.x;
        float b = line.y;

        // Đường thẳng vuông góc với đường thẳng ban đầu có hệ số góc b/a
        float perpendicularA = -b; // Hệ số góc mới là nghịch đảo âm
        float perpendicularB = a;

        // Tính hệ số c cho đường thẳng mới: perpendicularA * x + perpendicularB * y + c = 0
        float perpendicularC = -(perpendicularA * perpendicularLineThroughPoint.x + perpendicularB * perpendicularLineThroughPoint.y);

        return new Vector3(perpendicularA, perpendicularB, perpendicularC);
    }
    public static bool IsPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector3 linearEquation = LinearEquations(start, end);
        return Mathf.Min(start.x, end.x) <= point.x && point.x <= Mathf.Max(start.x, end.x) &&
               Mathf.Min(start.y, end.y) <= point.y && point.y <= Mathf.Max(start.y, end.y) &&
               Mathf.Abs(linearEquation.x * point.x + linearEquation.y * point.y + linearEquation.z) <= 0.001f;
    }
    public static bool IsPointOnLeftLine(Vector2 pointHead, Vector2 pointTail, Vector2 point)
    {
        Vector3 linearEquation = LinearEquations(pointHead, pointTail);
        return (linearEquation.x * point.x + linearEquation.y * point.y + linearEquation.z) >= 0;
    }
    public static Vector2? GetIntersectionOfLines(Vector2 point1Line1, Vector2 point2Line1, Vector2 point1Line2, Vector2 point2Line2)
    {
        Vector3 cLine1 = LinearEquations(point1Line1, point2Line1);
        Vector3 cLine2 = LinearEquations(point1Line2, point2Line2);
        float denominator = cLine1.x * cLine2.y - cLine2.x * cLine1.y;
        if (Mathf.Abs(denominator) < Mathf.Epsilon) return null;

        Vector2 intersection;
        intersection.x = (cLine1.y * cLine2.z - cLine2.y * cLine1.z) / denominator;
        intersection.y = (cLine2.x * cLine1.z - cLine1.x * cLine2.z) / denominator;

        return intersection;
    }
    public static Vector2? GetIntersectionOfSegment(Vector2 point1Line1, Vector2 point2Line1, Vector2 point1Line2, Vector2 point2Line2)
    {
        Vector3 cLine1 = LinearEquations(point1Line1, point2Line1);
        Vector3 cLine2 = LinearEquations(point1Line2, point2Line2);
        float denominator = cLine1.x * cLine2.y - cLine2.x * cLine1.y;
        if (Mathf.Abs(denominator) < Mathf.Epsilon) return null;

        Vector2 intersection;
        intersection.x = (cLine1.y * cLine2.z - cLine2.y * cLine1.z) / denominator;
        intersection.y = (cLine2.x * cLine1.z - cLine1.x * cLine2.z) / denominator;

        if (!IsPointOnSegment(intersection, point1Line1, point2Line1)) return null;
        if (!IsPointOnSegment(intersection, point1Line2, point2Line2)) return null;

        return intersection;
    }
    public static bool IsCounterClockwise(List<Vector2> points)
    {
        Vector2 p1;
        Vector2 p2;
        float sumS = 0;
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
        return points.Any(p => p != A && p != B && p != C && IsPointInTriangle(A, B, C, p));
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
    public static Vector2 GetPointCloser(Vector2 point1, Vector2 point2, Vector2 target)
    {
        if (Vector2.Distance(point1, target) <= Vector2.Distance(point2, target)) return point1;
        else return point2;
    }
    public static Vector2 GetPointBetweenLineAndNormalAtPoint(Vector2 point, Vector2 A, Vector2 B)
    {
        Vector2 AB = B - A;
        Vector2 AP = point - A;

        // Tích vô hướng của vector AB và AP
        float dotProduct = Vector2.Dot(AP, AB);

        // Độ dài bình phương của vector AB
        float lengthSquaredAB = AB.sqrMagnitude;

        // Tính tỉ lệ chiếu của vector AP lên AB
        float parameter = dotProduct / lengthSquaredAB;

        // Tìm điểm trên đường thẳng AB gần với điểm 'point' nhất
        Vector2 closestPointOnLine = A + parameter * AB;
        return closestPointOnLine;
    }
    public static float DistanceFromPointToLine(Vector2 point, Vector2 A, Vector2 B)
    {
        Vector3 linearEquation = LinearEquations(A, B);
        float dAB = Mathf.Abs(linearEquation.x * point.x + linearEquation.y * point.y + linearEquation.z) / Mathf.Sqrt(linearEquation.x * linearEquation.x + linearEquation.y * linearEquation.y);
        return dAB;
    }
    public static float DistanceFromPointToLine(Vector2 point, Vector3 linearEquation)
    {
        float dAB = Mathf.Abs(linearEquation.x * point.x + linearEquation.y * point.y + linearEquation.z) / Mathf.Sqrt(linearEquation.x * linearEquation.x + linearEquation.y * linearEquation.y);
        return dAB;
    }
    public static bool IsPointInsideAreaTwoDelta(Vector2 point, Vector2 A, Vector2 B)
    {
        float distanceDeltaA = DistanceFromPointToLine(point, LinearEquations(A, A, B));
        float distanceDeltaB = DistanceFromPointToLine(point, LinearEquations(B, A, B));
        return distanceDeltaA + distanceDeltaB - Vector2.Distance(A, B) <= 0.001f;
    }
    public static bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        int count = polygon.Count;
        bool result = false;
        int j = count - 1;
        for (int i = 0; i < count; i++)
        {
            if (IsPointOnSegment(point, polygon[i], polygon[(i + 1)%count])) return true;
        }
        for (int i = 0; i < count; i++)
        {
            if (polygon[i].y < point.y && polygon[j].y >= point.y || polygon[j].y < point.y && polygon[i].y >= point.y)
            {
                if (polygon[i].x + (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) * (polygon[j].x - polygon[i].x) < point.x)
                {
                    result = !result;
                }
            }
            j = i;
        }

        return result;
    }
    public static bool IsPolygonAInPolygonB(List<Vector2> polygonA, List<Vector2> polygonB)
    {
        foreach (var point in polygonA)
        {
            if (!IsPointInPolygon(point, polygonB))
            {
                return false;
            }
        }
        return true;
    }
}
