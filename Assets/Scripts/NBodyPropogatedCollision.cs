using UnityEngine;
using System.Collections;

public class NBodyPropogatedCollision : MonoBehaviour {

    public IControllableShip ShipRef = null;

	// Use this for initialization
	void Start () {
        foreach (Component c in GetComponents<IControllableShip>())
        {
            ShipRef = c as IControllableShip;
            break;
        }
	}
	
	// Update is called once per frame
	void Update () {	
	}

    public void Bounce(GameObject otherBody, float relVel)
    {
        ShipRef.Bounce(otherBody, relVel);
    }

    public void ExplodeCollide(GameObject otherBody)
    {
        ShipRef.ExplodeCollide(otherBody);
    }

    public void PerformDock(GameObject myNBodyChild, GameObject otherBody)
    {
        ShipRef.Dock(otherBody);
    }

}
