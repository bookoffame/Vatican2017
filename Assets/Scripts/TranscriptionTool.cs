﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Displays the transcription of the text.
/// </summary>
public class TranscriptionTool : MonoBehaviour {
	/// <summary>
	/// The location of the top left corner of the page.
	/// </summary>
	public Transform topLeft; 

	/// <summary>
	/// The location of the bottom right corner of the page.
	/// </summary>
	public Transform bottomRight;

	/// <summary>
	/// The boundaries of the transcription.
	/// </summary>
	public RectTransform[] annotations;

	public Transform annoObj;

	/// <summary>
	/// The canvas.
	/// </summary>
	public Transform canvas;

	public Vector3 offset;
	public float unitXScale, unitYScale, locationBias, positionBias, sizeXScale, sizeYScale;


	private Annotation.AnnotationBox[] boxes;
	private Texture2D texture;

	void Start()
	{
		texture = new Texture2D(1,1);
		texture.SetPixel(1,1, new Color(0,0,1.0f,0.5f));
		texture.Apply();
	}

	/// <summary>
	/// Updates the transcription.
	/// </summary>
	/// <param name="annos">The new transcription.</param>
	public void UpdatesTranscriptions(Annotation.AnnotationBox[] annos){
		if (annotations != null)
			for (int i = 0; i < annotations.Length; i++)
				if (annotations[i] != null)
					Destroy (annotations [i].gameObject);
		boxes = annos;
		annotations = new RectTransform[annos.Length];
		for (int i = 0; i < annos.Length; i++) {
			Transform o = Instantiate (annoObj);
			o.SetParent (transform, false);
			o.SetAsFirstSibling ();
			annotations[i] = o.GetComponent<RectTransform> ();
			o.GetComponentInChildren<Text>().text = annos[i].contents;
			o.gameObject.SetActive (true);
		}
	}
		
	void Update(){
		Vector3 myTopLeft = Camera.main.WorldToScreenPoint (topLeft.position);
		Vector3 myBottomRight = Camera.main.WorldToScreenPoint (bottomRight.position);

		myTopLeft.y = Screen.height - myTopLeft.y;
		myBottomRight.y = Screen.height - myBottomRight.y;

		float myWidth = myBottomRight.x - myTopLeft.x;
		float myHeight = myBottomRight.y - myTopLeft.y;

		for (int i = 0; i < boxes.Length; i++) {
			Rect pos = new Rect (
				myTopLeft.x + myWidth*boxes[i].x,
				myTopLeft.y + myHeight*boxes[i].y,
				myWidth * boxes[i].w * unitXScale,
				myHeight * boxes[i].h * unitYScale);

			Vector3 location = new Vector3 (pos.x, Screen.height - pos.y);

			annotations [i].localPosition = locationBias*location - positionBias*transform.position + offset;
			Vector2 size = new Vector2 (pos.width * sizeXScale, pos.height * sizeYScale);
			annotations[i].sizeDelta = size;
		}
	}
}
