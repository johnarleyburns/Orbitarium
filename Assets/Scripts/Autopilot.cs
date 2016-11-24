﻿using UnityEngine;
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
    public float RendevousDistToVFactor = 0.01f;
    public float RendevousMarginVPct = 0.5f;
    public float StrafeDistM = 100f;

    private float NavigationalConstant = 3;
    private float NavigationalConstantAPNG = 10;

    private RocketShip ship;
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
            //q = Quaternion.Euler(q.eulerAngles.x, q.eulerAngles.y, transform.rotation.eulerAngles.z);
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
            //q = Quaternion.Euler(q.eulerAngles.x, q.eulerAngles.y, transform.rotation.eulerAngles.z);
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
            //q = Quaternion.Euler(q.eulerAngles.x, q.eulerAngles.y, transform.rotation.eulerAngles.z);
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
        if (burnTimer > 0)
        {
            ship.MainEngineGo();
        }
        for (;;)
        {
            if (burnTimer <= 0)
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
        Vector3 negVec = -1 * relVelUnit;
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
        if (relv < 0)
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
            deltaVF = 0;
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
        float angleToTgt = 180; // swing pass
        float timeToRot = Mathf.Sqrt(angleToTgt / ship.CurrentRCSAngularDegPerSec());
        float timeToTgt = dist / Mathf.Abs(relv);
        float mecoTime = NavigationalConstant * timeToRot; // include aim
        if (relv > 0 && mecoTime > timeToTgt)
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
            if (relv < 0)
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
                deltaVF = 0;
            }
            else
            {
                deltaVF = deltaV - deltaVA;
            }
            Vector3 thrust = ((deltaVA / deltaV) * a.normalized + (deltaVF / deltaV) * f).normalized;
            yield return PushAndStartCoroutine(RotToUnitVec(thrust, MinAPNGDeltaTheta));
        }
    }

    IEnumerator RendezvousCo(GameObject target)
    {
        for (;;)
        {
            float dist;
            float relv;
            Vector3 relVelUnit;
            PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
            float secToTarget = dist / relv;
            float secToStop = relv / ship.CurrentMainEngineAccPerSec() + 10; // estimated rotate time;
            if (secToTarget < secToStop)
            {
                yield return PushAndStartCoroutine(KillRelVCo(target));
                yield return PushAndStartCoroutine(RotToTarget(target));
                yield break;
            }
            yield return PushAndStartCoroutine(APNGRotateBurnCo(target));
            yield return new WaitForEndOfFrame();
        }
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
                yield return PushAndStartCoroutine(RotTrackTargetAPNG(target));
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
