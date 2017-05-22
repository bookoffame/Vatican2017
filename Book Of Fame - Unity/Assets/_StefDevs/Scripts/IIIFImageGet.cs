using UnityEngine;
using System.Collections;
using System.IO;

/// <summary>
/// Retrieves an image from an IIIF server. 
/// </summary>
public class IIIFImageGet : ScriptableObject {
	/// <summary>
	/// The root web address to get the image from.
	/// </summary>
	public string webAddress;

	/// <summary>
	/// The horizontal crop offset. -1 If not used.
	/// </summary>
	public int cropOffsetX = -1;

	/// <summary>
	/// The vertical crop offset.
	/// </summary>
	public int cropOffsetY = -1;

	/// <summary>
	/// The width of the crop.
	/// </summary>
	public int cropWidth = -1;

	/// <summary>
	/// The height of the crop.
	/// </summary>
	public int cropHeight = -1;

	/// <summary>
	/// The width of the target image.
	/// </summary>
	public int targetWidth = -1;

	/// <summary>
	/// The height of the target image.
	/// </summary>
	public int targetHeight = -1;

	/// <summary>
	/// Is the image reflected?.
	/// </summary>
	public bool mirrored = false;

	/// <summary>
	/// The rotation of the image.
	/// </summary>
	public int rotation = 0;

	/// <summary>
	/// The quality of the image.
	/// </summary>
	public string quality = "default";

	/// <summary>
	/// The format of the image.
	/// </summary>
	public string format = ".jpg";

	/// <summary>
	/// The image obtained from the IIIF server.
	/// </summary>
	public Texture2D texture;

	private WWW iiifImage;

	private static bool isDownloading = false;

	/// <summary>
	/// Updates the image.
	/// </summary>
	public IEnumerator UpdateImage () {
		string location = getAddress ();
		yield return new WaitWhile (() => isDownloading);
		do 
		{
			iiifImage = new WWW (location);
			isDownloading = true;
			yield return new WaitUntil (() => iiifImage.isDone);
			if (!string.IsNullOrEmpty(iiifImage.error))
			{
				Debug.Log("Failed to download " + location + ":" + iiifImage.error);
				yield return new WaitForSeconds(1f);
			} 
		} while(!string.IsNullOrEmpty(iiifImage.error));

		Debug.Log("Finished downloading " + location);
		isDownloading = false;
		texture = iiifImage.texture;
	}

	/// <summary>
	/// Removes the tail from a web address.
	/// </summary>
	/// <returns>The web address with the tail removed.</returns>
	/// <param name="newAddress">The web address to remove the tail from.</param>
	public string removeTail(string newAddress){
		int remaining = 4;
		int index = newAddress.Length - 1;
		while (remaining > 0) {
			if (newAddress [index] == '/')
				remaining--;
			index--;
		}
		return newAddress.Substring (0,index + 1);
	}

	/// <summary>
	/// Changes the web address.
	/// </summary>
	/// <param name="newAddress">The new web address (still with the tail).</param>
	public void changeAddress(string newAddress){
		webAddress = removeTail (newAddress);
	}

	/// <summary>
	/// Calculates the web address for the IIIF image with this IIIFImageGet's settings.
	/// </summary>
	/// <returns>The IIIF web address corresponding to this IIIFImageGet.</returns>
	public string getAddress(){
		string location = webAddress;
		location = location.Insert (location.Length,"/");
		if (cropOffsetX == -1)
			location = location.Insert (location.Length,"full/");
		else
			location = location.Insert (location.Length, cropOffsetX.ToString () + ","
				+ cropOffsetY.ToString () + "," + cropWidth.ToString () + "," + cropHeight.ToString () + "/");
		if (targetWidth == -1 && targetHeight == -1)
			location = location.Insert (location.Length, "full/");
		else {
			if (targetWidth != -1)
				location = location.Insert (location.Length,targetWidth.ToString());
			location = location.Insert (location.Length,",");
			if (targetHeight != -1)
				location = location.Insert (location.Length,targetHeight.ToString());
			location = location.Insert (location.Length,"/");
		}
		if (mirrored)
			location = location.Insert (location.Length,"!");
		location = location.Insert (location.Length,rotation.ToString() + "/");
		location = location.Insert (location.Length, quality + format);
		return location;
	}

	/// <summary>
	/// Gets the current percentage downloaded of the image.
	/// </summary>
	/// <returns>The percentage downloaded of the image thus far. 1.0f if the image is downloaded.</returns>
	public float GetProgress(){
		if (iiifImage == null)
			return 1;
		else
			return iiifImage.progress;
	}

}
