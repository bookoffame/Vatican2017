using UnityEngine;
using System.Collections;

public class PageStart : MonoBehaviour {

	public Renderer pageRender;
	public Texture2D[] textures;

	void Start () {
		Texture2D atlas = new Texture2D(8192, 8192);
		atlas.PackTextures(textures, 0, 8192);
		pageRender.material.mainTexture = atlas;
	}
}
