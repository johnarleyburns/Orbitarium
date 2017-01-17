using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrionRCS : MonoBehaviour, IRCSModule {

    public ParticleSystem[] UpPlumes;
    public ParticleSystem[] DownPlumes;
    public ParticleSystem[] ForePlumes;
    public ParticleSystem[] BackPlumes;
    public ParticleSystem[] LeftPlumes;
    public ParticleSystem[] RightPlumes;

    private static float rcsMin = 0.01f;
    private bool anyPlay = true;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void RCSBurst(Vector3 dir)
    {
        Vector3 v = dir.normalized;
        Play(Vector3.Dot(dir, -transform.up), UpPlumes);
        Play(Vector3.Dot(dir, transform.up), DownPlumes);
        Play(Vector3.Dot(dir, -transform.forward), ForePlumes);
        Play(Vector3.Dot(dir, transform.forward), BackPlumes);
        Play(Vector3.Dot(dir, transform.right), LeftPlumes);
        Play(Vector3.Dot(dir, -transform.right), RightPlumes);
    }

    private void Play(float rcsDot, ParticleSystem[] rcs)
    {
        if (rcsDot > rcsMin && rcs != null)
        {
            foreach (ParticleSystem p in rcs)
            {
                if (!p.isPlaying)
                {
                    p.Play();
                }
            }
            anyPlay = true;
        }
    }

    private void Stop(ParticleSystem[] rcs)
    {
        if (rcs != null)
        {
            foreach (ParticleSystem p in rcs)
            {
                if (p.isPlaying)
                {
                    p.Stop();
                }
            }
        }
    }

    public void RCSAngularBurst(Quaternion dir)
    {
    }

    public void RCSCutoff()
    {
        if (anyPlay)
        {
            Stop(UpPlumes);
            Stop(DownPlumes);
            Stop(ForePlumes);
            Stop(BackPlumes);
            Stop(LeftPlumes);
            Stop(RightPlumes);
            anyPlay = false;
        }
    }

}
