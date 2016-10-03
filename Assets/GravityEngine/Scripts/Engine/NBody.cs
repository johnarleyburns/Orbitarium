using UnityEngine;
using System.Collections;

/// <summary>
/// N body.
///
/// Specifies the information required for NBody physics evolution of the associated game object. 
///
/// </summary>
public class NBody : MonoBehaviour {

	//! mass of object (mass scale in GravityEngine will be applied to get value used in simulation)
	public float mass;	

	//! Velocity 
	public Vector3 vel; 

	//! Automatically detect particle capture size from a child with a mesh
	public bool automaticParticleCapture = true;

	//! Particle capture radius. Particles closer than this will be inactivated.
	public double size = 0.1; 

	//! Opaque data maintained by the GravityEngine. Do not modify.
	public GravityEngine.EngineRef engineRef;  


	public void Awake() {
		// automatic size detection if there is an attached mesh filter
		if (automaticParticleCapture) {
			size = CalculateSize();
		}
	}

	public float CalculateSize() {
		foreach( Transform t in transform) {
			MeshFilter mf = t.gameObject.GetComponent<MeshFilter>();
			if (mf != null) {
				// cannot be sure it's a sphere, but assume it is
				return t.localScale.x/2f;
			}
		}
		// compound objects may not have a top level mesh
		return 1; 
	}

	/// <summary>
	/// Updates the velocity.
	/// The Gravity Engine does not copy back velocity updates during evolution. Calling this method causes
	/// an update to the velocity. 
	/// </summary>
	public void UpdateVelocity() {
		vel = GravityEngine.instance.GetVelocity(transform.gameObject);
	}
}
