using UnityEngine;
using System.Collections;

public class SetPortrait : MonoBehaviour {
	public Animator animator;
	private Transform active;

	// Use this for initialization
	void Start () {
		active = null;
	}

	public void SetImage(string name) {
		Transform toSet = this.transform.Find (name);
		if (toSet != null) {
			HideLast ();
			toSet.gameObject.SetActive (true);
			active = toSet;
		}
	}

	public void HideLast()
	{
		if (active != null) {
			active.gameObject.SetActive (false);
		}
	}

	public void PlayAnimation(string name, float length)
	{
		float clipLength = FindClipByName(name).length;
		animator.SetFloat("Speed", clipLength/length);
		animator.SetFloat ("Position", 0.0f);
		animator.SetTrigger (name);
	}

	public void PlayAnimationInReverse(string name, float length)
	{
		float clipLength = FindClipByName(name).length;
		animator.SetFloat("Speed", -clipLength/length);
		animator.SetFloat ("Position", clipLength);
		animator.SetTrigger (name);
	}

	private AnimationClip FindClipByName(string name)
	{
		foreach(AnimationClip clip in animator.runtimeAnimatorController.animationClips)
		{
			if (clip.name == name)
			{
				return clip;
			}
		}
		return null;
	}
}
