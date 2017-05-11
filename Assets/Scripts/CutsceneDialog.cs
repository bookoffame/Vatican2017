using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CutsceneDialog : MonoBehaviour {

	public SetPortrait leftPortrait,rightPortrait;
	public Image scroll;
	public Text text;
	private const int MAX_DIALOG_LENGTH = 250;

	public IEnumerator ShowDialog(string character, string dialog, string animation){
		if (animation.ToLower ().Contains ("Default")) {
			return ShowDialog (character, dialog, animation, false, false);
		} else if (animation.ToLower ().Contains ("!")) {
			return ShowDialog (character, dialog, animation, false, true);
		} else {
			return ShowDialog (character, dialog, animation, true, false);
		}

	}

	public IEnumerator ShowDialog(string character, string dialog, string animation, bool animateBefore, bool animateAfter){
		SetPortrait portrait = (animation.ToLower ().Contains ("left")) ? leftPortrait : rightPortrait;
		portrait.HideLast ();
		portrait.gameObject.SetActive (true);

		if (animateBefore) {
			portrait.PlayAnimation (GetAnimationName (animation), 1.0f);
			yield return new WaitForSeconds (0.05f);
			portrait.SetImage (character);
		} else
		{
			portrait.SetImage (character);
		}

		scroll.gameObject.SetActive (true);
		text.gameObject.SetActive (true);
		dialog = dialog.Replace ('\n', ' ');

		for (int i = 0, end = GetEnd(dialog, i); i < dialog.Length; i = end, end = GetEnd(dialog, i)) {
			text.text = dialog.Substring (i, end - i);
			yield return new WaitUntil (() => Input.GetMouseButtonDown(0));
			yield return new WaitForSeconds (0.1f);
		}
		if (animateAfter)
		{
			scroll.gameObject.SetActive (false);
			text.gameObject.SetActive (false);
			portrait.PlayAnimationInReverse (GetAnimationName(animation), 1.0f);
		}

	}

	public void EndCutscene()
	{
		leftPortrait.gameObject.SetActive(false);
		rightPortrait.gameObject.SetActive(false);
		scroll.gameObject.SetActive (false);
		text.gameObject.SetActive (false);
	}

	private int GetEnd(string dialog, int index){
		int lastWordEnd = index;
		int lastLastWordEnd = lastWordEnd;

		while (lastWordEnd - index < MAX_DIALOG_LENGTH && lastWordEnd < dialog.Length) {
			lastLastWordEnd = lastWordEnd;
			lastWordEnd = dialog.IndexOf (' ', lastWordEnd + 1);
			if (lastWordEnd == -1) {
				return dialog.Length;
			}
		}
		return lastLastWordEnd;
	}

	private string GetAnimationName(string animation)
	{
		string normalizedName = animation.Trim ().ToLower ();
		switch(normalizedName)
		{
		case "default left":
			return PortraitAnimation.DEFAULT_LEFT;

		case "pop up from left":
			return PortraitAnimation.POP_UP_LEFT;

		case "move in from left": case "come from left": case "enter from left":
			return PortraitAnimation.COME_FROM_LEFT;

		case "default right":
			return PortraitAnimation.DEFAULT_RIGHT;

		case "pop up from right":
			return PortraitAnimation.POP_UP_RIGHT;

		case "move in from right":  case "come from right": case "enter from right":
			return PortraitAnimation.COME_FROM_RIGHT;

		default:
			return null;
		}
	}
}
