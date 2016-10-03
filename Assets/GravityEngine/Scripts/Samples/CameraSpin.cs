using UnityEngine;
using System.Collections;

/// <summary>
/// Key control to roate camera boom using W-A-S-D keys.
///
/// Assumes the Main Camara is a child of the object holding this script with a local position offset
/// (the boom length) and oriented to point at this object. Then pressing the keys will spin the camera
/// around the object this script is attached to.
/// </summary>
public class CameraSpin : MonoBehaviour {

	//! Rate of spin (degrees per Update)
	public float spinRate = 1f; 

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKey(KeyCode.W)) {
			transform.rotation *= Quaternion.AngleAxis( spinRate, Vector3.right);
		} else if (Input.GetKey(KeyCode.S)) {
			transform.rotation *= Quaternion.AngleAxis( -spinRate, Vector3.right);
		} else if (Input.GetKey(KeyCode.D)) {
			transform.rotation *= Quaternion.AngleAxis( spinRate, Vector3.up);
		} else if (Input.GetKey(KeyCode.A)) {
			transform.rotation *= Quaternion.AngleAxis( -spinRate, Vector3.up);
		}		

	}
}
