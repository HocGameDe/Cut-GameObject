using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Dog : MonoBehaviour
{
    public PolygonCollider2D polygonCollider2D;
    public List<Vector2> pathOrigin = new List<Vector2>();
    public Material material;
    //public Shader shader;
    //public Texture2D texture2D;
    private void Awake()
    {
        polygonCollider2D = GetComponent<PolygonCollider2D>();
        
        //material = new Material(shader);
        //material.SetTexture("_MainTex", texture2D);
    }
    private void OnEnable()
    {
        if(pathOrigin.Count<=0) pathOrigin = polygonCollider2D.GetPath(0).ToList();
    }
    public void StartAnimationCut()
    {
        gameObject.SetActive(false);
        //StartCoroutine(AnimationCut());
    }
    IEnumerator AnimationCut()
    {
        polygonCollider2D.enabled = false;
        yield return new WaitForSeconds(1f);
        polygonCollider2D.enabled = true;
    }
}
