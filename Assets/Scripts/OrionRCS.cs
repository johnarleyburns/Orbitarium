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
        RCSBurst(dir, rcsMin);
    }
    private void RCSBurst(Vector3 dir, float min)
    {
        Vector3 v = dir.normalized;
        Play(Vector3.Dot(dir, -transform.up), UpPlumes, min);
        Play(Vector3.Dot(dir, transform.up), DownPlumes, min);
        Play(Vector3.Dot(dir, -transform.forward), ForePlumes, min);
        Play(Vector3.Dot(dir, transform.forward), BackPlumes, min);
        Play(Vector3.Dot(dir, transform.right), LeftPlumes, min);
        Play(Vector3.Dot(dir, -transform.right), RightPlumes, min);
    }

    private void Play(float rcsDot, ParticleSystem[] rcs, float min)
    {
        if (rcsDot > min && rcs != null)
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

    private static float angularMin = 5f;

    public void RCSAngularBurst(Quaternion dir)
    {
        Vector3 e = Map180(dir.eulerAngles);
        if (e.x > angularMin)
        {
            Play(1, DownPlumes, 0);
        }
        if (e.x < -angularMin)
        {
            Play(1, UpPlumes, 0);
        }
        if (e.y > angularMin)
        {
            Play(1, RightPlumes, 0);
        }
        if (e.y < -angularMin)
        {
            Play(1, LeftPlumes, 0);
        }
        /*
        if (e.z > angularMin)
        {
            Play(1, ForePlumes, 0);
        }
        if (e.z < -angularMin)
        {
            Play(1, BackPlumes, 0);
        }
        */
    }

    private float Map180(float deg)
    {
        return deg > 180f ? -(360f - deg) : deg;
    }

    private Vector3 Map180(Vector3 v)
    {
        return new Vector3(Map180(v.x), Map180(v.y), Map180(v.z));
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
