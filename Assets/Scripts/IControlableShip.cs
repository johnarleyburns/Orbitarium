using UnityEngine;
using System.Collections;

public interface IControllableShip {

    void Bounce(GameObject otherBody, float relVel);

    void ExplodeCollide(GameObject otherBody);

    void Dock(GameObject dockModel, bool withSound = true);

}
