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
    public float AutoRotationSpeedFactor = 20;

    private NBody nbody;
    private float currentFuelKg;
    private float currentTotalMassKg;
    private float RCSThrustPerSec;
    private float EngineThrustPerSec;
    private float RCSAngularDegPerSec;
    private bool mainEngineOn;
    private Quaternion currentSpinPerSec;
    private List<AutopilotCommand> currentProgram;
    private int currentProgramCounter;
    public class AutopilotCommand
    {
        public AutopilotCommand(AutopilotInstruction a, GameObject b, float c, int d)
        {
            instruction = a;
            target = b;
            number = c;
            nextPC = d;
        }
        public AutopilotInstruction instruction;
        public GameObject target;
        public float number;
        public int nextPC;
    }
    public enum AutopilotInstruction
    {
        NOOP,
        KILL_ROT,
        ROT_TO_TARGET,
        BURN_TO_V
    };

    void Start()
    {
        nbody = transform.parent.GetComponent<NBody>();
        mainEngineOn = false;
        currentTotalMassKg = EmptyMassKg + FuelMassKg;
        currentFuelKg = FuelMassKg;
        currentSpinPerSec = Quaternion.identity;
        ClearProgram();
        UpdateThrustRates();
    }

    void Update()
    {
        if (gameController != null)
        {
            switch (gameController.GetGameState())
            {
                case GameController.GameState.RUNNING:
                    UpdateExecuteInstruction();
                    UpdateThrustRates();
                    UpdateEngine();
                    UpdateApplyCurrentSpin();
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

    private void ClearProgram()
    {
        currentProgram = new List<AutopilotCommand>()
        {
            new AutopilotCommand(AutopilotInstruction.NOOP, null, 0, 0)
        };
        currentProgramCounter = 0;
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

    public void AutopilotOff()
    {
        ClearProgram();
    }

    private AutopilotCommand CurrentCommand()
    {
        return currentProgram[currentProgramCounter];
    }

    public bool IsRot()
    {
        AutopilotCommand currentCommand = CurrentCommand();
        return currentCommand.instruction == AutopilotInstruction.KILL_ROT
            || currentCommand.instruction == AutopilotInstruction.ROT_TO_TARGET
            || currentCommand.instruction == AutopilotInstruction.BURN_TO_V;
    }

    public bool IsKillRot()
    {
        AutopilotCommand currentCommand = CurrentCommand();
        return currentCommand.instruction == AutopilotInstruction.KILL_ROT;
    }

    public bool IsAutoRot()
    {
        AutopilotCommand currentCommand = CurrentCommand();
        return currentCommand.instruction == AutopilotInstruction.ROT_TO_TARGET;
    }

    public void KillRot()
    {
        currentProgram = new List<AutopilotCommand>()
        {
            new AutopilotCommand(AutopilotInstruction.KILL_ROT, null, 0, 1),
            new AutopilotCommand(AutopilotInstruction.NOOP, null, 0, 1)
        };
        currentProgramCounter = 0;
    }

    public void AutoRot(GameObject target)
    {
        currentProgram = new List<AutopilotCommand>()
        {
            new AutopilotCommand(AutopilotInstruction.ROT_TO_TARGET, target, 0, 1),
            new AutopilotCommand(AutopilotInstruction.NOOP, null, 0, 1)
        };
    }

    public void KillRelV(GameObject target)
    {
        GameObject negV = null;
        currentProgram = new List<AutopilotCommand>()
        {
            new AutopilotCommand(AutopilotInstruction.ROT_TO_TARGET, negV, 0, 1),
            new AutopilotCommand(AutopilotInstruction.BURN_TO_V, null, 0, 2),
            new AutopilotCommand(AutopilotInstruction.NOOP, null, 0, 2)
        };
        currentProgramCounter = 0;
    }

    public GameObject CurrentTarget()
    {
        AutopilotCommand currentCommand = currentProgram[currentProgramCounter];
        return currentCommand.target;
    }

    private void UpdateExecuteInstruction()
    {
        switch (CurrentCommand().instruction)
        {
            case AutopilotInstruction.NOOP:
                ExecuteNoop();
                break;
            case AutopilotInstruction.KILL_ROT:
                ExecuteKillRot();
                break;
            case AutopilotInstruction.ROT_TO_TARGET:
                ExecuteRotToTarget();
                break;
            case AutopilotInstruction.BURN_TO_V:
                ExecuteBurnToV();
                break;
        }
    }

    private void ExecuteNoop()
    {
        JumpToNextInstruction();
    }

    private void ExecuteKillRot()
    {
        ConvergeSpin(Quaternion.identity);
        if (Mathf.Abs(Quaternion.Angle(currentSpinPerSec, Quaternion.identity)) < minDeltaTheta)
        {
            currentSpinPerSec = Quaternion.identity;
            JumpToNextInstruction();
        }
    }

    private void ExecuteRotToTarget()
    {
        Vector3 b = (CurrentCommand().target.transform.position - transform.parent.transform.position).normalized;
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
            currentSpinPerSec = Quaternion.identity;
            JumpToNextInstruction();
        }
    }

    private void ExecuteBurnToV()
    {
        // check relv magnitude
        // if not burning and magnitude greater than RCS thrust then main engine burn
        // else if not burning and magnitude less than RCS thrust then RCS thrust once
        // else if burning and relv magnitude less than min delta then stop burning
        // else jumptonext
    }

    private void JumpToNextInstruction()
    {
        currentProgramCounter = CurrentCommand().nextPC;
    }

    public void UpdateApplyCurrentSpin()
    {
        Quaternion timeQ = Quaternion.Lerp(transform.rotation, transform.rotation * currentSpinPerSec, Time.deltaTime);
        transform.rotation = timeQ;
    }

    private void ConvergeSpin(Quaternion deltaQ)
    {
        float angle = Mathf.Abs(Quaternion.Angle(currentSpinPerSec, deltaQ));
        // float speed = RCSAngularDegPerSec;
        float speed = Mathf.Max(RCSAngularDegPerSec / angle, 1);
        currentSpinPerSec = Quaternion.Lerp(currentSpinPerSec, deltaQ, RotationSpeedFactor * speed * Time.deltaTime);
    }

}
