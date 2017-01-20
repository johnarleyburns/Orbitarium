using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Autopilot : MonoBehaviour
{

    public GameController gameController;
    public NBodyDimensions NBodyDimensions;

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

    private delegate void CompletedCallback();

    private void AutopilotOff()
    {
        StopAll();
        ship.MainEngineCutoff();
        ship.AuxEngineCutoff();
        ship.RCSCutoff();
        gameController.GetComponent<InputController>().PropertyChanged("CommandExecuted", Command.OFF);
        cmgActive = false;
    }

    public bool IsRot()
    {
        return cmgActive;
    }

    public void ExecuteCommand(Command command, GameObject target)
    {
        currentCommand = command;
        AutopilotOff();
        switch (command)
        {
            case Command.OFF:
                break;
            case Command.KILL_ROTATION:
                PushAndStartCoroutine(KillRotCo(AutopilotOff));
                break;
            case Command.FACE_TARGET:
                PushAndStartCoroutine(FaceTargetCo(target, true, AutopilotOff));
                break;
            case Command.FACE_NML_POS:
                PushAndStartCoroutine(RotToUnitVec(RelVNmlPosVec(target).normalized, true, AutopilotOff));
                break;
            case Command.FACE_NML_NEG:
                PushAndStartCoroutine(RotToUnitVec(-RelVNmlPosVec(target).normalized, true, AutopilotOff));
                break;
            case Command.FACE_POS:
                PushAndStartCoroutine(RotToUnitVec(RelVPosVec(target), true, AutopilotOff));
                break;
            case Command.FACE_NEG:
                PushAndStartCoroutine(RotToUnitVec(-RelVPosVec(target), true, AutopilotOff));
                break;
            case Command.ACTIVE_TRACK:
                PushAndStartCoroutine(ActiveTrackTargetCo(target));
                break;
            case Command.KILL_REL_V:
                PushAndStartCoroutine(KillRelVCo(target, AutopilotOff));
                break;
            case Command.INTERCEPT:
                PushAndStartCoroutine(APNGToTargetCo(target, AutopilotOff));
                break;
            case Command.STRAFE:
                PushAndStartCoroutine(StrafeTargetCo(target, AutopilotOff));
                break;
            case Command.RENDEZVOUS:
                PushAndStartCoroutine(RendezvousCo(target, AutopilotOff));
                break;
            case Command.HUNT:
                PushAndStartCoroutine(HuntCo(target, AutopilotOff));
                break;
            case Command.DOCK:
                PushAndStartCoroutine(DockCo(target, AutopilotOff));
                break;
        }
    }

    private Vector3 RelVPosVec(GameObject target)
    {
        return PhysicsUtils.CalcRelV(transform, target);
    }

    private Vector3 RelVNmlPosVec(GameObject target)
    {
        Vector3 relVec = PhysicsUtils.CalcRelV(transform, target);
        Vector3 a = relVec.normalized;
        Vector3 b = transform.up;
        Vector3 nmlPos = -Vector3.Cross(a, b); // left handed coordinate system
        return nmlPos;
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

    IEnumerator RotToUnitVec(Vector3 b, bool precision = false, CompletedCallback callback = null)
    {
        Quaternion p = transform.rotation; // original rot
        for (;;)
        {
            Quaternion q = Quaternion.LookRotation(b);
            yield return PushAndStartCoroutine(SpinFromToCo(p, q, precision));
                PopCoroutine();
                if (callback != null)
                {
                    callback();
                }
                yield break;
        }
    }
    
    private const float minKillRotTheta = 0.1f;
    private void CalcKillRotBurst(out Quaternion delta, out float sec)
    {
        float a = ship.CurrentRCSAngularDegPerSec();
        float v = Quaternion.Angle(Quaternion.identity, ship.CurrentSpinPerSec);
        float t = v / a;
        delta = Quaternion.Inverse(ship.CurrentSpinPerSec);
        sec = Mathf.Min(t, Mathf.Max(ship.RCSBurnMinSec, Time.deltaTime));
    }

    IEnumerator KillRotCo(CompletedCallback callback = null)
    {

        float prevAngle = 360f;
        for (;;)
        {
            Quaternion delta;
            float t;
            CalcKillRotBurst(out delta, out t);
            float angle = Quaternion.Angle(ship.CurrentSpinPerSec, Quaternion.identity);
            if (t < ship.RCSBurnMinSec || delta == Quaternion.identity || angle <= minKillRotTheta || angle > prevAngle)
            {
                ship.RCSAngularCutoff();
                ship.NullSpin();
                PopCoroutine();
                if (callback != null)
                {
                    callback();
                }
                yield break;
            }
            else
            {
                ship.RCSAngularBurst(delta, t);
                yield return new WaitForSeconds(t);
                prevAngle = angle;
            }
        }
    }

    IEnumerator ActiveTrackTargetCo(GameObject target)
    {
        Vector3 prevTVec = (target.transform.position - transform.parent.transform.position).normalized;
        float prevTimer = UpdateTrackTime;
        for (;;)
        {
            Quaternion p = transform.rotation;
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
            yield return PushAndStartCoroutine(SpinFromToCo(p, q));
        }
    }

    IEnumerator RotToTargetWithLeadCo(GameObject target)
    {
        Vector3 prevTVec = (target.transform.position - transform.parent.transform.position).normalized;
        float prevTimer = UpdateTrackTime;
        Quaternion p = transform.rotation; // original rot
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
            yield return PushAndStartCoroutine(SpinFromToCo(p, q));
            yield break;
        }
    }

    IEnumerator FaceTargetCo(GameObject target, bool precision = false, CompletedCallback callback = null)
    {
        float prevAngle = 360f;
        for (;;)
        {
            yield return PushAndStartCoroutine(KillRotCo());
            Quaternion p = transform.rotation; // original rot
            Vector3 b = CalcVectorToTarget(target).normalized;
            Quaternion q = Quaternion.LookRotation(b, transform.up);
            float angle = Quaternion.Angle(transform.rotation, q);
            float minTheta = precision ? 0.2f : 1f;
            if (angle < minTheta || angle > prevAngle)
            {
                PopCoroutine();
                if (callback != null)
                {
                    callback();
                }
                yield break;
            }
            else {
                prevAngle = angle;
                yield return PushAndStartCoroutine(SpinFromToCo(p, q, precision));
            }
        }
    }

    private float RCSAngularBurnTimeForAngle(float angle)
    {
        return Mathf.Sqrt(2f * angle / ship.CurrentRCSAngularDegPerSec());
    }

    private float CoastTimeForAngle(float angle, float angularV)
    {
        return angle / angularV;
    }

    private IEnumerator SpinFromToCo(Quaternion startQ, Quaternion _targetQ, bool precision = false)
    {
        Quaternion targetQ = Quaternion.Euler(_targetQ.eulerAngles.x, _targetQ.eulerAngles.y, startQ.eulerAngles.z);
        float totalAngle = Quaternion.Angle(startQ, targetQ);
        Quaternion currentQ = transform.rotation;

        float secToCoast = ship.SecToCoast(targetQ);
        float secToStop = ship.SecToStop(targetQ);
        float degPerSec = ship.SpinDegPerSec();

        // how far to go
        Quaternion deltaQ = targetQ * Quaternion.Inverse(startQ);
        float deltaAngle = Quaternion.Angle(Quaternion.identity, deltaQ);

        float maxAngularV = 36f; // deg per sec
        float maxRCSAngularBurnSec = maxAngularV / ship.CurrentRCSAngularDegPerSec();

        float quarterAngle = deltaAngle / 4f;
        float desiredRCSAngularBurnSec = RCSAngularBurnTimeForAngle(quarterAngle);

        float burnSec = Mathf.Min(desiredRCSAngularBurnSec, maxRCSAngularBurnSec);
        if (burnSec > ship.RCSBurnMinSec)
        {
            // start
            float halfAngle = deltaAngle / 4f;
            float angularVAfterBurn = ship.CurrentRCSAngularDegPerSec() * burnSec;
            Quaternion q = ship.QToSpinDelta(deltaQ);
            ship.RCSAngularBurst(q, burnSec);
            yield return new WaitForSeconds(burnSec);

            // coast
            float angleSoFar = Quaternion.Angle(startQ, transform.rotation);
            float coastAngle = totalAngle - 2f * angleSoFar;
            float currentAngularV = Quaternion.Angle(Quaternion.identity, ship.CurrentSpinPerSec);
            float coastSec = coastAngle / currentAngularV;
            yield return new WaitForSeconds(coastSec);

            // stop
            targetQ = Quaternion.Euler(_targetQ.eulerAngles.x, _targetQ.eulerAngles.y, transform.rotation.eulerAngles.z);
            deltaQ = targetQ * Quaternion.Inverse(transform.rotation);
            deltaAngle = Quaternion.Angle(Quaternion.identity, deltaQ);
            desiredRCSAngularBurnSec = RCSAngularBurnTimeForAngle(deltaAngle);
            burnSec = Mathf.Min(desiredRCSAngularBurnSec, maxRCSAngularBurnSec);
            q = Quaternion.Inverse(ship.QToSpinDelta(deltaQ));
            ship.RCSAngularBurst(q, burnSec);
            yield return new WaitForSeconds(burnSec);

            yield return PushAndStartCoroutine(KillRotCo());
        }
        else
        {
            yield return PushAndStartCoroutine(KillRotCo());
            PopCoroutine();
            yield break;
        }
    }

    IEnumerator RotShootTargetAPNG(GameObject target)
    {
        bool breakable = true;
        Vector3 prevTVec = (target.transform.position - transform.parent.transform.position).normalized;
        float prevTimer = UpdateTrackTime;
        for (;;)
        {
            Quaternion p = transform.rotation; // original rot
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
            yield return PushAndStartCoroutine(SpinFromToCo(p, q));
            float targetAngle = Quaternion.Angle(transform.rotation, q);
            bool aligned = targetAngle <= MinFireTargetAngle;
            float dist;
            PhysicsUtils.CalcDistance(gameObject, target, out dist);
            bool inRange = weapons == null ? true : dist <= weapons.MainGunRangeM;
            bool hasAmmo = weapons == null ? false : weapons.CurrentAmmo() > 0;
            bool gunsReady = weapons.GunsReady();
            bool targetActive = gameController.IsTargetActive(target);
            if (aligned && inRange && hasAmmo && gunsReady && targetActive)
            {
                yield return PushAndStartCoroutine(FireGunCo());
                PopCoroutine();
                yield break;
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

    private float RCSMaxV = 0.02f;

    IEnumerator KillRelVCo(GameObject target, CompletedCallback callback = null)
    {
        Vector3 relVec = PhysicsUtils.CalcRelV(transform.parent.transform, target);
        float dist;
        PhysicsUtils.CalcDistance(gameObject, target, out dist);

        float arelv = relVec.magnitude;
        Vector3 negVec = -relVec;
        bool closeEnough = arelv <= RCSMaxV;
        float secMain = CalcDeltaVMainBurnSec(arelv);
        float secAux = CalcDeltaVAuxBurnSec(arelv);
        float secRCS = CalcDeltaVBurnRCSSec(arelv);
        if (closeEnough)
        {
            ship.MainEngineCutoff();
            ship.AuxEngineCutoff();
            ship.RCSCutoff();
        }
        else if (secMain >= MinMainEngineBurnSec)
        {
            yield return PushAndStartCoroutine(RotThenBurnMain(negVec, secMain));
        }
        else if (secAux >= MinAuxEngineBurnSec)
        {
            yield return PushAndStartCoroutine(RotThenBurnAux(negVec, secAux));
        }
        else if (secRCS >= ship.RCSBurnMinSec)
        {
            ship.RCSBurst(negVec, secRCS);
            yield return new WaitForSeconds(secRCS);
        }
        PopCoroutine();
        if (callback != null)
        {
            callback();
        }
        yield break;
    }

    IEnumerator APNGRCSBurnCo(GameObject target, float relv, Vector3 relVelUnit)
    {
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
        PhysicsUtils.CalcRelV(gameObject, target, out targetVec, out relv, out relVelUnit);
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
    
    private void PositionApproachTarget(GameObject target, GameObject approachNBody) // destroy gameobjects after using
    {
        float radius = gameController.TargetData().GetTargetRadius(target);
        float dist = radius + RendezvousDistM;
        PointForward(target, dist, approachNBody);
    }

    private void PointForward(GameObject target, float dist, GameObject nBody) // destroy g after using
    {
        bool isDock = gameController.TargetData().GetTargetType(target) == TargetDB.TargetType.DOCK;
        float approachDir = isDock ? -1 : 1;
        DVector3 approachForward = new DVector3(approachDir * target.transform.forward);
        DVector3 approachPos = new DVector3(target.transform.position) + dist * approachForward;
        //Quaternion forwardQ = target.transform.rotation;
        //Quaternion approachRot = Quaternion.Euler(0f, 180f, 0f) * forwardQ;
        
        NBodyDimensions dim = NUtils.GetNBodyDimensions(gameController.GetPlayer());
        DVector3 nBodyPos = NUtils.TransformNearToFar(approachPos, dim.PlayerNBody, dim.NBodyToModelScaleFactor);
        DVector3 tVel = GravityEngine.instance.GetVelocity(NUtils.GetNBodyGameObject(target));

        //GravityEngine.instance.InactivateBody(nBody);
        GravityEngine.instance.UpdatePositionAndVelocity(nBody.GetComponent<NBody>(), nBodyPos, tVel);
        //GravityEngine.instance.ActivateBody(nBody);
    }

    private float MinRendezvousBurnSec = 0.5f;

    IEnumerator HuntCo(GameObject targetDock, CompletedCallback callback)
    {
        PopCoroutine();
        if (callback != null)
        {
            callback();
        }
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

    private float MinRendezvousEpsilonM = 10f;

    IEnumerator RendezvousCo(GameObject target, CompletedCallback callback)
    {
        GameObject myNBody = NUtils.GetNBodyGameObject(gameObject);
        GameObject player = gameController.GetPlayer();
        GameObject playerNBody = NUtils.GetNBodyGameObject(player);
        double scale = NUtils.GetNBodyToModelScale(player);
        GameObject approachNBody = GameObject.Instantiate(gameController.NBodyVirtualPrefab);
        approachNBody.name = "NBody Approach " + target.name + " For " + gameObject.name;
        GravityEngine.instance.AddBody(approachNBody);

        GameObject approachTarget = new GameObject();
        //approachTarget.AddComponent<MeshFilter>();
        //approachTarget.AddComponent<MeshRenderer>();
        approachTarget.name = "Approach Target" + target.name + " For " + gameObject.name;
        NBodyDimensions dim = approachTarget.AddComponent<NBodyDimensions>();
        dim.PlayerNBody = playerNBody;
        dim.NBodyToModelScaleFactor = (float)scale;
        dim.NBody = approachNBody;
        dim.UpdatePosition = true;

        ship.CutoffAll();
        yield return KillRelVCo(target);

        for (;;)
        {
            PositionApproachTarget(target, approachNBody);
            DVector3 approachNBodyPos;
            GravityEngine.instance.GetPosition(approachNBody.GetComponent<NBody>(), out approachNBodyPos);
            DVector3 myNBodyPos;
            GravityEngine.instance.GetPosition(myNBody.GetComponent<NBody>(), out myNBodyPos);
            DVector3 tVec = approachNBodyPos - myNBodyPos;
            Vector3 b = tVec.normalized.ToVector3();
            float dist = (float)(scale * tVec.magnitude);
            float minMainBurnDist = MinMainBurnDist(MinRendezvousBurnSec);
            float minAuxBurnDist = MinAuxBurnDist(MinRendezvousBurnSec);
            bool fireMain = dist >= minMainBurnDist && dist > MinRendezvousEpsilonM;
            bool fireAux = dist >= minAuxBurnDist && dist > MinRendezvousEpsilonM;
            if (fireMain)
            {
                float idealBurnSec = IdealMainBurnSec(dist);
                float burnSec = Mathf.Max(MinRendezvousBurnSec, idealBurnSec);
                yield return PushAndStartCoroutine(RotThenBurnMain(b, burnSec));
                yield return PushAndStartCoroutine(KillRelVCo(target));
            }
            else if (fireAux)
            {
                float idealBurnSec = IdealAuxBurnSec(dist);
                float burnSec = Mathf.Max(MinRendezvousBurnSec, idealBurnSec);
                yield return PushAndStartCoroutine(RotThenBurnAux(b, burnSec));
                yield return PushAndStartCoroutine(KillRelVCo(target));
            }
            else
            {
                break;
            }
        }

        Destroy(approachTarget);
        GravityEngine.instance.InactivateBody(approachNBody);
        yield return PushAndStartCoroutine(FaceTargetCo(target, true));
        PopCoroutine();
        if (callback != null)
        {
            callback();
        }
        yield break;
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

    IEnumerator DockCo(GameObject targetDock, CompletedCallback callback)
    {
        for (;;)
        {
            Vector3 dockVec = targetDock.transform.GetChild(0).transform.forward;
            Vector3 approachVec = -dockVec;
            Vector3 targetVec;
            float relv;
            Vector3 relunitvec;
            PhysicsUtils.CalcRelV(gameObject, targetDock, out targetVec, out relv, out relunitvec);
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
                if (callback != null)
                {
                    callback();
                }
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

    IEnumerator APNGSpeedupCo(GameObject target, float sec)
    {
        yield return PushAndStartCoroutine(FaceTargetCo(target));
        ship.MainEngineBurst(sec);
        yield return new WaitForSeconds(sec);
        PopCoroutine();
    }

    private float SidewinderBurnTime = 2f;

    IEnumerator APNGToTargetCo(GameObject target, CompletedCallback callback)
    {

        ship.MainEngineBurst(SidewinderBurnTime);
        for (;;)
        {
            Vector3 targetVec;
            float relv;
            Vector3 relVelUnit;
            PhysicsUtils.CalcRelV(gameObject, target, out targetVec, out relv, out relVelUnit);
            bool lostTarget = !gameController.TargetData().HasTarget(target);
            bool lowFuel = ship.NormalizedFuel() < 0.01f;
            bool wrongDirectionAfterBurn = !ship.IsMainEngineGo() && relv < 0;
            if (lostTarget || lowFuel || wrongDirectionAfterBurn)
            {
                ship.CutoffAll();
                ship.GetComponent<MissileShip>().ExplodeCollide(null);
                PopCoroutine();
                if (callback != null)
                {
                    callback();
                }
                yield break;
            }
            else
            {
                yield return PushAndStartCoroutine(APNGRCSBurnCo(target, relv, relVelUnit));
                yield return new WaitForEndOfFrame();
            }
        }
    }

    IEnumerator StrafeTargetCo(GameObject target, CompletedCallback callback)
    {
        ship.MainEngineGo();
        for (;;)
        {
            if (!gameController.TargetData().HasTarget(target))
            {
                PopCoroutine();
                if (callback != null)
                {
                    callback();
                }
                yield break;
            }
            else if (ship.IsMainEngineGo())
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
        PhysicsUtils.CalcRelV(gameObject, target, out targetVec, out relv, out relVelUnit);
        float N = NavigationalConstantAPNG;
        Vector3 vr = relv * -relVelUnit;
        Vector3 r = targetVec;
        Vector3 o = Vector3.Cross(r, vr) / Vector3.Dot(r, r);
        Vector3 a = Vector3.Cross(-N * Mathf.Abs(vr.magnitude) * r.normalized, o);
        return a;
    }

    #endregion

}
