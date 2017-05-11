using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MinigameFlashlight : MonoBehaviour {
	public Image light;
	public float MAX_RADIUS;

	private float radius;
	private Dictionary<Letter, bool> letters;

	void Start()
	{
		letters = new Dictionary<Letter, bool> ();
		radius = MAX_RADIUS;
	}

	void Update () {
		light.enabled = ButtonControls.current.isSpotlight;

		transform.localScale = Vector3.one * (radius / MAX_RADIUS);
		transform.position = Input.mousePosition;

		Dictionary<Letter, bool> newDir = new Dictionary<Letter, bool> ();

		foreach (KeyValuePair<Letter,bool> e in letters)
			newDir [e.Key] = false;

		letters = newDir;

		
		if (light.enabled) {
			Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint (Input.mousePosition);
			Vector3 screenNormal = Camera.main.ScreenPointToRay (Input.mousePosition).direction;

			RaycastHit[] hits = Physics.CapsuleCastAll (mouseWorldPosition, mouseWorldPosition + 100 * screenNormal, radius, screenNormal);

			if (Input.GetKey (KeyCode.Q)) {
				if (radius > 0.001f)
					radius -= 0.001f;
			} else if (Input.GetKey (KeyCode.E)) {
				if (radius < MAX_RADIUS)
					radius += 0.01f;
			}

			foreach (RaycastHit hit in hits) {
				Letter letter = hit.collider.gameObject.GetComponent<Letter> ();
				if (letter != null) {
					letter.SetScaredAmount (1.1f - (radius/MAX_RADIUS));
					letters [letter] = true;
					if (Input.GetMouseButtonDown (0)) {
						RaycastHit[] mouseHits = Physics.RaycastAll(mouseWorldPosition,screenNormal);
						foreach (RaycastHit possible in mouseHits)
						{
							if (possible.collider.gameObject.GetComponent<Letter> () == letter) {
								letters.Remove (letter);
								letter.Collect ();
							}
						}
					}
				}
			}
		}

		foreach(KeyValuePair<Letter, bool> entry in letters)
		{
			if (!letters [entry.Key])
				entry.Key.SetScaredAmount (0f);
		}
	}
}
