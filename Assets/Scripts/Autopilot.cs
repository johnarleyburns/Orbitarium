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
        SWAP,
        POP,
        DUP,
        VECTOR3_SCALAR_MULTIPLY,
        KILL_ROT,
        CALC_RELV,
        CALC_APNG,
        ROT_TO_UNITVEC,
        ROT_TO_TARGET,
        BURN_GO,
        BURN_STOP,
        BURN_SEC,
        BURN_TO_RELV,
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
                        MarkIdle("programCounter invalid pc=" + programCounter);
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
                if (instP != null)
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
        if (instructionP != null)
        {
            Instruction instruction = instructionP.Value;
            AutopilotFunction func;
            if (autopilotInstructions.TryGetValue(instruction, out func))
            {
                func();
            }
            else
            {
                MarkIdle("instruction definition not found inst=" + instruction);
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
            || inst == Instruction.ROT_TO_UNITVEC;
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
            target,
            Instruction.BURN_STOP,
            Instruction.ROT_TO_TARGET
        );
    }

    public void KillRelV(GameObject target)
    {
        LoadProgram(
            target,
            Instruction.DUP,
            Instruction.DUP,
            Instruction.BURN_STOP,
            Instruction.CALC_RELV,
            Instruction.SWAP,
            Instruction.POP,
            Instruction.SWAP,
            Instruction.POP,
            -1.0f,
            Instruction.SWAP,
            Instruction.VECTOR3_SCALAR_MULTIPLY,
            Instruction.ROT_TO_UNITVEC,
            0f,
            Instruction.BURN_TO_RELV
        );
    }

    public void Rendezvous(GameObject target)
    {
        LoadProgram(
            target,
            Instruction.DUP,
            Instruction.DUP,
            Instruction.BURN_STOP,
            Instruction.CALC_RELV,
            Instruction.SWAP,
            Instruction.POP,
            Instruction.SWAP,
            Instruction.POP,
            -1.0f,
            Instruction.SWAP,
            Instruction.VECTOR3_SCALAR_MULTIPLY,
            Instruction.ROT_TO_UNITVEC,
            0f,
            Instruction.BURN_TO_RELV,
            Instruction.ROT_TO_TARGET
        );
    }

    public void APNGToTarget(GameObject target)
    {
        LoadProgram(
            target,
            Instruction.BURN_STOP,
            Instruction.DUP,
            Instruction.CALC_RELV,
            Instruction.CALC_APNG,
            Instruction.DUP,
            Instruction.ROT_TO_UNITVEC,
            Instruction.BURN_APNG,
            2,
            Instruction.JUMP
        );
    }

    private void AddAutopilotDefinitions()
    {
        autopilotInstructions = new Dictionary<Instruction, AutopilotFunction>()
        {
            { Instruction.NOOP, ExecuteNoop },
            { Instruction.SWAP, ExecuteSwap },
            { Instruction.POP, ExecutePop },
            { Instruction.DUP, ExecuteDup },
            { Instruction.KILL_ROT, ExecuteKillRot },
            { Instruction.VECTOR3_SCALAR_MULTIPLY, ExecuteVector3ScalarMultiply },
            { Instruction.CALC_RELV, ExecuteCalcRelv },
            { Instruction.CALC_APNG, ExecuteCalcAPNG },
            { Instruction.ROT_TO_UNITVEC, ExecuteRotToUnitVec },
            { Instruction.ROT_TO_TARGET, ExecuteRotToTarget },
            { Instruction.BURN_GO, ExecuteBurnGo },
            { Instruction.BURN_STOP, ExecuteBurnStop },
            { Instruction.BURN_SEC, ExecuteBurnSec },
            { Instruction.BURN_TO_RELV, ExecuteBurnToRelV },
            { Instruction.BURN_APNG, ExecuteBurnAPNG },
            { Instruction.JUMP, ExecuteJump }
        };
    }

    private void ExecuteNoop()
    {
        MarkExecuted();
    }

    private void ExecuteSwap()
    {
        if (dataStack.Count >= 2)
        {
            object a = dataStack.Pop();
            object b = dataStack.Pop();
            dataStack.Push(a);
            dataStack.Push(b);
            MarkExecuted();
        }
        else
        {
            MarkIdle("swap must have at least two elements on the stack");
        }
    }

    private void ExecutePop()
    {
        if (dataStack.Count >= 1)
        {
            dataStack.Pop();
            MarkExecuted();
        }
        else
        {
            MarkIdle("pop must have at least one element on the stack");
        }
    }

    private void ExecuteDup()
    {
        if (dataStack.Count >= 1)
        {
            object a = dataStack.Pop();
            dataStack.Push(a);
            dataStack.Push(a);
            MarkExecuted();
        }
        else
        {
            MarkIdle("dup must have at least one element on the stack");
        }
    }

    private void ExecuteVector3ScalarMultiply()
    {
        bool good = false;
        if (dataStack.Count >= 2)
        {
            Vector3? v3 = dataStack.Pop() as Vector3?;
            float? s = dataStack.Pop() as float?;
            if (s != null && v3 != null)
            {
                good = true;
                Vector3 v = s.Value * v3.Value;
                dataStack.Push(v);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("vector3 scalar multiply needs a float and a vector3 on the stack");
        }
    }

    private void ExecuteKillRot()
    {
        bool convergedSpin = ship.KillRotation();
        if (convergedSpin)
        {
            MarkExecuted();
        }
    }

    private void ExecuteCalcRelv()
    {
        bool good = true;
        if (dataStack.Count >= 1)
        {
            GameObject target = dataStack.Pop() as GameObject;
            if (target != null)
            {
                good = true;
                float dist;
                float relv;
                Vector3 relVelUnit;
                PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
                dataStack.Push(dist);
                dataStack.Push(relv);
                dataStack.Push(relVelUnit);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("Calc Relv needs a target nbody gameobject on the stack");
        }
    }

    private void ExecuteRotToTarget()
    {
        bool good = true;
        if (dataStack.Count >= 1)
        {
            GameObject target = dataStack.Pop() as GameObject;
            if (target != null)
            {
                good = true;
                Vector3 b = (target.transform.position - transform.parent.transform.position).normalized;
                bool converged = RotToUnitVec(b);
                if (converged)
                {
                    MarkExecuted();
                }
                else
                {
                    dataStack.Push(target);
                }
            }
        }
        if (!good)
        {
            MarkIdle("Rotate to target needs a gameobject on the stack");
        }
    }

    private void ExecuteRotToUnitVec()
    {
        bool good = false;
        if (dataStack.Count >= 1)
        {
            Vector3? b = dataStack.Pop() as Vector3?;
            if (b != null)
            {
                good = true;
                bool converged = RotToUnitVec(b.Value);
                if (converged)
                {
                    MarkExecuted();
                }
                else
                {
                    dataStack.Push(b);
                }
            }
        }
        if (!good)
        {
            MarkIdle("Rotate to unit vec needs a unit vector3 on the stack");
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

    // ( target dist relv relVelUnit -- accVector3 )
    private void ExecuteCalcAPNG()
    {
        bool good = false;
        if (dataStack.Count >= 4)
        {
            Vector3? relVelUnit = dataStack.Pop() as Vector3?;
            float? relv = dataStack.Pop() as float?;
            float? dist = dataStack.Pop() as float?;
            GameObject target = dataStack.Pop() as GameObject;
            if (relVelUnit != null && relv != null && dist != null && target != null)
            {
                float N = NavigationalConstant;
                Vector3 vr = relv.Value * -relVelUnit.Value;
                Vector3 r = target.transform.position - transform.position;
                Vector3 o = Vector3.Cross(r, vr) / Vector3.Dot(r, r);
                Vector3 a = Vector3.Cross(-N * Mathf.Abs(vr.magnitude) * r.normalized, o);
                dataStack.Push(a);
                good = true;
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("calc APNG needs target, dist, relv, relVUnit on the stack");
        }
    }

    private float minRCSBurnTimeSec = 0.1f;
    private float minMainBurnTimeSec = 0.5f;
    private float burnTimerAPNG = -1;
    private bool isRCSGo = false;

    private void ExecuteBurnAPNG()
    {
        bool good = false;
        if (dataStack.Count >= 1)
        {
            Vector3? aP = dataStack.Pop() as Vector3?;
            if (aP != null)
            {
                good = true;
                Vector3 a = aP.Value;
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
                    MarkExecuted();
                }
            }
        }
        if (!good)
        {
            MarkIdle("APNG burn needs an acceleration Vector3 on the stack");
            return;
        }
    }

    private void ExecuteBurnGo()
    {
        if (!ship.IsMainEngineGo())
        {
            ship.MainEngineGo();
        }
        MarkExecuted();
    }

    private void ExecuteBurnStop()
    {
        if (ship.IsMainEngineGo())
        {
            ship.MainEngineCutoff();
        }
        MarkExecuted();
    }

    private float burnTimer = -1;

    private void ExecuteBurnSec()
    {
        if (burnTimer == -1)
        {
            bool good = false;
            if (dataStack.Count >= 1)
            {
                float? sec = dataStack.Pop() as float?;
                if (sec != null)
                {
                    good = true;
                    ship.MainEngineBurst(sec.Value);
                    burnTimer = sec.Value;
                }
            }
            if (!good)
            {
                MarkIdle("Execute Burn Sec must have time in seconds on the stack");
            }
        }
        else if (burnTimer > 0)
        {
            burnTimer -= Time.deltaTime;
        }
        else
        {
            burnTimer = -1;
            MarkExecuted();
        }
    }

    private void ExecuteBurnToRelV()
    {
        bool good = false;
        if (dataStack.Count >= 2)
        {
            float? desiredDeltaVP = dataStack.Pop() as float?;
            GameObject target = dataStack.Pop() as GameObject;
            if (desiredDeltaVP != null && target != null)
            {
                good = true;
                float desiredDeltaV = desiredDeltaVP.Value;
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
                    MarkExecuted();
                }
                else
                {
                    dataStack.Push(target);
                    dataStack.Push(desiredDeltaVP);
                }
            }
        }
        if (!good)
        {
            MarkIdle("burn to relV must have a target on the stack");
        }
    }

    private void ExecuteJump()
    {
        bool good = false;
        if (dataStack.Count >= 1)
        {
            int? newPC = dataStack.Pop() as int?;
            if (newPC != null)
            {
                good = true;
                programCounter = newPC.Value;
            }
        }
        if (!good)
        {
            MarkIdle("jump parameter invalid");
        }
    }

    private void MarkExecuted()
    {
        programCounter++;
    }

    private void MarkIdle(string error)
    {
        errorStack.Push(error);
        commandState = CommandState.IDLE;
    }

}
