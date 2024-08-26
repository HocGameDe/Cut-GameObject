using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GenerateMesh : MonoBehaviour
{
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    Mesh mesh;
    List<int> triangles;
    List<Vector3> vertices;
    List<Vector2> uvs;
    public void GenerateMeshFilter(List<Vector2> points, ObjectCanBeCut target, List<Vector2> pathOrigin)
    {
        GenerateTriangle(points);
        //GenerateTriangleC2(points);
        ConvertPostionToPercentUV(points, pathOrigin);
        Vector2 diffrentSizePercent = GetDiffrentSizePercent(pathOrigin, target);
        vertices = new List<Vector3>();
        float x;
        float y;
        for (int i = 0; i < points.Count; i++)
        {
            x = points[i].x * diffrentSizePercent.x;
            y = points[i].y * diffrentSizePercent.y;
            vertices.Add(new Vector3(x, y, 0));
        }
        DrawMesh(target, vertices.ToArray(), triangles.ToArray(), uvs.ToArray());
    }
    private void GenerateTriangle(List<Vector2> points)
    {
        triangles = new List<int>();
        Dictionary<int, List<int>> linksVectice = new Dictionary<int, List<int>>();
        int[] linkTemp = new int[2];
        List<Vector2> linkTempVector = new List<Vector2> { Vector2.zero, Vector2.zero, Vector2.zero };
        List<int> link0;
        List<int> link1;
        int indexCurrentLink0;
        int indexCurrentLink1;
        int currentVertice = 0;
        bool isCounterClockwise;
        bool notAnyPointsInTriangle;
        int dem = 0;
        for (int i = 0; i < points.Count; i++)
        {
            linkTemp[0] = i == 0 ? points.Count - 1 : i - 1;
            linkTemp[1] = (i + 1) % points.Count;
            linksVectice.Add(i, new List<int> { linkTemp[0], linkTemp[1] });
        }
        while (linksVectice.Count != 0)
        {
            dem++;
            if (dem > 10000)
            {
                Debug.LogError("Stack over flow");
                List<Vector2> listCheckBug = new List<Vector2>();
                foreach (var link in linksVectice)
                {
                    foreach (var item in link.Value)
                    {
                        listCheckBug.Add(points[item]);
                        Debug.Log(listCheckBug.Last());
                    }
                    Debug.Log("--");
                }
                ShowDebugLine(listCheckBug, Color.red, 0);
                ShowDebugLine(points, Color.green, 0);
                break;
            }
            if (linksVectice.ContainsKey(currentVertice) == false)
            {
                currentVertice = (currentVertice + 1) % points.Count;
                continue;
            }
            indexCurrentLink0 = linksVectice[currentVertice][0];
            indexCurrentLink1 = linksVectice[currentVertice][1];
            link0 = linksVectice[indexCurrentLink0];
            link1 = linksVectice[indexCurrentLink1];

            notAnyPointsInTriangle = CalculatorPoints.IsAnyPointsInTrianglePerfect(points[indexCurrentLink0], points[indexCurrentLink1], points[currentVertice], points) == false;

            linkTempVector[0] = points[indexCurrentLink0];
            linkTempVector[1] = points[indexCurrentLink1];
            linkTempVector[2] = points[currentVertice];
            linkTempVector.Sort((a, b) => points.IndexOf(a).CompareTo(points.IndexOf(b)));
            isCounterClockwise = CalculatorPoints.IsCounterClockwise(linkTempVector);

            if ((notAnyPointsInTriangle && isCounterClockwise))
            {
                triangles.Add(points.IndexOf(linkTempVector[0]));
                triangles.Add(points.IndexOf(linkTempVector[2]));
                triangles.Add(points.IndexOf(linkTempVector[1]));

                link0.Remove(currentVertice);
                link1.Remove(currentVertice);
                if (link0.Contains(indexCurrentLink1) && link1.Contains(indexCurrentLink0))
                {
                    link0.Remove(indexCurrentLink1);
                    link1.Remove(indexCurrentLink0);
                }
                else
                {
                    link0.Add(indexCurrentLink1);
                    link1.Add(indexCurrentLink0);
                }
                if (link0.Count == 0) linksVectice.Remove(indexCurrentLink0);
                if (link1.Count == 0) linksVectice.Remove(indexCurrentLink1);

                linksVectice[currentVertice].Remove(indexCurrentLink0);
                linksVectice[currentVertice].Remove(indexCurrentLink1);
                if (linksVectice[currentVertice].Count == 0) linksVectice.Remove(currentVertice);
            }
            currentVertice = (currentVertice + 1) % points.Count;
        }
    }
    private void GenerateTriangleC2(List<Vector2> points)
    {
        triangles = new List<int>();
        List<int[]> edges = new List<int[]>();
        for (int i = 0; i < points.Count; i++) edges.Add(new int[] { i, (i + 1) % points.Count });

        int linkPrevious;
        int linkNext;
        Vector2 midPoint;
        bool edgeInPolygon;
        bool edgeCutAnyEdges;
        for (int i = 0; i < points.Count - 1; i++)
        {
            linkPrevious = (i - 1) % points.Count;
            linkNext = (i + 1) % points.Count;
            for (int j = i + 2; j <points.Count||(j == points.Count && i != 0); j++)
            {
                if (j == points.Count) j = 0;
                midPoint = (points[i] + points[j]) / 2;
                edgeInPolygon = CalculatorPoints.IsPointInPolygon(midPoint, points);
                edgeCutAnyEdges = edges.Any(edge => CalculatorPoints.DoLinesIntersectPerfect(points[i], points[j], points[edge[0]], points[edge[1]]));

                if (!edgeCutAnyEdges && edgeInPolygon)
                {
                    var temp = new int[] { i, j };
                    if (edges.Any(edge => edge[0] == temp[0] && edge[1] == temp[1] || edge[0] == temp[1] && edge[1] == temp[0]) ==false) edges.Add(temp);
                    triangles.Add(i);
                    triangles.Add(j);
                    triangles.Add(linkNext);
                    linkNext = j;
                }
                if ((j+1)%points.Count == linkPrevious) break;
                if (j == 0) break;
            }
        }
    }
    private void FindBoundPathOrigin(List<Vector2> pathOrigin, out Vector2 boundMin, out Vector2 boundMax)
    {
        boundMin.x = pathOrigin.Min(p => p.x);
        boundMin.y = pathOrigin.Min(p => p.y);

        boundMax.x = pathOrigin.Max(p => p.x);
        boundMax.y = pathOrigin.Max(p => p.y);
    }
    Vector2 GetDiffrentSizePercent(List<Vector2> pathOrigin, ObjectCanBeCut target)
    {
        Vector2 boundMin;
        Vector2 boundMax;
        Vector2 result;
        FindBoundPathOrigin(pathOrigin, out boundMin, out boundMax);
        Vector2 textureSize;
        Texture2D texture = target.texture2D;
        textureSize.x = texture.width;
        textureSize.y = texture.height;
        Vector2 diffrentSizePercent;
        float pixelsPerUnit = target.pixelsPerUnit;
        diffrentSizePercent = textureSize / pixelsPerUnit;
        result.x = diffrentSizePercent.x * 1f / (boundMax.x - boundMin.x);
        result.y = diffrentSizePercent.y * 1f / (boundMax.y - boundMin.y);
        return result;
    }
    private void ConvertPostionToPercentUV(List<Vector2> points, List<Vector2> pathOrigin)
    {

        Vector2 boundMin;
        Vector2 boundMax;
        FindBoundPathOrigin(pathOrigin, out boundMin, out boundMax);

        Vector2 uv = new Vector2();
        uvs = new List<Vector2>();
        foreach (var point in points)
        {
            uv.x = (point.x - boundMin.x) / (boundMax.x - boundMin.x);
            uv.y = (point.y - boundMin.y) / (boundMax.y - boundMin.y);
            uvs.Add(uv);
        }
    }
    private void DrawMesh(ObjectCanBeCut target, Vector3[] vertice, int[] triangles, Vector2[] uvs)
    {
        meshRenderer = target.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = target.material;
        meshFilter = target.gameObject.AddComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        mesh.vertices = vertice;
        mesh.triangles = triangles;
        mesh.uv = uvs;
    }
    int countDebug = 0;
    private void ShowDebugLine(List<Vector2> points, Color color, int incPosition)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            Debug.DrawLine(points[i] + Vector2.right * countDebug, points[i + 1] + Vector2.right * countDebug, color, 100);
        }
        countDebug += incPosition;
    }
    private IEnumerator ShowDebugLineSlow(List<Vector2> points, Color color, int incPosition)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            Debug.DrawLine(points[i] + Vector2.right * countDebug, points[i + 1] + Vector2.right * countDebug, color, 100);
            yield return new WaitForSeconds(0.3f);
        }
        countDebug += incPosition;
    }
}
