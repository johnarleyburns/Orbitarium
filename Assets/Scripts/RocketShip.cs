using UnityEngine;
using System.Collections;

public class RocketShip : MonoBehaviour {

    public GameController gameController;
    public float EmptyMassKg = 10000;
    public float FuelMassKg = 9200;
    public float RCSThrustNewtons = 880; // 220*4 in the SM
    public float EngineThrustNewtons = 27700;
    public float RCSFuelKgPerSec = 0.14f;
    public float EngineFuelKgPerSec = 8.7f;
    public float DumpFuelRateKgPerSec = 100;
    public float RCSRadiusM = 2.5f;
    public float minDeltaTheta = 0.2f;
    public float minSpinDeltaDegPerSec = 10f;
    public float MaxRotationDegPerSec = 720;
    public float RotationSpeedFactor = 2;
    public float AutoRotationSpeedFactor = 20;

    private NBody nbody;
    private float currentFuelKg;
    private float currentTotalMassKg;
    private float RCSThrustPerSec;
    private float EngineThrustPerSec;
    private float RCSAngularDegPerSec;
    private bool mainEngineOn;
    private Quaternion currentSpinPerSec;
    private bool killingRot;
    private bool autoRotating;
    private GameObject autoRotatingTarget;

    void Start()
    {
        nbody = transform.parent.GetComponent<NBody>();
        mainEngineOn = false;
        currentTotalMassKg = EmptyMassKg + FuelMassKg;
        currentFuelKg = FuelMassKg;
        currentSpinPerSec = Quaternion.identity;
        killingRot = false;
        autoRotating = false;
        autoRotatingTarget = null;
        UpdateThrustRates();
    }

    void Update()
    {
        if (gameController != null)
        {
            switch (gameController.GetGameState())
            {
                case GameController.GameState.RUNNING:
                    UpdateThrustRates();
                    UpdateEngine();
                    ApplyCurrentSpin();
                    break;
            }
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

    public void DumpFuel()
    {
        ApplyFuel(-DumpFuelRateKgPerSec);
    }

    public void MainEngineGo()
    {
        mainEngineOn = true;
    }

    public void MainEngineCutoff()
    {
        mainEngineOn = false;
    }

    public bool IsMainEngineGo()
    {
        return mainEngineOn;
    }

    private void UpdateEngine()
    {
        if (mainEngineOn)
        {
            if (currentFuelKg > EngineFuelKgPerSec)
            {
                ApplyImpulse(transform.forward, EngineThrustPerSec);
                ApplyFuel(-EngineFuelKgPerSec);
            }
            else
            {
                MainEngineCutoff();
            }
        }
    }

    public float CurrentFuelKg()
    {
        return currentFuelKg;
    }

    public float NormalizedFuel()
    {
        return currentFuelKg / FuelMassKg;
    }

    public void ApplyRCSImpulse(Vector3 normalizedDirection)
    {
        ApplyImpulse(normalizedDirection, RCSThrustPerSec);
        ApplyFuel(-RCSFuelKgPerSec);
    }

    private void ApplyImpulse(Vector3 normalizedDirection, float thrustPerSec)
    {
        Vector3 thrust = normalizedDirection * thrustPerSec * Time.deltaTime;
        GravityEngine.instance.ApplyImpulse(nbody, thrust);
    }

    private void ApplyFuel(float deltaFuel)
    {
        currentFuelKg += deltaFuel * Time.deltaTime;
        if (currentFuelKg <= 0)
        {
            currentFuelKg = 0;
        }
        UpdateThrustRates();
    }

    private void UpdateThrustRates()
    {
        currentTotalMassKg = EmptyMassKg + currentFuelKg;
        RCSThrustPerSec = RCSThrustNewtons / currentTotalMassKg;
        EngineThrustPerSec = EngineThrustNewtons / currentTotalMassKg;
        RCSAngularDegPerSec = Mathf.Rad2Deg * Mathf.Sqrt(RCSThrustPerSec / RCSRadiusM);
    }

    public bool ApplyRCSSpin(Quaternion unitQuaternion)
    {
        Quaternion q = Quaternion.Lerp(currentSpinPerSec, currentSpinPerSec * unitQuaternion, RotationSpeedFactor * RCSAngularDegPerSec * Time.deltaTime);
        currentSpinPerSec = q;
        return unitQuaternion != Quaternion.identity;
    }

    public void NoRot()
    {
        killingRot = false;
        autoRotating = false;
    }

    public bool IsRot()
    {
        return killingRot || autoRotating;
    }

    public bool IsKillRot()
    {
        return killingRot;
    }

    public bool IsAutoRot()
    {
        return autoRotating;
    }

    public void KillRot()
    {
        killingRot = true;
        autoRotating = false;
    }

    public void AutoRot(GameObject target)
    {
        killingRot = false;
        autoRotating = true;
        autoRotatingTarget = target;
    }

    public GameObject AutoRotTarget()
    {
        return autoRotatingTarget;
    }

    public void ApplyCurrentSpin()
    {
        if (killingRot)
        {
            ConvergeSpin(Quaternion.identity);
            if (Mathf.Abs(Quaternion.Angle(currentSpinPerSec, Quaternion.identity)) < minDeltaTheta)
            {
                killingRot = false;
                currentSpinPerSec = Quaternion.identity;
            }
        }
        else if (autoRotating)
        {
            Vector3 b = (autoRotatingTarget.transform.position - transform.parent.transform.position).normalized;
            Quaternion q = Quaternion.LookRotation(b);
            q = Quaternion.Euler(q.eulerAngles.x, q.eulerAngles.y, transform.rotation.eulerAngles.z);
            float angle = Mathf.Abs(Quaternion.Angle(transform.rotation, q));
            float secToTurn = Mathf.Max(Mathf.Sqrt(2 * angle / RCSAngularDegPerSec), 1);
            Quaternion desiredQ = Quaternion.Lerp(transform.rotation, q, AutoRotationSpeedFactor * 1 / secToTurn);
            Quaternion deltaQ = Quaternion.Inverse(transform.rotation) * desiredQ;
            ConvergeSpin(deltaQ);
            if (Mathf.Abs(Quaternion.Angle(currentSpinPerSec, Quaternion.identity)) < minDeltaTheta
                && Mathf.Abs(Quaternion.Angle(transform.rotation, q)) < minDeltaTheta)
            {
                autoRotating = false;
                killingRot = false;
                currentSpinPerSec = Quaternion.identity;
            }
        }
        Quaternion timeQ = Quaternion.Lerp(transform.rotation, transform.rotation * currentSpinPerSec, Time.deltaTime);
        transform.rotation = timeQ;
    }

    private void ConvergeSpin(Quaternion deltaQ)
    {
        float angle = Mathf.Abs(Quaternion.Angle(currentSpinPerSec, deltaQ));
//        float speed = RCSAngularDegPerSec;
        float speed = Mathf.Max(RCSAngularDegPerSec / angle, 1);
        currentSpinPerSec = Quaternion.Lerp(currentSpinPerSec, deltaQ, RotationSpeedFactor * speed * Time.deltaTime);
    }

    /*
    private bool ApplySpinVector(Vector3 inputSpinVector)
    {
        Vector3 spinVector = new Vector3(
            Mathf.Clamp(inputSpinVector.x, -RCSAngularDegPerSec * Time.deltaTime, RCSAngularDegPerSec * Time.deltaTime),
            Mathf.Clamp(inputSpinVector.y, -RCSAngularDegPerSec * Time.deltaTime, RCSAngularDegPerSec * Time.deltaTime),
            Mathf.Clamp(inputSpinVector.z, -RCSAngularDegPerSec * Time.deltaTime, RCSAngularDegPerSec * Time.deltaTime)
            );
        Quaternion spinQ = Quaternion.Euler(spinVector);
        currentSpinPerSec *= spinQ;
        return spinVector != Vector3.zero;
    }

    public bool ApplySpinVector(float x, float y, float z)
    {
        Vector3 spinVector = new Vector3(x, y, z);
        return ApplySpinVector(spinVector);
    }
    */
}
