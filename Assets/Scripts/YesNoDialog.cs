using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class YesNoDialog : MonoBehaviour
{
	static public YesNoDialog dialog;

	public Text text;
	private bool isOpen;
	private bool choice;

	// Use this for initialization
	void  OnLevelWasLoaded ()
	{
		if (dialog != null)
			dialog = this;
	}

	static public IEnumerator ShowDialog(string info){
		dialog.isOpen = true;
		dialog.text.text = info;
		dialog.gameObject.SetActive (true);
		yield return new WaitWhile (() => dialog.isOpen);
		dialog.gameObject.SetActive (false);
	}

	public void Yes()
	{
		choice = true;
		isOpen = false;
	}

	public void No()
	{
		choice = false;
		isOpen = false;
	}

	public static bool Choice(){
		return dialog.choice;
	}
}

