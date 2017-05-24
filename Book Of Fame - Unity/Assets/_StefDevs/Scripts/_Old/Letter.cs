using UnityEngine;
using System.Collections;

public class Letter : MonoBehaviour {
	public Vector3 position;
	public char letter;

	private float scaredAmount;

	void Start(){
		scaredAmount = 0.0f;
	}

	void Update () {
		Vector2 dir = Random.insideUnitCircle;
		transform.localPosition = position + (new Vector3(dir.x,dir.y,0f))*scaredAmount*0.001f;
	}

	public void SetScaredAmount(float amount){
		scaredAmount = amount;
	}

	public void Collect(){
		InventoryBox.current.Add (letter);
		Destroy (gameObject);
	}
}
