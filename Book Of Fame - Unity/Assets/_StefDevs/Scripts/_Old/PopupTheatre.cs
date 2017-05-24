using UnityEngine;
using System.Collections;

public class PopupTheatre : MonoBehaviour {
	public GameObject otherStuff;
	public GameObject theatre;
	public GameObject myCamera,light;
	public Move cameraControls;
	public Transform normalPos, popupPos, popupCameraPos;
	public Popup[] popups;
	public Animator animator;
	public ParticleSystem smoke;
	private Vector3 oldCameraPos = Vector3.zero;
	private Quaternion oldCameraRot = Quaternion.identity;

	public IEnumerator SetupPopupTheatre(){
		otherStuff.SetActive (false);
		light.SetActive (true);
		if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
			yield return new WaitWhile (() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f);
		StartCoroutine(MyUtils.SmoothMove (transform, normalPos, popupPos, 5f));
		oldCameraPos = myCamera.transform.position;
		oldCameraRot = myCamera.transform.rotation;
		cameraControls.setActivated (false);
		StartCoroutine(MyUtils.SmoothMove (myCamera.transform, popupCameraPos, 5f));

		yield return StartCoroutine(PopupSprites());
	}

	public IEnumerator LeavePopupTheatre(){
		foreach (Popup p in popups)
			p.Reset ();
		light.SetActive (false);
		otherStuff.SetActive (true);
		theatre.SetActive (false);
		cameraControls.setActivated (true);
		StartCoroutine (MyUtils.SmoothMove (transform, popupPos, normalPos, 5f));
		yield return StartCoroutine (MyUtils.SmoothMove (myCamera.transform, oldCameraPos, oldCameraRot, 5f));
	}

	private IEnumerator PopupSprites()
	{
		yield return new WaitForSeconds (4f);
		smoke.Play ();
		yield return new WaitForSeconds (1f);
		theatre.SetActive (true);
		foreach (Popup p in popups)
			p.PopupObject ();
	}
		
	void Update()
	{
		if (Input.GetKeyDown (KeyCode.P))
			SetupPopupTheatre ();
		else if (Input.GetKeyDown(KeyCode.Q))
		    LeavePopupTheatre();
	}
}
