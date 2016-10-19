using UnityEngine;
using System.Collections;

public class PhysicsUtils : MonoBehaviour {

    public static float minRelVtoExplode = 5;

    public static bool ShouldBounce(GameObject myNBodyChild, GameObject otherBody)
    {
        float relVelDummy;
        return ShouldBounce(myNBodyChild, otherBody, out relVelDummy);
    }

    public static bool ShouldBounce(GameObject myNBodyChild, GameObject otherBody, out float relVel)
    {
        Vector3 relVelVec =
            GravityEngine.instance.GetVelocity(otherBody.transform.parent.gameObject)
            -
            GravityEngine.instance.GetVelocity(myNBodyChild.transform.parent.gameObject);
        relVel = relVelVec.magnitude;
        bool bouncing = relVel < minRelVtoExplode;
        return bouncing;
    }


}
