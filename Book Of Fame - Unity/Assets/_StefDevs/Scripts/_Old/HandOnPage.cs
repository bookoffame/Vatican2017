using UnityEngine;
using System.Collections;

/// <summary>
/// Controls page movement animation
/// </summary>
public class HandOnPage : MonoBehaviour {
	/// <summary>
	/// The animator of the page.
	/// </summary>
	public Animator animator;

	/// <summary>
	/// The Collider of the page.
	/// </summary>
	public Collider page;

	/// <summary>
	/// The presenter of the IIIF images.
	/// </summary>
	public PageImages pageImages;

	/// <summary>
	/// The width of the page.
	/// </summary>
	public float pageWidth;

	/// <summary>
	/// Is this the right page?
	/// </summary>
	public bool isRight;

	/// <summary>
	/// Pages to hide when this page is over them
	/// </summary>
	public Renderer[] others;

	public AudioSource sound;
	private float lastPos;
	private bool released,canLoadPages;

	void Start()
	{
		lastPos = 0;
		released = false;
		canLoadPages = true;
	}

	// Update is called once per frame
	void Update () {
		bool hand = ButtonControls.current.getSelected () == ButtonControls.HAND_TOOL;
		if (isPageTurning ()) {
			float movement = Screen.width * ((Input.mousePosition.x - lastPos) / pageWidth) * 2;
			lastPos = Input.mousePosition.x;
			if (isRight)
				movement = -movement;
			if (!released)
				animator.SetFloat ("HandMovement", movement);
		} 
		RaycastHit hit;
			
		if (hand && Input.GetMouseButtonDown (0) &&
			animator.GetCurrentAnimatorStateInfo (0).IsName ("Opened")) {
			if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit, Mathf.Infinity, 1 << 8)) {
				if (hit.collider.Equals (page)) { 
					if (isRight)
						animator.SetTrigger ("TurnLeft");
					else
						animator.SetTrigger ("TurnRight");
					released = false;
					canLoadPages = true;
				}
			}
		} else if (Input.GetMouseButtonUp (0) && isPageTurning()) {
			released = true;
			sound.Play ();
			if (animator.GetCurrentAnimatorStateInfo (0).normalizedTime < 0.5)
				animator.SetFloat ("HandMovement", -5);
			else
				animator.SetFloat ("HandMovement", 5);
		} else if (!Input.GetMouseButton (0) && isPageTurning()) {
			released = true;
			if (animator.GetCurrentAnimatorStateInfo (0).normalizedTime > 0.95 && canLoadPages) {
				loadPages ();
				animator.SetTrigger ("Released");
				canLoadPages = false;
			} else if (animator.GetCurrentAnimatorStateInfo (0).normalizedTime < 0.05) {
				animator.SetTrigger ("Released");
			}
		}
	}

	private void loadPages(){
		foreach (Renderer r in others)
			r.enabled = true;
		if (isRight) {
			StartCoroutine (pageImages.TurnPageLeft ());
		} else {
			StartCoroutine (pageImages.TurnPageRight ());
		}
	}

	private bool isPageTurning(){
		if (isRight)
			return animator.GetCurrentAnimatorStateInfo (0).IsName ("TurnPageLeft");
		else
			return animator.GetCurrentAnimatorStateInfo (0).IsName ("TurnPageRight");
	}

	//Code for pages
	/*if (!((pageImages.IsLoadingLeft () && isRight) || (pageImages.IsLoadingRight () && !isRight))) {
			lastPos = Input.mousePosition.x;

			if (animator.GetCurrentAnimatorStateInfo (0).IsName ("MovePages")) {
				if (animator.GetCurrentAnimatorStateInfo (0).normalizedTime < 0.7) {
					foreach (Renderer r in others)
						r.enabled = true;
				} else {
					foreach (Renderer r in others)
						r.enabled = false;
				}
			}
			if (hand && Input.GetMouseButtonDown (0) &&
			   animator.GetCurrentAnimatorStateInfo (0).IsName ("Default")) {
				if (page.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit, 1000)) {
					animator.SetTrigger ("Grabbed");
					released = false;
				}
			} else if (Input.GetMouseButtonUp (0) &&
			          animator.GetCurrentAnimatorStateInfo (0).IsName ("MovePages")) {
				released = true;
				if (animator.GetCurrentAnimatorStateInfo (0).normalizedTime < 0.35)
					animator.SetFloat ("HandMovement", -5);
				else
					animator.SetFloat ("HandMovement", 5);
			} else if (!Input.GetMouseButton (0) &&
			          animator.GetCurrentAnimatorStateInfo (0).IsName ("MovePages")) {
				if (animator.GetCurrentAnimatorStateInfo (0).normalizedTime > 0.95) {
					loadPages ();
					animator.SetTrigger ("Released");
				} else if (animator.GetCurrentAnimatorStateInfo (0).normalizedTime < 0.01) {
					animator.SetTrigger ("Released");
				}
			}*/
}
