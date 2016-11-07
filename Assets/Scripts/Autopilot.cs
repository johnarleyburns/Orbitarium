using UnityEngine;
using System.Collections;
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
    public float MinMainEngineBurnSec = 0.5f;
    public float RendevousDistToVFactor = 0.01f;
    public float RendevousMarginVPct = 0.5f;

    private RocketShip ship;
    private float burnTimer = -1f;
    private bool cmgActive = false;
    private bool autopilotRunning = false;

    Stack<IEnumerator> callStack = new Stack<IEnumerator>();

    void Start()
    {
        ship = GetComponent<RocketShip>();
    }

    void Update()
    {

    }

    public void KillRot()
    {
        AutopilotOff();
        autopilotRunning = true;
        ship.MainEngineCutoff();
        PushAndStartCoroutine(KillRotCo());
    }

    public void AutopilotOff()
    {
        StopAll();
        ship.MainEngineCutoff();
        autopilotRunning = false;
        cmgActive = false;
    }

    public bool IsRot()
    {
        return cmgActive;
    }

    public bool IsKillRot()
    {
        return cmgActive;
    }

    public bool IsAutoRot()
    {
        return cmgActive;
    }

    public void AutoRot(GameObject target)
    {
        AutopilotOff();
        autopilotRunning = true;
        ship.MainEngineCutoff();
        Vector3 targetVec = target.transform.position - transform.parent.transform.position;
        Vector3 normalTargetVec = targetVec.normalized;
        PushAndStartCoroutine(RotToUnitVec(normalTargetVec));
    }

    public void KillRelV(GameObject target)
    {
        ship.MainEngineCutoff();
        float dist;
        float relv;
        Vector3 relVelUnit;
        PhysicsUtils.CalcRelV(transform.parent.transform, target, out dist, out relv, out relVelUnit);
        Vector3 negVec = -1 * relVelUnit;
        float sec = CalcDeltaVBurnSec(relv);
        if (sec >= MinMainEngineBurnSec)
        {
            PushAndStartCoroutine(RotThenBurn(negVec, sec));
        }
    }

    public void APNGToTarget(GameObject target)
    { }

    public void Rendezvous(GameObject target)
    {

    }

    #region CoroutineHandlers

    Coroutine PushAndStartCoroutine(IEnumerator routine)
    {
        callStack.Push(routine);
        return StartCoroutine(routine);
    }

    void PopAndStop()
    {
        IEnumerator active = callStack.Pop();
        StopCoroutine(active);
    }

    void StopAll()
    {
        while (callStack.Count > 0)
        {
            PopAndStop();
        }
    }

    void PopCoroutine()
    {
        callStack.Pop();
    }

    #endregion

    #region Coroutines

    IEnumerator KillRotCo()
    {
        for (;;)
        {
            bool convergedSpin = ship.KillRotation();
            if (convergedSpin)
            {
                cmgActive = false;
                PopCoroutine();
                yield break;
            }
            else
            {
                cmgActive = true;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator RotToUnitVec(Vector3 b)
    {
        for (;;)
        {
            Quaternion q = Quaternion.LookRotation(b);
            q = Quaternion.Euler(q.eulerAngles.x, q.eulerAngles.y, transform.rotation.eulerAngles.z);
            bool converged = ship.ConvergeSpin(q, MinRotToTargetDeltaTheta);
            if (converged)
            {
                cmgActive = false;
                PopCoroutine();
                yield break;
            }
            else
            {
                cmgActive = true;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator RotThenBurn(Vector3 b, float sec)
    {
        yield return PushAndStartCoroutine(RotToUnitVec(b));
        yield return PushAndStartCoroutine(MainEngineBurnSec(sec));
    }

    IEnumerator MainEngineBurnSec(float sec)
    {
        float burnTimer = sec;
        if (burnTimer > 0)
        {
            ship.MainEngineGo();
        }
        for (;;)
        {
            if (burnTimer <= 0)
            {
                ship.MainEngineCutoff();
                PopCoroutine();
                yield break;
            }
            burnTimer -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }

    #endregion

    #region Utility Functions

    private float CalcDeltaVBurnSec(float deltaV)
    {
        float mainEngineA = ship.CurrentMainEngineAccPerSec();
        float sec = deltaV / mainEngineA;
        sec = sec > 0 ? sec : 0;
        return sec;
    }

    #endregion

    /*






        void Update()
        {
            if (gameController != null && gameController.GetGameState() == GameController.GameState.RUNNING)
            {
                switch (commandState)
                {
                    case CommandState.IDLE:
                        break;
                    case CommandState.EXECUTING:
                        if (currentFunction != null)
                        {
                            currentFunction();
                        }
                        break;
                }
            }

        }

        private void ClearProgram()
        {
            commandState = CommandState.IDLE;
            currentFunction = null;
        }

        public void AutopilotOff()
        {
            ClearProgram();
        }

        public bool IsRot()
        {
            return currentFunction == ExecuteKillRot || currentFunction == ExecuteRotToUnitVec;
        }

        public bool IsKillRot()
        {
            return currentFunction == ExecuteKillRot;
        }

        public bool IsAutoRot()
        {
            return currentFunction == ExecuteRotToUnitVec;
        }

        public void LoadCodeLibrary()
        {
            codeLibrary = new Dictionary<string, List<object>>()
            {
                {
                    "KillRot", // ( -- )
                    new List<object>()
                    {
                    }
                },
                {
                    "AutoRot", // ( target -- )
                    new List<object>()
                    {
                        Instruction.BURN_STOP,
                        Instruction.CALC_TARGET_VEC,
                        Instruction.VECTOR3_NORMALIZE,
                        Instruction.ROT_TO_UNITVEC
                    }

                },
                {
                    "KillRelV", // ( target -- )
                    new List<object>()
                    {
                        Instruction.BURN_STOP,
                        Instruction.CALC_RELV, // ( target dist relv relvec )
                        Instruction.ROT, // ( a b c -- b c a ) // ( target dist relv relvec -- target relv relvec dist ) 
                        Instruction.DROP, // ( relv relvec )
                        -1.0f, // ( relv relvec -1.0f )
                        Instruction.SWAP, // ( relv -1.0f relvec )
                        Instruction.VECTOR3_SCALAR_MULTIPLY, // ( relv -relvec )
                        Instruction.ROT_TO_UNITVEC, // ( relv )
                        Instruction.CALC_DELTA_V_BURN_SEC, // ( sec )
                        Instruction.BURN_SEC
                    }
                },
                {
                    "KillRelVTo", // ( target relvDesired -- )
                    new List<object>()
                    {
                        Instruction.SWAP, // ( relvDes target )
                        Instruction.BURN_STOP, // ( relvDes target)
                        Instruction.CALC_RELV, // ( relvDes dist relv relvec )
                        Instruction.ROT, // ( a b c -- b c a ) // ( relvDes relv relvec dist ) 
                        Instruction.DROP, // ( relvDes relv relvec )
                        Instruction.MINUSROT, // ( relvec relvDes relv )
                        Instruction.MINUS, // ( relvec deltaV )
                        Instruction.DUP, // ( relvec deltaV deltaV )
                        0f, // ( relvec deltaV deltaV 0f )
                        Instruction.GTE, // ( relvec deltaV deltaVGT0? )
                        18, // clearstack2
                        Instruction.BRANCHIF, // ( relvec deltaV )
                        Instruction.DUP, // ( relvec deltaV deltaV )
                        Instruction.ABS, // ( relvec deltaV deltaVMag )
                        Instruction.CALC_DELTA_V_BURN_SEC, // ( relvec deltaV deltaVAbsSec )
                        Instruction.DUP, // ( relvec deltaV deltaVAbsSec deltaVAbsSec)
                        0.5f, // ( relvec deltaV deltaVAbsSec deltaVAbsSec 0.5f)
                        Instruction.LTE,  // ( relvec deltaV deltaVAbsSec smallEnough? )
                        9, // clearstack
                        Instruction.BRANCHIF, // ( relvec deltaV deltaVAbsSec )
                        Instruction.SWAP, // ( relvec deltaVAbsSec deltaV )                    
                        Instruction.SIGN, // ( relvec deltaVAbsSec deltaVSign )
                        Instruction.ROT, // ( deltaVAbsSec deltaVSign relvec )
                        Instruction.VECTOR3_SCALAR_MULTIPLY, // ( deltaVAbsSec relVecDir )
                        Instruction.ROT_TO_UNITVEC, // ( deltaVAbsSec )
                        Instruction.BURN_SEC, // ( )
                        4,
                        Instruction.JUMP,
                        Instruction.DROP, // clearstack
                        Instruction.DROP, // clearstack2
                        Instruction.DROP,
                        Instruction.NOOP
                    }
                },
                {
                    "APNGToTarget", // ( target minInterceptV interceptDist -- )
                    new List<object>()
                    {
                        "interceptDist",
                        Instruction.STORE,
                        "minInterceptV",
                        Instruction.STORE,
                        "target",
                        Instruction.STORE,
                        // ( )
                        Instruction.BURN_STOP,

                        // CALCRELV
                        "target",
                        Instruction.LOAD,
                        // ( target )
                        Instruction.CALC_RELV,
                        // ( dist relv relvec )

                        // check if we're close enough to quit APNG
                        Instruction.ROT,
                        // ( relv relvec dist )
                        Instruction.DUP,
                        // ( relv relvec dist dist )
                        "interceptDist",
                        Instruction.LOAD,
                        // ( relv relvec dist dist interceptDist )
                        Instruction.LTE,
                        54, // DONE
                        Instruction.BRANCHIF,
                        // ( relv relvec dist )

                        // turn around?
                        // ( relv relvec dist )
                        Instruction.MINUSROT,
                        // ( dist relv relvec )
                        Instruction.OVER,
                        // ( dist relv relvec relv )
                        0f,
                        Instruction.GTE,
                        9, // goto SPEEDCHECK
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
                        Instruction.OVER,
                        // ( dist relv relvec relv )
                        "minInterceptV",
                        Instruction.LOAD,
                        // ( dist relv relvec relv INTCPdV )
                        Instruction.GTE,
                        23, // APNG
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

                        Instruction.DUP,
                        // ( dist relv relvec relv relv )
                        0f,
                        Instruction.GTE,
                        // ( dist relv relvec relv -relv? )
                        1,
                        Instruction.BRANCHIF,
                        // ( dist relv relvec relv  )
                        Instruction.DROP,
                        Instruction.DROP,
                        Instruction.DROP,
                        Instruction.DROP,
                        -46,
                        Instruction.JUMP,

                        InterceptDeltaV,
                        Instruction.SWAP,
                        Instruction.MINUS,
                        // ( dist relv relvec deltaV )
                        Instruction.CALC_DELTA_V_BURN_SEC,
                        Instruction.BURN_SEC,

                        // APNG
                        // ( dist relv relvec )
                        "target",
                        Instruction.LOAD,
                        Instruction.CALC_TARGET_VEC,
                        // ( dist relv relvec tgtvec )
                        Instruction.CALC_APNG, // ( a )
                        Instruction.DUP, // ( a a )
                        Instruction.ROT_TO_UNITVEC, // ( a )
                        Instruction.VECTOR3_MAGNITUDE, // ( |a| )
                        Instruction.CALC_DELTA_V_BURN_SEC, // ( sec )
                        Instruction.BURN_SEC, // ( )

                        // GO BACK TO CALCRELV
                        -62,
                        Instruction.JUMP,

                        // DONE
                        // ( relv relvec dist )
                        Instruction.DROP,
                        Instruction.DROP,
                        Instruction.DROP,
                        Instruction.NOOP
                    }
                },
                {
                    "Rendezvous", // ( target intcV intcD )
                    new List<object>()
                    {
                        // initial variables
                        "intcD",
                        Instruction.STORE,
                        "intcV",
                        Instruction.STORE,
                        "target",
                        Instruction.STORE,

                        // APNG to target
                        "target",
                        Instruction.LOAD,
                        "intcV",
                        Instruction.LOAD,
                        "intcD",
                        Instruction.LOAD,
                        "APNGToTarget",
                        Instruction.CALL,

                        // slow if necessary
                        "target",
                        Instruction.LOAD,
                        "intcV",
                        Instruction.LOAD,
                        "KillRelVTo",
                        Instruction.CALL,

                        // divide V and D by half
                        "intcV",
                        Instruction.LOAD,
                        2f,
                        Instruction.DIVIDE,
                        "intcV",
                        Instruction.STORE,
                        "intcD",
                        Instruction.LOAD,
                        2f,
                        Instruction.DIVIDE,
                        "intcD",
                        Instruction.STORE,

                        // loop if continuing
                        "intcD",
                        Instruction.LOAD,
                        50f,
                        Instruction.GTE,
                        -31, // loop
                        Instruction.BRANCHIF,

                        // done
                        "target",
                        Instruction.LOAD,
                        "AutoRot",
                        Instruction.CALL

                    }
                }
            };
        }

        public void AutoRot(GameObject target)
        {
            LoadProgram(
                target,
                "AutoRot",
                Instruction.CALL
            );
        }

        public void KillRelV(GameObject target)
        {
            LoadProgram(
                target,
                0f,
                "KillRelVTo",
                Instruction.CALL
            );
        }

        public void Rendezvous(GameObject target)
        {
            LoadProgram(
                target,
                50f,
                10000f,
                "Rendezvous",
                Instruction.CALL
            );
        }

        public void APNGToTarget(GameObject target)
        {
            LoadProgram(
                target,
                MinInterceptDeltaV,
                0f,
                "APNGToTarget",
                Instruction.CALL
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
                { Instruction.MULTIPLY, ExecuteMultiply },
                { Instruction.DIVIDE, ExecuteDivide },
                { Instruction.SIGN, ExecuteSign },
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
                { Instruction.LTE, ExecuteLessThanEqualTo },
                { Instruction.GTE, ExecuteGreaterThanEqualTo },
                { Instruction.CALL, ExecuteCall }
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
                dataStack.Pop();
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

        // ( a b -- c )
        private void ExecuteMultiply()
        {
            bool good = false;
            if (dataStack.Count >= 2)
            {
                float? b = dataStack.Pop() as float?;
                float? a = dataStack.Pop() as float?;
                if (a != null && b != null)
                {
                    good = true;
                    float c = a.Value * b.Value;
                    dataStack.Push(c);
                    MarkExecuted();
                }
            }
            if (!good)
            {
                MarkIdle("multiply needs two floats on the stack");
            }
        }

        // ( a b -- c )
        private void ExecuteDivide()
        {
            bool good = false;
            if (dataStack.Count >= 2)
            {
                float? b = dataStack.Pop() as float?;
                float? a = dataStack.Pop() as float?;
                if (a != null && b != null)
                {
                    good = true;
                    float c = a.Value / b.Value;
                    dataStack.Push(c);
                    MarkExecuted();
                }
            }
            if (!good)
            {
                MarkIdle("divide needs two floats on the stack");
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

        // ( a -- a )
        private void ExecuteSign()
        {
            bool good = false;
            if (dataStack.Count >= 1)
            {
                float? a = dataStack.Pop() as float?;
                if (a != null)
                {
                    good = true;
                    float c = Mathf.Sign(a.Value);
                    dataStack.Push(c);
                    MarkExecuted();
                }
            }
            if (!good)
            {
                MarkIdle("Sign needs one float on the stack");
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
                        float s = sec.Value;
                        if (s >= MinMainEngineBurnSec)
                        {
                            ship.MainEngineBurst(s);
                            burnTimer = s;
                        }
                        else
                        {
                            burnTimer = -1;
                            MarkExecuted();
                        }
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
                    sec = sec > 0 ? sec : 0;
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
        private void ExecuteLessThanEqualTo()
        {
            bool good = false;
            if (dataStack.Count >= 2)
            {
                float? b = dataStack.Pop() as float?;
                float? a = dataStack.Pop() as float?;
                if (a != null && b != null)
                {
                    good = true;
                    bool c = a.Value <= b.Value;
                    dataStack.Push(c);
                    MarkExecuted();
                }
            }
            if (!good)
            {
                MarkIdle("LT requires two floats on the stack");
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
                int? offset = dataStack.Pop() as int?;
                if (offset != null)
                {
                    good = true;
                    currentContext.programCounter += offset.Value;
                }
            }
            if (!good)
            {
                MarkIdle("jump requires one integer address offset on the stack");
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
                        currentContext.programCounter += b.Value;
                    }
                    else
                    {
                        currentContext.programCounter++;
                    }
                }
            }
            if (!good)
            {
                MarkIdle("BranchIf requires one bool and one integer offset address on the stack");
            }
        }

        // ( a -- )
        private void ExecuteCall()
        {
            bool good = false;
            if (dataStack.Count >= 1)
            {
                string a = dataStack.Pop() as string;
                if (a != null && codeLibrary.ContainsKey(a))
                {
                    good = true;
                    List<object> code = codeLibrary[a];
                    callStack.Push(currentContext);
                    currentContext = new ProgramContext();
                    currentContext.program = code;
                    currentContext.programCounter = 0;
                }
            }
            if (!good)
            {
                MarkIdle("Call requires one string library name on the stack");
            }

        }

        #endregion

        private void MarkExecuted()
        {
            currentContext.programCounter++;
        }

        private void MarkIdle(string error)
        {
            errorStack.Push(error);
            commandState = CommandState.IDLE;
        }
        */
}
