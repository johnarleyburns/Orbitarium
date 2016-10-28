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

    public static void CalcRelV(Transform source, GameObject target, out float dist, out float relv, out Vector3 relVelUnit)
    {
        dist = (target.transform.position - source.transform.position).magnitude;
        Vector3 myVel = GravityEngine.instance.GetVelocity(source.gameObject);
        Vector3 targetVel = GravityEngine.instance.GetVelocity(target);
        Vector3 relVel = myVel - targetVel;
        Vector3 targetPos = target.transform.position;
        Vector3 myPos = source.transform.position;
        Vector3 relLoc = targetPos - myPos;
        float relVelDot = Vector3.Dot(relVel, relLoc);
        float relVelScalar = relVel.magnitude;
        relv = Mathf.Sign(relVelDot) * relVelScalar;
        relVelUnit = relVel.normalized;
    }

}
