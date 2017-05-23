using UnityEngine;
using System.Collections;
using System.IO;

/// <summary>
/// Retrieves an image from an IIIF server. 
/// </summary>
public class IIIFImageGet : ScriptableObject {
	// The root web address to get the image from.
	public string webAddress;

	// The horizontal crop offset. -1 If not used.
	public int cropOffsetX = -1;

	// The vertical crop offset.
	public int cropOffsetY = -1;

	// The width of the crop.
	public int cropWidth = -1;

	// The height of the crop.
	public int cropHeight = -1;

	// The width of the target image.
	public int targetWidth = -1;

	// The height of the target image.
	public int targetHeight = -1;

	/// Is the image reflected?.
	public bool mirrored = false;

	/// The rotation of the image.
	public int rotation = 0;

	/// The quality of the image.
	public string quality = "default";

	/// The format of the image.
	public string format = ".jpg";

	/// The image obtained from the IIIF server.
	public Texture2D texture;

	private WWW iiifImage;

	private static bool isDownloading = false;

    // Updates the image.
    public IEnumerator UpdateImage()
    {
        string location = getAddress();
        yield return new WaitWhile(() => isDownloading);
        do
        {
            iiifImage = new WWW(location);
            isDownloading = true;
            yield return new WaitUntil(() => iiifImage.isDone);
            if (!string.IsNullOrEmpty(iiifImage.error))
            {
                Debug.Log("Failed to download " + location + ":" + iiifImage.error);
                yield return new WaitForSeconds(1f);
            }
        } while (!string.IsNullOrEmpty(iiifImage.error));

        Debug.Log("Finished downloading " + location);
        isDownloading = false;
        texture = iiifImage.texture;
    }

	// Removes the tail from a web address.
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

	// Changes the web address.
	public void changeAddress(string newAddress){
		webAddress = removeTail (newAddress);
	}

	// Calculates the web address for the IIIF image with this IIIFImageGet's settings.
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

	/// Gets the current percentage downloaded of the image.
	public float GetProgress(){
		if (iiifImage == null)
			return 1;
		else
			return iiifImage.progress;
	}

}
