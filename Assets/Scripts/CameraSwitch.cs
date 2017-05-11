using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections;

/// <summary>
/// Switches between the navigation and book mode cameras.
/// </summary>
public class CameraSwitch : MonoBehaviour {
	/// <summary>
	/// The player.
	/// </summary>
	public MovePlayer player;

	/// <summary>
	/// The book mode camera's movement logic.
	/// </summary>
	public Move book;

	/// <summary>
	/// The navigation mode camera.
	/// </summary>
	public Camera playerCam;

	/// <summary>
	/// The book mode camera.
	/// </summary>
	public Camera bookCam;

	/// <summary>
	/// The canvas.
	/// </summary>
	public GameObject canvas;

	/// <summary>
	/// The player's movement logic.
	/// </summary>
	public FirstPersonController fpc;

	void Start () {
		playerCam.enabled = true;
		bookCam.enabled = false;
		player.ChangeControl (true);
		book.setActivated (false);
		canvas.SetActive (false);
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (KeyCode.Space)) {
			playerCam.enabled = !playerCam.enabled;
			bookCam.enabled = !bookCam.enabled;
			player.ChangeControl (playerCam.enabled);
			book.setActivated (bookCam.enabled);
			canvas.SetActive (bookCam.enabled);
			fpc.m_MouseLook.SetCursorLock (!fpc.m_MouseLook.lockCursor);
		}
	}
}
