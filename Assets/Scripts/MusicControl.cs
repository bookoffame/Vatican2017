using UnityEngine;
using System.Collections;

public class MusicControl : MonoBehaviour {
	public static MusicControl controller;
	public AudioSource sound;
	public AudioClip MAIN_MUSIC, MINIGAME_MUSIC, COURT_MUSIC;

	// Use this for initialization
	void Start () {
		if (controller == null) {
			controller = this;
		}
	}

	public IEnumerator ChangeSong(AudioClip newSong)
	{
		yield return StartCoroutine (FadeOut(5f,0.01f));
		sound.Stop ();
		sound.clip = newSong;
		sound.Play ();
		yield return StartCoroutine (FadeIn(3f,0.01f));
	}

	private IEnumerator FadeIn(float length, float timeTillUpdate){
		while (sound.volume < 0.99f) {
			sound.volume += timeTillUpdate / length;
			yield return new WaitForSeconds (timeTillUpdate);
		}
	}

	private IEnumerator FadeOut(float length, float timeTillUpdate){
		while (sound.volume > 0.01f) {
			sound.volume -= timeTillUpdate / length;
			yield return new WaitForSeconds (timeTillUpdate);
		}
	}
}
