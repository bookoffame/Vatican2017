using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;

/// <summary>
/// Creates and retrieves annotations.
/// </summary>
public class Annotation : MonoBehaviour {

	public int pageNum;

	/// <summary>
	/// The width of the image.
	/// </summary>
	/// <value>
	/// The width used to calculate annotation coordinates 
	/// </value>
	public int pageWidth;

	/// <summary>
	/// The height of the image.
	/// </summary>
	/// <value>
	/// The height used to calculate annotation coordinates 
	/// </value>
	public int pageHeight;

	/// <summary>
	/// The Collider used to calculate where on the page the user clicked.
	/// </summary>
	public Collider page;
	public PageImages webdata;

	/// <summary>
	/// The location of the top left corner of the page.
	/// </summary>
	public Transform topLeft;

	/// <summary>
	/// The location of the bottom right corner of the page.
	/// </summary>
	public Transform bottomRight;

	/// <summary>
	/// The web address to write to the annotation file.
	/// </summary>
	public string webAddress;

	/// <summary>
	/// The original starting x for a new annotation.
	/// </summary>
	private int sx;

	/// <summary>
	/// The original starting y for a new annotation.
	/// </summary>
	private int sy;

	/// <summary>
	/// The width of a new annotation.
	/// </summary>
	private int w;

	/// <summary>
	/// The height of a new annotation.
	/// </summary>
	private int h;

	/// <summary>
	/// Is the user creating a new annotation?
	/// </summary>
	private bool annotating;

	/// <summary>
	/// The new annotation to create.
	/// </summary>
	private AnnotationBox anno;

	/// <summary>
	/// The texture used when selecting an area for a new annotation.
	/// </summary>
	private Texture2D texture;


	void Start()
	{
		texture = new Texture2D(1,1);
		texture.SetPixel(1,1, new Color(1.0f,1.0f,0.0f,0.5f));
		texture.Apply();
	}

	void Update () {
		
		if (ButtonControls.current.getSelected () == ButtonControls.ANNOTATION_TOOL)
			MarkAnnotation ();
		
	}


	/// <summary>
	/// Updates the web address used for writting annotations.
	/// </summary>
	/// <param name="newAddress">The new web address to write for new annotations.</param>
	public void UpdateWebAddress(string newAddress){
		webAddress = newAddress;
	}

	/// <summary>
	/// Get all the annotations corresponding to a specific IIIF image. 
	/// </summary>
	/// <returns>An ArrayList of all the annotations corresponding to a webpage (each annotation is an AnnotationBox).</returns>
	/// <param name="data">The source annotation file to parse as a String.</param>
	/// <param name="url">The URL that represents the IIIF image to look for annotations for.</param>
	public ArrayList GetAnnotations (string data, string url)
	{
		if (data.Length > 1)
		    data = data.Substring (1);
		Regex regex = new Regex ("{(\\s|.)*?\"@type\": \"oa:Annotation\",(\\s|.)*?\"@type\": \"cnt:ContentAsText\"," +
		              "(\\s|.)*?\"chars\": \"([^\"]*?)\",(\\s|.)*?\"on\": \""
		              + Regex.Escape (url) + "#xywh=(\\d*?),(\\d*?),(\\d*?),(\\d*?)\"(\\s|.)*?}");
		ArrayList list = new ArrayList ();
		foreach (string s in GetPair(data)) {
			if (s.Equals (data))
				continue;
			MatchCollection matches = regex.Matches (s);
			foreach (Match m in matches) {
				AnnotationBox a;
				a.contents = m.Groups [4].ToString ();
				a.x = (float)int.Parse (m.Groups [6].ToString ()) / pageWidth;
				a.y = (float)int.Parse (m.Groups [7].ToString ()) / pageHeight;
				a.w = (float)int.Parse (m.Groups [8].ToString ()) / pageWidth;
				a.h = (float)int.Parse (m.Groups [9].ToString ()) / pageHeight;
				list.Add (a);
			}
		}
		return list;
	}

	/// <summary>
	/// Gets each pair of matching "{" "}" braces.
	/// </summary>
	/// <returns>Each pair of "{" "}" braces.</returns>
	/// <param name="s">The string to look for pairs in.</param>
	private IEnumerable GetPair(string s){
		int count = 0;

		//The position of the last "{" brace
		ArrayList last = new ArrayList();

		for (int i = 0; i < s.Length; i++) {
			//If we find a "{", add its position to the end of last
			if (s [i] == '{') {
				last.Add (i);
				count++;
			}
			//Else if we find a "}", group it with its corresponding "{" and return the string between them
			else if (s [i] == '}' && count > 0) {
				count--;
				int start = (int)last [count];
				last.RemoveAt (count);
				yield return s.Substring (start, i - start + 1);
			}
		}
	}

	/// <summary>
	/// Marks the region of the annotation on screen.
	/// </summary>
	private void MarkAnnotation(){
		RaycastHit hit;
		Vector3 topLeftCorr = Camera.main.WorldToScreenPoint (topLeft.position);
		Vector3 bottomRightCorr = Camera.main.WorldToScreenPoint (bottomRight.position);

		topLeftCorr.y = Screen.height - topLeftCorr.y;
		bottomRightCorr.y = Screen.height - bottomRightCorr.y;

		float myWidth = bottomRightCorr.x - topLeftCorr.x;
		float myHeight = bottomRightCorr.y - topLeftCorr.y;

		if (page.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit, 1000)) {
			Vector3 hitPoint = Input.mousePosition;
			hitPoint.y = Screen.height - hitPoint.y;
			if (Input.GetMouseButtonDown (0)) {
				annotating = true;

				sx = (int)(pageWidth * ((hitPoint.x - topLeftCorr.x)/myWidth));
				sy = (int)(pageHeight * ((hitPoint.y - topLeftCorr.y)/myHeight));

				anno.x = (hitPoint.x - topLeftCorr.x) / myWidth;
				anno.y = (hitPoint.y - topLeftCorr.y) / myHeight;
				anno.w = 0;
				anno.h = 0;

			} else if (Input.GetMouseButton (0)) {
				w = (int)(pageWidth * ((hitPoint.x - topLeftCorr.x)/myWidth)) - sx;
				h = (int)(pageHeight * ((hitPoint.y - topLeftCorr.y)/myHeight)) - sy;

				anno.w = (float)w/pageWidth;
				anno.h =(float)h/pageHeight;
				if (anno.w < 0) {
					anno.x = (float)sx/pageWidth + anno.w;
					anno.w = -anno.w;
				}
				if (anno.h < 0) {
					anno.y = (float)sy/pageHeight + anno.h;
					anno.h = -anno.h;
				}
					
			} else if (Input.GetMouseButtonUp (0)) {
				if (annotating) {
					if (w < 0) {
						sx += w;
						w = -w;
					}
					if (h < 0) {
						sy += h;
						h = -h;
					}
					annotating = false;
					StartCoroutine (MakeAnnotation (sx, sy, w, h, anno));
				}
			}
		}
		if (!Input.GetMouseButton (0)) {
			annotating = false;
		}
	}

	/// <summary>
	/// Get the path to the local annotation file.
	/// </summary>
	/// <returns>The path to the local annotation file.</returns>
	public string LocalAnnotationFile(){
		return  Application.persistentDataPath + "/anno.json";
	}

	private IEnumerator MakeAnnotation(int x, int y, int w, int h, AnnotationBox newAnno){
		yield return ButtonControls.current.PopUp ();
		string anno = ButtonControls.current.getPopupText ();
		newAnno.contents = anno;
		if (!anno.Equals ("")) {
			FileStream writter;
			string toWrite = ",\n";
			string filename = LocalAnnotationFile ();

			if (!File.Exists (filename)) {
				toWrite = "\n";
				File.WriteAllText (filename,
					"{\n\t\"@context\": \"http://www.shared-canvas.org/ns/context.json\","
					+ "\n\t\"@id\": \"" + filename + "\""
					+ ",\n\t\"@type\": \"sc:AnnotationList\",\n\t\"resources\": [\n\t\n\t]\n}\n");
			}
			toWrite += "\t\t{\n";
			toWrite += "\t\t\t\"id\": \"_:an" + System.DateTime.UtcNow.ToBinary().ToString () + "\",\n";
			toWrite += "\t\t\t\"@type\": \"oa:Annotation\",\n";
			toWrite += "\t\t\t\"resource\": {\n";
			toWrite += "\t\t\t\t\"id\": \"_:an" + System.DateTime.UtcNow.ToBinary().ToString () + "\",\n";
			toWrite += "\t\t\t\t\"@type\": \"cnt:ContentAsText\",\n";
			toWrite += "\t\t\t\t\"format\": \"text/plain\",\n";
			toWrite += "\t\t\t\t\"chars\": \"" + anno + "\",\n";
			toWrite += "\t\t\t\t\"language\": \"en\",\n";
			toWrite += "\t\t\t},\n";
			toWrite += "\t\t\t\"on\": \"" + webAddress + "#xywh=" + x.ToString() + "," + y.ToString() 
				+ "," + w.ToString() + "," + h.ToString() + "\"\n";
			toWrite += "\t\t}";

			writter = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
			writter.Seek(0, SeekOrigin.End);
			while (writter.ReadByte() != ']')
				writter.Seek(-2, SeekOrigin.Current);
			writter.Seek(-3, SeekOrigin.Current);
			byte[] myBytes = System.Text.Encoding.ASCII.GetBytes (toWrite);
			writter.Write(myBytes,0,myBytes.Length);

			myBytes = System.Text.Encoding.ASCII.GetBytes ("\n\t]\n}\n");
			writter.Write(myBytes,0,myBytes.Length);

			writter.Close ();
		}
		webdata.AddNewAnnotation(pageNum, newAnno);
	}

	void OnGUI(){
		if (annotating) {
			Vector3 myTopLeft = Camera.main.WorldToScreenPoint (topLeft.position);
			Vector3 myBottomRight = Camera.main.WorldToScreenPoint (bottomRight.position);
			myTopLeft.y = Screen.height - myTopLeft.y;
			myBottomRight.y = Screen.height - myBottomRight.y;
			float myWidth = myBottomRight.x - myTopLeft.x;
			float myHeight = myBottomRight.y - myTopLeft.y;
			Rect pos = new Rect (
				          myTopLeft.x + myWidth * anno.x,
				          myTopLeft.y + myHeight * anno.y,
				          myWidth * anno.w,
				          myHeight * anno.h);

			GUI.DrawTexture (pos, texture);
		}
	}

	/// <summary>
	/// Contains data needed to display an annotation.
	/// </summary>
	public struct AnnotationBox
	{
		/// <summary>
		/// The contents of the annotation.
		/// </summary>
		public string contents;

		/// <summary>
		/// The horizontal offset of the annotation from the left side of the page, as a percentage of page width.
		/// </summary>
		public float x;

		/// <summary>
		/// The vertical offset of the annotation from the top side of the page, as a percentage of page height.
		/// </summary>
		public float y;

		/// <summary>
		/// The width of the annotation, as a percentage of page width.
		/// </summary>
		public float w;

		/// <summary>
		/// The height of the annotation, as a percentage of page height.
		/// </summary>
		public float h;
	};
}
