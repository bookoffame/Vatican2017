using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// The transcription lens movement logic.
/// </summary>
public class MoveTranscriptionLens : MonoBehaviour {
	/// <summary>
	/// The image of the lens.
	/// </summary>
	public Image lensImg;

	/// <summary>
	/// The image of the mask.
	/// </summary>
	public Image maskImg;

	void Update () {
		lensImg.enabled = ButtonControls.current.getSelected () == ButtonControls.LENS_TOOL;
		maskImg.enabled = ButtonControls.current.getSelected () == ButtonControls.LENS_TOOL;
		transform.GetChild (0).gameObject.SetActive (ButtonControls.current.getSelected () == ButtonControls.LENS_TOOL);
		transform.position = Input.mousePosition;
	}
}
