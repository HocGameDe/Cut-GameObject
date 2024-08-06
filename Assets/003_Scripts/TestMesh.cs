using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(MeshFilter))]
public class TestMesh : MonoBehaviour
{
    [SerializeField] MeshFilter meshFilter;
    Vector3[] vertices;
    Vector2[] uv;
    int[] triangles;
    Mesh mesh;
    [SerializeField] Vector2 uv0;
    [SerializeField] Vector2 uv1;
    [SerializeField] Vector2 uv2;
    [SerializeField] Vector2 uv3;

    [SerializeField] Vector3 vertices0 = new Vector3(0, 0);
    [SerializeField] Vector3 vertices1 = new Vector3(0, 10);
    [SerializeField] Vector3 vertices2 = new Vector3(10, 0);
    [SerializeField] Vector3 vertices3 = new Vector3(10, 10);

    [SerializeField] int triangles0 = 0;
    [SerializeField] int triangles1 = 1;
    [SerializeField] int triangles2 = 2;
    [SerializeField] int triangles3 = 2;
    [SerializeField] int triangles4 = 1;
    [SerializeField] int triangles5 = 3;
    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }
    private void Start()
    {
        mesh = new Mesh();
        vertices = new Vector3[4];
        uv = new Vector2[4];
        triangles = new int[2 * 3];
    }
    private void ChangeMesh()
    {
       
        vertices[0] = vertices0;
        vertices[1] = vertices1;
        vertices[2] = vertices2;
        vertices[3] = vertices3;

        triangles[0] = triangles0;
        triangles[1] = triangles1;
        triangles[2] = triangles2;
        triangles[3] = triangles3;
        triangles[4] = triangles4;
        triangles[5] = triangles5;

        uv[0] = uv0;
        uv[1] = uv1;
        uv[2] = uv2;
        uv[3] = uv3;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        meshFilter.mesh = mesh;

    }
    private void Update()
    {
        ChangeMesh();
    }
}
