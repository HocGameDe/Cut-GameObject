using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class ObjectCanBeCut : MonoBehaviour
{
    public List<Vector2> pathOrigin = new List<Vector2>();
    public PolygonCollider2D polygonCollider2D;
    public Material material;
    public Shader shader;
    public Texture2D texture2D;
    private void Awake()
    {
        polygonCollider2D = GetComponent<PolygonCollider2D>();
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
    public void StartAnimationCut()
    {
        StartCoroutine(AnimationCut());
    }
    IEnumerator AnimationCut()
    {
        gameObject.SetActive(false);
        yield return null;
    }
}
