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
    public void GenerateMeshFilter(List<Vector2> points, GameObject target, List<Vector2> path)
    {
        GenerateTriangle(points);
        ConvertPostionToPercentUV(points,path);
        vertices = new List<Vector3>();
        foreach (var point in points) vertices.Add(point);
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

            notAnyPointsInTriangle = CalculatorCutSpriteRenderer.IsAnyPointsInTriangle(points[indexCurrentLink0], points[indexCurrentLink1], points[currentVertice], points) == false;

            linkTempVector[0] = points[indexCurrentLink0];
            linkTempVector[1] = points[indexCurrentLink1];
            linkTempVector[2] = points[currentVertice];
            linkTempVector.Sort((a, b) => points.IndexOf(a).CompareTo(points.IndexOf(b)));

            isNotClockwise = CalculatorCutSpriteRenderer.IsNotClockwise(linkTempVector);

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
    }
    public Vector2 ConvertPositionWithScale(Vector2 position, Transform target)
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
    private void ConvertPostionToPercentUV(List<Vector2> points, List<Vector2> path)
    {

        Vector2 boundMin = new Vector2();
        Vector2 boundMax = new Vector2();
        var listBoundX = path.Select(p => p.x).OrderBy(p => p);
        var listBoundY = path.Select(p => p.y).OrderBy(p => p);

        boundMin.x = listBoundX.First();
        boundMin.y = listBoundY.First();

        boundMax.x = listBoundX.Last();
        boundMax.y = listBoundY.Last();
        Vector2 uv = new Vector2();
        uvs = new List<Vector2>();
        foreach (var point in points)
        {
            uv.x = (point.x - boundMin.x) / (boundMax.x - boundMin.x);
            uv.y = (point.y - boundMin.y) / (boundMax.y - boundMin.y);
            uvs.Add(uv);
        }
    }
    private void DrawMesh(GameObject target, Vector3[] vertice, int[] triangles, Vector2[] uvs)
    {
        meshRenderer = target.AddComponent<MeshRenderer>();
        meshRenderer.material = Cutter.Instance.target.material;
        meshFilter = target.AddComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        mesh.vertices = vertice;
        mesh.triangles = triangles;
        mesh.uv = uvs;
    }
}
