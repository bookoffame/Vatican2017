using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// The controls logic for the spotlight tool.
/// </summary>
public class MoveSpotlight : MonoBehaviour {
	/// <summary>
	/// The width of spotlight movement.
	/// </summary>
	public int width;

	/// <summary>
	/// The height of the spotlight movement.
	/// </summary>

	public int height;

	/// <summary>
	/// The book mode camera
	/// </summary>
	public Camera cam;

	/// <summary>
	/// The spot light.
	/// </summary>
	public Light spotLight;

	/// <summary>
	/// The world lighting.
	/// </summary>
	public Light worldLight;

	/// <summary>
	/// The ceiling light.
	/// </summary>
	public Light ceilingLight;

	/// <summary>
	/// The preview color of the spotlight.
	/// </summary>
	public Image preview;

	/// <summary>
	/// The Toggle that controls if this spotlight is frozen.
	/// </summary>
	public Toggle frozenToggle;

	/// <summary>
	/// The Toggle that controls hiding/showing the settings of this spotlight.
	/// </summary>
	public Toggle hideToggle;

	/// <summary>
	/// Is the spotlight frozen in position?
	/// </summary>
	public bool frozen = false;

	/// <summary>
	/// Are the settings of this spotlight hidden?
	/// </summary>
	public bool hidingProperties = false;

	/// <summary>
	/// The settings of this spotlight.
	/// </summary>
	public Transform properties;

	// Update is called once per frame
	void Update () {
		bool isSpotlight = ButtonControls.current.isSpotlight;
		spotLight.enabled = isSpotlight;
		worldLight.enabled = !isSpotlight && !ceilingLight.enabled;
		if (Input.GetKeyDown (KeyCode.F))
			freezePosition (!frozen);
		if (Input.GetKeyDown (KeyCode.H))
			hideProperties (!hidingProperties);
		if (Input.GetKeyDown (KeyCode.C))
			ceilingLight.enabled = !ceilingLight.enabled;
		if (!frozen) {
			Vector3 pos = new Vector3 ();
			Vector3 mousePos = Input.mousePosition;
			pos.x = (width) * (mousePos.x / Screen.width);
			pos.y = (height) * (mousePos.y / Screen.height);
			pos.z = transform.localPosition.z;
			transform.localPosition = Vector3.MoveTowards (transform.localPosition, pos, 1f);
		}
	}

	/// <summary>
	/// Updates the hue of the spotlight.
	/// </summary>
	/// <param name="hue">The new hue (between 0.0f and 1.0f).</param>
	public void updateHue(float hue){
		float h,s,v;
		Color.RGBToHSV (spotLight.color, out h, out s, out v);
		Color newColor = Color.HSVToRGB (hue, s, v);
		spotLight.color = newColor;
		preview.color = newColor;
	}

	/// <summary>
	/// Updates the saturation of the spotlight.
	/// </summary>
	/// <param name="sat">The new saturation (between 0.0f and 1.0f).</param>
	public void updateSat(float sat){
		float h,s,v;
		Color.RGBToHSV (spotLight.color, out h, out s, out v);
		Color newColor = Color.HSVToRGB (h, sat, v);
		spotLight.color = newColor;
		preview.color = newColor;
	}

	/// <summary>
	/// Updates the value of the spotlight.
	/// </summary>
	/// <param name="value">The new value (between 0.0f and 1.0f).</param>
	public void updateValue(float value){
		float h,s,v;
		Color.RGBToHSV (spotLight.color, out h, out s, out v);
		Color newColor = Color.HSVToRGB (h, s, value);
		spotLight.color = newColor;
		preview.color = newColor;
	}

	/// <summary>
	/// Updates the size of the spotlight.
	/// </summary>
	/// <param name="newSize">New size of the spotlight (between 0.0f and 1.0f).</param>
	public void updateSize(float newSize){
		spotLight.spotAngle = newSize * 180;
	}

	/// <summary>
	/// Updates the brightness of the spotlight.
	/// </summary>
	/// <param name="brightness">The new brightness of the spotlight (between 0.0f and 1.0f).</param>
	public void updateBrightness(float brightness){
		spotLight.intensity = brightness * 8;
	}

	/// <summary>
	/// Freezes/Unfreezes the position of the spotlight.
	/// </summary>
	/// <param name="isFreeze">If set to <c>true</c> freezes the position. Otherwises, unfreezes the position.</param>
	public void freezePosition(bool isFreeze){
		frozen = isFreeze;
		frozenToggle.isOn = isFreeze;
	}

	/// <summary>
	/// Hides/Shows the settings of the spotlight.
	/// </summary>
	/// <param name="isHiding">If set to <c>true</c> hides the settings. Otherwise, show the settings.</param>
	public void hideProperties(bool isHiding){
		hidingProperties = isHiding;
		hideToggle.isOn = isHiding;
		properties.gameObject.SetActive(!isHiding);
	}
}
