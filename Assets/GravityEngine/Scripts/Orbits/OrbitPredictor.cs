using UnityEngine;
using System.Collections;

/// <summary>
/// Orbit predictor.
/// An in-scene object that will determine the future orbit based on the current velocity. 
/// Depending on the velocity the orbit may be an ellipse or a hyperbola. This class
/// requires a delegate of each type to compute the orbital path. 
///
/// Orbit prediction is based on the two-body problem and is with respect to one other
/// body (presumably the dominant source of gravity for the affected object). 
///
/// The general orbit prediction problem is significantly harder - it would require simulating the
/// entire scene into the future - re-computing whenever user input was provided. Not practical.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class OrbitPredictor: MonoBehaviour  {

	// Needs to have an Ellipse and Hyper delegates, and need to calculate OrbitData

	public int numPoints = 100; 
	public GameObject body;
	public GameObject centerBody;

	private NBody nbody;  
	private NBody aroundNBody; 
	private OrbitData orbitData; 
	private EllipseBase ellipseBase; 
	private OrbitHyper orbitHyper; 

	private LineRenderer lineR;
	private bool initOk;

	// Use this for initialization
	void Start () {

		nbody = body.GetComponent<NBody>();
		if (nbody == null) {
			Debug.LogWarning("Cannot show orbit - Body requires NBody component");
			return;
		}
		aroundNBody = centerBody.GetComponent<NBody>();
		if (aroundNBody == null) {
			Debug.LogWarning("Cannot show orbit - centerBody requires NBody component");
			return;
		}
		initOk = true;
		lineR = GetComponent<LineRenderer>();
		lineR.SetVertexCount(numPoints);
		orbitData = new OrbitData(); 

		ellipseBase = transform.gameObject.AddComponent<EllipseBase>();
		ellipseBase.centerObject = centerBody;

		orbitHyper = transform.gameObject.AddComponent<OrbitHyper>();
		orbitHyper.centerObject = centerBody;
	}

	// Update is called once per frame
	void Update () {
		if (initOk) {
			// velocities not normally updated in NBody (to reduce CPU). Need to ask
			// object to update its velocity from Gravity Engine if engine is running
			if (GravityEngine.instance.GetEvolve()) {
				nbody.UpdateVelocity();
			}
			orbitData.SetOrbitForVelocity(nbody, aroundNBody);
			// Is the resulting orbit and ellipse or hyperbola?
			if (orbitData.ecc < 1f) {
				ellipseBase.InitFromOrbitData(orbitData);
				lineR.SetPositions(ellipseBase.OrbitPositions(numPoints));
			} else {
				orbitHyper.InitFromOrbitData(orbitData);
				lineR.SetPositions(orbitHyper.OrbitPositions(numPoints));
			}
		}
	}
}
