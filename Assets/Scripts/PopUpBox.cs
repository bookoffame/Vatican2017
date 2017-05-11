using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// A popup box used to get input from the user.
/// </summary>
public class PopUpBox : MonoBehaviour {
	/// <summary>
	/// The input field to get input from.
	/// </summary>
	public InputField input;

	/// <summary>
	/// The input from the user.
	/// </summary>
	private string myText;

	/// <summary>
	/// Did the user input something?
	/// </summary>
	private bool done;


	/// <summary>
	/// Display the popup window.
	/// </summary>
	public IEnumerator PopUp(){
		done = false;
		myText = "";
		yield return new WaitUntil(() => done);
	}

	/// <summary>
	/// Gets the text inputed by the user.
	/// </summary>
	/// <returns>The text inputed by the user.</returns>
	public string getText(){
		return myText;
	}

	/// <summary>
	/// The action to perform when the user submits their input.
	/// </summary>
	public void Submit(){
		myText = input.text;
		done = true;
	}

	/// <summary>
	/// The action to perform when the user cancels their input.
	/// </summary>
	public void Cancel(){
		myText = "";
		done = true;
	}

	/// <summary>
	/// Reset this instance.
	/// </summary>
	public void reset(){
		done = false;
		input.text = "";
	}


}
