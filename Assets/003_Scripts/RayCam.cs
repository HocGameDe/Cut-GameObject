using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCam : MonoBehaviour
{
    [SerializeField] float Speed=1;
    Coroutine m_Coroutine;

    private void Start()
    {
        InputManager.Instance.SubEventInput(EventInputCategory.MouseDownLeft, MoveCamera);
    }
    void MoveCamera()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.Raycast(ray.origin, ray.direction);
        //var hit = Physics2D.Raycast(InputManager.Instance.mousePoistion, Vector2.down,0.01f);
        if(hit.collider!=null)
        {
            Debug.Log(hit.transform.name);            
            Vector3 newPos = hit.transform.position;
            newPos.z=Camera.main.transform.position.z;
            if (m_Coroutine != null) StopCoroutine(m_Coroutine);
            m_Coroutine = StartCoroutine(IMoveCamera(newPos));
        }
        else Debug.Log("Null");
    }
    IEnumerator IMoveCamera(Vector3 pos)
    {
        while(!Camera.main.transform.position.Equals(pos))
        {
           Camera.main.transform.position = Vector3.MoveTowards(Camera.main.transform.position,pos, Time.deltaTime*Speed);
            yield return null;
        }
    }
}
