﻿using UnityEngine;
using System.Collections;

// based on http://unitytipsandtricks.blogspot.com/2013/05/camera-shake.html
public class PerlinShake : MonoBehaviour
{
    public float duration = 2f;
    public float speed = 20f;
    public float magnitude = 2f;
    public AnimationCurve damper = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.9f, .33f, -2f, -2f), new Keyframe(1f, 0f, -5.65f, -5.65f));
    public bool testNormal = false;
    public bool testProjection = false;
    public bool testCamera = false;

    Vector3 originalPos;
    Vector3 originalCameraPos;


    void OnEnable()
    {
        originalPos = transform.localPosition;
        originalCameraPos = Camera.main.transform.localPosition;
    }


    void Update()
    {
        if (testNormal)
        {
            testNormal = false;
            StopAllCoroutines();
            StartCoroutine(Shake(transform, originalPos, duration, speed, magnitude, damper));
        }
        if (testProjection)
        {
            testProjection = false;
            StopAllCoroutines();
            StartCoroutine(ShakeCamera(Camera.main, duration, speed, magnitude, damper));
        }
        if (testCamera)
        {
            testCamera = false;
            StopAllCoroutines();
            StartCoroutine(Shake(Camera.main.transform, originalCameraPos, duration, speed, magnitude, damper));
        }
    }


    IEnumerator Shake(Transform transform, Vector3 originalPosition, float duration, float speed, float magnitude, AnimationCurve damper = null)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damperedMag = (damper != null) ? (damper.Evaluate(elapsed / duration) * magnitude) : magnitude;
            float x = (Mathf.PerlinNoise(Time.time * speed, 0f) * damperedMag) - (damperedMag / 2f);
            float y = (Mathf.PerlinNoise(0f, Time.time * speed) * damperedMag) - (damperedMag / 2f);
            transform.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);
            yield return null;
        }
        transform.localPosition = originalPosition;
    }


    IEnumerator ShakeCamera(Camera camera, float duration, float speed, float magnitude, AnimationCurve damper = null)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damperedMag = (damper != null) ? (damper.Evaluate(elapsed / duration) * magnitude) : magnitude;
            float x = (Mathf.PerlinNoise(Time.time * speed, 0f) * damperedMag) - (damperedMag / 2f);
            float y = (Mathf.PerlinNoise(0f, Time.time * speed) * damperedMag) - (damperedMag / 2f);
            // offset camera obliqueness - http://answers.unity3d.com/questions/774164/is-it-possible-to-shake-the-screen-rather-than-sha.html
            float frustrumHeight = 2 * camera.nearClipPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustrumWidth = frustrumHeight * camera.aspect;
            Matrix4x4 mat = camera.projectionMatrix;
            mat[0, 2] = 2 * x / frustrumWidth;
            mat[1, 2] = 2 * y / frustrumHeight;
            camera.projectionMatrix = mat;
            yield return null;
        }
        camera.ResetProjectionMatrix();
    }

}
