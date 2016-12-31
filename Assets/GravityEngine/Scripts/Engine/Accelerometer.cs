using UnityEngine;
using System.Collections;

public class Accelerometer : MonoBehaviour {

	private int frameCount = 0; 

	private Vector3 lastV; 
	private float lastTime = 0;

	void Start() {
		lastV = Vector3.zero;
	}

	// Update is called once per frame
	void Update () {
		if (frameCount++ > 30) {
			Vector3 v = GravityEngine.Instance().GetVelocity(this.gameObject);
			float t = Time.time;
			frameCount = 0; 
			Vector3 a = (v - lastV)/(t-lastTime); 
			Debug.Log("Accel:" + a + " mag=" + Vector3.Magnitude(a));
			lastV = v;
			lastTime = t;
		}
	}
}
