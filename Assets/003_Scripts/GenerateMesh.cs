using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMesh : MonoBehaviour
{
    MeshFilter meshFilter;
    Mesh mesh;
    List<int> triangles = new List<int>();
    List<Vector3> vertices = new List<Vector3>();
    List<Vector2> uv = new List<Vector2>();
    public bool IsAnyPointsInTriangle(Vector2 A, Vector2 B, Vector2 C, List<Vector2> points)
    {
        foreach (var point in points)
        {
            if (point != A && point != B && point != C && IsPointInTriangle(A, B, C, point)) return true;
        }
        return false;
    }
    public bool IsPointInTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
    {
        float areaABC = Area(A, B, C);
        float areaABD = Area(A, B, D);
        float areaBCD = Area(B, C, D);
        float areaCAD = Area(C, A, D);
        return Mathf.Abs(areaABC - (areaABD + areaBCD + areaCAD)) < 0.001f;
    }
    public bool CanFormTriangle(Vector2 A, Vector2 B, Vector2 C)
    {
        float area = Area(A, B, C);
        return area >= 0.001f;
    }
    public float Area(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return Mathf.Abs(p1.x * (p2.y - p3.y) + p2.x * (p3.y - p1.y) + p3.x * (p1.y - p2.y)) / 2.0f;
    }
    public void GenerateTriangle(List<Vector2> points)
    {
        triangles.Clear();
        Dictionary<int, List<int>> linksVectice = new Dictionary<int, List<int>>();
        int[] linkTemp = new int[2];
        List<Vector2> linkTempVector = new List<Vector2> { Vector2.zero, Vector2.zero, Vector2.zero };
        List<int> link0;
        List<int> link1;
        int indexCurrentLink0;
        int indexCurrentLink1;
        int currentVertice = 0;
        bool isNotClockwise;
        bool notAnyPointsInTriangle;
        int dem = 0;
        for (int i = 0; i < points.Count; i++)
        {
            linkTemp[0] = i == 0 ? points.Count - 1 : i - 1;
            linkTemp[1] = (i + 1) % points.Count;
            linksVectice.Add(i, new List<int> { linkTemp[0], linkTemp[1] });
        }
        while (!IsDoneGenerateCaculator(linksVectice))
        {
            dem++;
            if (dem > 10000)
            {
                Debug.LogError("Stack over flow");
                foreach (var link in linksVectice)
                {
                    foreach (var item in link.Value) Debug.Log(item);
                    Debug.Log("---");
                }
                break;
            }
            if (linksVectice[currentVertice].Count == 0)
            {
                currentVertice = (currentVertice + 1) % points.Count;
                continue;
            }
            indexCurrentLink0 = linksVectice[currentVertice][0];
            indexCurrentLink1 = linksVectice[currentVertice][1];
            link0 = linksVectice[indexCurrentLink0];
            link1 = linksVectice[indexCurrentLink1];

            notAnyPointsInTriangle = IsAnyPointsInTriangle(points[indexCurrentLink0], points[indexCurrentLink1], points[currentVertice], points) == false;

            linkTempVector[0] = points[indexCurrentLink0];
            linkTempVector[1] = points[indexCurrentLink1];
            linkTempVector[2] = points[currentVertice];
            linkTempVector.Sort((a, b) => points.IndexOf(a).CompareTo(points.IndexOf(b)));
            isNotClockwise = Cutter.Instance.IsNotClockwise(linkTempVector);

            if (notAnyPointsInTriangle && isNotClockwise)
            {
                triangles.Add(points.IndexOf(linkTempVector[0]));
                triangles.Add(points.IndexOf(linkTempVector[2]));
                triangles.Add(points.IndexOf(linkTempVector[1]));

                link0.Remove(currentVertice);
                if (link0.Contains(indexCurrentLink1)) link0.Remove(indexCurrentLink1);
                else link0.Add(indexCurrentLink1);

                link1.Remove(currentVertice);
                if (link1.Contains(indexCurrentLink0)) link1.Remove(indexCurrentLink0);
                else link1.Add(indexCurrentLink0);

                linksVectice[currentVertice].Remove(indexCurrentLink0);
                linksVectice[currentVertice].Remove(indexCurrentLink1);

            }
            currentVertice = (currentVertice + 1) % points.Count;
        }
        for (int i = 0; i < triangles.Count; i += 3)
        {
            Debug.DrawLine(ConvertPositionWithScale(points[triangles[i + 0]], Dog.Instance.transform), ConvertPositionWithScale(points[triangles[i + 1]], Dog.Instance.transform), Color.red, 100, false);
            Debug.DrawLine(ConvertPositionWithScale(points[triangles[i + 0]], Dog.Instance.transform), ConvertPositionWithScale(points[triangles[i + 2]], Dog.Instance.transform), Color.red, 100, false);
            Debug.DrawLine(ConvertPositionWithScale(points[triangles[i + 2]], Dog.Instance.transform), ConvertPositionWithScale(points[triangles[i + 1]], Dog.Instance.transform), Color.red, 100, false);
        }
    }
    private Vector2 ConvertPositionWithScale(Vector2 position, Transform target)
    {
        Vector2 result = new Vector2();
        result.x = target.TransformPoint(position).x;
        result.y = target.TransformPoint(position).y;
        return result;
    }
    private bool IsDoneGenerateCaculator(Dictionary<int, List<int>> links)
    {
        foreach (var link in links) if (link.Value.Count > 0) return false;
        return true;
    }
}
