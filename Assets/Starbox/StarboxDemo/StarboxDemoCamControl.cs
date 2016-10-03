using UnityEngine;
using System.Collections.Generic;

public class StarboxDemoCamControl : MonoBehaviour {
	float yaw = 0f;
	float pitch = 0f;

	void Update() {
		yaw += Input.GetAxis("Horizontal") * 360 * Time.deltaTime;
		pitch = Mathf.Clamp(pitch - Input.GetAxis("Vertical") * 180 * Time.deltaTime, -90, 90);

		transform.rotation = Quaternion.Euler(pitch, yaw, 0);
	}

	void OnGUI() {
		GUI.Label(new Rect(20, Screen.height-20, 400, 20),
		          "Use arrow keys to rotate camera.");
	}
}
