using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingBar : MonoBehaviour
{
	public Image loadingBar;
	public ImageBufferer buffer;

	// Update is called once per frame
	void Update ()
	{
		loadingBar.fillAmount = buffer.Progress ();
	}
}

