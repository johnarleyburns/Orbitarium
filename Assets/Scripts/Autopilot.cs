using UnityEngine;
using System.Collections.Generic;

public class Autopilot : MonoBehaviour
{

    public GameController gameController;
    public float MaxRCSAutoBurnSec = 10;
    public float MinDeltaVforBurn = 0.1f;
    public float InterceptDeltaV = 30;
    public float NavigationalConstant = 3;
    public float MinRotToTargetDeltaTheta = 0.2f;
    public float MinAPNGDeltaTheta = 1f;
    public bool APNG = true;

    private RocketShip ship;
    private List<AutopilotCommand> currentProgram = new List<AutopilotCommand>()
    {
        new AutopilotCommand(AutopilotInstruction.NOOP, null, 0)
    };
    private int currentProgramCounter;
    private class AutopilotCommand
    {
        public AutopilotCommand(AutopilotInstruction a, object b, int c)
        {
            instruction = a;
            parameter = b;
            nextPC = c;
        }
        public AutopilotInstruction instruction;
        public object parameter;
        public int nextPC;
    }
    private enum AutopilotInstruction
    {
        NOOP,
        KILL_ROT,
        ROT_TO_UNITVEC,
        ROT_TO_TARGET,
        ROT_TO_TARGET_APNG,
        BURN_GO,
        BURN_SEC,
        BURN_TO_ZERO_RELV,
        BURN_TO_INTER_RELV,
        BURN_APNG
    };

    void Start()
    {
        ship = GetComponent<RocketShip>();
        ClearProgram();
    }

    void Update()
    {
        if (gameController != null)
        {
            switch (gameController.GetGameState())
            {
                case GameController.GameState.RUNNING:
                    UpdateExecuteInstruction();
                    break;
            }
        }
    }

    private void ClearProgram()
    {
        LoadProgram(new AutopilotCommand(AutopilotInstruction.NOOP, null, 0));
    }

    public void AutopilotOff()
    {
        ClearProgram();
    }

    private AutopilotCommand CurrentCommand()
    {
        lock (this)
        {
            return currentProgram[currentProgramCounter];
        }
    }

    private void LoadProgram(params AutopilotCommand[] commands)
    {
        lock (this)
        {
            currentProgram = new List<AutopilotCommand>(commands);
            currentProgramCounter = 0;
        }
    }

    public bool IsRot()
    {
        AutopilotCommand currentCommand = CurrentCommand();
        return currentCommand.instruction == AutopilotInstruction.KILL_ROT
            || IsAutoRot();
    }

    public bool IsKillRot()
    {
        AutopilotCommand currentCommand = CurrentCommand();
        return currentCommand.instruction == AutopilotInstruction.KILL_ROT;
    }

    public bool IsAutoRot()
    {
        AutopilotCommand currentCommand = CurrentCommand();
        return currentCommand.instruction == AutopilotInstruction.ROT_TO_TARGET
            || currentCommand.instruction == AutopilotInstruction.ROT_TO_UNITVEC
            || currentCommand.instruction == AutopilotInstruction.ROT_TO_TARGET_APNG;
    }

    public void KillRot()
    {
        LoadProgram(
            new AutopilotCommand(AutopilotInstruction.KILL_ROT, null, 1),
            new AutopilotCommand(AutopilotInstruction.NOOP, null, 1)
        );
    }

    public void AutoRot(GameObject target)
    {
        LoadProgram(
            new AutopilotCommand(AutopilotInstruction.ROT_TO_TARGET, target, 1),
            new AutopilotCommand(AutopilotInstruction.NOOP, null, 1)
        );
    }

    public void KillRelV(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        LoadProgram(
            new AutopilotCommand(AutopilotInstruction.ROT_TO_UNITVEC, -relVelUnit, 1),
            new AutopilotCommand(AutopilotInstruction.BURN_TO_ZERO_RELV, target, 2),
            new AutopilotCommand(AutopilotInstruction.ROT_TO_TARGET, target, 3),
            new AutopilotCommand(AutopilotInstruction.NOOP, null, 3)
        );
    }

    public void APNGToTarget(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        LoadProgram(
            new AutopilotCommand(AutopilotInstruction.ROT_TO_TARGET_APNG, target, 1),
            new AutopilotCommand(AutopilotInstruction.BURN_APNG, target, 0)
        );
    }

    public GameObject CurrentTarget()
    {
        AutopilotCommand currentCommand = currentProgram[currentProgramCounter];
        return currentCommand.parameter as GameObject;
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
            case AutopilotInstruction.ROT_TO_UNITVEC:
                ExecuteRotToUnitVec();
                break;
            case AutopilotInstruction.ROT_TO_TARGET:
                ExecuteRotToTarget();
                break;
            case AutopilotInstruction.ROT_TO_TARGET_APNG:
                ExecuteRotToTargetAPNG();
                break;
            case AutopilotInstruction.BURN_GO:
                ExecuteBurnGo();
                break;
            case AutopilotInstruction.BURN_SEC:
                ExecuteBurnSec();
                break;
            case AutopilotInstruction.BURN_TO_ZERO_RELV:
                ExecuteBurnToV(0f);
                break;
            case AutopilotInstruction.BURN_TO_INTER_RELV:
                ExecuteBurnToV(InterceptDeltaV);
                break;
            case AutopilotInstruction.BURN_APNG:
                ExecuteBurnAPNG();
                break;
        }
    }

    private void ExecuteNoop()
    {
        JumpToNextInstruction();
    }

    private void ExecuteKillRot()
    {
        bool convergedSpin = ship.KillRotation();
        if (convergedSpin)
        {
            JumpToNextInstruction();
        }
    }

    private void ExecuteRotToTarget()
    {
        GameObject target = CurrentCommand().parameter as GameObject;
        if (target != null)
        {
            Vector3 b = (target.transform.position - transform.parent.transform.position).normalized;
            ExecuteRotToUnitVec(b);
        }
        else
        {
            JumpToNextInstruction();
        }
    }

    private void ExecuteRotToUnitVec(Vector3? targetUnitVec = null)
    {
        Vector3 b;
        if (targetUnitVec.HasValue)
        {
            b = targetUnitVec.Value;
        }
        else
        {
            object o = CurrentCommand().parameter;
            if (o is Vector3)
            {
                b = (Vector3)o;
            }
            else
            {
                b = Vector3.zero;
            }
        }
        bool converged;
        if (b == Vector3.zero)
        {
            converged = true;
        }
        else
        {
            Quaternion q = Quaternion.LookRotation(b);
            q = Quaternion.Euler(q.eulerAngles.x, q.eulerAngles.y, transform.rotation.eulerAngles.z);
            converged = ship.ConvergeSpin(q, MinRotToTargetDeltaTheta);
        }
        if (converged)
        {
            JumpToNextInstruction();
        }
    }

    private Vector3 CalcAPNG(Transform source, GameObject targetNBody)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(source, targetNBody, out dist, out relv, out relVelUnit);
        float N = NavigationalConstant;

        Vector3 vr = relv * -relVelUnit;
        Vector3 r = targetNBody.transform.position - source.position;
        Vector3 o = Vector3.Cross(r, vr) / Vector3.Dot(r, r);
        Vector3 a = Vector3.Cross(-N * Mathf.Abs(vr.magnitude) * r.normalized, o);

        return a;
    }

    private void ExecuteRotToTargetAPNG()
    {
        GameObject target = CurrentCommand().parameter as GameObject;
        Vector3 a = CalcAPNG(transform.parent.transform, target);
        Quaternion q = Quaternion.LookRotation(a);
        bool converged = ship.ConvergeSpin(q, MinAPNGDeltaTheta * 4);
        if (converged)
        {
            JumpToNextInstruction();
        }
    }

    private float minRCSBurnTimeSec = 0.1f;
    private float minMainBurnTimeSec = 0.5f;
    private float burnTimerAPNG = -1;
    private bool isRCSGo = false;

    private void ExecuteBurnAPNG()
    {
        GameObject target = CurrentCommand().parameter as GameObject;
        Vector3 a = CalcAPNG(transform.parent.transform, target);
        bool shouldBurnRCS = a.magnitude > ship.CurrentRCSAccelerationPerSec() * minRCSBurnTimeSec && a.magnitude < ship.CurrentMainEngineAccPerSec() * minMainBurnTimeSec;
        bool shouldBurnMain = !shouldBurnRCS && a.magnitude > MinDeltaVforBurn;

        if (shouldBurnMain) // takes priority
        {
            if (!ship.IsMainEngineGo())
            {
                ship.MainEngineGo();
                burnTimerAPNG = minMainBurnTimeSec; // reset timer
            }
        }
        else if (shouldBurnRCS)
        {
            if (!isRCSGo)
            {
                ship.ApplyRCSImpulse(transform.forward);
                isRCSGo = true;
                burnTimerAPNG = minRCSBurnTimeSec;
            }
        }

        if (ship.IsMainEngineGo() || isRCSGo)
        {
            if (isRCSGo)
            {
                ship.ApplyRCSImpulse(transform.forward);
            }
        }

        if (burnTimerAPNG != -1)
        {
            burnTimerAPNG -= Time.deltaTime;
        }

        if (burnTimerAPNG > -1 && burnTimerAPNG < 0)
        {
            isRCSGo = false;
            ship.MainEngineCutoff();
            burnTimerAPNG = -1;
        }

        if (!ship.IsMainEngineGo() && !isRCSGo)
        {
            JumpToNextInstruction();
        }
    }


    private void ExecuteBurnGo()
    {
        if (!ship.IsMainEngineGo())
        {
            ship.MainEngineGo();
        }
        JumpToNextInstruction();
    }

    private float burnTimer = -1;

    private void ExecuteBurnSec()
    {
        if (burnTimer == -1)
        {
            float? sec = CurrentCommand().parameter as float?;
            if (sec.HasValue)
            {
                ship.MainEngineBurst(sec.Value);
                burnTimer = sec.Value;
            }
        }
        else if (burnTimer <= 0)
        {
            burnTimer = -1;
            JumpToNextInstruction();
        }
        else
        {
            burnTimer -= Time.deltaTime;
        }
    }

    private void ExecuteBurnToV(float desiredDeltaV)
    {
        bool done = false;
        bool burnMain = false;
        bool burnRCS = false;
        bool burning = ship.IsMainEngineGo();

        // check relv magnitude
        GameObject target = CurrentCommand().parameter as GameObject;
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        float absRelVGap = Mathf.Abs(desiredDeltaV - relv);
        if ((desiredDeltaV > 0 && relv > desiredDeltaV)
            || absRelVGap <= MinDeltaVforBurn) // burned enough or too much
        {
            burnMain = false;
            burnRCS = false;
            done = true;
        }
        else
        {
            // is RCS sufficient?
            bool rcsSufficient = absRelVGap <= ship.RCSFuelKgPerSec;
            // if not burning and magnitude greater than RCS thrust ten sec then main engine burn
            float maxRCSDeltaV = MaxRCSAutoBurnSec * ship.CurrentRCSAccelerationPerSec();
            burnMain = absRelVGap > maxRCSDeltaV;
            burnRCS = absRelVGap <= maxRCSDeltaV;
        }

        if (burnMain)
        {
            if (!burning)
            {
                ship.MainEngineGo();
            }
        }
        else
        {
            if (burning)
            {
                ship.MainEngineCutoff();
            }
            if (burnRCS)
            {
                ship.ApplyRCSImpulse(-relVelUnit);
            }
        }

        if (done)
        {
            JumpToNextInstruction();
        }
    }

    private void JumpToNextInstruction()
    {
        currentProgramCounter = CurrentCommand().nextPC;
    }

}
