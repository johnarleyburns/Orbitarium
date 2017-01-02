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
    public float MinSpinDeltaTheta = 1f;
    public float MinMainEngineBurnSec = 0.5f;
    public float MinAuxEngineBurnSec = 0.5f;
    public float RendezvousDistM = 100f;
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
        Command.HUNT,
        Command.DOCK
    };

    public enum Command
    {
        OFF,
        KILL_ROTATION,
        FACE_TARGET,
        FACE_NML_POS,
        FACE_NML_NEG,
        FACE_POS,
        FACE_NEG,
        ACTIVE_TRACK,
        KILL_REL_V,
        INTERCEPT,
        STRAFE,
        RENDEZVOUS,
        HUNT,
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
            case Command.FACE_NML_POS:
                TurnToTargetNmlPos(target);
                break;
            case Command.FACE_NML_NEG:
                TurnToTargetNmlNeg(target);
                break;
            case Command.FACE_POS:
                TurnToTargetPos(target);
                break;
            case Command.FACE_NEG:
                TurnToTargetNeg(target);
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
            case Command.HUNT:
                Hunt(target);
                break;
            case Command.DOCK:
                Dock(target);
                break;
        }
    }

    private void ActiveTrackTarget(GameObject target)
    {
        AutopilotOff();
        PushAndStartCoroutine(ActiveTrackTargetCo(target));
    }

    private void TurnToTarget(GameObject target)
    {
        AutopilotOff();
        PushAndStartCoroutine(FaceTargetCo(target));
    }

    private void TurnToTargetPos(GameObject target)
    {
        AutopilotOff();
        Vector3 relVec = PhysicsUtils.CalcRelV(transform, target);
        PushAndStartCoroutine(RotToUnitVec(relVec.normalized));
    }

    private void TurnToTargetNeg(GameObject target)
    {
        AutopilotOff();
        Vector3 relVec = PhysicsUtils.CalcRelV(transform, target);
        PushAndStartCoroutine(RotToUnitVec(-relVec.normalized));
    }

    private void TurnToTargetNmlPos(GameObject target)
    {
        AutopilotOff();
        Vector3 relVec = PhysicsUtils.CalcRelV(transform, target);
        Vector3 a = relVec.normalized;
        Vector3 b = transform.up;
        Vector3 nmlPos = -Vector3.Cross(a, b); // left handed coordinate system
        PushAndStartCoroutine(RotToUnitVec(nmlPos.normalized));
    }

    private void TurnToTargetNmlNeg(GameObject target)
    {
        AutopilotOff();
        Vector3 relVec = PhysicsUtils.CalcRelV(transform, target);
        Vector3 a = relVec.normalized;
        Vector3 b = transform.up;
        Vector3 nmlPos = -Vector3.Cross(a, b);
        PushAndStartCoroutine(RotToUnitVec(-nmlPos.normalized));
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

    private void Hunt(GameObject target)
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
        for (;;)
        {
            Quaternion q = Quaternion.LookRotation(b);
            bool converged = ship.ConvergeSpin(q);
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

    IEnumerator ActiveTrackTargetCo(GameObject target)
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
            bool converged = ship.ConvergeSpin(q);
            cmgActive = !converged;
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator RotToTargetWithLeadCo(GameObject target)
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
            bool converged = ship.ConvergeSpin(q);
            cmgActive = !converged;
            if (converged)
            {
                PopCoroutine();
                yield break;
            }
            else
            {
                yield return new WaitForEndOfFrame();
            }
        }
    }

    IEnumerator FaceTargetCo(GameObject target)
    {
        for (;;)
        {
            Vector3 b = CalcVectorToTarget(target).normalized;
            Vector3 up = target.transform.childCount > 0 ? target.transform.GetChild(0).transform.up : target.transform.up;
            Quaternion q = Quaternion.LookRotation(b, target.transform.GetChild(0).transform.up);
            bool converged = ship.ConvergeSpin(q);
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

    IEnumerator AlignToTarget(GameObject target)
    {
        for (;;)
        {
            Quaternion q = Quaternion.FromToRotation(transform.forward, target.transform.forward) * transform.rotation;
            bool converged = ship.ConvergeSpin(q);
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
            bool converged = ship.ConvergeSpin(q);
            float targetAngle = Quaternion.Angle(transform.rotation, q);
            bool aligned = targetAngle <= MinFireTargetAngle;
            float dist;
            PhysicsUtils.CalcDistance(transform, target, out dist);
            bool inRange = weapons == null ? true : dist <= weapons.MainGunRangeM;
            bool hasAmmo = weapons == null ? false : weapons.CurrentAmmo() > 0;
            bool gunsReady = weapons.GunsReady();
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

    IEnumerator FireGunCo()
    {
        yield return weapons.FireGunsCo();
        PopCoroutine();
    }

    IEnumerator RotThenBurnMain(Vector3 b, float sec)
    {
        yield return PushAndStartCoroutine(RotToUnitVec(b));
        yield return PushAndStartCoroutine(MainEngineBurnSec(sec));
    }

    IEnumerator RotThenBurnAux(Vector3 b, float sec)
    {
        yield return PushAndStartCoroutine(RotToUnitVec(b));
        yield return PushAndStartCoroutine(AuxEngineBurnSec(sec));
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

    IEnumerator AuxEngineBurnSec(float sec)
    {
        float burnTimer = sec;
        if (burnTimer > 0f)
        {
            ship.AuxEngineGo();
        }
        for (;;)
        {
            if (burnTimer <= 0f)
            {
                ship.AuxEngineCutoff();
                PopCoroutine();
                yield break;
            }
            burnTimer -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }

    private float RCSDist = 10f;
    private float RCSMaxV = 0.1f;

    IEnumerator KillRelVCo(GameObject target)
    {
        Vector3 relVec = PhysicsUtils.CalcRelV(transform.parent.transform, target);
        float dist;
        PhysicsUtils.CalcDistance(transform, target, out dist);

        float arelv = relVec.magnitude;
        Vector3 negVec = -relVec;
        bool closeEnough = arelv <= RCSMaxV;
        float secMain = CalcDeltaVMainBurnSec(arelv);
        float secAux = CalcDeltaVAuxBurnSec(arelv);
//        float secRCS = CalcDeltaVBurnRCSSec(arelv);
        if (closeEnough)
        {
            ship.MainEngineCutoff();
            ship.AuxEngineCutoff();
            PopCoroutine();
            yield break;

        }
        else if (secMain >= MinMainEngineBurnSec)
        {
            yield return PushAndStartCoroutine(RotThenBurnMain(negVec, secMain));
        }
        else if (secAux >= MinAuxEngineBurnSec)
        {
            yield return PushAndStartCoroutine(RotThenBurnAux(negVec, secAux));
        }
//        else if (secRCS >= ship.RCSBurnMinSec)
//        {
//            ship.RCSBurst(negVec, secRCS);
//            yield return new WaitForSeconds(secRCS);
//        }
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
            yield return PushAndStartCoroutine(RotToUnitVec(thrust));
        }
    }

    private float AimTimeSec = 3.45f;

    private Vector3 CreateVirtualApproach(GameObject target)
    {
        float radius = gameController.TargetData().GetTargetRadius(target);
        float dist = radius + RendezvousDistM;
        Vector3 approachVec = PointForwardVec(target, dist);
        return approachVec;
    }

    private GameObject CreateVirtualApproachTarget(GameObject target) // destroy g after using
    {
        float radius = gameController.TargetData().GetTargetRadius(target);
        float dist = radius + RendezvousDistM;
        GameObject approach = PointForward(target, dist);
        return approach;
    }

    private GameObject PointForward(GameObject target, float dist) // destroy g after using
    {
        Vector3 approachPos = PointForwardVec(target, dist);
        Quaternion forwardQ = target.transform.rotation;
//        Quaternion forwardQ = target.transform.GetChild(0).transform.rotation;
        //bool isDock = gameController.TargetData().GetTargetType(target) == TargetDB.TargetType.DOCK;
        //Quaternion approachRot = isDock ? forwardQ : Quaternion.Euler(0f, 180f, 0f) * forwardQ;
        Quaternion approachRot = Quaternion.Euler(0f, 180f, 0f) * forwardQ;
        GameObject g = new GameObject();
        g.transform.position = approachPos;
        g.transform.rotation = approachRot;
        return g;
    }

    private Vector3 PointForwardVec(GameObject target, float dist)
    {
        bool isDock = gameController.TargetData().GetTargetType(target) == TargetDB.TargetType.DOCK;
        float approachDir = isDock ? -1 : 1;
//        Vector3 approachForward = approachDir * target.transform.GetChild(0).transform.forward;
        Vector3 approachForward = approachDir * target.transform.forward;
        Vector3 approachPos = target.transform.position + dist * approachForward;
        return approachPos;
    }

    private float MinRendezvousBurnSec = 0.5f;

    IEnumerator ApproachCo(GameObject targetDock)
    {
        yield break;
        /*
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
        */
    }

    private float MinRendezvousEpsilonM = 1;

    IEnumerator RendezvousCo(GameObject target)
    {
        for (;;)
        {
            if (ship.IsMainEngineGo())
            {
                ship.MainEngineCutoff();
            }
            if (ship.IsAuxEngineGo())
            {
                ship.AuxEngineCutoff();
            }
            if (ship.IsRCSFiring())
            {
                ship.RCSCutoff();
            }
            //GameObject targetApproach = CreateVirtualApproachTarget(target);
            Vector3 targetApproach = CreateVirtualApproach(target);
            yield return PushAndStartCoroutine(KillRelVCo(target));
            Vector3 b = (targetApproach - transform.position).normalized;
            float dist;
            PhysicsUtils.CalcDistance(transform.position, targetApproach, out dist);
            float minMainBurnDist = MinMainBurnDist(MinRendezvousBurnSec);
            float minAuxBurnDist = MinAuxBurnDist(MinRendezvousBurnSec);
//            float minRCSBurnDist = MinRCSBurnDist(MinRendezvousBurnSec);
            bool closeEnough = dist < MinRendezvousEpsilonM;
            bool fireMain = dist >= minMainBurnDist;
            bool fireAux = dist >= minAuxBurnDist && dist > RCSDist;
//            bool fireRCS = dist >= minRCSBurnDist && dist > MinRendezvousEpsilonM;
            if (!closeEnough && fireMain)
            {
                float idealBurnSec = IdealMainBurnSec(dist);
                float burnSec = Mathf.Max(MinRendezvousBurnSec, idealBurnSec);
                yield return PushAndStartCoroutine(RotThenBurnMain(b, burnSec));
                yield return PushAndStartCoroutine(KillRelVCo(target));
            }
            else if (!closeEnough && fireAux)
            {
                float idealBurnSec = IdealAuxBurnSec(dist);
                float burnSec = Mathf.Max(MinRendezvousBurnSec, idealBurnSec);
                yield return PushAndStartCoroutine(RotThenBurnAux(b, burnSec));
                yield return PushAndStartCoroutine(KillRelVCo(target));
            }
//            else if (!closeEnough && fireRCS)
//            {
//                yield return PushAndStartCoroutine(FaceTargetCo(target));
//                float idealBurnSec = IdealRCSBurnSec(dist);
//                float burnSec = Mathf.Max(ship.RCSBurnMinSec, idealBurnSec);
//                ship.RCSBurst(b, burnSec);
//                yield return new WaitForSeconds(burnSec);
//                ship.RCSBurst(-b, burnSec);
//                yield return new WaitForSeconds(burnSec);
//                yield return PushAndStartCoroutine(KillRelVCo(target));
//                closeEnough = true;
//            }
            if (closeEnough)
            {
                yield return PushAndStartCoroutine(FaceTargetCo(target));
                yield break;
            }
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
                yield return PushAndStartCoroutine(KillRelVCo(targetDock));
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

    public float MinMainBurnDist(float sec)
    {
        return MinBurnDist(sec, ship.CurrentMainEngineAccPerSec(), ship.CurrentRCSAngularDegPerSec());
    }

    public float MinAuxBurnDist(float sec)
    {
        return MinBurnDist(sec, ship.CurrentAuxAccPerSec(), ship.CurrentRCSAngularDegPerSec());
    }

    public float MinRCSBurnDist(float sec)
    {
        return MinBurnDist(sec, ship.CurrentRCSAccelerationPerSec(), 0);
    }

    public float MinBurnDist(float sec, float mainEngineAcc, float angularAcc)
    {
        float t = sec;
        float a = mainEngineAcc;
        float ao = angularAcc;
        float burnDist = 0.5f * a * Mathf.Pow(t, 2f);
        float stopDist = burnDist;
        float turnDist = ao > 0 ? Turn180Sec(ao) : 0;
        float dist = burnDist + turnDist + stopDist;
        return dist;
    }

    public float IdealMainBurnSec(float dist)
    {
        return IdealBurnSec(dist, ship.CurrentMainEngineAccPerSec(), ship.CurrentRCSAngularDegPerSec());
    }

    public float IdealAuxBurnSec(float dist)
    {
        return IdealBurnSec(dist, ship.CurrentAuxAccPerSec(), ship.CurrentRCSAngularDegPerSec());
    }

    public float IdealRCSBurnSec(float dist)
    {
        return IdealBurnSec(dist, ship.CurrentRCSAccelerationPerSec(), 0);
    }

    public float IdealBurnSec(float dist, float mainEngineAcc, float angularAcc)
    {
        float d = dist;
        float a = mainEngineAcc;
        float tr = angularAcc > 0 ? Turn180Sec(angularAcc) : 0;
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

        yield return PushAndStartCoroutine(FaceTargetCo(target));
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
