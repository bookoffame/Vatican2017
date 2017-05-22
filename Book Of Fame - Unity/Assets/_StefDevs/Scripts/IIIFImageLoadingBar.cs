using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// The loading bar for an IIIF image.
/// </summary>
public class IIIFImageLoadingBar : MonoBehaviour {
	/// <summary>
	/// The image to monitor the download progress of.
	/// </summary>
	public IIIFImageGet image;

	/// <summary>
	/// The background of the loading bar.
	/// </summary>
	public Image back;

	/// <summary>
	/// The foreground of the loading bar.
	/// </summary>
	public Image progressBar;

	void Update(){
		if (image.GetProgress () < 0.9999) {
			progressBar.color = Color.green;
			back.color = Color.black;
		} else {
			progressBar.color = Color.clear;
			back.color = Color.clear;
		}
		progressBar.fillAmount = image.GetProgress();
	}
}
