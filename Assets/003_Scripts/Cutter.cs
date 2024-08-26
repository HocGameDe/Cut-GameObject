using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;
[RequireComponent(typeof(GenerateMesh))]
public class Cutter : MonoBehaviour
{
    public static Cutter Instance;
    public bool cutContinous;
    public GenerateMesh generateMesh;
    public LayerMask layerMaskCanCut;
    private bool cutting;
    private ObjectCanBeCut target;
    private List<ObjectCanBeCut> targets;
    [SerializeField] private float forceCut;
    [SerializeField] private Vector2 beginCutPos;
    [SerializeField] private Vector2 endCutPos;
    [SerializeField] private Vector2 pointBeginCollision;
    [SerializeField] private Vector2 pointEndCollision;
    private List<List<Vector2>> listPaths = new List<List<Vector2>>();

    private RaycastHit2D hit;
    private RaycastHit2D[] hits;
    private List<List<Vector2>> pointsCollision;

    private void Awake()
    {
        generateMesh = GetComponent<GenerateMesh>();
        Instance = this;
    }
    private void Start()
    {
        InputManager.Instance.SubEventInput(EventInputCategory.MouseDownLeft, SetBeginCutPos);
        InputManager.Instance.SubEventInput(EventInputCategory.MouseDownLeft, () => cutting = true);
        InputManager.Instance.SubEventInput(EventInputCategory.MouseDownLeft, () =>
        {
           
        });
        InputManager.Instance.SubEventInput(EventInputCategory.MouseUpLeft, Cut);
        InputManager.Instance.SubEventInput(EventInputCategory.MouseUpLeft, () => cutting = false);
    }
    public void Cut()
    {
        if (cutContinous)
        {
            ResetCutPos();
            return;
        }
        SetEndCutPos();
        if (Vector2.Distance(beginCutPos, endCutPos) < 0.001f)
        {
            ResetCutPos();
            return;
        }
        StandardizedCutPoints();
        DrawRayCut();
        ResetCutPos();
    }

    public void SetBeginCutPos()
    {
        beginCutPos = InputManager.Instance.mousePoistion;
    }
    public void SetEndCutPos()
    {
        endCutPos = InputManager.Instance.mousePoistion;
    }
    public void ResetCutPos()
    {
        beginCutPos = Vector2.zero;
        endCutPos = Vector2.zero;
    }
    private void StandardizedCutPoints()
    {
        if (beginCutPos.x > endCutPos.x)
        {
            Vector2 pos = beginCutPos;
            beginCutPos = endCutPos;
            endCutPos = pos;
        }
    }
    private void FindPointBeginCollision()
    {
        //hit = Physics2D.Raycast(beginCutPos, endCutPos - beginCutPos, Vector2.Distance(beginCutPos, endCutPos), LayerMask.GetMask("Object"));
        //if (hit.collider != null) pointBeginCollision = target.transform.InverseTransformPoint(hit.point);

        hits = Physics2D.RaycastAll(beginCutPos, endCutPos - beginCutPos, Vector2.Distance(beginCutPos, endCutPos), layerMaskCanCut);
        if (hits.Length <= 0 || hits == null) return;
        targets = new List<ObjectCanBeCut>();
        pointsCollision = new List<List<Vector2>>();
        for (int i = 0; i < hits.Count(); i++)
        {
            targets.Add(hits[i].collider.GetComponent<ObjectCanBeCut>());
            pointsCollision.Add(new List<Vector2>());
            pointsCollision[i].Add(hits[i].transform.InverseTransformPoint(hits[i].point));
        }
    }
    private void FindPointEndCollision()
    {
        //hit = Physics2D.Raycast(endCutPos, beginCutPos - endCutPos, Vector2.Distance(beginCutPos, endCutPos), LayerMask.GetMask(nameLayerCanCut));
        //if (hit.collider != null) pointEndCollision = target.transform.InverseTransformPoint(hit.point);

        hits = Physics2D.RaycastAll(endCutPos, beginCutPos - endCutPos, Vector2.Distance(beginCutPos, endCutPos), layerMaskCanCut);
        if (hits.Length <= 0 || hits == null) return;
        for (int i = 0; i < hits.Count(); i++)
        {
            pointsCollision[hits.Count() - 1 - i].Add(hits[i].transform.InverseTransformPoint(hits[i].point));
        }
    }
    private void DrawRayCut()
    {
        hit = Physics2D.Raycast(beginCutPos, Vector2.down, 0.001f, layerMaskCanCut);
        if (hit.collider != null)
        {
            Debug.LogWarning("Cut Fail: BeginCutPos in ObjectCanBeCut!");
            return;
        }
        hit = Physics2D.Raycast(endCutPos, Vector2.down, 0.001f, layerMaskCanCut);
        if (hit.collider != null)
        {
            Debug.LogWarning("Cut Fail!: EndCutPos in ObjectCanBeCut!");
            return;
        }
        else
        {
            hit = Physics2D.Raycast(endCutPos, beginCutPos - endCutPos, Vector2.Distance(beginCutPos, endCutPos), layerMaskCanCut);
            if (hit.collider == null)
            {
                Debug.LogWarning("Cut Fail!: GameObject Not Found!");
                return;
            }
            else target = hit.collider.GetComponent<ObjectCanBeCut>();
        }
        FindPointBeginCollision();
        FindPointEndCollision();
        foreach (var target in targets)
        {
            this.target = target;
            pointBeginCollision = pointsCollision[targets.IndexOf(target)][0];
            pointEndCollision = pointsCollision[targets.IndexOf(target)][1];
            CutGameObject();
        }
    }

    private void CutGameObject()
    {
        GetPolygon2DPaths();
        if (listPaths.First().Count > 2) SplitPolygon(listPaths.First());
        else Debug.LogWarning("Polygon GameObject error!");
        listPaths.Clear();
    }
    private void GetPolygon2DPaths()
    {
        int pathCount = target.polygonCollider2D.pathCount;
        listPaths.Clear();
        List<Vector2> path;
        for (int i = 0; i < pathCount; i++)
        {
            path = new List<Vector2>();
            target.polygonCollider2D.GetPath(i, path);
            listPaths.Add(path);
        }
    }

    private void SplitPolygon(List<Vector2> path)
    {
        if (DeletePointRedundant(path) == false) return;
        List<List<Vector2>> listPointsSplit;

        listPointsSplit = GetListPointsSplit(path, pointBeginCollision, pointEndCollision);
        if (listPointsSplit.Count <= 1)
        {
            Debug.LogWarning("Path Polygon GameObject Split count <=1!");
            return;
        }
        StandardizedHeadTail(listPointsSplit, pointBeginCollision, pointEndCollision);
        if (listPointsSplit.Count <= 1)
        {
            Debug.LogWarning("Path Polygon GameObject Split count <=1 After StandradHeadTail!");
            return;
        }
        InsertCutPointIntoListPointsSplit(listPointsSplit, pointBeginCollision, pointEndCollision);
        List<List<Vector2>> listPointsClockwise = new List<List<Vector2>>();
        List<Vector2> pointsMain = new List<Vector2>();
        int indexMain = 0;
        if (listPointsSplit.Count > 2)
        {
            indexMain = GetMainPointsIndex(listPointsSplit);
            MergePointsClockwiseIntoMainPoints(listPointsSplit, indexMain);
        }
        SpawnSlices(listPointsSplit);

        //Method in Method-----------------------------
        List<List<Vector2>> GetListPointsSplit(List<Vector2> path, Vector2 pointBeginCollision, Vector2 pointEndCollision)
        {
            int indexListPathsSplit = 0;
            List<List<Vector2>> listPointsSplit = new List<List<Vector2>>();
            listPointsSplit.Add(new List<Vector2>());
            bool IsValueOnLeftLine_i_Diffrent_IsValueOnLeftLine_i1;
            bool IsNextPointOutCutLine;
            for (int i = 0; i < path.Count - 1; i++)
            {
                listPointsSplit[indexListPathsSplit].Add(path[i]);
                IsValueOnLeftLine_i_Diffrent_IsValueOnLeftLine_i1 = CalculatorPoints.IsPointOnLeftLine(pointBeginCollision, pointEndCollision, path[i])
                                                                         != CalculatorPoints.IsPointOnLeftLine(pointBeginCollision, pointEndCollision, path[i + 1]);
                IsNextPointOutCutLine = CalculatorPoints.CanFormTriangle(path[i + 1], pointBeginCollision, pointEndCollision);
                if (IsValueOnLeftLine_i_Diffrent_IsValueOnLeftLine_i1 && IsNextPointOutCutLine)
                {
                    listPointsSplit.Add(new List<Vector2>());
                    indexListPathsSplit++;
                }
            }
            listPointsSplit[indexListPathsSplit].Add(path.Last());
            return listPointsSplit;
        }
        void StandardizedHeadTail(List<List<Vector2>> listPointsSplit, Vector2 pointBeginCollision, Vector2 pointEndCollision)
        {
            if (CalculatorPoints.IsPointOnLeftLine(pointBeginCollision, pointEndCollision, listPointsSplit.First()[0])
                == CalculatorPoints.IsPointOnLeftLine(pointBeginCollision, pointEndCollision, listPointsSplit.Last()[0]))
            {
                listPointsSplit.First().InsertRange(0, listPointsSplit.Last());
                listPointsSplit.Remove(listPointsSplit.Last());
            }
        }
        void InsertCutPointIntoListPointsSplit(List<List<Vector2>> listPointsSplit, Vector2 pointBeginCollision, Vector2 pointEndCollision)
        {
            foreach (var points in listPointsSplit)
            {
                InsertCutPointIntoPoints(path, points, pointBeginCollision, pointEndCollision);
            }
        }
        int GetMainPointsIndex(List<List<Vector2>> listPointsSplit)
        {
            var indexMain = 0;
            for (int i = 0; i < listPointsSplit.Count; i++)
            {
                if (CalculatorPoints.IsCounterClockwise(listPointsSplit[(i - 1 < 0) ? listPointsSplit.Count - 1 : (i - 1)]) == true
                    && CalculatorPoints.IsCounterClockwise(listPointsSplit[(i + 1) % listPointsSplit.Count]) == true
                    && CalculatorPoints.IsCounterClockwise(listPointsSplit[i]) == true) indexMain = i;
            }
            pointsMain = listPointsSplit[indexMain].ToList();
            return indexMain;
        }
        void MergePointsClockwiseIntoMainPoints(List<List<Vector2>> listPointsSplit, int indexMain)
        {
            for (int i = 0; i < listPointsSplit.Count; i++)
            {
                if (CalculatorPoints.IsCounterClockwise(listPointsSplit[i]) == false)
                {
                    listPointsSplit[indexMain].AddRange(listPointsSplit[i]);
                    listPointsClockwise.Add(listPointsSplit[i]);
                    listPointsSplit.RemoveAt(i);
                    i--;
                }
            }
        }

        void InsertCutPointIntoPoints(List<Vector2> pathOrigin, List<Vector2> pointsSplit, Vector2 pointBeginCollision, Vector2 pointEndCollision)
        {
            int indexPointFirst = pathOrigin.IndexOf(pointsSplit.First());
            int indexPointLast = pathOrigin.IndexOf(pointsSplit.Last());
            Vector2? pointInsertFirst = CalculatorPoints.GetIntersectionOfLines(pathOrigin[indexPointFirst], pathOrigin[indexPointFirst == 0 ? pathOrigin.Count - 1 : indexPointFirst - 1], pointBeginCollision, pointEndCollision);
            Vector2? pointInsertLast = CalculatorPoints.GetIntersectionOfLines(pathOrigin[indexPointLast], pathOrigin[indexPointLast == pathOrigin.Count - 1 ? 0 : indexPointLast + 1], pointBeginCollision, pointEndCollision);
            if (pointInsertFirst != null && pointInsertLast != null)
            {
                pointsSplit.Insert(0, (Vector2)pointInsertFirst);
                pointsSplit.Add((Vector2)pointInsertLast);
            }
            else Debug.LogError("GetIntersectionOfLines Error!");
        }
        void AddEmptyPolygon(List<Vector2> pointsSplit, PolygonCollider2D polygonCollider2D)
        {
            if (listPaths.Count <= 1) return;
            List<List<Vector2>> listPointsEmpty = new List<List<Vector2>>();
            bool isHavePointsMain = listPointsClockwise.Count > 0 && pointsMain.Any(point => pointsSplit.Contains(point) && point != pointsMain.First() && point != pointsMain.Last() && point != pointsSplit.First() && point != pointsSplit.Last());
            List<List<Vector2>> listPointsEmptyClockwise = new List<List<Vector2>>();
            foreach (var path in listPaths.Skip(1))
            {
                DeletePointRedundant(path);
                if (path.All(point => CalculatorPoints.IsPointInPolygon(point, pointsSplit) == false))
                {
                    continue;
                }
                if (path.All(point => CalculatorPoints.IsPointInPolygon(point, pointsSplit)))
                {
                    polygonCollider2D.pathCount++;
                    polygonCollider2D.SetPath(polygonCollider2D.pathCount - 1, path);
                    continue;
                }
                List<List<Vector2>> pointsEmptySplit;
                List<Vector2> pointsEmpty;
                bool direction = CalculatorPoints.IsPointOnLeftLine(pointsSplit.First(), pointsSplit.Last(), pointsSplit[1]);
                pointsEmptySplit = GetListPointsSplit(path, pointsSplit.First(), pointsSplit.Last());
                StandardizedHeadTail(pointsEmptySplit, pointsSplit.First(), pointsSplit.Last());
                pointsEmptySplit.Sort((a, b) => isHavePointsMain ? Vector2.Distance(a.First(), pointsMain.Last()).CompareTo(Vector2.Distance(b.First(), pointsMain.Last()))
                                                                : Vector2.Distance(a.First(), pointsSplit.Last()).CompareTo(Vector2.Distance(b.First(), pointsSplit.Last())));
                pointsEmptySplit.ForEach(points => InsertCutPointIntoPoints(path, points, pointsSplit.First(), pointsSplit.Last()));
                pointsEmpty = pointsEmptySplit.Where(points => CalculatorPoints.IsCounterClockwise(points) == false && CalculatorPoints.IsPointOnLeftLine(pointsSplit.First(), pointsSplit.Last(), points[1]) == direction)
                                              .SelectMany(point => point)
                                              .Where(point => CalculatorPoints.IsPointInPolygon(point, pointsSplit))
                                              .ToList();
                listPointsEmptyClockwise.AddRange(pointsEmptySplit.Where(points => CalculatorPoints.IsCounterClockwise(points) && CalculatorPoints.IsPointOnLeftLine(pointsSplit.First(), pointsSplit.Last(), points[1]) == direction));
                if (pointsEmpty.Count <= 0)
                {
                    continue;
                }
                listPointsEmpty.Add(pointsEmpty);
            }

            if (listPointsEmpty.Count > 0)
            {

                listPointsEmpty.Sort((a, b) => isHavePointsMain ? Vector2.Distance(a.First(), pointsMain.Last()).CompareTo(Vector2.Distance(b.First(), pointsMain.Last()))
                                                                : Vector2.Distance(a.First(), pointsSplit.Last()).CompareTo(Vector2.Distance(b.First(), pointsSplit.Last())));

                foreach (var pointsEmptyClockwise in listPointsEmptyClockwise)
                {
                    List<Vector2> listPoint = null;
                    foreach (var list in listPointsClockwise) if (CalculatorPoints.IsPolygonAInPolygonB(list, pointsEmptyClockwise)) listPoint = list;
                    if (listPoint != null)
                    {
                        listPoint.AddRange(pointsEmptyClockwise);
                        SpawnNewSlice(listPoint.ToList());
                        listPointsClockwise.Remove(listPoint);
                    }
                    else
                    {
                        SpawnNewSlice(pointsEmptyClockwise.ToList());
                    }
                }

                if (CalculatorPoints.IsPointOnSegment(listPointsEmpty.First().First(), pointsSplit.First(), pointsSplit.Last())
                    && CalculatorPoints.IsPointOnSegment(listPointsEmpty.Last().Last(), pointsSplit.First(), pointsSplit.Last()))
                {
                    var points = listPointsEmpty.SelectMany(point => point).ToList();
                    pointsSplit.AddRange(points);
                    polygonCollider2D.SetPath(0, pointsSplit);
                }
                else
                {
                    listPointsEmpty.AddRange(listPointsClockwise);
                    listPointsEmpty.Sort((a, b) => isHavePointsMain ? Vector2.Distance(a.First(), pointsMain.Last()).CompareTo(Vector2.Distance(b.First(), pointsMain.Last()))
                                                          : Vector2.Distance(a.First(), pointsSplit.Last()).CompareTo(Vector2.Distance(b.First(), pointsSplit.Last())));
                    var points = listPointsEmpty.SelectMany(point => point).ToList();
                    pointsSplit.Clear();
                    pointsSplit.AddRange(pointsMain);
                    pointsSplit.AddRange(points);
                    polygonCollider2D.SetPath(0, pointsSplit);
                }
            }

        }
        void SpawnSlices(List<List<Vector2>> listPointsSplit)
        {
            if (listPointsSplit.Any(listPoints => !DeletePointRedundant(listPoints))) return;
            foreach (var listPoints in listPointsSplit)
            {
                SpawnNewSlice(listPoints);
            }
            target.StartAnimationCut(pointBeginCollision, pointEndCollision);
        }
        void SpawnNewSlice(List<Vector2> points)
        {
            GameObject newSlice = new GameObject();
            newSlice.layer = target.gameObject.layer;
            newSlice.transform.position = target.transform.position;
            newSlice.transform.rotation = target.transform.rotation;
            newSlice.transform.localScale = target.transform.localScale;
            ObjectCanBeCut objectCutComponent = newSlice.AddComponent<ObjectCanBeCut>();
            objectCutComponent.pathOrigin = target.pathOrigin;
            objectCutComponent.pixelsPerUnit = target.pixelsPerUnit;
            objectCutComponent.material = target.material;
            objectCutComponent.shader = target.shader;
            objectCutComponent.texture2D = target.texture2D;
            objectCutComponent.polygonCollider2D.SetPath(0, points);

            AddEmptyPolygon(points, objectCutComponent.polygonCollider2D);
            generateMesh.GenerateMeshFilter(points, objectCutComponent, target.pathOrigin);
            objectCutComponent.gameObject.AddComponent<Rigidbody2D>().AddForce((CalculatorPoints.IsPointOnLeftLine(pointBeginCollision, pointEndCollision, points[1]) == false ? 1 : -1) * (Vector2.Perpendicular(pointEndCollision - pointBeginCollision)).normalized * forceCut, ForceMode2D.Impulse);
        }
        //---------------------------------------------
    }
    public bool DeletePointRedundant(List<Vector2> points)
    {
        for (int i = 0; i < points.Count - 2; i++)
        {
            if (Vector2.Distance(points[i], points[i + 1]) <= 0.05f && points.Count > 3)
            {
                points.RemoveAt(i + 1);
                i--;
            }
        }
        Vector2 p1;
        Vector2 p2;
        for (int i = 0; i < points.Count && points.Count > 2; i++)
        {
            p1 = points[i == 0 ? points.Count - 1 : i - 1];
            p2 = points[(i + 1) % points.Count];
            if (!CalculatorPoints.CanFormTriangle(points[i], p1, p2))
            {
                if (points.Count == 3)
                {
                    Debug.LogWarning("GameObject has many combo tree points straight. ReCut!");
                    return false;
                }
                points.RemoveAt(i);
                i--;
            }
        }
        return true;
    }
    public void EnterObject()
    {
        if (!cutting || !cutContinous) return;
        beginCutPos = InputManager.Instance.mousePoistion;

    }
    public void ExitObject(ObjectCanBeCut target)
    {
        if (!cutContinous || beginCutPos == Vector2.zero || !cutting) return;
        this.target = target;
        endCutPos = InputManager.Instance.mousePoistion;
        Vector2 direcion = endCutPos - beginCutPos;
        beginCutPos -= direcion;
        endCutPos += direcion;
        StandardizedCutPoints();
        FindPointBeginCollision();
        FindPointEndCollision();
        foreach (var item in targets)
        {
            if (item == this.target)
            {
                pointBeginCollision = pointsCollision[targets.IndexOf(item)][0];
                pointEndCollision = pointsCollision[targets.IndexOf(item)][1];
                CutGameObject();
                break;
            }
        }
        ResetCutPos();
    }

    [SerializeField] private GameObject sword;
    private void Update()
    {
        sword.transform.position = InputManager.Instance.mousePoistion;
    }
    private void OnDrawGizmos()
    {
        if (beginCutPos != Vector2.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(beginCutPos, InputManager.Instance.mousePoistion);
        }
    }
}
