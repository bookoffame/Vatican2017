using UnityEngine;
using System.Collections;
using AssemblyCSharp;

/// <summary>
/// The movement logic for the book mode camera.
/// </summary>
public class Move : MonoBehaviour {
	/// <summary>
	/// The position of the book mode camera
	/// </summary>
	public Transform myTransform;

	/// <summary>
	/// The speed of the book mode camera.
	/// </summary>
	public float speed;

	public float maxX, minX;
	public float maxY, minY;
	public float maxZ, minZ;

	private bool on;

	void Update () {
		float nx, ny, nz;
		if (on) {
			nx = speed * Input.GetAxis ("Horizontal") + myTransform.localPosition.x;
			ny = speed * Input.GetAxis ("Vertical") + myTransform.localPosition.y;
			nz = speed * Input.GetAxis ("Forward") + myTransform.localPosition.z;

			nx = Mathf.Min (maxX, nx);
			nx = Mathf.Max (minX, nx);

			ny = Mathf.Min (maxY, ny);
			ny = Mathf.Max (minY, ny);

			nz = Mathf.Min (maxZ, nz);
			nz = Mathf.Max (minZ, nz);

			myTransform.localPosition = new Vector3 (nx, ny, nz);
		}
	}

	/// <summary>
	/// Enable/Disable movement.
	/// </summary>
	/// <param name="isActivated">If set to <c>true</c> enables movement. Else, disables movement.</param>
	public void setActivated(bool isActivated){
		on = isActivated;
	}
}
