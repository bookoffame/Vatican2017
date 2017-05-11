using UnityEngine;
using System.Collections;

public class Popup : MonoBehaviour {
	public Vector3 angle = Vector3.zero;
	public void PopupObject(){
		StartCoroutine (MyUtils.SmoothMove (transform, transform.position, Quaternion.Euler (angle.x, angle.y, angle.z), 4f));
	}

	public void Reset(){
		transform.Rotate (-45f, 0f, 0f);
	}
}
