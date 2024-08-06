using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dog : MonoBehaviour
{
    public static Dog Instance;
    public PolygonCollider2D polygonCollider2D;
    private void Awake()
    {
        polygonCollider2D = GetComponent<PolygonCollider2D>();
        Instance = this;
    }
}
