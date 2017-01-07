using UnityEngine;
using System.Collections;
using System;

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
            DVector3 relVelVec =
                GravityEngine.instance.GetVelocity(otherNBody)
                -
                GravityEngine.instance.GetVelocity(myNBody);
            relVel = (float)(NUtils.GetNBodyToModelScale(otherBody) * relVelVec.magnitude);
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
            DVector3 relVelVec =
                GravityEngine.instance.GetVelocity(otherNBody)
                -
                GravityEngine.instance.GetVelocity(myNBody);
            float relVel = (float)(NUtils.GetNBodyToModelScale(otherBody) * relVelVec.magnitude);
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
        DVector3 myVel = GravityEngine.instance.GetVelocity(NUtils.GetNBodyGameObject(source.gameObject));
        DVector3 targetVel = GravityEngine.instance.GetVelocity(NUtils.GetNBodyGameObject(target));
        DVector3 relVec = myVel - targetVel;
        return (NUtils.GetNBodyToModelScale(target) * relVec).ToVector3();
    }


    public static void CalcRelV(GameObject source, GameObject target, out Vector3 targetVec, out float relv, out Vector3 relVelUnit)
    {
        double scale = NUtils.GetNBodyToModelScale(target);
        GameObject sourceNBody = NUtils.GetNBodyGameObject(source);
        GameObject targetNBody = NUtils.GetNBodyGameObject(target);
        DVector3 myVel = GravityEngine.instance.GetVelocity(sourceNBody);
        DVector3 targetVel = GravityEngine.instance.GetVelocity(targetNBody);
        DVector3 relVel = myVel - targetVel;
        DVector3 sPos;
        DVector3 tPos;
        GravityEngine.instance.GetPosition(sourceNBody.GetComponent<NBody>(), out sPos);
        GravityEngine.instance.GetPosition(targetNBody.GetComponent<NBody>(), out tPos);
        DVector3 tVec = tPos - sPos;
        double relVelDot = DVector3.Dot(relVel, tVec);
        relv = (float)(scale * Math.Sign(relVelDot) * relVel.magnitude);
        targetVec = (scale * tVec).ToVector3();
        relVelUnit = relVel.normalized.ToVector3();
    }

    public static void CalcDistance(GameObject source, GameObject target, out float dist)
    {
        GameObject sNBody = NUtils.GetNBodyGameObject(source);
        GameObject tNBody = NUtils.GetNBodyGameObject(target);
        float scale = NUtils.GetNBodyToModelScale(target);
        dist = scale * (tNBody.transform.position - sNBody.transform.position).magnitude;
    }

    public static void CalcDockPlanar(Transform source, GameObject targetDock, float relv, Vector3 relunitvec, out float closingDist, out float closingRelv, out Vector2 planarVec)
    {
        Transform dockGhostModel = targetDock.transform.GetChild(0);
        Vector3 planeOrigin = dockGhostModel.position;
        Vector3 planeNormal = dockGhostModel.forward;
        Vector3 dockUp = dockGhostModel.up;
        GameObject emptyGO = new GameObject();
        emptyGO.name = "Dock Planar Virtual Point";
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
