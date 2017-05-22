using UnityEngine;
using System.Collections;

/// <summary>
/// Movement logic for the navigation mode camera
/// </summary>
public class MovePlayer : MonoBehaviour {
	/// <summary>
	/// The script that controls the player.
	/// </summary>
	public MonoBehaviour moveScript;

	/// <summary>
	/// Is the player in navigation mode?
	/// </summary>
	public bool inControl;

	void Start()
	{
		moveScript.enabled = inControl;
	}

	/// <summary>
	/// Enables/Disables movement.
	/// </summary>
	/// <param name="newControl">If set to <c>true</c> enables movement. Otherwise, disables movements.</param>
	public void ChangeControl(bool newControl){
		moveScript.enabled = newControl;
		inControl = newControl;
	}
}
