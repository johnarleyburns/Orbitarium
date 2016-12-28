using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Autopilot : MonoBehaviour
{

    public GameController gameController;

    public float UpdateTrackTime = 0.2f;
    public float MaxRCSAutoBurnSec = 60f;
    public float MinDeltaVforBurn = 0.1f;
    public float MaxStrafeSpeed = 30f;
    public float MinRotToTargetDeltaTheta = 0.2f;
    public float MinAPNGDeltaTheta = 1f;
    public float MinMainEngineBurnSec = 0.5f;
    public float MinAuxEngineBurnSec = 0.01f;
    public float RendezvousDistM = 50f;
    public float StrafeDistM = 150f;
    public float GunRangeM = 1000f;
    public float MinFireTargetAngle = 0.5f;
    public bool IsPlayer = false;

    private float NavigationalConstant = 3;
    private float NavigationalConstantAPNG = 10;

    private RocketShip ship;
    private ShipWeapons weapons;
    private bool cmgActive = false;
    private Command currentCommand = Command.OFF;

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
        Command.APPROACH,
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
        APPROACH,
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
        currentCommand = Command.OFF;
        ship = GetComponent<RocketShip>();
        weapons = GetComponent<ShipWeapons>();
        if (weapons != null)
        {
            weapons.SetGameController(gameController);
        }
    }

    void Update()
    {
        if (gameController != null)
        {
            switch (gameController.GetGameState())
            {
                case GameController.GameState.RUNNING:
                    break;
                case GameController.GameState.PAUSED:
                    break;
                default:
                    StopAll();
                    break;
            }
        }
    }

    public Command CurrentCommand()
    {
        return currentCommand;
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
        currentCommand = command;
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
            case Command.APPROACH:
                Approach(target);
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

    private void Approach(GameObject target)
    {
        AutopilotOff();
        PushAndStartCoroutine(ApproachCo(target));
    }

    private void Dock(GameObject target)
    {
        AutopilotOff();
        PushAndStartCoroutine(DockCo(target));
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
        if (callStack.Count == 0 && currentCommand != Command.OFF)
        {
            ExecuteCommand(Command.OFF, null);
            if (IsPlayer)
            {
                gameController.InputControl().PropertyChanged("CommandExecuted", Command.OFF);
            }
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
            bool converged = ship.ConvergeSpin(q, MinAPNGDeltaTheta);
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
            bool inRange = weapons == null ? true : dist <= weapons.MainGunRangeM;
            bool hasAmmo = weapons == null ? false : weapons.CurrentAmmo() > 0;
            bool gunsReady = GunsReady();
            bool targetActive = gameController.IsTargetActive(target);
            if (aligned && inRange && hasAmmo && gunsReady && targetActive)
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

    public void UserFireGun()
    {
        PushAndStartCoroutine(FireGunCo());
    }

    public bool GunsReady()
    {
        return weapons != null && weapons.MainGun != null && weapons.MainGun.ReadyToFire();
    }

    IEnumerator FireGunCo()
    {
        if (weapons != null && weapons.MainGun != null)
        {
            yield return weapons.MainGun.AIFiringCo();
        }
        else
        {
            yield break;
        }
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
        Vector3 targetVec;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out targetVec, out relv, out relVelUnit);
        Vector3 negVec = -1f * relVelUnit;
        float sec = CalcDeltaVMainBurnSec(relv);
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

    IEnumerator KillRelVRCSCo(GameObject target)
    {
        Vector3 targetVec;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out targetVec, out relv, out relVelUnit);
        Vector3 negVec = -1f * relVelUnit;
        float sec = CalcDeltaVBurnRCSSec(relv);
        if (sec >= ship.RCSBurnMinSec)
        {
            yield return PushAndStartCoroutine(RCSBurst(negVec, sec));
        }
        else
        {
            PopCoroutine();
            yield break;
        }
    }

    IEnumerator APNGRotateBurnCo(GameObject target)
    {
        Vector3 targetVec;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out targetVec, out relv, out relVelUnit);
        Vector3 f = relVelUnit;
        Vector3 a = CalcAPNG(target.transform.position, relv, relVelUnit);
        float maxRCSBurn = 1f;
        float sec = Mathf.Max(CalcDeltaVBurnRCSSec(a.magnitude), maxRCSBurn);
        if (sec > 0f)
        {
            ship.RCSBurst(a.normalized, sec);
        }
        yield break;
    }

    IEnumerator StrafeCo(GameObject target)
    {
        Vector3 targetVec;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out targetVec, out relv, out relVelUnit);
        float dist = targetVec.magnitude;
        Vector3 f = relVelUnit;

        // break if close enough
        float timeToRot = Turn180Sec(); // swing pass
        float timeToTgt = (dist - StrafeDistM) / Mathf.Abs(relv);
        float mecoTime = NavigationalConstant * timeToRot; // include aim
        //if (relv > 0 && mecoTime > timeToTgt)
        if (dist <= GunRangeM || (relv > 0 && mecoTime > timeToTgt) || relv > MaxStrafeSpeed)
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

    private float AimTimeSec = 3.45f;

    private GameObject CreateVirtualApproachTarget(GameObject targetDock) // destroy g after using
    {
        float radius = gameController.TargetData().GetTargetRadius(targetDock);
        float distFromDock = radius + RendezvousDistM;
        GameObject approach = PointForward(targetDock, distFromDock);
        return approach;
    }

    private GameObject PointForward(GameObject targetDock, float distFromDock) // destroy g after using
    {
        Vector3 approachPos = PointForwardVec(targetDock, distFromDock);
        Quaternion dockQ = targetDock.transform.GetChild(0).transform.rotation;
        Quaternion approachRot = Quaternion.Euler(0f, 180f, 0f) * dockQ;
        GameObject g = new GameObject();
        g.transform.position = approachPos;
        g.transform.rotation = approachRot;
        return g;
    }

    private Vector3 PointForwardVec(GameObject targetDock, float distFromDock)
    {
        Vector3 approachPos = targetDock.transform.position + distFromDock * targetDock.transform.GetChild(0).transform.forward;
        return approachPos;
    }

    private float MinRendezvousBurnSec = 2f;

    IEnumerator ApproachCo(GameObject targetDock)
    {
        for (;;)
        {
            if (ship.IsMainEngineGo())
            {
                ship.MainEngineCutoff();
            }
            yield return PushAndStartCoroutine(KillRelVCo(targetDock));
            float dist;
            PhysicsUtils.CalcDistance(transform, targetDock, out dist);
            float minBurnDist = MinBurnDist(MinRendezvousBurnSec);
            if (dist < minBurnDist)
            {
                yield break;
            }
            else
            {
                float idealBurnSec = IdealBurnSec(dist);
                float burnSec = Mathf.Max(1f, idealBurnSec);
                yield return PushAndStartCoroutine(RotToTarget(targetDock));
                yield return PushAndStartCoroutine(MainEngineBurnSec(burnSec));
                yield return PushAndStartCoroutine(KillRelVCo(targetDock));
            }
        }
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
            float minBurnDist = MinBurnDist(MinRendezvousBurnSec);
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
            Destroy(targetApproach);
        }
    }

    private float DockContactDistM = 1f;
    private float DockSpeedMPS = 2f;
    private float DockBurnRCSSec = 5f;
    private float MinDockDeltaTheta = 0.5f;
    private float MaxCenteredRelv = 0.05f;
    //private float coastSec = 3f;

    private void CalcDockRCSPlaneBurn(Vector3 targetVec, float relv, Vector3 relunitvec, out Vector3 rcsDir, out float rcsBurnSec)
    {
        Vector3 relvec = Mathf.Abs(relv) * relunitvec;
        Vector3 planeVec = Vector3.ProjectOnPlane(targetVec, transform.forward);
        Vector3 planeRelVec = Vector3.ProjectOnPlane(relvec, transform.forward);
        //float planeAngle = Vector3.Angle(planeVec, planeRelVec);
        //float planeDist = planeVec.magnitude;
        //float planeRelv = planeRelVec.magnitude;
        //float planeTimeToStop = planeRelv / ship.CurrentRCSAccelerationPerSec();
        //float planeTimeToCenter = planeDist / planeRelv;
        //float timeBeforeStop = planeTimeToStop;
        //     if (planeRelVec.magnitude <= MaxCenteredRelv)
        //   {
        // close enough
        //   rcsDir = Vector3.zero;
        //     rcsBurnSec = 0;
        //   }
        //else
        if (planeRelVec.magnitude > MaxCenteredRelv)
        {
            // stop
            rcsDir = -planeRelVec.normalized;
            rcsBurnSec = planeRelVec.magnitude / ship.CurrentRCSAccelerationPerSec();
        }
        else
        {
            // accelerate
            rcsDir = planeVec.normalized;
            rcsBurnSec = Mathf.Sqrt(planeVec.magnitude / ship.CurrentRCSAccelerationPerSec());
        }

        /*
        float signedAngle = Vector3.Angle(transform.right, planeVec) *
        Mathf.Sign(Vector3.Dot(transform.up, planeVec));
        if (Mathf.Abs(signedAngle) <= 45)
        {
            rcsDir = transform.right;
        }
        else if (Mathf.Abs(signedAngle) >= 135)
        {
            rcsDir = -transform.right;
        }
        else if (Mathf.Sign(signedAngle) == 1)
        {
            rcsDir = transform.up;
        }
        else
        {
            rcsDir = -transform.up;
        }
        float rcsDirDist = Vector3.Project(planeVec, rcsDir).magnitude;
        rcsBurnSec = Mathf.Sqrt(rcsDirDist / ship.CurrentRCSAccelerationPerSec());

        rcsBurnSec = Mathf.Min(rcsBurnSec, MaxRCSAutoBurnSec);
        */
    }

    private float MainEngineDockBurstSec = 1f;

    IEnumerator DockCo(GameObject targetDock)
    {
        for (;;)
        {
            Vector3 dockVec = targetDock.transform.GetChild(0).transform.forward;
            Vector3 approachVec = -dockVec;
            Vector3 targetVec;
            float relv;
            Vector3 relunitvec;
            PhysicsUtils.CalcRelV(transform.parent.transform, targetDock, out targetVec, out relv, out relunitvec);
            float targetAngle = Quaternion.Angle(transform.rotation, Quaternion.LookRotation(approachVec, transform.up));
            //            Vector3 targetVec = CalcVectorToTarget(targetDock);
            //            float relvecAngle = Quaternion.Angle(Quaternion.LookRotation(transform.forward), Quaternion.LookRotation(relvec));
            //            float stopSec = relv < 0 ? 0 : relv / ship.CurrentRCSAccelerationPerSec();
            //            float stopDist = 0.5f * ship.CurrentRCSAccelerationPerSec() * Mathf.Pow(stopSec, 2);
            float MinDeltaVForRCSBurn = ship.CurrentRCSAccelerationPerSec() * ship.RCSBurnMinSec;
            float dist = targetVec.magnitude;

            Vector3 planeVec = Vector3.ProjectOnPlane(targetVec, transform.forward);
            float dockTheta = Mathf.Rad2Deg * Mathf.Asin(planeVec.magnitude / targetVec.magnitude);
            float mainBurstDist = 0.5f * ship.CurrentMainEngineAccPerSec() * Mathf.Pow(MinMainEngineBurnSec, 2f);
            float stopAfterMainBurstSec = ship.CurrentMainEngineAccPerSec() * MinMainEngineBurnSec / ship.CurrentRCSAccelerationPerSec();
            float stopAfterRcsDist = 0.5f * ship.CurrentRCSAccelerationPerSec() * Mathf.Pow(stopAfterMainBurstSec, 2);
            if (dist <= DockContactDistM && Mathf.Abs(targetAngle) < MinDockDeltaTheta && Mathf.Abs(relv) < MinDeltaVForRCSBurn) // close enough
            {
                PopCoroutine();
                yield break;
            }
            else if (relv > MaxCenteredRelv)
            {
                yield return PushAndStartCoroutine(KillRelVRCSCo(targetDock));
            }
            else if (Mathf.Abs(targetAngle) > MinDockDeltaTheta) // rot to tgt
            {
                yield return PushAndStartCoroutine(RotToUnitVec(approachVec));
            }
            else if (dockTheta > MinDockDeltaTheta)
            {
                Vector3 planeBurnDir;
                float planeBurnSec;
                CalcDockRCSPlaneBurn(targetVec, relv, relunitvec, out planeBurnDir, out planeBurnSec);
                yield return RCSBurst(planeBurnDir, planeBurnSec);
            }
            else if (dist > (mainBurstDist + stopAfterRcsDist))
            {
                ship.MainEngineBurst(MinMainEngineBurnSec);
                yield return new WaitForSeconds(MinMainEngineBurnSec);
            }
            else if (dist > DockContactDistM)
            {
                // approach burst
                yield return PushAndStartCoroutine(RCSBurst(targetVec.normalized, DockBurnRCSSec));
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator RCSBurst(Vector3 direction, float sec)
    {
        float s = Mathf.Min(sec, MaxRCSAutoBurnSec);
        if (sec >= ship.RCSBurnMinSec)
        {
            ship.RCSBurst(direction, s);
            yield return new WaitForSeconds(s);
        }
        PopCoroutine();
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
        float maxDeltaV = CalcMainEngineMaxDeltaV();
        float secToMax = CalcDeltaVMainBurnSec(maxDeltaV);
        float dist;
        PhysicsUtils.CalcDistance(transform, target, out dist);
        float speedupDist = 0.1f * dist;
        float secSpeedup = Mathf.Sqrt(2f * speedupDist / ship.CurrentMainEngineAccPerSec());
        float sec = Mathf.Min(secSpeedup, secToMax);

        yield return PushAndStartCoroutine(RotToTarget(target));
        ship.MainEngineBurst(sec);
        yield return new WaitForSeconds(sec);

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

    private float CalcMainEngineMaxDeltaV()
    {
        float ve = ship.MainEngineExhaustVelocity();
        float massStart = ship.EmptyMassKg + ship.CurrentFuelKg();
        float massEnd = ship.EmptyMassKg;
        float deltaV = Mathf.Log(massStart / massEnd) * ve;
        return deltaV;
    }

    private float CalcDeltaVMainBurnSec(float deltaV)
    {
        float mainEngineA = ship.CurrentMainEngineAccPerSec();
        float sec = deltaV / mainEngineA;
        sec = sec > MinMainEngineBurnSec ? sec : 0;
        return sec;
    }

    private float CalcDeltaVAuxBurnSec(float deltaV)
    {
        float mainEngineA = ship.CurrentAuxAccPerSec();
        float sec = deltaV / mainEngineA;
        sec = sec > MinAuxEngineBurnSec ? sec : 0;
        return sec;
    }

    private float CalcDeltaVBurnRCSSec(float deltaV)
    {
        float rcsA = ship.CurrentRCSAccelerationPerSec();
        float sec = deltaV / rcsA;
        sec = sec >= ship.RCSBurnMinSec ? sec : 0;
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
        Vector3 targetVec;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out targetVec, out relv, out relVelUnit);
        float N = NavigationalConstantAPNG;
        Vector3 vr = relv * -relVelUnit;
        Vector3 r = targetVec;
        Vector3 o = Vector3.Cross(r, vr) / Vector3.Dot(r, r);
        Vector3 a = Vector3.Cross(-N * Mathf.Abs(vr.magnitude) * r.normalized, o);
        return a;
    }

    #endregion

}
