using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Autopilot : MonoBehaviour
{

    public GameController gameController;

    public float UpdateTrackTime = 0.2f;
    public float MaxRCSAutoBurnSec = 10;
    public float MinDeltaVforBurn = 0.1f;
    public float InterceptDeltaV = 30;
    public float MinRotToTargetDeltaTheta = 0.2f;
    public float MinAPNGDeltaTheta = 1f;
    public float MinMainEngineBurnSec = 0.5f;
    public float RendezvousDistM = 100;
    public float StrafeDistM = 150f;
    public float GunRangeM = 1000f;
    public float MinFireTargetAngle = 0.5f;

    private float NavigationalConstant = 3;
    private float NavigationalConstantAPNG = 10;

    private RocketShip ship;
    private Weapon mainGun;
    private bool cmgActive = false;
    public static readonly List<Command> Commands = new List<Command>()
    {
        Command.OFF,
        Command.KILL_ROTATION,
        Command.FACE_TARGET,
        Command.ACTIVE_TRACK,
        Command.KILL_REL_V,
        Command.INTERCEPT,
        Command.STRAFE,
        Command.RENDEZVOUS,
        Command.DOCK
    };

    public enum Command
    {
        OFF,
        KILL_ROTATION,
        FACE_TARGET,
        ACTIVE_TRACK,
        KILL_REL_V,
        INTERCEPT,
        STRAFE,
        RENDEZVOUS,
        DOCK
    }

    public static Command CommandFromInt(int index)
    {
        Command command;
        if (index >= 0 && index < Commands.Count)
        {
            command = Commands[index];
        }
        else
        {
            command = Command.OFF;
        }
        return command;
    }

    Stack<IEnumerator> callStack = new Stack<IEnumerator>();

    void Start()
    {
        ship = GetComponent<RocketShip>();
        mainGun = GetComponent<PlayerShip>().MainGun;
    }

    void Update()
    {

    }

    private void KillRot()
    {
        AutopilotOff();
        ship.MainEngineCutoff();
        PushAndStartCoroutine(KillRotCo());
    }

    private void AutopilotOff()
    {
        StopAll();
        ship.MainEngineCutoff();
        cmgActive = false;
    }

    public bool IsRot()
    {
        return cmgActive;
    }
    
    public void ExecuteCommand(Command command, GameObject target)
    {
        switch (command)
        {
            case Command.OFF:
                AutopilotOff();
                break;
            case Command.KILL_ROTATION:
                KillRot();
                break;
            case Command.FACE_TARGET:
                TurnToTarget(target);
                break;
            case Command.ACTIVE_TRACK:
                ActiveTrackTarget(target);
                break;
            case Command.KILL_REL_V:
                KillRelV(target);
                break;
            case Command.INTERCEPT:
                APNGToTarget(target);
                break;
            case Command.STRAFE:
                Strafe(target);
                break;
            case Command.RENDEZVOUS:
                Rendezvous(target);
                break;
            case Command.DOCK:
                Dock(target);
                break;
        }
    }

    private void ActiveTrackTarget(GameObject target)
    {
        AutopilotOff();
        PushAndStartCoroutine(RotTrackTarget(target, false));
    }

    private void TurnToTarget(GameObject target)
    {
        AutopilotOff();
        PushAndStartCoroutine(RotTrackTarget(target, true));
    }

    private void KillRelV(GameObject target)
    {
        AutopilotOff();
        PushAndStartCoroutine(KillRelVCo(target));
    }

    private void APNGToTarget(GameObject target)
    {
        AutopilotOff();
        PushAndStartCoroutine(APNGToTargetCo(target));
    }

    private void Strafe(GameObject target)
    {
        AutopilotOff();
        PushAndStartCoroutine(StrafeTargetCo(target));
    }

    private void Rendezvous(GameObject target)
    {
        AutopilotOff();
        PushAndStartCoroutine(RendezvousCo(target));
    }
    
    private void Dock(GameObject target)
    {
        AutopilotOff();
    }

    #region CoroutineHandlers

    Coroutine PushAndStartCoroutine(IEnumerator routine)
    {
        callStack.Push(routine);
        return StartCoroutine(routine);
    }

    void PopAndStop()
    {
        if (callStack.Count > 0)
        {
            IEnumerator active = callStack.Pop();
            StopCoroutine(active);
        }
    }

    void StopAll()
    {
        while (callStack.Count > 0)
        {
            PopAndStop();
        }
    }

    void PopCoroutine()
    {
        if (callStack.Count > 0)
        {
            callStack.Pop();
        }
    }

    #endregion

    #region Coroutines

    IEnumerator KillRotCo()
    {
        for (;;)
        {
            bool convergedSpin = ship.KillRotation();
            if (convergedSpin)
            {
                cmgActive = false;
                PopCoroutine();
                yield break;
            }
            else
            {
                cmgActive = true;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator RotToUnitVec(Vector3 b)
    {
        return RotToUnitVec(b, MinRotToTargetDeltaTheta);
    }

    IEnumerator RotToUnitVec(Vector3 b, float deltaTheta)
    {
        for (;;)
        {
            Quaternion q = Quaternion.LookRotation(b);
            bool converged = ship.ConvergeSpin(q, MinRotToTargetDeltaTheta);
            if (converged)
            {
                cmgActive = false;
                PopCoroutine();
                yield break;
            }
            else
            {
                cmgActive = true;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator RotTrackTarget(GameObject target, bool breakable)
    {
        return RotTrackTarget(target, breakable, MinRotToTargetDeltaTheta);
    }

    IEnumerator RotTrackTargetAPNG(GameObject target)
    {
        return RotTrackTarget(target, true, MinAPNGDeltaTheta);
    }

    IEnumerator RotTrackTarget(GameObject target, bool breakable, float deltaTheta)
    {
        Vector3 prevTVec = (target.transform.position - transform.parent.transform.position).normalized;
        float prevTimer = UpdateTrackTime;
        for (;;)
        {
            Vector3 tVec = (target.transform.position - transform.parent.transform.position).normalized;
            if (prevTimer <= 0)
            {
                prevTVec = tVec;
                prevTimer = UpdateTrackTime;
            }
            else
            {
                prevTimer -= Time.deltaTime;
            }
            Vector3 deltaB = (tVec - prevTVec) / UpdateTrackTime;
            Vector3 b = tVec + NavigationalConstant * deltaB;
            Quaternion q = Quaternion.LookRotation(b);
            bool converged = ship.ConvergeSpin(q, MinRotToTargetDeltaTheta);
            if (converged)
            {
                cmgActive = false;
                if (breakable)
                {
                    PopCoroutine();
                    yield break;
                }
            }
            else
            {
                cmgActive = true;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator RotToTarget(GameObject target)
    {
        for (;;)
        {
            Vector3 b = CalcVectorToTarget(target).normalized;
            Quaternion q = Quaternion.LookRotation(b);
            bool converged = ship.ConvergeSpin(q, MinRotToTargetDeltaTheta);
            if (converged)
            {
                cmgActive = false;
                PopCoroutine();
                yield break;
            }
            else
            {
                cmgActive = true;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    private float MainGunRangeM = 1000;

    IEnumerator RotShootTargetAPNG(GameObject target)
    {
        bool breakable = true;
        float deltaTheta = MinAPNGDeltaTheta;
        Vector3 prevTVec = (target.transform.position - transform.parent.transform.position).normalized;
        float prevTimer = UpdateTrackTime;
        for (;;)
        {
            Vector3 tVec = (target.transform.position - transform.parent.transform.position).normalized;
            if (prevTimer <= 0f)
            {
                prevTVec = tVec;
                prevTimer = UpdateTrackTime;
            }
            else
            {
                prevTimer -= Time.deltaTime;
            }
            Vector3 deltaB = (tVec - prevTVec) / UpdateTrackTime;
            Vector3 b = tVec + NavigationalConstant * deltaB;
            Quaternion q = Quaternion.LookRotation(b);
            bool converged = ship.ConvergeSpin(q, MinRotToTargetDeltaTheta);
            float targetAngle = Quaternion.Angle(transform.rotation, q);
            bool aligned = targetAngle <= MinFireTargetAngle;
            float dist;
            PhysicsUtils.CalcDistance(transform, target, out dist);
            bool inRange = dist <= MainGunRangeM;
            bool targetActive = gameController.IsEnemyActive(target);
            if (aligned && inRange && targetActive)
            {
                yield return PushAndStartCoroutine(FireGunCo());
            }
            if (converged)
            {
                cmgActive = false;
                if (breakable)
                {
                    PopCoroutine();
                    yield break;
                }
            }
            else
            {
                cmgActive = true;
            }
            yield return new WaitForEndOfFrame();
        }
    }


   IEnumerator FireGunCo()
   {
       yield return mainGun.AIFiringCo();
       PopCoroutine();
    }

    IEnumerator RotThenBurn(Vector3 b, float sec)
    {
        yield return PushAndStartCoroutine(RotToUnitVec(b));
        yield return PushAndStartCoroutine(MainEngineBurnSec(sec));
    }

    IEnumerator RotThenBurnAPNG(Vector3 b, float sec)
    {
        yield return PushAndStartCoroutine(RotToUnitVec(b, MinAPNGDeltaTheta));
        yield return PushAndStartCoroutine(MainEngineBurnSec(sec));
    }

    IEnumerator MainEngineBurnSec(float sec)
    {
        float burnTimer = sec;
        if (burnTimer > 0f)
        {
            ship.MainEngineGo();
        }
        for (;;)
        {
            if (burnTimer <= 0f)
            {
                ship.MainEngineCutoff();
                PopCoroutine();
                yield break;
            }
            burnTimer -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator KillRelVCo(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        Vector3 negVec = -1f * relVelUnit;
        float sec = CalcDeltaVBurnSec(relv);
        if (sec >= MinMainEngineBurnSec)
        {
            yield return PushAndStartCoroutine(RotThenBurn(negVec, sec));
        }
        else
        {
            PopCoroutine();
            yield break;
        }
    }
    
    IEnumerator APNGRotateBurnCo(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        Vector3 f = relVelUnit;
        Vector3 a = CalcAPNG(target.transform.position, relv, relVelUnit);
        if (relv < 0f)
        {
            f = -f;
            a = Vector3.zero;
        }
        float deltaVA = a.magnitude;
        float deltaVF;
        float deltaV = ship.CurrentMainEngineAccPerSec();
        if (deltaVA >= deltaV)
        {
            deltaVA = deltaV;
            deltaVF = 0f;
        }
        else
        {
            deltaVF = deltaV - deltaVA;
        }
        Vector3 thrust = ((deltaVA / deltaV) * a.normalized + (deltaVF / deltaV) * f).normalized;
        yield return PushAndStartCoroutine(RotToUnitVec(thrust, MinAPNGDeltaTheta));
    }

    IEnumerator StrafeCo(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        Vector3 f = relVelUnit;

        // break if close enough
        float timeToRot = Turn180Sec(); // swing pass
        float timeToTgt = (dist - StrafeDistM)/ Mathf.Abs(relv);
        float mecoTime = NavigationalConstant * timeToRot; // include aim
        //if (relv > 0 && mecoTime > timeToTgt)
        if (dist <= GunRangeM || (relv > 0 && mecoTime > timeToTgt))
        {
            ship.MainEngineCutoff();
            yield break;
        }
        else
        {
            // target should stand off from ship
            Quaternion normalRot = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(relVelUnit, -relVelUnit), 90);
            Vector3 offsetVec = StrafeDistM * (normalRot * relVelUnit);
            Vector3 strafeTarget = target.transform.position + offsetVec;
            Vector3 a = CalcAPNG(strafeTarget, relv, relVelUnit);
            if (relv < 0f)
            {
                f = -f;
                a = Vector3.zero;
            }
            float deltaVA = a.magnitude;
            float deltaVF;
            float deltaV = ship.CurrentMainEngineAccPerSec();
            if (deltaVA >= deltaV)
            {
                deltaVA = deltaV;
                deltaVF = 0f;
            }
            else
            {
                deltaVF = deltaV - deltaVA;
            }
            Vector3 thrust = ((deltaVA / deltaV) * a.normalized + (deltaVF / deltaV) * f).normalized;
            yield return PushAndStartCoroutine(RotToUnitVec(thrust, MinAPNGDeltaTheta));
        }
    }

    private float AimTimeSec = 3.55f;

    private GameObject CreateVirtualApproachTarget(GameObject targetDock)
    {
        float radius = gameController.TargetData().GetTargetRadius(targetDock);
        float distFromDock = radius + RendezvousDistM;
        GameObject approach = PointForward(targetDock, distFromDock);
        return approach;
    }

    private GameObject PointForward(GameObject targetDock, float distFromDock)
    {
        Vector3 approachPos = targetDock.transform.position + distFromDock * targetDock.transform.forward;
        Quaternion dockQ = targetDock.transform.rotation;
        Quaternion approachRot = Quaternion.Euler(0f, 180f, 0f) * dockQ;
        GameObject g = new GameObject();
        g.transform.position = approachPos;
        g.transform.rotation = approachRot;
        return g;
    }

    IEnumerator RendezvousCo(GameObject targetDock)
    {
        for (;;)
        {
            GameObject targetApproach = CreateVirtualApproachTarget(targetDock);
            if (ship.IsMainEngineGo())
            {
                ship.MainEngineCutoff();
            }
            yield return PushAndStartCoroutine(KillRelVCo(targetDock));
            float dist;
            PhysicsUtils.CalcDistance(transform, targetApproach, out dist);
            float minBurnDist = MinBurnDist(1f);
            if (dist < minBurnDist)
            {
                yield return PushAndStartCoroutine(RotToTarget(targetDock));
                yield break;
            }
            else
            {
                float idealBurnSec = IdealBurnSec(dist);
                float burnSec = Mathf.Max(1f, idealBurnSec);
                yield return PushAndStartCoroutine(RotToTarget(targetApproach));
                yield return PushAndStartCoroutine(MainEngineBurnSec(burnSec));
                yield return PushAndStartCoroutine(KillRelVCo(targetDock));
            }
        }
    }

    public float MinBurnDist(float sec)
    {
        return MinBurnDist(sec, ship.CurrentRCSAccelerationPerSec(), ship.CurrentRCSAngularDegPerSec());
    }

    public float MinBurnDist(float sec, float mainEngineAcc, float angularAcc)
    {
        float t = sec;
        float a = mainEngineAcc;
        float ao = angularAcc;
        float burnDist = 0.5f * a * Mathf.Pow(t, 2f);
        float stopDist = burnDist;
        float turnDist = Turn180Sec(ao);
        float dist = burnDist + turnDist + stopDist;
        return dist;
    }

    public float IdealBurnSec(float dist)
    {
        return IdealBurnSec(dist, ship.CurrentMainEngineAccPerSec(), ship.CurrentRCSAngularDegPerSec());
    }

    public float IdealBurnSec(float dist, float mainEngineAcc, float angularAcc)
    {
        float d = dist;
        float a = mainEngineAcc;
        float tr = Turn180Sec(angularAcc);
        float t = (-tr + Mathf.Sqrt(Mathf.Pow(tr, 2f) + 4f * (d / a))) / 2f;
        return t;
    }

    public float Turn180Sec()
    {
        return Turn180Sec(ship.CurrentRCSAngularDegPerSec());
    }

    public float Turn180Sec(float angularAcc)
    {
        return 2f * TurnSec(90f, angularAcc) + AimTimeSec;
    }

    public float TurnSec(float angle, float angularAcc)
    {
        float o = angle;
        float a = angularAcc;
        float t = Mathf.Sqrt(2f * o / a);
        return t;
    }

    IEnumerator APNGToTargetCo(GameObject target)
    {
        ship.MainEngineGo();
        for (;;)
        {
            yield return PushAndStartCoroutine(APNGRotateBurnCo(target));
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator StrafeTargetCo(GameObject target)
    {
        ship.MainEngineGo();
        for (;;)
        {
            if (ship.IsMainEngineGo())
            {
                yield return PushAndStartCoroutine(StrafeCo(target));
                yield return new WaitForEndOfFrame();
            }
            else
            {
                yield return PushAndStartCoroutine(RotShootTargetAPNG(target));
                yield return new WaitForEndOfFrame();
            }
        }
    }


    #endregion

    #region Utility Functions

    private Vector3 CalcVectorToTarget(GameObject target)
    {
        Vector3 b = target.transform.position - transform.parent.transform.position;
        return b;
    }


    private float CalcDeltaVBurnSec(float deltaV)
    {
        float mainEngineA = ship.CurrentMainEngineAccPerSec();
        float sec = deltaV / mainEngineA;
        //sec = sec > MinMainEngineBurnSec ? sec : 0;
        sec = sec > 0.1f ? sec : 0;
        return sec;
    }

    private Vector3 CalcAPNG(Vector3 position, float relv, Vector3 relVelUnit)
    {
        float N = NavigationalConstantAPNG;
        Vector3 vr = relv * -relVelUnit;
        Vector3 r = position - transform.parent.transform.position;
        Vector3 o = Vector3.Cross(r, vr) / Vector3.Dot(r, r);
        Vector3 a = Vector3.Cross(-N * Mathf.Abs(vr.magnitude) * r.normalized, o);
        return a;
    }

    private Vector3 CalcAPNGStrafe(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        float N = NavigationalConstantAPNG;
        Vector3 vr = relv * -relVelUnit;
        Vector3 r = CalcVectorToTarget(target);
        Vector3 o = Vector3.Cross(r, vr) / Vector3.Dot(r, r);
        Vector3 a = Vector3.Cross(-N * Mathf.Abs(vr.magnitude) * r.normalized, o);
        return a;
    }

    #endregion

}
