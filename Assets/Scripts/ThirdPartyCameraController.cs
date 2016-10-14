using UnityEngine;
using System.Collections;

public class ThirdPartyCameraController : MonoBehaviour {

    public GameObject player;
    private Vector3 cameraOffset = Vector3.zero;

	// Use this for initialization
	void Start () {
	}
	
    public void UpdatePlayer(GameObject newPlayer)
    {
        player = newPlayer;
        cameraOffset = transform.position - player.transform.position;
    }

    // Update is called once per frame
    void Update () {
        if (player != null && player.activeInHierarchy)
        {
            transform.position = cameraOffset + player.transform.position;
        }
    }
}
