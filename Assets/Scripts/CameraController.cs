using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

    public GameObject player;
    private Vector3 cameraOffset;

	// Use this for initialization
	void Start () {
        cameraOffset = transform.position - player.transform.position;
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = (player.transform.rotation * cameraOffset) + player.transform.position;
        transform.rotation = player.transform.rotation;
	}
}
