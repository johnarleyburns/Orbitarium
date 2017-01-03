﻿using UnityEngine;
using System.Collections;

public class FPSCameraController : MonoBehaviour
{

    public GameObject player;
    public CameraDirection Direction;
    public float duration = 2f;
    public float speed = 20f;
    public float collisionShakeMagnitude = 10f;
    public float mainEngineShakeMagnitude = 2f;
    public float auxEngineShakeMagnitude = 0.3f;
    public AnimationCurve damper = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.9f, .33f, -2f, -2f), new Keyframe(1f, 0f, -5.65f, -5.65f));
    public enum CameraDirection
    {
        FORWARD,
        LEFT,
        RIGHT,
        REAR
    }
    private Vector3 cameraOffset = Vector3.zero;
    private Quaternion shakeRotation;
    private float shakeElapsed = 0;
    private bool shaking = false;
    private float shakeDuration = 0;
    private float shakeMagnitude = 0;
    private Vector3 originalPos = Vector3.zero;

    void Awake()
    {
        originalPos = transform.position;
    }

    // Use this for initialization
    void Start()
    {
        StopShake();
    }

    public void UpdatePlayer(GameObject newPlayer)
    {
        player = newPlayer;
        transform.position = originalPos;
        cameraOffset = transform.position - player.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (player != null && player.activeInHierarchy)
        {
            transform.position = (player.transform.rotation * cameraOffset) + player.transform.position;
            Quaternion newRotation;
            switch (Direction)
            {
                default:
                case CameraDirection.FORWARD:
                    newRotation = player.transform.rotation;
                    break;
                case CameraDirection.REAR:
                    newRotation = Quaternion.LookRotation(-player.transform.forward, player.transform.up);
                    break;
                case CameraDirection.RIGHT:
                    newRotation = Quaternion.LookRotation(player.transform.right, player.transform.up);
                    break;
                case CameraDirection.LEFT:
                    newRotation = Quaternion.LookRotation(-player.transform.right, player.transform.up);
                    break;
            }
            if (shaking)
            {
                ApplyShake();
                transform.rotation = shakeRotation * newRotation;
            }
            else
            {
                transform.rotation = newRotation;
            }
        }
    }

    private void PlayShake(float mag, float dur)
    {
        shakeElapsed = 0;
        shakeRotation = Quaternion.identity;
        shakeDuration = dur;
        shakeMagnitude = mag;
        shaking = true;
    }

    public void PlayCollisionShake()
    {
        PlayShake(duration, collisionShakeMagnitude);
    }

    private void PlayMainEngineShake()
    {
        PlayShake(duration, mainEngineShakeMagnitude);
    }

    private void PlayAuxEngineShake()
    {
        PlayShake(duration*.15f, auxEngineShakeMagnitude);
    }

    public void StartContinuousMainEngineShake()
    {
        if (!shaking)
        {
            PlayMainEngineShake();
        }
    }
    public void StartContinuousAuxEngineShake()
    {
        if (!shaking)
        {
            PlayAuxEngineShake();
        }
    }

    public void StopShake()
    {
        shaking = false;
        shakeRotation = Quaternion.identity;
        shakeElapsed = 0;
    }

    void ApplyShake()
    {
        shakeElapsed += Time.deltaTime;
        if (shakeElapsed > duration)
        {
            StopShake();
        }
        float damperedMag = (damper != null)
            ? (damper.Evaluate(shakeElapsed / duration) * shakeMagnitude)
            : mainEngineShakeMagnitude;
        float x = (Mathf.PerlinNoise(Time.time * speed, 0f) * damperedMag) - (damperedMag / 2f);
        float y = (Mathf.PerlinNoise(0f, Time.time * speed) * damperedMag) - (damperedMag / 2f);
        float z = (x + y) / 2;
        shakeRotation = Quaternion.Euler(x, y, z);
    }

}
