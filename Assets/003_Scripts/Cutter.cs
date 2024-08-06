using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[RequireComponent(typeof(GenerateMesh))]
public class Cutter : MonoBehaviour
{
    public static Cutter Instance;
    public GenerateMesh generateMesh;
    [SerializeField] private Vector2 beginCutPos;
    [SerializeField] private Vector2 endCutPos;
    [SerializeField] private Vector2 pointBeginPos;
    [SerializeField] private Vector2 pointEndPos;

    private Vector2[][] paths;
    private List<List<Vector2>> listPaths = new List<List<Vector2>>();
    private List<List<Vector2>> listPointsSplit = new List<List<Vector2>>();
    private RaycastHit2D hit;
    private int pathCount;
    private void Awake()
    {
        generateMesh = GetComponent<GenerateMesh>();
        Instance = this;
    }
    private void Start()
    {
        InputManager.Instance.SubEventInput(EventInputCategory.MouseDownLeft, SetBeginCutPos);
        InputManager.Instance.SubEventInput(EventInputCategory.MouseUpLeft, ()
        =>
        {
            SetEndCutPos();
            StandardizedCutLine();
            DrawRayCut();
        });
    }
    public void SetBeginCutPos()
    {
        beginCutPos = InputManager.Instance.mousePoistion;
    }
    public void SetEndCutPos()
    {
        endCutPos = InputManager.Instance.mousePoistion;
    }
    private void StandardizedCutLine()
    {
        if (beginCutPos.x > endCutPos.x)
        {
            Vector2 pos = beginCutPos;
            beginCutPos = endCutPos;
            endCutPos = pos;
        }
    }
    private void DrawRayFromBeginCut()
    {
        hit = Physics2D.Raycast(beginCutPos, endCutPos - beginCutPos, Vector2.Distance(beginCutPos, endCutPos));
        if (hit.collider != null) pointBeginPos = Dog.Instance.transform.InverseTransformPoint(hit.point);
    }
    private void DrawRayFromEndCut()
    {
        hit = Physics2D.Raycast(endCutPos, beginCutPos - endCutPos, Vector2.Distance(beginCutPos, endCutPos));
        if (hit.collider != null) pointEndPos = Dog.Instance.transform.InverseTransformPoint(hit.point);
    }
    private void DrawRayCut()
    {
        hit = Physics2D.Raycast(endCutPos, Vector2.down, 0.001f);
        if (hit.collider != null)
        {
            Debug.LogWarning("Cut Fail!");
            return;
        }
        DrawRayFromBeginCut();
        DrawRayFromEndCut();
        SplitGameObjectByLine();
    }
    //LineEquation: Ax+By+C=0
    // A = y2-y1
    // B = x1-x2
    // C = x2*y1 - x1*y2
    float A;
    float B;
    float C;
    private void CalculateLineEquation()
    {
        Vector3 result = LinearEquations(pointBeginPos, pointEndPos);
        A = result.x;
        B = result.y;
        C = result.z;
    }
    private Vector3 LinearEquations(Vector3 point1, Vector3 point2)
    {
        Vector3 result = new Vector3();
        result.x = point2.y - point1.y;
        result.y = point1.x - point2.x;
        result.z = point2.x * point1.y - point1.x * point2.y;
        return result;
    }
    private bool GetValueByLine(Vector2 point)
    {
        return (A * point.x + B * point.y + C) > 0;
    }
    private Vector2 GetIntersectionPointBetweenTwoLines(Vector3 point1Line1, Vector3 point2Line1, Vector3 point1Line2, Vector3 point2Line2)
    {
        //coefficient Line
        Vector3 cLine1 = LinearEquations(point1Line1, point2Line1);
        Vector3 cLine2 = LinearEquations(point1Line2, point2Line2);
        Vector2 result = new Vector2();
        result.y = (cLine2.x * cLine1.z - cLine1.x * cLine2.z) / (cLine2.y * cLine1.x - cLine2.x * cLine1.y);
        result.x = -cLine1.z / cLine1.x - cLine1.y * result.y / cLine1.x;
        return result;
    }
    private void SplitGameObjectByLine()
    {
        GetPolygon2DPaths();
        CalculateLineEquation();
        foreach (var path in listPaths)
        {
            if (path.Count > 2) SplitPath(path);
            else Debug.LogWarning("Polygon GameObject error!");
        }
        listPaths.Clear();
    }
    int indexListPathsSplit = 0;
    int temp;
    int indexMain;
    List<int> indexRemove = new List<int>();
    private void SplitPath(List<Vector2> path)
    {
        SplitPath();
        if (listPointsSplit.Count <= 1)
        {
            Debug.LogWarning("GameObject Not Found!");
            return;
        }
        StandardHeadTail();
        InsertCutPointIntoPoints();
        if (listPointsSplit.Count > 2)
        {
            FindMainPointsIndex();
            MergePointsFailIntoMainPoints();
        }
        SpawnNewSlices();

        //Method in Method-----------------------------
        void SplitPath()
        {
            listPointsSplit.Clear();
            indexListPathsSplit = 0;
            listPointsSplit.Add(new List<Vector2>());
            for (int i = 0; i < path.Count - 1; i++)
            {
                listPointsSplit[indexListPathsSplit].Add(path[i]);
                if (GetValueByLine(path[i]) != GetValueByLine(path[i + 1]))
                {
                    listPointsSplit.Add(new List<Vector2>());
                    indexListPathsSplit++;
                }
            }
        }
        void StandardHeadTail()
        {
            if (GetValueByLine(listPointsSplit.First()[0]) == GetValueByLine(listPointsSplit.Last()[0]))
            {
                listPointsSplit.First().InsertRange(0, listPointsSplit.Last());
                listPointsSplit.Remove(listPointsSplit.Last());
            }
        }
        void FindMainPointsIndex()
        {
            indexMain = 0;
            indexRemove.Clear();
            for (int i = 0; i < listPointsSplit.Count; i++)
            {
                if (IsNotClockwise(listPointsSplit[(i - 1 < 0) ? listPointsSplit.Count - 1 : (i - 1)]) == true
                    && IsNotClockwise(listPointsSplit[(i + 1) % listPointsSplit.Count]) == true
                    && IsNotClockwise(listPointsSplit[i]) == true)
                {
                    indexMain = i;
                }

                if (IsNotClockwise(listPointsSplit[i]) == false)
                {
                    indexRemove.Add(i);
                }
            }
        }
        void InsertCutPointIntoPoints()
        {
            foreach (var points in listPointsSplit)
            {
                int indexPointFirst = path.IndexOf(points.First());
                int indexPointLast = path.IndexOf(points.Last());
                Vector2 pointInsertFirst = GetIntersectionPointBetweenTwoLines(path[indexPointFirst], path[indexPointFirst == 0 ? path.Count - 1 : indexPointFirst - 1], pointBeginPos, pointEndPos);
                Vector2 pointInsertLast = GetIntersectionPointBetweenTwoLines(path[indexPointLast], path[indexPointLast == path.Count - 1 ? 0 : indexPointLast + 1], pointBeginPos, pointEndPos);
                points.Insert(0, pointInsertFirst);
                points.Add(pointInsertLast);
            }
        }
        void MergePointsFailIntoMainPoints()
        {
            for (int i = 0; i < indexRemove.Count; i++) listPointsSplit[indexMain].AddRange(listPointsSplit[indexRemove[i]]);
            temp = 0;
            foreach (var index in indexRemove)
            {
                listPointsSplit.RemoveAt(index - temp);
                temp++;
            }
            indexRemove.Clear();
        }
        void SpawnNewSlices()
        {
            foreach (var path in listPointsSplit)
            {
                DeletePointRedundant(path);
                
                GameObject newObj = new GameObject();
                newObj.AddComponent(typeof(PolygonCollider2D));
                var polygon = newObj.GetComponent<PolygonCollider2D>();
                polygon.SetPath(0, path.ToArray());
                newObj.transform.localScale = Dog.Instance.transform.localScale;
                newObj.transform.position = Dog.Instance.transform.position;

                generateMesh.GenerateMeshFilter(path, newObj.transform);
                path.Clear();
            }
        }
        void DeletePointRedundant(List<Vector2> points)
        {
            for (int i = 0; i < points.Count; i++)
            {
                p1 = points[i == 0 ? points.Count - 1:i-1];
                p2 = points[(i + 1) % points.Count];
                if (!generateMesh.CanFormTriangle(points[i],p1,p2))
                {
                    points.RemoveAt(i);
                    i--;
                }
            }
        }
        //---------------------------------------------
    }
    Vector2 p1;
    Vector2 p2;
    float sumS;
    public bool IsNotClockwise(List<Vector2> points)
    {
        sumS = 0;
        for (int i = 0; i < points.Count; i++)
        {
            p1 = points[i];
            p2 = points[(i + 1) % points.Count];
            sumS += (p2.x - p1.x) * (p2.y + p1.y);
        }

        return sumS < 0;
    }

    private void GetPolygon2DPaths()
    {
        pathCount = Dog.Instance.polygonCollider2D.pathCount;
        paths = new Vector2[pathCount][];
        listPaths.Clear();
        for (int i = 0; i < pathCount; i++)
        {
            paths[i] = Dog.Instance.polygonCollider2D.GetPath(i);
            listPaths.Add(paths[i].ToList());
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.red;
        Gizmos.DrawLine(beginCutPos, endCutPos);
    }
}
