using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LetterInInventory : MonoBehaviour
{
	public Image img;
	public Color NORMAL = Color.white;
	public Color HIGHLIGHTED = Color.yellow;
	public bool selected;

	private Button button;

	public int position;

	void Start(){
		button = gameObject.GetComponent<Button> ();
	}
	void Update()
	{
		button.enabled = !selected;	
	}

	public void ButtonPressed()
	{
		selected = true;
		InventoryBox.current.ClickedSpot (position);
	}
}

