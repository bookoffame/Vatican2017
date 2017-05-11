using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using AssemblyCSharp;

/// <summary>
/// Presents the IIIF images from a manifest on 6 pages.
/// </summary>
public class ImageBufferer : MonoBehaviour {
	/// <summary>
	/// The images for the buffered pages
	/// </summary>
	private Texture2D[,] pageImages;
	private bool[] dirty;

	/// <summary>
	/// The manifest URL.
	/// </summary>
	public string manifestURL;

	/// <summary>
	/// The texture to display when loading images
	/// </summary>
	public Texture2D loadingTexture;

	private IIIFGetManifest data;

	private const int OFFSET = 2;
	private const int SIZE = 12;

	private Hashtable pageToImage;

	private int curr;
	private int loading;

	private Texture2D leftPage,rightPage,backLeft,backRight;

	// Use this for initialization
	void Start () {
		pageImages = new Texture2D[SIZE,2];
		dirty = new bool[SIZE];
		data = new IIIFGetManifest ();
		pageToImage = new Hashtable();
		leftPage = loadingTexture;
		rightPage = loadingTexture;
		backLeft = loadingTexture;
		backRight = loadingTexture;
		for (int i = 0; i < SIZE; i++) {
			pageImages[i,0] = loadingTexture;
			pageImages[i,1] = loadingTexture;
		}
		curr = 72;
		loading = 0;
		data.download(manifestURL);
		for (int i = 0; i < SIZE; i++) {
			StartCoroutine (LoadPage (i,0));
			StartCoroutine (LoadPage (i,1));
		}
	}

	/// <summary>
	/// Shifts page's textures to the left and loads the next two pages.
	/// </summary>
	public void TurnPageLeft(){
		pageToImage.Remove (curr * 2 - 3);
		pageToImage.Remove (curr * 2 - 2);

		for (int i = 2; i < SIZE; i++){
			if (pageToImage.ContainsKey (curr * 2 - 3 + i))
				pageToImage[curr * 2 - 3 + i] = i - 2;
		}

		curr++;

		for (int i = 0; i < SIZE - 2; i++){
			pageImages [i,0] = pageImages [i + 2,0];
			pageImages [i,1] = pageImages [i + 2,1];
		}
		MarkDirty ();
		StartCoroutine (LoadPage (SIZE - 2,0));
		StartCoroutine (LoadPage (SIZE - 1,0));
		StartCoroutine (LoadPage (SIZE - 2,1));
		StartCoroutine (LoadPage (SIZE - 1,1));
	}

	private void MarkDirty(){
		for (int i = 0; i < SIZE; i++)
			dirty [i] = true;
	}

	public void GotoPage(int pageNum){
		pageToImage.Clear ();
		for (int i = 0; i < SIZE; i++) {
			pageImages[i,0] = loadingTexture;
			pageImages[i,1] = loadingTexture;
		}
		curr = pageNum;
		for (int i = 0; i < SIZE; i++) {
			StartCoroutine (LoadPage (i,0));
			StartCoroutine (LoadPage (i,1));
		}
	}

	/// <summary>
	/// Shifts page's textures to the right and loads the previous two pages.
	/// </summary>
	public void TurnPageRight(){
		pageToImage.Remove (curr * 2 - 4 + 12);
		pageToImage.Remove (curr * 2 - 5 + 12);

		for (int i = 0; i < SIZE - 2; i++) {
			if (pageToImage.ContainsKey (curr * 2 - 3 + i))
				pageToImage [curr * 2 - 3 + i] = i + 2;
		}

		curr--;

		for (int i = SIZE - 1; i > 1; i--){
			pageImages [i,0] = pageImages [i - 2,0];
			pageImages [i,1] = pageImages [i - 2,1];
	    }
		MarkDirty ();    
		StartCoroutine (LoadPage (0,0));
		StartCoroutine (LoadPage (1,0));
		StartCoroutine (LoadPage (0,1));
		StartCoroutine (LoadPage (1,1));
	}

	public Texture2D GetImage(int page, bool flipped){
		if (flipped)
			return pageImages [page + OFFSET,1];
		else
		    return pageImages [page + OFFSET,0];
	}

	public string GetURL(int pageNum){
		return data.getPage (pageNum);
	}

	private IEnumerator LoadPage(int image, int flipped)
	{
		int pageNum = curr * 2 - 3 + image;
		pageToImage [pageNum] = image;
		loading += 2;
		if (pageNum > 0 && pageNum < data.getNumOfPages ()) {
			IIIFImageGet downloader = ScriptableObject.CreateInstance<IIIFImageGet>();
			downloader.cropOffsetY = 210;
			downloader.cropWidth = 2900;
			downloader.cropHeight = 4000;
			downloader.targetWidth = 1500;
			downloader.targetHeight = 2305;
			downloader.rotation = (0 >= 2)? 0 : 180;
			downloader.mirrored = flipped == 1;
			downloader.quality = "default";
			downloader.format = ".jpg";
			pageImages [(int)pageToImage[pageNum],flipped] = loadingTexture;
			if (pageNum % 2 == 1) {
				downloader.cropOffsetX = 175;
			} else {
				downloader.cropOffsetX = 60;
			}
			downloader.changeAddress (data.getPage (pageNum));
			downloader.targetWidth = downloader.cropWidth/2;
			downloader.targetHeight = downloader.cropHeight/2;

			yield return StartCoroutine (downloader.UpdateImage ());
			loading--;
			if (!pageToImage.Contains(pageNum)) {
				loading--;
				yield break;
			}
			pageImages [(int)pageToImage[pageNum],flipped] = downloader.texture;

			downloader.targetWidth = downloader.cropWidth;
			downloader.targetHeight = downloader.cropHeight;
			yield return StartCoroutine (downloader.UpdateImage ());
			loading--;
			if (!pageToImage.Contains(pageNum)) {
				yield break;
			}
			pageImages [(int)pageToImage[pageNum],flipped] = downloader.texture;
		} 
		else {
			pageImages [(int)pageToImage[pageNum],flipped] = loadingTexture;
		}
	}

	public bool IsLoaded()
	{
		return loading == 0;
	}

	public float Progress(){
		return (16f - loading) / 16f;
	}

	/*public Texture2D GetDualTexture(int front, int back){
		front = (int)(pageToImage [front]);
		back = (int)(pageToImage [back]);
		bool isRight = front > back;
		if (!dirty [front] || !dirty[back] || pageImages [back].width != pageImages [front].width) {
			if (front == back) {
				if (front == OFFSET)
					return backLeft;
				else
					return backRight;
			}
			if (isRight)
				return rightPage;
			else
				return leftPage;
		}
			
		Texture2D left = pageImages [back];
		Texture2D right = pageImages [front];
		Color[] leftColors, rightColors;

		if (front != back) {
			if (isRight) {
				leftColors = left.GetPixels ();
				rightColors = FlipColorArray (right.width, right.GetPixels ());
			} else {
				leftColors = FlipColorArray (left.width, left.GetPixels ());
				rightColors = right.GetPixels ();
			}
		} else {
			if (front == OFFSET) {
				backLeft = left;
				return left;
			} else {
				backRight = right;
				return right;
			}
		}

		Texture2D output = new Texture2D (left.width*2,left.height + 315 + 380);
		output.SetPixels(0, 315, left.width, left.height, leftColors);
		output.SetPixels(left.width, 315, right.width, right.height, rightColors);
		output.Apply ();

		if (front == back) {
			if (front == OFFSET)
				backLeft = output;
			else
				backRight = output;
		} else {
			if (isRight)
				rightPage = output;
			else
				leftPage = output;
		}
		dirty [front] = false;
		dirty [back] = false;
		return output;
	}

	private Color[] FlipColorArray(int width, Color[] arr){
		for (int i = 0; i < arr.Length; i++) {
			int other = width - (i % width) - 1;
			if (other > (i % width)) {
				Color temp = arr [i];
				arr [i] = arr [i - (i % width) + other];
				arr [i - (i % width) + other] = temp;
			}
		}
		return arr;
	}/*

	/*void OnGUI(){
		string output = "";
		ArrayList list = new ArrayList ();
		foreach (int key in pageToImage.Keys)
			list.Add (key);
		list.Sort ();
		foreach (int key in list)
		    output += "Loading page " + (key - curr * 2 - 3) + " to " + pageToImage [key].ToString () + ".\n";
		GUI.Box (new Rect (0,0,200,20*pageToImage.Count), output);
	}*/

}
