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
    private Stack<object> dataStack = new Stack<object>();
    private Stack<object> errorStack = new Stack<object>();
    private List<AutopilotCommand> programCommands = new List<AutopilotCommand>();
    private int programCounter;
    private CommandState commandState;
    private enum CommandState
    {
        IDLE,
        EXECUTING
    }
    private class AutopilotCommand
    {
        public AutopilotCommand(AutopilotInstruction a, object b)
        {
            instruction = a;
            parameter = b;
        }
        public AutopilotInstruction instruction;
        public object parameter;
    }
    private enum AutopilotInstruction
    {
        NOOP,
        KILL_ROT,
        ROT_TO_UNITVEC,
        ROT_TO_TARGET,
        ROT_TO_TARGET_APNG,
        BURN_GO,
        BURN_STOP,
        BURN_SEC,
        BURN_TO_ZERO_RELV,
        BURN_TO_INTER_RELV,
        BURN_APNG,
        JUMP
    };

    void Start()
    {
        ship = GetComponent<RocketShip>();
        AddAutopilotDefinitions();
        ClearProgram();
    }

    void Update()
    {
        if (gameController != null && gameController.GetGameState() == GameController.GameState.RUNNING)
        {
            switch (commandState)
            {
                case CommandState.IDLE:
                    break;
                case CommandState.EXECUTING:
                    if (programCounter >= 0 && programCounter < programCommands.Count)
                    {
                        ExecuteCommand();
                    }
                    else
                    {
                        errorStack.Push("programCounter invalid pc=" + programCounter);
                        commandState = CommandState.IDLE;
                    }
                    break;
            }
        }
    }

    private void ClearProgram()
    {
        commandState = CommandState.IDLE;
        programCounter = -1;
        dataStack.Clear();
        errorStack.Clear();
        programCommands.Clear();
    }

    public void AutopilotOff()
    {
        ClearProgram();
    }

    private AutopilotCommand CurrentCommand()
    {
        AutopilotCommand command;
        lock (programCommands)
        {
            if (programCounter >= 0 && programCounter < programCommands.Count)
            {
                command = programCommands[programCounter];
            }
            else
            {
                command = new AutopilotCommand(AutopilotInstruction.NOOP, null);
            }
        }
        return command;
    }

    private void ExecuteCommand()
    {
        AutopilotCommand command = CurrentCommand();
        AutopilotInstruction instruction = command.instruction;
        AutopilotFunction func;
        if (!autopilotInstructions.TryGetValue(instruction, out func))
        {
            errorStack.Push("instruction definition not found inst=" + instruction);
        }
        func();
    }

    private void LoadProgram(params AutopilotCommand[] commands)
    {
        lock (this)
        {
            ClearProgram();
            programCommands = new List<AutopilotCommand>(commands);
            programCounter = 0;
            commandState = CommandState.EXECUTING;
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
            new AutopilotCommand(AutopilotInstruction.BURN_STOP, null),
            new AutopilotCommand(AutopilotInstruction.KILL_ROT, null)
        );
    }

    public void AutoRot(GameObject target)
    {
        LoadProgram(
            new AutopilotCommand(AutopilotInstruction.BURN_STOP, null),
            new AutopilotCommand(AutopilotInstruction.ROT_TO_TARGET, target)
        );
    }

    public void KillRelV(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        LoadProgram(
            new AutopilotCommand(AutopilotInstruction.BURN_STOP, null),
            new AutopilotCommand(AutopilotInstruction.ROT_TO_UNITVEC, -relVelUnit),
            new AutopilotCommand(AutopilotInstruction.BURN_TO_ZERO_RELV, target),
            new AutopilotCommand(AutopilotInstruction.ROT_TO_TARGET, target)
        );
    }

    public void Rendezvous(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        LoadProgram(
            new AutopilotCommand(AutopilotInstruction.BURN_STOP, null),
            new AutopilotCommand(AutopilotInstruction.ROT_TO_UNITVEC, -relVelUnit),
            new AutopilotCommand(AutopilotInstruction.BURN_TO_ZERO_RELV, target),
            new AutopilotCommand(AutopilotInstruction.ROT_TO_TARGET, target)
        );
    }

    public void APNGToTarget(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        LoadProgram(
            new AutopilotCommand(AutopilotInstruction.BURN_STOP, null),
            new AutopilotCommand(AutopilotInstruction.ROT_TO_TARGET_APNG, target),
            new AutopilotCommand(AutopilotInstruction.BURN_APNG, target),
            new AutopilotCommand(AutopilotInstruction.JUMP, 1)
        );
    }

    public GameObject CurrentTarget()
    {
        AutopilotCommand currentCommand = programCommands[programCounter];
        return currentCommand.parameter as GameObject;
    }

    private delegate void AutopilotFunction();
    private Dictionary<AutopilotInstruction, AutopilotFunction> autopilotInstructions;

    private void AddAutopilotDefinitions()
    {
        autopilotInstructions = new Dictionary<AutopilotInstruction, AutopilotFunction>()
        {
            { AutopilotInstruction.NOOP,
                ExecuteNoop },
            { AutopilotInstruction.KILL_ROT,
                ExecuteKillRot },
            { AutopilotInstruction.ROT_TO_UNITVEC,
                ExecuteRotToUnitVec },
            { AutopilotInstruction.ROT_TO_TARGET,
                ExecuteRotToTarget },
            { AutopilotInstruction.ROT_TO_TARGET_APNG,
                ExecuteRotToTargetAPNG },
            { AutopilotInstruction.BURN_GO,
                ExecuteBurnGo },
            { AutopilotInstruction.BURN_STOP,
                ExecuteBurnStop },
            { AutopilotInstruction.BURN_SEC,
                ExecuteBurnSec },
            { AutopilotInstruction.BURN_TO_ZERO_RELV,
                ExecuteBurnToZeroRelV },
            { AutopilotInstruction.BURN_TO_INTER_RELV,
                ExecuteBurnToInterceptRelV },
            { AutopilotInstruction.BURN_APNG,
                ExecuteBurnAPNG },
            { AutopilotInstruction.JUMP,
                ExecuteJump
            }
        };
    }

    private void ExecuteNoop()
    {
        MarkCompleted();
    }

    private void ExecuteKillRot()
    {
        bool convergedSpin = ship.KillRotation();
        if (convergedSpin)
        {
            MarkCompleted();
        }
    }

    private void ExecuteRotToTarget()
    {
        bool converged = false;
        GameObject target = CurrentCommand().parameter as GameObject;
        Vector3 b = (target.transform.position - transform.parent.transform.position).normalized;
        ExecuteRotToUnitVec(b);
    }

    private void ExecuteRotToUnitVec()
    {
        Vector3? o = CurrentCommand().parameter as Vector3?;
        if (o.HasValue)
        {
            ExecuteRotToUnitVec(o.Value);
        }
        else
        {
            errorStack.Push("Rotate to unit vec needs a target parameter");
            commandState = CommandState.IDLE;
        }
    }

    private void ExecuteRotToUnitVec(Vector3 b)
    {
        Quaternion q = Quaternion.LookRotation(b);
        q = Quaternion.Euler(q.eulerAngles.x, q.eulerAngles.y, transform.rotation.eulerAngles.z);
        bool converged = ship.ConvergeSpin(q, MinRotToTargetDeltaTheta);
        if (converged)
        {
            MarkCompleted();
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
            MarkCompleted();
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
            MarkCompleted();
        }
    }


    private void ExecuteBurnGo()
    {
        if (!ship.IsMainEngineGo())
        {
            ship.MainEngineGo();
        }
        MarkCompleted();
    }

    private void ExecuteBurnStop()
    {
        if (ship.IsMainEngineGo())
        {
            ship.MainEngineCutoff();
        }
        MarkCompleted();
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
            MarkCompleted();
        }
        else
        {
            burnTimer -= Time.deltaTime;
        }
    }

    private void ExecuteBurnToZeroRelV()
    {
        ExecuteBurnToRelV(0f);
    }

    private void ExecuteBurnToInterceptRelV()
    {
        ExecuteBurnToRelV(InterceptDeltaV);
    }

    private void ExecuteBurnToRelV(float desiredDeltaV)
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
            MarkCompleted();
        }
    }

    private void MarkCompleted()
    {
        programCounter++;
    }

    private void ExecuteJump()
    {
        int? newPC = CurrentCommand().parameter as int?;
        if (newPC.HasValue)
        {
            programCounter = newPC.Value;
        }
        else
        {
            errorStack.Push("jump parameter invalid");
            programCounter = -1;
        }
    }

}
