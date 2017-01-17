using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrionRCS : MonoBehaviour, IRCSModule {

    public ParticleSystem UpPlume1;
    public ParticleSystem UpPlume2;
    public ParticleSystem DownPlume1;
    public ParticleSystem DownPlume2;

    private static float rcsMin = 0.01f;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void RCSBurst(Vector3 dir)
    {
        Vector3 v = dir.normalized;
        float upPart = Vector3.Dot(dir, -transform.up);
        if (upPart > rcsMin && !UpPlume1.isPlaying)
        {
            UpPlume1.Play();
        }
        if (upPart > rcsMin && !UpPlume2.isPlaying)
        {
            UpPlume2.Play();
        }
        float downPart = -upPart;
        if (downPart > rcsMin && !DownPlume1.isPlaying)
        {
            DownPlume1.Play();
        }
        if (downPart > rcsMin && !DownPlume2.isPlaying)
        {
            DownPlume2.Play();
        }
    }

    public void RCSAngularBurst(Quaternion dir)
    {
    }

    public void RCSCutoff()
    {
        if (UpPlume1.isPlaying)
        {
            UpPlume1.Stop();
        }
        if (UpPlume2.isPlaying)
        {
            UpPlume2.Stop();
        }
        if (DownPlume1.isPlaying)
        {
            DownPlume1.Stop();
        }
        if (DownPlume2.isPlaying)
        {
            DownPlume2.Stop();
        }
    }

}
