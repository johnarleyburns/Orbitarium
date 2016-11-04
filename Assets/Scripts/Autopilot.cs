using UnityEngine;
using System.Collections.Generic;

public class Autopilot : MonoBehaviour
{

    public GameController gameController;
    public float MaxRCSAutoBurnSec = 10;
    public float MinDeltaVforBurn = 0.1f;
    public float MinInterceptDeltaV = 20;
    public float InterceptDeltaV = 30;
    public float NavigationalConstant = 3;
    public float MinRotToTargetDeltaTheta = 0.2f;
    public float MinAPNGDeltaTheta = 1f;
    public bool APNG = true;

    private RocketShip ship;
    private float burnTimer = -1f;
    private Stack<object> dataStack = new Stack<object>();
    private Dictionary<string, object> dataVariables = new Dictionary<string, object>();
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

        DUP,
        DROP,
        SWAP,
        OVER,
        ROT,
        MINUSROT,
        NIP,
        TUCK,

        STORE,
        LOAD,

        ADD,
        MINUS,
        ABS,

        VECTOR3_MAGNITUDE,
        VECTOR3_NORMALIZE,
        VECTOR3_SCALAR_MULTIPLY,

        CALC_RELV,
        CALC_APNG,
        CALC_TARGET_VEC,
        CALC_DELTA_V_BURN_SEC,

        KILL_ROT,
        ROT_TO_UNITVEC,
        BURN_GO,
        BURN_STOP,
        BURN_SEC,

        EQ,
        GTE,
        BRANCHIF,
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
        dataVariables.Clear();
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
        return inst == Instruction.ROT_TO_UNITVEC;
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
            Instruction.CALC_TARGET_VEC,
            Instruction.VECTOR3_NORMALIZE,
            Instruction.ROT_TO_UNITVEC
        );
    }

    public void KillRelV(GameObject target)
    {
        LoadProgram(
            target,
            Instruction.BURN_STOP,
            Instruction.CALC_RELV, // ( relv relvec )
            Instruction.ROT, // ( a b c -- b c a ) // ( target dist relv relvec -- target relv relvec dist ) 
            Instruction.DROP, // ( relv relvec )
            -1.0f, // ( relv relvec -1.0f )
            Instruction.SWAP, // ( relv -1.0f relvec )
            Instruction.VECTOR3_SCALAR_MULTIPLY, // ( relv -relvec )
            Instruction.ROT_TO_UNITVEC, // ( relv )
            Instruction.CALC_DELTA_V_BURN_SEC, // ( sec )
            Instruction.BURN_SEC
        );
    }

    public void Rendezvous(GameObject target)
    {
        LoadProgram(
            target,
            Instruction.DUP,
            Instruction.BURN_STOP,
            Instruction.CALC_RELV, // ( dist relv relvec )
            Instruction.ROT, // ( a b c -- b c a ) // ( target dist relv relvec -- target relv relvec dist ) 
            Instruction.DROP, // ( relv relvec )
            -1.0f, // ( relv relvec -1.0f )
            Instruction.SWAP, // ( relv -1.0f relvec )
            Instruction.VECTOR3_SCALAR_MULTIPLY, // ( relv -relvec )
            Instruction.ROT_TO_UNITVEC, // ( relv )
            Instruction.CALC_DELTA_V_BURN_SEC, // ( sec )
            Instruction.BURN_SEC,
            Instruction.CALC_TARGET_VEC,
            Instruction.VECTOR3_NORMALIZE,
            Instruction.ROT_TO_UNITVEC
        );
    }

    public void APNGToTarget(GameObject target)
    {
        LoadProgram(
            target,
            "target",
            Instruction.STORE,
            // ( )
            Instruction.BURN_STOP,
            "target", // ADDR 4
            Instruction.LOAD,
            // ( target )

            // find relv
            Instruction.CALC_RELV,
            // ( dist relv relvec )

            // turn around?
            // ( dist relv relvec )
            Instruction.OVER,
            // ( dist relv relvec relv )
            0f,
            Instruction.GTE,
            20, // addr SPEEDCHECK
            Instruction.BRANCHIF, // brach if relv >= 0

            // STOP
            // we need to stop first, relv is neg
            // ( dist relv relvec )
            -1f,
            // ( dist relv relvec -1f )
            Instruction.OVER,
            // ( dist relv relvec -1f relvec )
            Instruction.VECTOR3_SCALAR_MULTIPLY,
            // ( dist relv relvec -relvec )
            Instruction.ROT_TO_UNITVEC,
            // ( dist relv relvec )
            Instruction.OVER,
            // ( dist relv relvec relv )
            Instruction.ABS,
            // ( dist relv relvec |relv| )
            Instruction.CALC_DELTA_V_BURN_SEC,
            Instruction.BURN_SEC,
            // ( dist relv relvec )

            // SPEEDCHECK
            // moving towards target, are we fast enough?
            // ( dist relv relvec )
            Instruction.OVER, // ADDR 20
            // ( dist relv relvec relv )
            MinInterceptDeltaV,
            // ( dist relv relvec relv INTCPdV )
            Instruction.GTE,
            36, // APNG
            Instruction.BRANCHIF,

            // SPEEDUP
            // ( dist relv relvec )
            "target",
            Instruction.LOAD,
            Instruction.CALC_TARGET_VEC,
            Instruction.VECTOR3_NORMALIZE,
            Instruction.ROT_TO_UNITVEC,
            // ( dist relv relvec )
            Instruction.OVER,
            // ( dist relv relvec relv )
            InterceptDeltaV,
            Instruction.SWAP,
            Instruction.MINUS,
            // ( dist relv relvec deltaV )
            Instruction.CALC_DELTA_V_BURN_SEC,
            Instruction.BURN_SEC,

            // APNG
            // ( dist relv relvec )
            "target", // ADDR 36
            Instruction.LOAD,
            Instruction.CALC_TARGET_VEC,
            // ( dist relv relvec tgtvec )
            Instruction.CALC_APNG, // ( a )
            Instruction.DUP, // ( a a )
            Instruction.ROT_TO_UNITVEC, // ( a )
            Instruction.VECTOR3_MAGNITUDE, // ( |a| )
            Instruction.CALC_DELTA_V_BURN_SEC, // ( sec )
            Instruction.BURN_SEC, // ( )

            // GO BACK TO BEGINNING
            4,
            Instruction.JUMP
        );
    }

    private void AddAutopilotDefinitions()
    {
        autopilotInstructions = new Dictionary<Instruction, AutopilotFunction>()
        {
            { Instruction.NOOP, ExecuteNoop },
            { Instruction.DUP, ExecuteDup },
            { Instruction.DROP, ExecuteDrop },
            { Instruction.SWAP, ExecuteSwap },
            { Instruction.OVER, ExecuteOver },
            { Instruction.ROT, ExecuteRot },
            { Instruction.MINUSROT, ExecuteMinusRot },
            { Instruction.NIP, ExecuteNip },
            { Instruction.TUCK, ExecuteTuck },

            { Instruction.STORE, ExecuteStore },
            { Instruction.LOAD, ExecuteLoad },

            { Instruction.ADD, ExecuteAdd },
            { Instruction.MINUS, ExecuteMinus },
            { Instruction.ABS, ExecuteAbs },

            { Instruction.VECTOR3_MAGNITUDE, ExecuteVector3Magnitude },
            { Instruction.VECTOR3_NORMALIZE, ExecuteVector3Normalize },
            { Instruction.VECTOR3_SCALAR_MULTIPLY, ExecuteVector3ScalarMultiply },

            { Instruction.CALC_RELV, ExecuteCalcRelv },
            { Instruction.CALC_APNG, ExecuteCalcAPNG },
            { Instruction.CALC_TARGET_VEC, ExecuteCalcTargetVec },
            { Instruction.CALC_DELTA_V_BURN_SEC, ExecuteCalcDeltaVBurnSec },

            { Instruction.KILL_ROT, ExecuteKillRot },
            { Instruction.ROT_TO_UNITVEC, ExecuteRotToUnitVec },
            { Instruction.BURN_GO, ExecuteBurnGo },
            { Instruction.BURN_STOP, ExecuteBurnStop },
            { Instruction.BURN_SEC, ExecuteBurnSec },

            { Instruction.JUMP, ExecuteJump },
            { Instruction.BRANCHIF, ExecuteBranchIf },
            { Instruction.EQ, ExecuteEqualTo },
            { Instruction.GTE, ExecuteGreaterThanEqualTo }
        };
    }


    private void ExecuteNoop()
    {
        MarkExecuted();
    }

    #region stack operations

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

    private void ExecuteDrop()
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

    // ( a b -- a b a )
    private void ExecuteOver()
    {
        if (dataStack.Count >= 2)
        {
            object b = dataStack.Pop();
            object a = dataStack.Pop();
            dataStack.Push(a);
            dataStack.Push(b);
            dataStack.Push(a);
            MarkExecuted();
        }
        else
        {
            MarkIdle("over must have at least two elements on the stack");
        }
    }

    // ( a b c -- b c a ) // rot left
    private void ExecuteRot()
    {
        if (dataStack.Count >= 3)
        {
            object c = dataStack.Pop();
            object b = dataStack.Pop();
            object a = dataStack.Pop();
            dataStack.Push(b);
            dataStack.Push(c);
            dataStack.Push(a);
            MarkExecuted();
        }
        else
        {
            MarkIdle("rot must have at least three elements on the stack");
        }
    }

    // ( a b c -- c a b ) // rot right
    private void ExecuteMinusRot()
    {
        if (dataStack.Count >= 3)
        {
            object c = dataStack.Pop();
            object b = dataStack.Pop();
            object a = dataStack.Pop();
            dataStack.Push(c);
            dataStack.Push(a);
            dataStack.Push(b);
            MarkExecuted();
        }
        else
        {
            MarkIdle("-rot must have at least three elements on the stack");
        }
    }

    // ( a b -- b )
    private void ExecuteNip()
    {
        if (dataStack.Count >= 2)
        {
            object b = dataStack.Pop();
            object a = dataStack.Pop();
            dataStack.Push(b);
            MarkExecuted();
        }
        else
        {
            MarkIdle("nip must have at least two elements on the stack");
        }
    }

    // ( a b -- b a b )
    private void ExecuteTuck()
    {
        if (dataStack.Count >= 2)
        {
            object b = dataStack.Pop();
            object a = dataStack.Pop();
            dataStack.Push(b);
            dataStack.Push(a);
            dataStack.Push(b);
            MarkExecuted();
        }
        else
        {
            MarkIdle("tuck must have at least two elements on the stack");
        }
    }

    #endregion

    #region Variable Operations

    // ( a b -- )
    private void ExecuteStore()
    {
        bool good = false;
        if (dataStack.Count >= 2)
        {
            string b = dataStack.Pop() as string;
            object a = dataStack.Pop() as object;
            if (b != null)
            {
                good = true;
                dataVariables[b] = a;
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("Store needs a variable name string and a value object on the stack");
        }
    }

    // ( a -- b )
    private void ExecuteLoad()
    {
        bool good = false;
        if (dataStack.Count >= 1)
        {
            string a = dataStack.Pop() as string;
            if (a != null && dataVariables.ContainsKey(a))
            {
                good = true;
                object b = dataVariables[a];
                dataStack.Push(b);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("Load needs a variable name string currently in the variable dictionary on the stack");
        }
    }

    #endregion

    #region Arithmetic Operations

    // ( a b -- c )
    private void ExecuteAdd()
    {
        bool good = false;
        if (dataStack.Count >= 2)
        {
            float? b = dataStack.Pop() as float?;
            float? a = dataStack.Pop() as float?;
            if (a != null && b != null)
            {
                good = true;
                float c = a.Value + b.Value;
                dataStack.Push(c);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("add needs two floats on the stack");
        }
    }

    // ( a b -- c )
    private void ExecuteMinus()
    {
        bool good = false;
        if (dataStack.Count >= 2)
        {
            float? b = dataStack.Pop() as float?;
            float? a = dataStack.Pop() as float?;
            if (a != null && b != null)
            {
                good = true;
                float c = a.Value - b.Value;
                dataStack.Push(c);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("minus needs two floats on the stack");
        }
    }

    // ( a -- a )
    private void ExecuteAbs()
    {
        bool good = false;
        if (dataStack.Count >= 1)
        {
            float? a = dataStack.Pop() as float?;
            if (a != null)
            {
                good = true;
                float c = Mathf.Abs(a.Value);
                dataStack.Push(c);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("Abs needs one float on the stack");
        }
    }

    #endregion

    #region vector operations

    // ( a -- |a| )
    private void ExecuteVector3Magnitude()
    {
        bool good = false;
        if (dataStack.Count >= 1)
        {
            Vector3? v3 = dataStack.Pop() as Vector3?;
            if (v3 != null)
            {
                good = true;
                float mag = v3.Value.magnitude;
                dataStack.Push(mag);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("vector3 magnitude needs a vector3 on the stack");
        }
    }

    // ( a -- a.Normalized )
    private void ExecuteVector3Normalize()
    {
        bool good = false;
        if (dataStack.Count >= 1)
        {
            Vector3? v3 = dataStack.Pop() as Vector3?;
            if (v3 != null)
            {
                good = true;
                Vector3 v = v3.Value.normalized;
                dataStack.Push(v);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("vector3 normalize needs a vector3 on the stack");
        }
    }

    // ( a b -- a*b )
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

    #endregion

    #region command operations
    private void ExecuteKillRot()
    {
        bool convergedSpin = ship.KillRotation();
        if (convergedSpin)
        {
            MarkExecuted();
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
                Quaternion q = Quaternion.LookRotation(b.Value);
                q = Quaternion.Euler(q.eulerAngles.x, q.eulerAngles.y, transform.rotation.eulerAngles.z);
                bool converged = ship.ConvergeSpin(q, MinRotToTargetDeltaTheta);
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

    private void ExecuteBurnSec()
    {
        // ( a -- )
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
                good = true;
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

    #endregion


    #region calcation operations

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

    private void ExecuteCalcTargetVec()
    {
        bool good = true;
        if (dataStack.Count >= 1)
        {
            GameObject target = dataStack.Pop() as GameObject;
            if (target != null)
            {
                good = true;
                Vector3 b = target.transform.position - transform.parent.transform.position;
                dataStack.Push(b);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("Calc vec to target needs a gameobject on the stack");
        }
    }

    // ( dist relv relVelUnit tgtVec -- accVec )
    private void ExecuteCalcAPNG()
    {
        bool good = false;
        if (dataStack.Count >= 4)
        {
            Vector3? targetVec = dataStack.Pop() as Vector3?;
            Vector3? relVelUnit = dataStack.Pop() as Vector3?;
            float? relv = dataStack.Pop() as float?;
            float? dist = dataStack.Pop() as float?;
            if (relVelUnit != null && relv != null && dist != null && targetVec != null)
            {
                float N = NavigationalConstant;
                Vector3 vr = relv.Value * -relVelUnit.Value;
                Vector3 r = targetVec.Value;
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

    private void ExecuteCalcDeltaVBurnSec()
    {
        bool good = false;
        if (dataStack.Count >= 1)
        {
            float? deltaVP = dataStack.Pop() as float?;
            if (deltaVP != null)
            {
                good = true;
                //v = a * t; // t  = v / a
                float deltaV = deltaVP.Value;
                float mainEngineA = ship.CurrentMainEngineAccPerSec();
                float sec = deltaV / mainEngineA;
                dataStack.Push(sec);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("Execute Burn Sec must have time in seconds on the stack");
        }
    }

    #endregion

    #region control operators   

    // ( a b -- c )
    private void ExecuteEqualTo()
    {
        bool good = false;
        if (dataStack.Count >= 2)
        {
            float? b = dataStack.Pop() as float?;
            float? a = dataStack.Pop() as float?;
            if (a != null && b != null)
            {
                good = true;
                bool c = a.Value == b.Value;
                dataStack.Push(c);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("EQ requires two floats on the stack");
        }
    }

    // ( a b -- c )
    private void ExecuteGreaterThanEqualTo()
    {
        bool good = false;
        if (dataStack.Count >= 2)
        {
            float? b = dataStack.Pop() as float?;
            float? a = dataStack.Pop() as float?;
            if (a != null && b != null)
            {
                good = true;
                bool c = a.Value >= b.Value;
                dataStack.Push(c);
                MarkExecuted();
            }
        }
        if (!good)
        {
            MarkIdle("GTE requires two floats on the stack");
        }
    }

    // ( a -- )
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
            MarkIdle("jump requires one integer address on the stack");
        }
    }

    // ( a b -- )
    private void ExecuteBranchIf()
    {
        bool good = false;
        if (dataStack.Count >= 2)
        {
            int? b = dataStack.Pop() as int?;
            bool? a = dataStack.Pop() as bool?;
            if (a != null && b != null)
            {
                good = true;
                if (a.Value)
                {
                    programCounter = b.Value;
                }
                else
                {
                    programCounter++;
                }
            }
        }
        if (!good)
        {
            MarkIdle("BranchIf requires one bool and one integer address on the stack");
        }
    }


    #endregion

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
