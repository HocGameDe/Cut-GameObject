using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PolygonCollider2D))]
public class ObjectCanBeCut : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    public List<Vector2> pathOrigin = new List<Vector2>();
    public PolygonCollider2D polygonCollider2D;
    public Material material;
    public Shader shader;
    public Texture2D texture2D;
    public float pixelsPerUnit;
    private void Awake()
    {
        polygonCollider2D = GetComponent<PolygonCollider2D>();
        if(TryGetComponent<SpriteRenderer>(out SpriteRenderer spriteRenderer))
        {
            pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;
        };
    }
    private void OnEnable()
    {      
        if (pathOrigin.Count <= 0) pathOrigin = polygonCollider2D.GetPath(0).ToList();
        if (material == null && shader != null)
        {
            material = new Material(shader);
            if (texture2D != null && material != null) material.SetTexture("_MainTex", texture2D);
        }
    }
    private void Start()
    {
        if (shader == null || texture2D == null)
        {
            Debug.LogError("Please add Shader and Texture for ObjectCanBecut of " + transform.name);
            return;
        }
    }
    public void StartAnimationCut(Vector2 pointBeginCollision, Vector2 pointEndCollision)
    {
        StartCoroutine(AnimationCut(pointBeginCollision, pointEndCollision));
    }
    IEnumerator AnimationCut(Vector2 pointBeginCollision, Vector2 pointEndCollision)
    {
        gameObject.SetActive(false);
        GameManager.Instance.SpawnCutVfx(transform.TransformPoint((pointEndCollision + pointBeginCollision) / 2));
        yield return null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Cutter.Instance.EnterObject();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cutter.Instance.ExitObject(this);
    }
}
