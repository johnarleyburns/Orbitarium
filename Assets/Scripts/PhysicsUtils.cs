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

    public static void CalcRelV(Transform source, GameObject target, out Vector3 targetVec, out float relv, out Vector3 relVelUnit)
    {
        Vector3 myVel = GravityEngine.instance.GetVelocity(source.gameObject);
        Vector3 targetVel = GravityEngine.instance.GetVelocity(target);
        Vector3 relVel = myVel - targetVel;
        Vector3 targetPos = target.transform.position;
        Vector3 myPos = source.transform.position;
        targetVec = targetPos - myPos;
        float relVelDot = Vector3.Dot(relVel, targetVec);
        float relVelScalar = relVel.magnitude;
        relv = Mathf.Sign(relVelDot) * relVelScalar;
        relVelUnit = relVel.normalized;
    }

    public static void CalcDistance(Transform source, GameObject target, out float dist)
    {
        dist = (target.transform.position - source.transform.position).magnitude;
    }

    public static void CalcDockPlanar(Transform source, GameObject targetDock, float relv, Vector3 relunitvec, out float closingDist, out float closingRelv, out Vector2 planarVec)
    {
        Transform dockGhostModel = targetDock.transform.GetChild(0);
        Vector3 planeOrigin = dockGhostModel.position;
        Vector3 planeNormal = dockGhostModel.forward;
        Vector3 dockUp = dockGhostModel.up;
        GameObject emptyGO = new GameObject();
        Vector3 shipModelPoint = source.position;
        emptyGO.transform.position = planeOrigin;
        emptyGO.transform.rotation = Quaternion.LookRotation(planeNormal, dockUp);
        Vector3 localPoint = emptyGO.transform.InverseTransformPoint(shipModelPoint);
        localPoint.z = 0; // project to the plane.
        Destroy(emptyGO);
        planarVec = localPoint;

        Vector3 targetVec = planeOrigin - shipModelPoint;
        Vector3 relvec = Mathf.Abs(relv) * relunitvec;
        Vector3 planeVec = Vector3.ProjectOnPlane(targetVec, source.forward);
        Vector3 planeRelVec = Vector3.ProjectOnPlane(relvec, source.forward);
        Vector3 closingVec = targetVec - planeVec;
        Vector3 closingRelVec = relv * relunitvec - planeRelVec;
        float closingAngle = Mathf.Rad2Deg * Vector3.Angle(closingVec, closingRelVec);
        closingDist = closingVec.magnitude;
        closingRelv = (closingAngle <= 90 ? 1 : -1) * closingRelVec.magnitude;
    }

}
