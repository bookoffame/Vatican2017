using UnityEngine;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
	public GameObject world;
	public BookHandler book;
	public AnnotationDrawer[] drawers;
	public ImageBufferer buffer;
	
	// Update is called once per frame
	void Update ()
	{
		if (buffer.IsLoaded ()) {
			Debug.Log ("Loaded");
			this.gameObject.SetActive (false);
			world.SetActive (true);
			book.enabled = true;
			foreach (AnnotationDrawer d in drawers)
				d.enabled = true;
		}
	}
}

