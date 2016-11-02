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
    private List<object> program = new List<object>();
    private int programCounter;
    private CommandState commandState;
    private enum CommandState
    {
        IDLE,
        EXECUTING
    }
    private enum Instruction
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
    private delegate void AutopilotFunction();
    private Dictionary<Instruction, AutopilotFunction> autopilotInstructions;

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
                    if (programCounter >= 0 && programCounter < program.Count)
                    {
                        object line = program[programCounter];
                        ProcessLine(line);
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
        program.Clear();
    }

    public void AutopilotOff()
    {
        ClearProgram();
    }

    private Instruction CurrentInstruction()
    {
        Instruction inst = Instruction.NOOP;
        lock (program)
        {
            if (commandState == CommandState.EXECUTING && programCounter >= 0 && programCounter < program.Count)
            {
                Instruction? instP = program[programCounter] as Instruction?;
                if (instP != null && instP.HasValue)
                {
                    inst = instP.Value;
                }
            }
        }
        return inst;
    }

    private void ProcessLine(object line)
    {
        Instruction? instructionP = line as Instruction?;
        if (instructionP != null && instructionP.HasValue)
        {
            Instruction instruction = instructionP.Value;
            AutopilotFunction func;
            if (autopilotInstructions.TryGetValue(instruction, out func))
            {
                func();
            }
            else
            {
                errorStack.Push("instruction definition not found inst=" + instruction);
                commandState = CommandState.IDLE;
            }
        }
        else
        {
            dataStack.Push(line);
            programCounter++;
        }
    }

    private void LoadProgram(params object[] programList)
    {
        lock (this)
        {
            ClearProgram();
            program = new List<object>(programList);
            programCounter = 0;
            commandState = CommandState.EXECUTING;
        }
    }

    public bool IsRot()
    {
        return CurrentInstruction() == Instruction.KILL_ROT
            || IsAutoRot();
    }

    public bool IsKillRot()
    {
        return CurrentInstruction() == Instruction.KILL_ROT;
    }

    public bool IsAutoRot()
    {
        Instruction inst = CurrentInstruction();
        return inst == Instruction.ROT_TO_TARGET
            || inst == Instruction.ROT_TO_UNITVEC
            || inst == Instruction.ROT_TO_TARGET_APNG;
    }

    public void KillRot()
    {
        LoadProgram(
            Instruction.BURN_STOP,
            Instruction.KILL_ROT
        );
    }

    public void AutoRot(GameObject target)
    {
        LoadProgram(
            Instruction.BURN_STOP,
            target,
            Instruction.ROT_TO_TARGET
        );
    }

    public void KillRelV(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        LoadProgram(
            Instruction.BURN_STOP,
            -relVelUnit,
            Instruction.ROT_TO_UNITVEC,
            target,
            Instruction.BURN_TO_ZERO_RELV,
            target,
            Instruction.ROT_TO_TARGET
        );
    }

    public void Rendezvous(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        LoadProgram(
            Instruction.BURN_STOP,
            -relVelUnit,
            Instruction.ROT_TO_UNITVEC,
            target,
            Instruction.BURN_TO_ZERO_RELV,
            target,
            Instruction.ROT_TO_TARGET
        );
    }

    public void APNGToTarget(GameObject target)
    {
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        LoadProgram(
            Instruction.BURN_STOP,
            target,
            Instruction.ROT_TO_TARGET_APNG,
            target,
            Instruction.BURN_APNG,
            1,
            Instruction.JUMP
        );
    }

    private void AddAutopilotDefinitions()
    {
        autopilotInstructions = new Dictionary<Instruction, AutopilotFunction>()
        {
            { Instruction.NOOP,
                ExecuteNoop },
            { Instruction.KILL_ROT,
                ExecuteKillRot },
            { Instruction.ROT_TO_UNITVEC,
                ExecuteRotToUnitVec },
            { Instruction.ROT_TO_TARGET,
                ExecuteRotToTarget },
            { Instruction.ROT_TO_TARGET_APNG,
                ExecuteRotToTargetAPNG },
            { Instruction.BURN_GO,
                ExecuteBurnGo },
            { Instruction.BURN_STOP,
                ExecuteBurnStop },
            { Instruction.BURN_SEC,
                ExecuteBurnSec },
            { Instruction.BURN_TO_ZERO_RELV,
                ExecuteBurnToZeroRelV },
            { Instruction.BURN_TO_INTER_RELV,
                ExecuteBurnToInterceptRelV },
            { Instruction.BURN_APNG,
                ExecuteBurnAPNG },
            { Instruction.JUMP,
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
        GameObject target = dataStack.Peek() as GameObject;
        if (target != null)
        {
            Vector3 b = (target.transform.position - transform.parent.transform.position).normalized;
            bool converged = RotToUnitVec(b);
            if (converged)
            {
                MarkCompleted();
            }
        }
        else
        {
            errorStack.Push("Rotate to target needs a gameobject on the stack");
            commandState = CommandState.IDLE;
        }
    }

    private void ExecuteRotToUnitVec()
    {
        Vector3? b = dataStack.Peek() as Vector3?;
        if (b != null && b.HasValue)
        {
            bool converged = RotToUnitVec(b.Value);
            if (converged)
            {
                MarkCompleted();
            }
        }
        else
        {
            errorStack.Push("Rotate to unit vec needs a unit vector3 on the stack");
            commandState = CommandState.IDLE;
        }
    }

    private bool RotToUnitVec(Vector3 b)
    {
        Quaternion q = Quaternion.LookRotation(b);
        q = Quaternion.Euler(q.eulerAngles.x, q.eulerAngles.y, transform.rotation.eulerAngles.z);
        bool converged = ship.ConvergeSpin(q, MinRotToTargetDeltaTheta);
        return converged;
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
        GameObject target = dataStack.Peek() as GameObject;
        if (target != null)
        {
            Vector3 a = CalcAPNG(transform.parent.transform, target);
            Quaternion q = Quaternion.LookRotation(a);
            bool converged = ship.ConvergeSpin(q, MinAPNGDeltaTheta * 4);
            if (converged)
            {
                dataStack.Pop();
                MarkCompleted();
            }
        }
        else
        {
            errorStack.Push("rotate to target APNG needs a target on the stack");
            commandState = CommandState.IDLE;
        }
    }

    private float minRCSBurnTimeSec = 0.1f;
    private float minMainBurnTimeSec = 0.5f;
    private float burnTimerAPNG = -1;
    private bool isRCSGo = false;

    private void ExecuteBurnAPNG()
    {
        GameObject target = dataStack.Peek() as GameObject;
        if (target != null)
        {
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
                dataStack.Pop();
            MarkCompleted();
        }
        }
        else
        {
            errorStack.Push("APNG burn needs a target on the stack");
            commandState = CommandState.IDLE;
            return;
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
            float? sec = dataStack.Peek() as float?;
            if (sec != null && sec.HasValue)
            {
                ship.MainEngineBurst(sec.Value);
                burnTimer = sec.Value;
            }
        }
        else if (burnTimer > 0)
        {
            burnTimer -= Time.deltaTime;
        }
        else
        {
            burnTimer = -1;
            dataStack.Pop();
            MarkCompleted();
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
        // check relv magnitude
        GameObject target = dataStack.Peek() as GameObject;
        if (target != null)
        {
            bool done = false;
            bool burnMain = false;
            bool burnRCS = false;
            bool burning = ship.IsMainEngineGo();

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
        else
        {
            errorStack.Push("burn to relV must have a target on the stack");
            commandState = CommandState.IDLE;
        }
    }

    private void MarkCompleted()
    {
        programCounter++;
    }

    private void ExecuteJump()
    {
        int? newPC = dataStack.Peek() as int?;
        if (newPC != null && newPC.HasValue)
        {
            dataStack.Pop();
            programCounter = newPC.Value;
        }
        else
        {
            errorStack.Push("jump parameter invalid");
            commandState = CommandState.IDLE;
        }
    }

}
