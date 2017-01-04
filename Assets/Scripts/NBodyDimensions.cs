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
            transform.position = (n.transform.position - p.transform.position) * NBodyToModelScaleFactor;
        }
	}
}
