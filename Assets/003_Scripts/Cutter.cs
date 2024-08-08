using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[RequireComponent(typeof(GenerateMesh))]
public class Cutter : MonoBehaviour
{
    public static Cutter Instance;
    public bool cutContinous;
    private bool cutting;
    public GenerateMesh generateMesh;
    public ObjectCanBeCut target;
    public List<ObjectCanBeCut> targets;
    public LayerMask layerMaskCanCut;
    [SerializeField] private Vector2 beginCutPos;
    [SerializeField] private Vector2 endCutPos;
    [SerializeField] private Vector2 pointBeginCollision;
    [SerializeField] private Vector2 pointEndCollision;

    private Vector2[][] paths;
    private List<List<Vector2>> listPaths = new List<List<Vector2>>();
    private List<List<Vector2>> listPointsSplit = new List<List<Vector2>>();
    private RaycastHit2D hit;
    private RaycastHit2D[] hits;
    private List<List<Vector2>> pointsCollision;
    private int pathCount;
    private void Awake()
    {
        generateMesh = GetComponent<GenerateMesh>();
        Instance = this;
    }
    private void Start()
    {
        InputManager.Instance.SubEventInput(EventInputCategory.MouseDownLeft, SetBeginCutPos);
        InputManager.Instance.SubEventInput(EventInputCategory.MouseDownLeft, ()=>cutting=true);
        InputManager.Instance.SubEventInput(EventInputCategory.MouseUpLeft, Cut);
        InputManager.Instance.SubEventInput(EventInputCategory.MouseUpLeft, ()=>cutting = false);
    }
    public void Cut()
    {
        if (cutContinous)
        {
            ResetCutPos();
            return;
        }
        SetEndCutPos();
        if(Vector2.Distance(beginCutPos,endCutPos)<0.001f)
        {
            ResetCutPos();
            return;
        }
        StandardizedCutPoints();
        DrawRayCut();
        ResetCutPos();
    }
    public void EnterObject()
    {
        if (!cutting||!cutContinous) return;
        beginCutPos = InputManager.Instance.mousePoistion;

    }
    public void ExitObject(ObjectCanBeCut target)
    {
        if (!cutContinous||beginCutPos==Vector2.zero||!cutting) return;
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
        foreach (var path in listPaths)
        {
            if (path.Count > 2) SplitPath(path);
            else Debug.LogWarning("Polygon GameObject error!");
        }
        listPaths.Clear();
    }
    private void GetPolygon2DPaths()
    {
        pathCount = target.polygonCollider2D.pathCount;
        paths = new Vector2[pathCount][];
        listPaths.Clear();
        for (int i = 0; i < pathCount; i++)
        {
            paths[i] = target.polygonCollider2D.GetPath(i);
            listPaths.Add(paths[i].ToList());
        }
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
            Debug.LogWarning("Path Polygon GameObject Split count <=1!");
            return;
        }
        StandardizedHeadTail();
        if (listPointsSplit.Count <= 1)
        {
            Debug.LogWarning("Path Polygon GameObject Split count <=1 After StandradHeadTail!");
            return;
        }
        InsertCutPointIntoListPointsSplit();
        if (listPointsSplit.Count > 2)
        {
            FindMainPointsIndex();
            MergePointsClockwiseIntoMainPoints();
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
                if (CalculatorCutSpriteRenderer.IsValueOnLeftLine(pointBeginCollision, pointEndCollision, path[i]) != CalculatorCutSpriteRenderer.IsValueOnLeftLine(pointBeginCollision, pointEndCollision, path[i + 1]))
                {
                    listPointsSplit.Add(new List<Vector2>());
                    indexListPathsSplit++;
                }
            }
            listPointsSplit[indexListPathsSplit].Add(path.Last());
        }
        void StandardizedHeadTail()
        {
            if (CalculatorCutSpriteRenderer.IsValueOnLeftLine(pointBeginCollision, pointEndCollision, listPointsSplit.First()[0])
                == CalculatorCutSpriteRenderer.IsValueOnLeftLine(pointBeginCollision, pointEndCollision, listPointsSplit.Last()[0]))
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
                if (CalculatorCutSpriteRenderer.IsNotClockwise(listPointsSplit[(i - 1 < 0) ? listPointsSplit.Count - 1 : (i - 1)]) == true
                    && CalculatorCutSpriteRenderer.IsNotClockwise(listPointsSplit[(i + 1) % listPointsSplit.Count]) == true
                    && CalculatorCutSpriteRenderer.IsNotClockwise(listPointsSplit[i]) == true)
                {
                    indexMain = i;
                }

                if (CalculatorCutSpriteRenderer.IsNotClockwise(listPointsSplit[i]) == false)
                {
                    indexRemove.Add(i);
                }
            }
        }
        void InsertCutPointIntoListPointsSplit()
        {
            foreach (var points in listPointsSplit)
            {
                int indexPointFirst = path.IndexOf(points.First());
                int indexPointLast = path.IndexOf(points.Last());
                Vector2 pointInsertFirst = CalculatorCutSpriteRenderer.GetIntersectionPointBetweenTwoLines(path[indexPointFirst], path[indexPointFirst == 0 ? path.Count - 1 : indexPointFirst - 1], pointBeginCollision, pointEndCollision);
                Vector2 pointInsertLast = CalculatorCutSpriteRenderer.GetIntersectionPointBetweenTwoLines(path[indexPointLast], path[indexPointLast == path.Count - 1 ? 0 : indexPointLast + 1], pointBeginCollision, pointEndCollision);
                points.Insert(0, pointInsertFirst);
                points.Add(pointInsertLast);
            }
        }
        void MergePointsClockwiseIntoMainPoints()
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
        bool DeletePointRedundant(List<Vector2> points)
        {
            if (points.Count < 3)
            {
                Debug.LogError("DeletePointRedundant error: Points count <3!");
                return false;
            }
            Vector2 p1;
            Vector2 p2;
            for (int i = 0; i < points.Count; i++)
            {
                p1 = points[i == 0 ? points.Count - 1 : i - 1];
                p2 = points[(i + 1) % points.Count];
                if (!CalculatorCutSpriteRenderer.CanFormTriangle(points[i], p1, p2))
                {
                    if (points.Count == 3)
                    {
                        Debug.LogWarning("GameObject have tree ponits straight. ReCut!");
                        return false;
                    }
                    points.RemoveAt(i);
                    i--;
                }
            }
            return true;
        }
        void SpawnNewSlices()
        {
            foreach (var listPoints in listPointsSplit)
            {
                if (!DeletePointRedundant(listPoints)) return;
                GameObject newDog = new GameObject();
                newDog.layer = target.gameObject.layer;
                newDog.transform.position = target.transform.position;
                newDog.transform.rotation = target.transform.rotation;
                newDog.transform.localScale = target.transform.localScale;
                ObjectCanBeCut dogComponent = newDog.AddComponent<ObjectCanBeCut>();
                dogComponent.pathOrigin = target.pathOrigin;
                dogComponent.material = target.material;
                dogComponent.shader = target.shader;
                dogComponent.texture2D = target.texture2D;
                dogComponent.polygonCollider2D.SetPath(0, listPoints.ToArray());
                generateMesh.GenerateMeshFilter(listPoints, newDog, target.pathOrigin);
                dogComponent.gameObject.AddComponent<Rigidbody2D>().AddForce((CalculatorCutSpriteRenderer.IsValueOnLeftLine(pointBeginCollision, pointEndCollision, listPoints[1]) == false ? 1 : -1) * (Vector2.Perpendicular(pointEndCollision - pointBeginCollision)).normalized * 3.5f, ForceMode2D.Impulse);
            }
            target.StartAnimationCut();
        }

        //---------------------------------------------
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.red;
        Gizmos.DrawLine(beginCutPos, endCutPos);
    }
}
