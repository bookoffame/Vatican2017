using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Bug : MonoBehaviour
{
    public SpriteRenderer image;
	public Color offColor, onColor;
	public float duration;
	private bool isFlashing,isShowing,isFlashGettingBrighter;
	private float time;

	void Start()
	{
		isShowing = false;
		isFlashing = false;
		isFlashGettingBrighter = false;
	}
	// Update is called once per frame
	void Update ()
	{
		isShowing = isFlashing;
		if (isShowing) {
			image.color = Color.Lerp (offColor, onColor, time/duration);
			if (isFlashGettingBrighter) {
				time += Time.deltaTime;
				if (time >= duration) {
					time = duration;
					isFlashGettingBrighter = false;
				}
			} else {
				time -= Time.deltaTime;
				if (time <= 0) {
					time = 0;
					isFlashGettingBrighter = true;
				}
			}
		}
		else
		{
			image.color = offColor;
		}
		Hide ();
	}


	public void Show(){
		isFlashing = true;
	}

	public void Hide(){
		isFlashing = false;
	}

	public bool IsShowing(){
		return isShowing;
	}

	void OnMouseExit()
	{
		Hide ();
	}
		
}

