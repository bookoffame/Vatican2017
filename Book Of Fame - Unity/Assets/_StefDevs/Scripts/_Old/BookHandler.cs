using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the opening and closing of the book.
/// </summary>
public class BookHandler : MonoBehaviour {
	/// <summary>
	/// The animator of the book.
	/// </summary>
	public Animator animator;

	/// <summary>
	/// The pages of the book.
	/// </summary>
	public GameObject[] pages;

	/// <summary>
	/// The Colliders for the front and back covers of the book.
	/// </summary>
	public Collider[] models;

	/// <summary>
	/// The Toolbar UI.
	/// </summary>
	public UIPopUp myUI;

	public AudioSource sound;

	void Update () {
		RaycastHit hit;
		bool isHit = false;
		if (ButtonControls.current.getSelected() == ButtonControls.SELECTION_TOOL && myUI.IsShowing()) {
			foreach (Collider model in models) {
				if (!isHit && model.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit, 1000)) {
					if (Input.GetMouseButtonDown (0)) {
						animator.SetTrigger ("Grabbed");
						sound.Play ();
						ButtonControls.current.changeSelected (ButtonControls.HAND_TOOL);
					}
				}
			}
		}

		if (animator.GetCurrentAnimatorStateInfo (0).IsName ("Opened")) {
			foreach (GameObject page in pages)
			    page.SetActive (true);
		}

		else if (animator.GetCurrentAnimatorStateInfo (0).IsName ("CloseState")) {
			foreach (GameObject page in pages)
				page.SetActive (false);
		}
	}
}
