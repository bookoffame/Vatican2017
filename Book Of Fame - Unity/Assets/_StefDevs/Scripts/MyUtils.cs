using UnityEngine;
using System.Collections;

public class MyUtils
{
	public static IEnumerator SmoothMove(Transform transform, Transform fromPos, Transform toPos, float seconds){
		float time = 0f;
		while (time < seconds) {
			float newX = Mathf.Lerp(fromPos.position.x,toPos.position.x,time/seconds);
			float newY = Mathf.Lerp(fromPos.position.y,toPos.position.y,time/seconds);
			float newZ = Mathf.Lerp(fromPos.position.z,toPos.position.z,time/seconds);
			Quaternion newRot = Quaternion.Slerp (fromPos.rotation, toPos.rotation, time / seconds);
			transform.position = new Vector3 (newX, newY, newZ);
			transform.rotation = newRot;
			time += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		transform.position = toPos.position;
		transform.rotation = toPos.rotation;
	}

	public static IEnumerator SmoothMove(Transform transform, Transform toPos, float seconds){
		float time = 0f;
		Vector3 fromPos = transform.position;
		Quaternion fromRot = transform.rotation;
		while (time < seconds) {
			float newX = Mathf.Lerp(fromPos.x,toPos.position.x,time/seconds);
			float newY = Mathf.Lerp(fromPos.y,toPos.position.y,time/seconds);
			float newZ = Mathf.Lerp(fromPos.z,toPos.position.z,time/seconds);
			Quaternion newRot = Quaternion.Slerp (fromRot, toPos.rotation, time / seconds);
			transform.position = new Vector3 (newX, newY, newZ);
			transform.rotation = newRot;
			time += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		transform.position = toPos.position;
		transform.rotation = toPos.rotation;
	}

	public static IEnumerator SmoothMove(Transform transform, Vector3 toPos, Quaternion toRot, float seconds){
		float time = 0f;
		Vector3 fromPos = transform.position;
		Quaternion fromRot = transform.rotation;
		while (time < seconds) {
			float newX = Mathf.Lerp(fromPos.x,toPos.x,time/seconds);
			float newY = Mathf.Lerp(fromPos.y,toPos.y,time/seconds);
			float newZ = Mathf.Lerp(fromPos.z,toPos.z,time/seconds);
			Quaternion newRot = Quaternion.Slerp (fromRot, toRot, time / seconds);
			transform.position = new Vector3 (newX, newY, newZ);
			transform.rotation = newRot;
			time += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		transform.position = toPos;
		transform.rotation = toRot;
	}

	public static IEnumerator SmoothMove(Transform transform, Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot, float seconds){
		float time = 0f;
		while (time < seconds) {
			float newX = Mathf.Lerp(fromPos.x,toPos.x,time/seconds);
			float newY = Mathf.Lerp(fromPos.y,toPos.y,time/seconds);
			float newZ = Mathf.Lerp(fromPos.z,toPos.z,time/seconds);
			Quaternion newRot = Quaternion.Slerp (fromRot, toRot, time / seconds);
			transform.position = new Vector3 (newX, newY, newZ);
			transform.rotation = newRot;
			time += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		transform.position = toPos;
		transform.rotation = toRot;
	}
}

