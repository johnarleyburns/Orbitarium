using UnityEngine;
using System.Collections;

public class PhysicsUtils : MonoBehaviour {

    public static float minRelVtoExplode = 5;
    public static string NeverBounceTag = "Projectile";

    public static bool ShouldBounce(GameObject myNBodyChild, GameObject otherBody)
    {
        float relVelDummy;
        return ShouldBounce(myNBodyChild, otherBody, out relVelDummy);
    }

    public static bool ShouldBounce(GameObject myNBodyChild, GameObject otherBody, out float relVel)
    {
        if (otherBody.tag == NeverBounceTag || myNBodyChild.tag == NeverBounceTag)
        {
            relVel = 0;
            return false;
        }
        else
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


}
