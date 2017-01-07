using UnityEngine;
using System.Collections;

public class ObjectRotation : MonoBehaviour {

    const float MARS_DAY_SEC = 88775;

	public float planetSpeedRotationDegPerSec = 360.0f / MARS_DAY_SEC;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void LateUpdate () {
	
		transform.Rotate(-Vector3.up * Time.deltaTime * planetSpeedRotationDegPerSec);
	}
}
