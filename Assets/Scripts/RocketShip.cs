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
    public float minDeltaTheta = 0.2f;
    public float minSpinDeltaDegPerSec = 10f;
    public float MaxRotationDegPerSec = 720;
    public float RotationSpeedFactor = 2;

    private NBody nbody;
    private float currentFuelKg;
    private float currentTotalMassKg;
    private float RCSThrustPerSec;
    private float EngineThrustPerSec;
    private float RCSAngularDegPerSec;
    private bool mainEngineOn;
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
        return RCSThrustPerSec / currentTotalMassKg;
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

    public void UpdateApplyCurrentSpin()
    {
        Quaternion timeQ = Quaternion.Lerp(transform.rotation, transform.rotation * currentSpinPerSec, Time.deltaTime);
        transform.rotation = timeQ;
    }

    public bool ConvergeSpin(Quaternion deltaQ, Quaternion targetQ)
    {
        bool converged;
        float angle = Mathf.Abs(Quaternion.Angle(currentSpinPerSec, deltaQ));
        // float speed = RCSAngularDegPerSec;
        float speed = Mathf.Max(RCSAngularDegPerSec / angle, 1);
        currentSpinPerSec = Quaternion.Lerp(currentSpinPerSec, deltaQ, RotationSpeedFactor * speed * Time.deltaTime);
        bool convergedSpin = Mathf.Abs(Quaternion.Angle(currentSpinPerSec, Quaternion.identity)) < minDeltaTheta;
        bool convergedRotation = targetQ == Quaternion.identity || Mathf.Abs(Quaternion.Angle(transform.rotation, targetQ)) < minDeltaTheta;
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
