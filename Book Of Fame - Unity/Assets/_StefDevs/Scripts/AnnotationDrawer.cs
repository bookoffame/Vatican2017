using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Draws annotation to the screen.
/// </summary>
public class AnnotationDrawer : MonoBehaviour {
	/// <summary>
	/// The top left corner of the page to draw to.
	/// </summary>
	public Transform topLeft;

	/// <summary>
	/// The bottom right corner of the page to draw to.
	/// </summary>
	public Transform bottomRight;

	/// <summary>
	/// The annotation object to draw.
	/// </summary>
	public Transform annoObj;

	/// <summary>
	/// The location of the canvas.
	/// </summary>
	public Transform canvas;

	private Annotation.AnnotationBox[] boxes = new Annotation.AnnotationBox[0];
	private RectTransform[] annotations = new RectTransform [0];
	private Texture2D texture;
	private bool isShowing = false;

	void Start()
	{
		texture = new Texture2D(1,1);
		texture.SetPixel(1,1, new Color(0,0,1.0f,0.5f));
		texture.Apply();
	}

	/// <summary>
	/// Updates the annotations this instance draws.
	/// </summary>
	/// <param name="annos">The new annotations to draw.</param>
	public void UpdatesAnnotations(Annotation.AnnotationBox[] annos){
		if (annotations != null)
			for (int i = 0; i < annotations.Length; i++)
				if (annotations[i] != null)
				    Destroy (annotations [i].gameObject);
		boxes = annos;
		annotations = new RectTransform[annos.Length];
		for (int i = 0; i < annos.Length; i++) {
			Transform o = Instantiate (annoObj);
			o.SetParent (canvas, false);
			o.SetAsFirstSibling ();
			annotations[i] = o.GetComponent<RectTransform> ();
			o.GetComponentInChildren<Text>().text = annos[i].contents;
			o.gameObject.SetActive (isShowing);
		}
	}

	public void AddNewAnnotation(Annotation.AnnotationBox anno){
		int i;
		Annotation.AnnotationBox[] newAnnos = new Annotation.AnnotationBox[boxes.Length + 1];
		for (i = 0; i < boxes.Length; i++){
			newAnnos [i] = boxes [i];
		}
		newAnnos [i] = anno;

		UpdatesAnnotations (newAnnos);
		annotations [i].gameObject.SetActive(true);
		annotations [i].SetParent (canvas, false);
	}

	/// <summary>
	/// Shows/Hides the annotations.
	/// </summary>
	/// <param name="isShowing">If set to <c>true</c> show the annotations, else hides the annotations.</param>
	public void ShowAnnotations(bool isShowing){
		this.isShowing = isShowing;
		foreach (Transform o in annotations) {
			o.gameObject.SetActive (isShowing);
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
				myTopLeft.x + myWidth * boxes[i].x,
				myTopLeft.y + myHeight * boxes[i].y,
				myWidth * boxes[i].w,
				myHeight * boxes[i].h);

			Vector2 location = new Vector2 (pos.x / Screen.width, 1 - (pos.y / Screen.height));
			annotations [i].anchorMin = location;
			annotations [i].anchorMax = location;
			annotations [i].sizeDelta = new Vector2 (pos.width, pos.height);
		}
	}
}
