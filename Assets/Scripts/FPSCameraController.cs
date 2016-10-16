using UnityEngine;
using System.Collections;

public class FPSCameraController : MonoBehaviour
{

    public GameObject player;
    public float duration = 2f;
    public float speed = 20f;
    public float magnitude = 2f;
    public AnimationCurve damper = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.9f, .33f, -2f, -2f), new Keyframe(1f, 0f, -5.65f, -5.65f));
    public bool testShake = false;

    private Vector3 cameraOffset = Vector3.zero;
    private Quaternion shakeRotation;
    private float shakeElapsed = 0;
    private bool shaking = false;
    private float shakeDuration = 0;
    private Vector3 originalPos = Vector3.zero;

    void Awake()
    {
        originalPos = transform.position;
    }

    // Use this for initialization
    void Start()
    {
        if (testShake)
        {
            PlayShake();
        }
        else
        {
            StopShake();
        }
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
            if (shaking)
            {
                ApplyShake();
                transform.rotation = player.transform.rotation * shakeRotation;
            }
            else
            {
                transform.rotation = player.transform.rotation;
            }
        }
    }

    public void PlayShake(float dur)
    {
        shakeElapsed = 0;
        shakeRotation = Quaternion.identity;
        shakeDuration = dur;
        shaking = true;
    }

    public void PlayShake()
    {
        PlayShake(duration);
    }

    public static float CONTINUOUS_SHAKE_DURATION;
    public void StartContinuousShake()
    {
        if (!shaking || shakeDuration < CONTINUOUS_SHAKE_DURATION)
        {
            PlayShake(CONTINUOUS_SHAKE_DURATION);
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
            ? (damper.Evaluate(shakeElapsed / duration) * magnitude)
            : magnitude;
        float x = (Mathf.PerlinNoise(Time.time * speed, 0f) * damperedMag) - (damperedMag / 2f);
        float y = (Mathf.PerlinNoise(0f, Time.time * speed) * damperedMag) - (damperedMag / 2f);
        float z = (x + y) / 2;
        shakeRotation = Quaternion.Euler(x, y, z);
    }

}
