using UnityEngine;
using System.Collections;

public class ThirdPartyCameraController : MonoBehaviour {

    public GameObject player;
    private Vector3 cameraOffset = Vector3.zero;

	// Use this for initialization
	void Start () {
	}
	
    public void UpdatePlayerPos()
    {
        cameraOffset = transform.position - player.transform.position;
    }

    // Update is called once per frame
    void Update () {
        transform.position = cameraOffset + player.transform.position;
	}
}
