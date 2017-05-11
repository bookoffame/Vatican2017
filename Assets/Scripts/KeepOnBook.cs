using UnityEngine;
using System.Collections;

public class KeepOnBook : MonoBehaviour {
	public Transform topLeft, bottomRight;
	public Vector2 location;

	private const float CAM_SCALE = 0.9f;
	private const float CAM_MIN_X = -2;
	private const float CAM_MAX_X = 2;
	private const float CAM_MIN_Y = -2f;
	private const float CAM_MAX_Y = 2f;
	private const float CAM_MIN_Z = -2f;
	private const float CAM_MAX_Z = 2.5f;

	// Update is called once per frame
	void Update () {
		Vector3 OFFSET = new Vector3 (-0.5f*Screen.width,-0.4f*Screen.height,0);
		Vector3 tl = Camera.main.WorldToViewportPoint (topLeft.position);
		Vector3 br = Camera.main.WorldToViewportPoint (bottomRight.position);
		Vector3 camPos = new Vector3 (
			(Camera.main.transform.localPosition.x - CAM_MIN_X) / (CAM_MAX_X - CAM_MIN_X),
			(Camera.main.transform.localPosition.y - CAM_MIN_Y) / (CAM_MAX_Y - CAM_MIN_Y),
			(Camera.main.transform.localPosition.z - CAM_MIN_Z) / (CAM_MAX_Z - CAM_MIN_Z));
		float width = tl.x - br.x;
		float height = tl.y - br.y;
		float cd = camPos.z;
		float scale = 1f + cd*CAM_SCALE;

		transform.localPosition = new Vector3 (
			(tl.x*0.95f - (location.x*width)*0.95f)*Screen.width,
			(br.y*0.725f + (location.y - 0.1f)*height*0.725f)*Screen.height,
			 0) + OFFSET;
		//MIN: -0.01125 MAX: -0.01
		transform.localScale = new Vector3 (scale, scale, scale);
	}

}
