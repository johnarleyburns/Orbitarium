using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Autopilot : MonoBehaviour
{

    public GameController gameController;

    public int TrackNavigationFactor = 3;
    public float UpdateTrackTime = 0.2f;

    public float MaxRCSAutoBurnSec = 10;
    public float MinDeltaVforBurn = 0.1f;
    public float MinInterceptDeltaV = 20;
    public float InterceptDeltaV = 30;
    public float NavigationalConstant = 3;
    public float MinRotToTargetDeltaTheta = 0.2f;
    public float MinAPNGDeltaTheta = 1f;
    public float MinMainEngineBurnSec = 0.5f;
    public float RendevousDistToVFactor = 0.01f;
    public float RendevousMarginVPct = 0.5f;

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
        Command.STANDOFF,
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
        STANDOFF,
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

    public void ExecuteCommand(Command command)
    {
        ExecuteCommand(command, gameController.GetHUD().GetReferenceBody());

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
            case Command.STANDOFF:
                Standoff(target);
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

    private void Standoff(GameObject target)
    {
        AutopilotOff();
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
            Vector3 deltaB = (tVec - prevTVec)/UpdateTrackTime;
            Vector3 b = tVec + TrackNavigationFactor * deltaB;
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
    
    IEnumerator KillRelVCoAPNG(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        Vector3 negVec = -1 * relVelUnit;
        float sec = CalcDeltaVBurnSec(relv);
        sec = Mathf.Min(sec, 5);
        if (sec >= MinMainEngineBurnSec)
        {
            yield return PushAndStartCoroutine(RotThenBurnAPNG(negVec, sec));
        }
        else
        {
            PopCoroutine();
            yield break;
        }
    }

    IEnumerator APNGKillRelVCo(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        if (relv < 0)
        {
            yield return PushAndStartCoroutine(KillRelVCoAPNG(target));
        }
        else
        {
            PopCoroutine();
            yield break;
        }
    }

    IEnumerator APNGSpeedupCo(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        float neededDeltaV = MinInterceptDeltaV - relv;
        float sec = CalcDeltaVBurnSec(neededDeltaV);
        sec = Mathf.Min(sec, 5);
        if (sec > 0)
        {
            yield return PushAndStartCoroutine(RotTrackTargetAPNG(target));
            yield return PushAndStartCoroutine(MainEngineBurnSec(sec));
        }
        else
        {
            PopCoroutine();
            yield break;
        }
    }

    IEnumerator APNGRotateBurnCo(GameObject target)
    {
        Vector3 a = CalcAPNG(target);
        float deltaV = a.magnitude;
        float sec = CalcDeltaVBurnSec(deltaV);
        sec = Mathf.Min(sec, 5);
        if (sec > 0)
        {
            yield return PushAndStartCoroutine(RotThenBurnAPNG(a, sec));
        }
        else
        {
            PopCoroutine();
            yield break;
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
            yield return PushAndStartCoroutine(APNGKillRelVCo(target));
            yield return PushAndStartCoroutine(APNGSpeedupCo(target));
            yield return PushAndStartCoroutine(APNGRotateBurnCo(target));
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator APNGToTargetCo(GameObject target)
    {
        for (;;)
        {
            yield return PushAndStartCoroutine(APNGKillRelVCo(target));
            yield return PushAndStartCoroutine(APNGSpeedupCo(target));
            yield return PushAndStartCoroutine(APNGRotateBurnCo(target));
            yield return new WaitForEndOfFrame();
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

    private Vector3 CalcAPNG(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        float N = NavigationalConstant;
        Vector3 vr = relv * -relVelUnit;
        Vector3 r = CalcVectorToTarget(target);
        Vector3 o = Vector3.Cross(r, vr) / Vector3.Dot(r, r);
        Vector3 a = Vector3.Cross(-N * Mathf.Abs(vr.magnitude) * r.normalized, o);
        return a;
    }

    #endregion
    
}
