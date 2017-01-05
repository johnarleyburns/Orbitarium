using UnityEngine;
using System.Collections;

public class Accelerometer : MonoBehaviour {

	private int frameCount = 0; 

	private DVector3 lastV; 
	private float lastTime = 0;

	void Start() {
		lastV = DVector3.zero;
	}

	// Update is called once per frame
	void Update () {
		if (frameCount++ > 30) {
			DVector3 v = GravityEngine.Instance().GetVelocity(this.gameObject);
			float t = Time.time;
			frameCount = 0; 
			DVector3 a = (v - lastV)/(t-lastTime); 
			Debug.Log("Accel:" + a + " mag=" + a.magnitude);
			lastV = v;
			lastTime = t;
		}
	}
}
