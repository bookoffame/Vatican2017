using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventoryBox : MonoBehaviour
{
	public static InventoryBox current;

	public LetterInInventory[] items;
	public Text text;
	public MinigameControl minigameControl;
	private char[] itemValues;
	private int itemCount, selected;

	void  Start (){
		if (current == null)
			current = this;
		itemCount = 0;
		itemValues = new char[items.Length];
		OnClose ();
	}
	/// <summary>
	/// Makes the InventoryBox appear.
	/// </summary>
	public void Show(){
		gameObject.SetActive (true);
	}

	/// <summary>
	/// Action to perform when closed is clicked.
	/// </summary>
	public void OnClose ()
	{
		text.text = "Hmm? You wish to consult your captured letters?";
		if (selected != -1)
		    items [selected].selected = false;
		selected = -1;
		gameObject.SetActive (false);
	}

	public void Add(char item){
		items [itemCount].img.sprite = getLetterSprite (item);
		itemValues [itemCount] = item;
		itemCount++;

		if (itemCount == 1) {
			text.text = "Nice catch! We'll just fit this little guy in right here for now. And because I'm such a nice bird, I'm going to keep track of all the letters you collect in your inventory.";
			Show ();
		} else if (itemCount == 10) {
			text.text = "Way to go! That's all of them.";
			Show ();
		}
	}

	public string GetWord(){
		string output = "";
		for (int i = 0; i < itemValues.Length - 1; i++)
			output += itemValues[i].ToString().ToUpper() + '-';
		output += itemValues [itemValues.Length - 1].ToString ().ToUpper ();
		return output;
	}

	public void ClickedSpot(int i){
		if (i >= itemCount)
			return;
		if (selected == -1) {
			selected = i;
		} else {
			Sprite tempS = items[selected].img.sprite;
			char tempC = itemValues[selected];

			items [selected].img.sprite = items [i].img.sprite;
			itemValues [selected] = itemValues [i];

			items [i].img.sprite = tempS;
			itemValues [i] = tempC;

			items [selected].selected = false;
			items [i].selected = false;
			selected = -1;
		}
	}

	private Sprite getLetterSprite(char letter){
		return Resources.Load<Sprite> ("Letters/" + letter);
	}
}

