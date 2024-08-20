using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
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

    private Vector2[][] paths;
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
        paths = new Vector2[pathCount][];
        listPaths.Clear();
        for (int i = 0; i < pathCount; i++)
        {
            paths[i] = target.polygonCollider2D.GetPath(i);
            listPaths.Add(paths[i].ToList());
        }
    }

    private void SplitPolygon(List<Vector2> path)
    {
        if (DeletePointRedundant(path) == false) return;
        List<List<Vector2>> listPointsSplit;
        listPointsSplit = SplitPath(path);
        if (listPointsSplit.Count <= 1)
        {
            Debug.LogWarning("Path Polygon GameObject Split count <=1!");
            return;
        }
        StandardizedHeadTail(listPointsSplit);
        if (listPointsSplit.Count <= 1)
        {
            Debug.LogWarning("Path Polygon GameObject Split count <=1 After StandradHeadTail!");
            return;
        }
        InsertCutPointIntoListPointsSplit(listPointsSplit,pointBeginCollision, pointEndCollision);
        int indexListPathsSplit = 0;
        int indexMain = 0;
        if (listPointsSplit.Count > 2)
        {
            indexMain = FindMainPointsIndex(listPointsSplit);
            MergePointsClockwiseIntoMainPoints(listPointsSplit, indexMain);
        }
        SpawnNewSlices(listPointsSplit);

        //Method in Method-----------------------------
        List<List<Vector2>> SplitPath(List<Vector2> path)
        {
            List<List<Vector2>> listPointsSplit = new List<List<Vector2>>();
            indexListPathsSplit = 0;
            listPointsSplit.Add(new List<Vector2>());
            for (int i = 0; i < path.Count - 1; i++)
            {
                listPointsSplit[indexListPathsSplit].Add(path[i]);
                bool IsValueOnLeftLine_i_Diffrent_IsValueOnLeftLine_i1 = CalculatorPoints.IsValueOnLeftLine(pointBeginCollision, pointEndCollision, path[i]) != CalculatorPoints.IsValueOnLeftLine(pointBeginCollision, pointEndCollision, path[i + 1]);
                bool IsNextPointOutCutLine = CalculatorPoints.CanFormTriangle(path[i + 1], pointBeginCollision, pointEndCollision);
                if (IsValueOnLeftLine_i_Diffrent_IsValueOnLeftLine_i1 && IsNextPointOutCutLine)
                {
                    listPointsSplit.Add(new List<Vector2>());
                    indexListPathsSplit++;
                }
            }
            listPointsSplit[indexListPathsSplit].Add(path.Last());
            return listPointsSplit;
        }
        void StandardizedHeadTail(List<List<Vector2>> listPointsSplit)
        {
            if (CalculatorPoints.IsValueOnLeftLine(pointBeginCollision, pointEndCollision, listPointsSplit.First()[0])
                == CalculatorPoints.IsValueOnLeftLine(pointBeginCollision, pointEndCollision, listPointsSplit.Last()[0]))
            {
                listPointsSplit.First().InsertRange(0, listPointsSplit.Last());
                listPointsSplit.Remove(listPointsSplit.Last());
            }
        }
        void InsertCutPointIntoListPointsSplit(List<List<Vector2>> listPointsSplit,Vector2 pointBeginCollision, Vector2 pointEndCollision)
        {
            foreach (var points in listPointsSplit)
            {
                int indexPointFirst = path.IndexOf(points.First());
                int indexPointLast = path.IndexOf(points.Last());
                Vector2? pointInsertFirst = CalculatorPoints.GetIntersectionOfLines(path[indexPointFirst], path[indexPointFirst == 0 ? path.Count - 1 : indexPointFirst - 1], pointBeginCollision, pointEndCollision);
                Vector2? pointInsertLast = CalculatorPoints.GetIntersectionOfLines(path[indexPointLast], path[indexPointLast == path.Count - 1 ? 0 : indexPointLast + 1], pointBeginCollision, pointEndCollision);
                if (pointInsertFirst != null && pointInsertLast != null)
                {
                    points.Insert(0, (Vector2)pointInsertFirst);
                    points.Add((Vector2)pointInsertLast);
                }
                else Debug.LogError("GetIntersectionOfLines Error!");
            }
        }
        
        int FindMainPointsIndex(List<List<Vector2>> listPointsSplit)
        {
            var indexMain = 0;
            for (int i = 0; i < listPointsSplit.Count; i++)
            {
                if (CalculatorPoints.IsCounterClockwise(listPointsSplit[(i - 1 < 0) ? listPointsSplit.Count - 1 : (i - 1)]) == true
                    && CalculatorPoints.IsCounterClockwise(listPointsSplit[(i + 1) % listPointsSplit.Count]) == true
                    && CalculatorPoints.IsCounterClockwise(listPointsSplit[i]) == true) indexMain = i;
            }
            return indexMain;
        }
        void MergePointsClockwiseIntoMainPoints(List<List<Vector2>> listPointsSplit, int indexMain)
        {
            foreach (var points in listPointsSplit)
            {
                if (CalculatorPoints.IsCounterClockwise(points) == false)
                {
                    listPointsSplit[indexMain].AddRange(points);
                    listPointsSplit.Remove(points);
                }
            }
        }
        bool DeletePointRedundant(List<Vector2> points)
        {
            for (int i = 0; i < points.Count - 2; i++)
            {
                if (Vector2.Distance(points[i], points[i + 1]) <= 0.01f && points.Count > 3)
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
        void SpawnNewSlices(List<List<Vector2>> listPointsSplit)
        {
            if (listPointsSplit.Any(listPoints => !DeletePointRedundant(listPoints))) return;
            foreach (var listPoints in listPointsSplit)
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
                objectCutComponent.polygonCollider2D.SetPath(0, listPoints.ToArray());
                generateMesh.GenerateMeshFilter(listPoints, objectCutComponent, target.pathOrigin);
                objectCutComponent.gameObject.AddComponent<Rigidbody2D>().AddForce((CalculatorPoints.IsValueOnLeftLine(pointBeginCollision, pointEndCollision, listPoints[1]) == false ? 1 : -1) * (Vector2.Perpendicular(pointEndCollision - pointBeginCollision)).normalized * forceCut, ForceMode2D.Impulse);
            }
            target.StartAnimationCut(pointBeginCollision, pointEndCollision);
        }
        //---------------------------------------------
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
    private void OnDrawGizmos()
    {
        if (beginCutPos != Vector2.zero)
        {
            Gizmos.color = UnityEngine.Color.red;
            Gizmos.DrawLine(beginCutPos, InputManager.Instance.mousePoistion);
        }
    }
}
