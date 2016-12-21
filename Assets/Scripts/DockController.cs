﻿using UnityEngine;
using System.Collections;

public class DockController : MonoBehaviour {

    public GameController gameController;
    public float minRelVtoDamage = 1f;
    private FPSAudioController audioController;
    private FPSCameraController cameraController;

    // Use this for initialization
    void Start () {
        audioController = gameController.FPSCamera.GetComponent<FPSAudioController>();
        cameraController = gameController.FPSCamera.GetComponent<FPSCameraController>();
    }

    // Update is called once per frame
    void Update () {
	
	}
    /*
    void OnTriggerEnter(Collider collider)
    {
        if (gameController != null)
        {
            GameObject otherBody = collider.attachedRigidbody.gameObject;
            float relVel;
            if (PhysicsUtils.ShouldBounce(gameObject, otherBody, out relVel))
            {
                if (relVel >= minRelVtoDamage)
                {
                    cameraController.PlayCollisionShake();
                }
                else
                {
                    PerformDock(otherBody);
                }
            }
            else
            {
                // let player handle game over
                ExplodeDock();
            }
        }
    }

    private void PerformDock(GameObject otherShip)
    {
        audioController.Play(FPSAudioController.AudioClipEnum.SPACESHIP_DOCK);
    }

    private void ExplodeDock()
    {

    }
    */
}