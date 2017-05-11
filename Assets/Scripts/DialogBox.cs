using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// A Dialog Box used to display text to the user.
/// </summary>
public class DialogBox : MonoBehaviour {
	/// <summary>
	/// My text to display to the user.
	/// </summary>
	public Text myText;

	/// <summary>
	/// Show the specified text.
	/// </summary>
	/// <param name="text">The text to display.</param>
	public void Show(string text){
		myText.text = text;
		gameObject.SetActive (true);
	}

	/// <summary>
	/// Action to perform when okay is clicked.
	/// </summary>
	public void OnOkay(){
		gameObject.SetActive (false);
	}
}
