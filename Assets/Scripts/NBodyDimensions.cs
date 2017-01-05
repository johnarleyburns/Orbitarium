using UnityEngine;
using System.Collections;

public class NBodyDimensions : MonoBehaviour {

    public float RadiusM;
    public float NBodyToModelScaleFactor = 1000;
    public GameObject PlayerNBody;
    public GameObject NBody;
    public bool UpdatePosition = true;

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	    if (UpdatePosition && NBody != null && PlayerNBody != null)
        {
            NBody p = PlayerNBody.GetComponent<NBody>();
            NBody n = NBody.GetComponent<NBody>();
            DVector3 pVec;
            DVector3 nVec;
            GravityEngine.instance.GetPosition(p, out pVec);
            GravityEngine.instance.GetPosition(n, out nVec);
            double scale = NBodyToModelScaleFactor;
            DVector3 originOffset = nVec - pVec;
            DVector3 scaledOriginOffset = scale * originOffset;
            DVector3 scaledPos = scaledOriginOffset; // player always at 0 in near space
            transform.position = scaledPos.ToVector3();
        }
	}
}
