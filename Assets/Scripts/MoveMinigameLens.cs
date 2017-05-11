using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MoveMinigameLens : MonoBehaviour
{
	/// <summary>
	/// The image of the lens.
	/// </summary>
	public Image lensImg;

	private Bug myBug;

	void Update () {
		lensImg.enabled = ButtonControls.current.getSelected () == ButtonControls.MINI_GAME_LENS_TOOL;
		transform.position = Input.mousePosition;
		Ray r = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit[] hits = Physics.RaycastAll (r);

		foreach (RaycastHit info in hits) {
			myBug = info.collider.gameObject.GetComponent<Bug> ();
			if (myBug) {
				myBug.Show ();
			}
		}
	}
}

