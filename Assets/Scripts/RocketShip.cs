using UnityEngine;
using System.Collections.Generic;

public class RocketShip : MonoBehaviour {

    public GameController gameController;
    public NBodyDimensions NBodyDimensions;
    public GameObject DockingPort;
    public float EmptyMassKg = 10000;
    public float FuelMassKg = 9200;
    public float RCSThrustNewtons = 880; // 220*4 in the SM
    public float EngineThrustNewtons = 25700; // 1 in the SM
    public float AuxThrustNewtons = 3920; // 490 * 8 in the SM
    public float RCSFuelKgPerSec = 0.14f;
    public float EngineFuelKgPerSec = 8.7f;
    public float AuxFuelKgPerSec = 1.3f;
    public float DumpFuelRateKgPerSec = 100;
    public float RCSRadiusM = 2.5f;
    public float minSpinDeltaDegPerSec = 0.2f;
    public float MaxRotationDegPerSec = 720;
    public float RCSBurnMinSec = 0.02f;
    public float RCSFineControlFactor = 0.5f;

    private float currentFuelKg;
    private float currentTotalMassKg;
    private float RCSThrustPerSec;
    private float EngineThrustPerSec;
    private float AuxThrustPerSec;
    private float RCSAngularDegPerSec;
    private bool mainEngineOn;
    private bool auxEngineOn;
    private bool rcsFineControlOn;
    private bool rcsOn;
    private float mainEngineCutoffTimer = -1;
    private float auxEngineCutoffTimer = -1;
    private float rcsCutoffTimer = -1;
    private Vector3 rcsDirection;
    private Quaternion currentSpinPerSec;

    void Start()
    {
        mainEngineOn = false;
        auxEngineOn = false;
        rcsFineControlOn = false;
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
                    UpdateEngines();
                    UpdateRCS();
                    UpdateApplyCurrentSpin();
                    break;
            }
        }
    }

    public float CurrentRCSAccelerationPerSec()
    {
        float adj = currentRCSFactor();
        return RCSThrustPerSec * adj;
    }

    public float CurrentMainEngineAccPerSec()
    {
        return EngineThrustPerSec;
    }

    public float CurrentAuxAccPerSec()
    {
        return AuxThrustPerSec;
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

    public void CutoffAll()
    {
        MainEngineCutoff();
        AuxEngineCutoff();
        RCSCutoff();
    }

    public bool IsMainEngineGo()
    {
        return mainEngineOn;
    }

    public void AuxEngineGo()
    {
        auxEngineOn = true;
    }

    public void AuxEngineCutoff()
    {
        auxEngineOn = false;
    }

    public bool IsAuxEngineGo()
    {
        return auxEngineOn;
    }

    public bool IsSpinning()
    {
        return currentSpinPerSec != Quaternion.identity;
    }

    public void RCSFineControlOn()
    {
        rcsFineControlOn = true;
    }

    public void RCSFineControlOff()
    {
        rcsFineControlOn = false;
    }

    public bool IsRCSFineControlOn()
    {
        return rcsFineControlOn;
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

    private void UpdateEngines()
    {
        if (mainEngineOn)
        {
            if (currentFuelKg > EngineFuelKgPerSec && (mainEngineCutoffTimer <= -1 || mainEngineCutoffTimer > 0))
            {
                ApplyImpulse(transform.forward, EngineThrustPerSec, Time.deltaTime);
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
        if (auxEngineOn)
        {
            if (currentFuelKg > AuxFuelKgPerSec && (auxEngineCutoffTimer <= -1 || auxEngineCutoffTimer > 0))
            {
                ApplyImpulse(transform.forward, AuxThrustPerSec, Time.deltaTime);
                ApplyFuel(-AuxFuelKgPerSec);
                if (auxEngineCutoffTimer > 0)
                {
                    auxEngineCutoffTimer -= Time.deltaTime;
                }
            }
            else
            {
                AuxEngineCutoff();
                auxEngineCutoffTimer = -1;
            }
        }
    }

    public void RCSCutoff()
    {
        rcsOn = false;
    }

    public bool IsRCSFiring()
    {
        return rcsOn;
    }

    public void RCSBurst(Vector3 rcsDir, float sec)
    {
        float adj = currentRCSFactor();
        if (rcsDir.magnitude > 0 && currentFuelKg > sec * RCSFuelKgPerSec * adj)
        {
            rcsDirection = rcsDir;
            rcsCutoffTimer = sec;
            rcsOn = true;
        }
        else
        {
            RCSCutoff();
        }
    }

    private float currentRCSFactor()
    {
        float adj = rcsFineControlOn ? RCSFineControlFactor : 1.0f;
        return adj;
    }

    private void UpdateRCS()
    {
        if (rcsOn)
        {
            float adj = currentRCSFactor();
            if (currentFuelKg > RCSFuelKgPerSec * adj && (rcsCutoffTimer <= -1 || rcsCutoffTimer > 0))
            {
                ApplyImpulse(rcsDirection, RCSThrustPerSec * adj, Time.deltaTime);
                ApplyFuel(-RCSFuelKgPerSec * adj);
                if (rcsCutoffTimer > 0)
                {
                    rcsCutoffTimer -= Time.deltaTime;
                }
            }
            else
            {
                RCSCutoff();
                rcsCutoffTimer = -1;
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

    public void ApplyImpulse(Vector3 normalizedDirection, float thrustPerSec, float sec)
    {
        Vector3 thrust = normalizedDirection * thrustPerSec * sec;
        GravityEngine.instance.ApplyImpulse(NBodyDimensions.NBody.GetComponent<NBody>(), thrust / NBodyDimensions.NBodyToModelScaleFactor);
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

    private void ApplyFuel(float deltaFuel, float sec)
    {
        currentFuelKg += deltaFuel * sec;
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
        AuxThrustPerSec = AuxThrustNewtons / currentTotalMassKg;
        RCSAngularDegPerSec = Mathf.Rad2Deg * Mathf.Sqrt(RCSThrustPerSec / RCSRadiusM);
    }

    public void ApplyRCSSpin(Quaternion spinDelta)
    {
        Quaternion timeSpinQ = Quaternion.Slerp(currentSpinPerSec, currentSpinPerSec * spinDelta, Time.deltaTime);
        currentSpinPerSec = timeSpinQ;
    }

    public void UpdateApplyCurrentSpin()
    {
        Quaternion timeQ = Quaternion.Slerp(transform.rotation, transform.rotation * currentSpinPerSec, Time.deltaTime);
        transform.rotation = timeQ;
    }

    public Quaternion CurrentSpinPerSec
    {
        get
        {
            return currentSpinPerSec;
        }
    }

    public void NullSpin() // only for things like docking
    {
        currentSpinPerSec = Quaternion.identity;
    }

    private static readonly float minSpinDeltaTheta = 1f; // too small and it snafus
    private static readonly float precisionMinSpinDeltaTheta = 0.2f; // too small and it snafus

    public bool ConvergeSpin(Quaternion targetQ, bool precision = false)
    {
        float dTheta = precision ? precisionMinSpinDeltaTheta : minSpinDeltaTheta;

        Quaternion deltaQ = Quaternion.Inverse(transform.rotation) * targetQ;

        float angleLeft = Mathf.Abs(Quaternion.Angle(transform.rotation, targetQ));
        float spinSpeed = Mathf.Abs(Quaternion.Angle(currentSpinPerSec, Quaternion.identity));

        float secToCoast = angleLeft / spinSpeed;
        float secToStop = Mathf.Sqrt(2 * spinSpeed / RCSAngularDegPerSec);

        if (secToStop < secToCoast) // speedup
        {
            currentSpinPerSec = Quaternion.RotateTowards(currentSpinPerSec, deltaQ, RCSAngularDegPerSec * Time.deltaTime);
        }
        else // stop
        {
            currentSpinPerSec = Quaternion.RotateTowards(currentSpinPerSec, Quaternion.identity, RCSAngularDegPerSec * Time.deltaTime);
        }
        bool convergedSpin = Mathf.Abs(Quaternion.Angle(currentSpinPerSec, Quaternion.identity)) < dTheta;
        bool convergedRotation = targetQ == Quaternion.identity || Mathf.Abs(Quaternion.Angle(transform.rotation, targetQ)) < dTheta;
        bool converged = convergedSpin && convergedRotation;
        if (converged)
        {
            currentSpinPerSec = Quaternion.identity;
        }
        return converged;
    }

    public float MainEngineExhaustVelocity()
    {
        return EngineThrustNewtons / EngineFuelKgPerSec;
    }

    public float MainEngineISP()
    {
        return MainEngineExhaustVelocity() / PhysicsUtils.g;
    }

}
