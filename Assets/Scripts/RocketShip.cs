using UnityEngine;
using System.Collections.Generic;

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
    public float minSpinDeltaDegPerSec = 0.2f;
    public float MaxRotationDegPerSec = 720;

    private NBody nbody;
    private float currentFuelKg;
    private float currentTotalMassKg;
    private float RCSThrustPerSec;
    private float EngineThrustPerSec;
    private float RCSAngularDegPerSec;
    private bool mainEngineOn;
    private float mainEngineCutoffTimer = -1;
    private Quaternion currentSpinPerSec;

    void Start()
    {
        nbody = transform.parent.GetComponent<NBody>();
        mainEngineOn = false;
        currentTotalMassKg = EmptyMassKg + FuelMassKg;
        currentFuelKg = FuelMassKg;
        currentSpinPerSec = Quaternion.identity;
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
                    UpdateApplyCurrentSpin();
                    break;
            }
        }
    }

    public float CurrentRCSAccelerationPerSec()
    {
        return RCSThrustPerSec;
    }

    public float CurrentMainEngineAccPerSec()
    {
        return EngineThrustPerSec;
    }

    public float CurrentRCSAngularDegPerSec()
    {
        return RCSAngularDegPerSec;
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

    public void MainEngineBurst(float sec)
    {
        if (currentFuelKg > sec * EngineFuelKgPerSec)
        {
            mainEngineCutoffTimer = sec;
            MainEngineGo();
        }
        else
        {
            MainEngineCutoff();
        }
    }

    private void UpdateEngine()
    {
        if (mainEngineOn)
        {
            if (currentFuelKg > EngineFuelKgPerSec && (mainEngineCutoffTimer <= -1 || mainEngineCutoffTimer > 0))
            {
                ApplyImpulse(transform.forward, EngineThrustPerSec);
                ApplyFuel(-EngineFuelKgPerSec);
                if (mainEngineCutoffTimer > 0)
                {
                    mainEngineCutoffTimer -= Time.deltaTime;
                }
            }
            else
            {
                MainEngineCutoff();
                mainEngineCutoffTimer = -1;
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
        Quaternion q = Quaternion.LerpUnclamped(currentSpinPerSec, currentSpinPerSec * unitQuaternion, RCSAngularDegPerSec * Time.deltaTime);
        currentSpinPerSec = q;
        return unitQuaternion != Quaternion.identity;
    }

    public void UpdateApplyCurrentSpin()
    {
        Quaternion timeQ = Quaternion.Lerp(transform.rotation, transform.rotation * currentSpinPerSec, Time.deltaTime);
        transform.rotation = timeQ;
    }

    public bool KillRotation()
    {
        bool converged;
        currentSpinPerSec = Quaternion.RotateTowards(currentSpinPerSec, Quaternion.identity, RCSAngularDegPerSec * Time.deltaTime);
        bool convergedSpin = Mathf.Abs(Quaternion.Angle(currentSpinPerSec, Quaternion.identity)) < minSpinDeltaDegPerSec;
        if (convergedSpin)
        {
            currentSpinPerSec = Quaternion.identity;
            converged = true;
        }
        else
        {
            converged = false;
        }
        return converged;
    }

    public bool ConvergeSpin(Quaternion targetQ, float minDTheta)
    {
        Quaternion deltaQ = Quaternion.Inverse(transform.rotation) * targetQ;
        bool converged;

        float angleLeft = Mathf.Abs(Quaternion.Angle(transform.rotation, targetQ));
        float spinSpeed = Mathf.Abs(Quaternion.Angle(currentSpinPerSec, Quaternion.identity));

        float secToCoast = angleLeft / spinSpeed;
        float secToStop = Mathf.Sqrt(2 * spinSpeed / RCSAngularDegPerSec);

        float maxDeg = RCSAngularDegPerSec;
        if (secToStop < secToCoast) // speedup
        {
            currentSpinPerSec = Quaternion.RotateTowards(currentSpinPerSec, deltaQ, RCSAngularDegPerSec * Time.deltaTime);
        }
        else // stop
        {
            currentSpinPerSec = Quaternion.RotateTowards(currentSpinPerSec, Quaternion.identity, RCSAngularDegPerSec * Time.deltaTime);
        }
        bool convergedSpin = Mathf.Abs(Quaternion.Angle(currentSpinPerSec, Quaternion.identity)) < minDTheta;
        bool convergedRotation = targetQ == Quaternion.identity || Mathf.Abs(Quaternion.Angle(transform.rotation, targetQ)) < minDTheta;
        if (convergedSpin && convergedRotation)
            {
            currentSpinPerSec = Quaternion.identity;
            converged = true;
        }
        else
        {
            converged = false;
        }
        return converged;
    }

}
