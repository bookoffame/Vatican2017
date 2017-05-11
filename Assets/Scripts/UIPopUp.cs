using UnityEngine;
using System.Collections;
/// <summary>
/// Hides/Shows UI elements.
/// </summary>
public class UIPopUp : MonoBehaviour {
	/// <summary>
	/// The vertical position the UI should move to for hiding.
	/// </summary>
	public float hideY;

	/// <summary>
	/// The vertical position the UI should move to for showing.
	/// </summary>
	public float showY;

	/// <summary>
	/// How low/high the cursor needs to be to show the UI (0.0f for bottom of the screen, 1.0f for top of the screen).
	/// </summary>
	public float triggerPos;

	/// <summary>
	/// The position of the UI.
	/// </summary>
	public RectTransform pos;

	/// <summary>
	/// Shoud the UI hide from the top of the screen, or the bottom?
	/// </summary>
	public bool hideAbove;

	private bool isHiding;
	// Update is called once per frame
	void Update () {
		if (((Screen.height - Input.mousePosition.y) / Screen.height < triggerPos) ^ hideAbove) {
			pos.localPosition = new Vector3 (pos.localPosition.x, Mathf.Lerp (pos.localPosition.y, hideY, 0.1f), pos.localPosition.z);
			isHiding = false;
		} else {
			pos.localPosition = new Vector3 (pos.localPosition.x, Mathf.Lerp (pos.localPosition.y, showY, 0.1f), pos.localPosition.z);
			isHiding = true;
		}
	}

	/// <summary>
	/// Determines whether this instance is showing.
	/// </summary>
	/// <returns><c>true</c> if this instance is showing; otherwise, <c>false</c>.</returns>
	public bool IsShowing(){
		return !isHiding;
	}
}
