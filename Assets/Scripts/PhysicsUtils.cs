using UnityEngine;
using System.Collections;

public class PhysicsUtils : MonoBehaviour {

    public static readonly float g = 9.81f; // m/s^2
    public static float minRelVtoExplode = 5f;
    public static float minRelVtoDock = 0.1f;
    public static float maxRelVtoDock = 1f;
    public static float maxThetatoDock = 5f;
    public static float maxDistToDock = 2f;
    public static string NeverBounceTag = "Projectile";
    public static string[] FusedExplosionTags = { "Missile" };

    public static bool Fused(GameObject g)
    {
        bool explodes = false;
        foreach (string tag in FusedExplosionTags)
        {
            if (g.tag == tag)
            {
                explodes = true;
                break;
            }
        }
        return explodes;
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
            GameObject otherNBody = NUtils.GetNBodyGameObject(otherBody);
            GameObject myNBody = NUtils.GetNBodyGameObject(myNBodyChild);
            Vector3 relVelVec =
                GravityEngine.instance.GetVelocity(otherNBody)
                -
                GravityEngine.instance.GetVelocity(myNBody);
            relVel = relVelVec.magnitude;
            bool bouncing = relVel < minRelVtoExplode;
            return bouncing;
        }
    }

    public static bool ShouldDock(GameObject myNBodyChild, GameObject otherBody)
    {
        bool dock = false;
        if (otherBody.tag == "Dock" && (myNBodyChild.tag == "Player" || myNBodyChild.tag == "Enemy"))
        {
            RocketShip rocket = myNBodyChild.GetComponent<RocketShip>();
            float dist = Vector3.Distance(rocket.DockingPort.transform.position, otherBody.transform.position);
            GameObject otherNBody = NUtils.GetNBodyGameObject(otherBody);
            GameObject myNBody = NUtils.GetNBodyGameObject(myNBodyChild);
            Vector3 relVelVec =
                GravityEngine.instance.GetVelocity(otherNBody)
                -
                GravityEngine.instance.GetVelocity(myNBody);
            float relVel = relVelVec.magnitude;
            Transform dockGhostModel = otherBody.transform.GetChild(0).GetChild(0).transform;
            float relTheta = Quaternion.Angle(myNBodyChild.transform.rotation, dockGhostModel.rotation);
            bool isRelv = relVel >= minRelVtoDock && relVel <= maxRelVtoDock;
            bool isPos = dist <= maxDistToDock;
            bool isAligned = relTheta <= maxThetatoDock;
            if (isRelv && isPos && isAligned)
            {
                dock = true;
            }
        }
        return dock;
    }

    public static Vector3 CalcRelV(Transform source, GameObject target)
    {
        Vector3 myVel = GravityEngine.instance.GetVelocity(NUtils.GetNBodyGameObject(source.gameObject));
        Vector3 targetVel = GravityEngine.instance.GetVelocity(NUtils.GetNBodyGameObject(target));
        Vector3 relVec = myVel - targetVel;
        return relVec;
    }

    public static void CalcRelV(Transform source, GameObject target, out Vector3 targetVec, out float relv, out Vector3 relVelUnit)
    {
        Vector3 myVel = GravityEngine.instance.GetVelocity(NUtils.GetNBodyGameObject(source.gameObject));
        Vector3 targetVel = GravityEngine.instance.GetVelocity(NUtils.GetNBodyGameObject(target));
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

    public static void CalcDistance(Vector3 s, Vector3 t, out float dist)
    {
        dist = (t - s).magnitude;
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
