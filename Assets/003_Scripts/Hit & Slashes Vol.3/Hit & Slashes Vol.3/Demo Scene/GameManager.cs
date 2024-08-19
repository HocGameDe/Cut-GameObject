using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour 
{
	public static GameManager Instance;
	public TextMesh text_fx_name;
	public GameObject[] fx_prefabs;
	public int index_fx = 0;
	private void Awake()
	{
		Instance = this;
	}
	void Start () 
	{
		text_fx_name.text = "[" + (index_fx + 1) + "] " + fx_prefabs[ index_fx ].name;
	}

	void Update () 
	{
		if ( Input.GetKeyDown("z") || Input.GetKeyDown("left") ){
			GameObject.Find("UI-arrow-left").SendMessage("Go");
			index_fx--;
			if(index_fx <= -1)
				index_fx = fx_prefabs.Length - 1;
			text_fx_name.text = "[" + (index_fx + 1) + "] " + fx_prefabs[ index_fx ].name;	
		}

		if ( Input.GetKeyDown("x") || Input.GetKeyDown("right")){
			GameObject.Find("UI-arrow-right").SendMessage("Go");
			index_fx++;
			if(index_fx >= fx_prefabs.Length)
				index_fx = 0;
			text_fx_name.text = "[" + (index_fx + 1) + "] " + fx_prefabs[ index_fx ].name;
		}

		if ( Input.GetKeyDown("space") ){
			Instantiate(fx_prefabs[ index_fx ], new Vector3(0, 0, 0), Quaternion.identity);	
		}
	}
	public void SpawnCutVfx(Vector2 pos)
	{
        Instantiate(fx_prefabs[index_fx],pos, Quaternion.identity);
        Instantiate(fx_prefabs[index_fx+1],pos, Quaternion.identity);
        Instantiate(fx_prefabs[index_fx+2],pos, Quaternion.identity);
    }
}
