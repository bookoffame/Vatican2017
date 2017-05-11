using UnityEngine;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
	public GameObject world;
	public ImageBufferer buffer;
	
	// Update is called once per frame
	void Update ()
	{
		if (buffer.IsLoaded ()) {
			this.gameObject.SetActive (false);
			world.SetActive (true);
		}
	}
}

