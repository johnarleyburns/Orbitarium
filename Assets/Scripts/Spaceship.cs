using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Greyman;

public class Spaceship : MonoBehaviour {

    public GameController gameController;

    //! Thrust scale
    public float EmptyMassKg = 10000;
    public float FuelMassKg = 9200;
    public float RCSThrustNewtons = 880; // 220*4 in the SM
    public float EngineThrustNewtons = 27700;
    public float RCSFuelKgPerSec = 0.14f;
    public float EngineFuelKgPerSec = 8.7f;
    public float DumpFuelRateKgPerSec = 100;
    public float RCSRadiusM = 2.5f;
    public float minRelVtoDamage = 1;
    public float minRelVtoExplode = 5;
    public float minDeltaTheta = 0.1f;
    public float minSpinDeltaDegPerSec = 0.1f;
    public float MaxRotationDegPerSec = 45;
    public float RotationTimeDampeningFactor = 1.2f;
    public int healthMax = 3;
    public GameObject ShipExplosion;

    private NBody nbody; 
    private enum RCSMode { Rotate, Translate };
    private RCSMode currentRCSMode;
    private Vector3 currentSpin;
    private bool killingRot;
    private bool autoRotating;
    private GameObject autoRotatingTarget;
    private enum CameraMode { FPS, ThirdParty };
    private CameraMode currentCameraMode;
    private int health;
    private float currentFuelKg;
    private float currentTotalMassKg;
    private float RCSThrustPerSec;
    private float EngineThrustPerSec;
    private float RCSAngularDegPerSec;
    private bool mainEngineOn;
    private bool isInitStatic = false;
    private bool isInitReady = false;
    private bool rotInput = false;

    void Start() {
    }

    private void InitStatic()
    {
        GravityEngine.instance.Clear();
        if (transform.parent == null)
        {
            Debug.LogError("Spaceship script must be applied to a model that is a child of an NBody object.");
            return;
        }
        nbody = transform.parent.GetComponent<NBody>();
        if (nbody == null)
        {
            Debug.LogError("Parent must have an NBody script attached.");
        }
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.parent.transform.position = Vector3.zero;
        transform.parent.transform.rotation = Quaternion.identity;
        health = healthMax;
        mainEngineOn = false;
        currentTotalMassKg = EmptyMassKg + FuelMassKg;
        currentFuelKg = FuelMassKg;
        currentRCSMode = RCSMode.Rotate;
        currentSpin = Vector3.zero;
        killingRot = false;
        autoRotating = false;
        autoRotatingTarget = null;
        currentCameraMode = CameraMode.FPS;
        GravityEngine.instance.Setup();
    }

    private void InitReady()
    {
        if (!isInitStatic)
        {
            InitStatic();
            isInitStatic = true;
        }
        GravityEngine.instance.SetEvolve(true);
        UpdateRCSMode();
        UpdateCameraMode();
        UpdateFuel();
        UpdateEngine();
    }

    // Update is called once per frame
    void Update()
    {
        switch (gameController.GetGameState())
        {
            case GameController.GameState.SPLASH:
                if (!isInitStatic)
                {
                    InitStatic();
                    isInitStatic = true;
                }
                break;
            case GameController.GameState.RUNNING:
                if (!isInitReady)
                {
                    InitReady();
                    isInitReady = true;
                }
                UpdateCameraInput();
                UpdateRCSInput();
                UpdateEngineInput();
                ApplyCurrentRotation();
                UpdateDumpFuel();
                UpdateHUD();
                break;
            case GameController.GameState.OVER:
                if (isInitStatic || isInitReady)
                {
                    PrepareDestroy();
                }
                break;
        }
    }

    public void PrepareDestroy()
    {
        if (gameController.FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE))
        {
            gameController.FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
        }
        if (gameController.FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCS))
        {
            gameController.FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
        }
        if (gameController.FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG))
        {
            gameController.FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG);
        }
        isInitStatic = false;
        isInitReady = false;
    }

    public void SetRCSModeRotate()
    {
        currentRCSMode = RCSMode.Rotate;
        UpdateRCSMode();
    }

    public void SetRCSModeTranslate()
    {
        currentRCSMode = RCSMode.Translate;
        UpdateRCSMode();
    }

    public void DumpFuelPressed()
    {
        gameController.GetComponent<InputController>().DumpFuelButton.isToggled = !gameController.GetComponent<InputController>().DumpFuelButton.isToggled;
    }

    public void UpdateDumpFuel()
    {
        if (gameController.GetComponent<InputController>().DumpFuelButton.isToggled)
        {
            ApplyFuel(-DumpFuelRateKgPerSec * Time.deltaTime);
        }
    }

    void UpdateRCSMode()
    {
        switch (currentRCSMode)
        {
            case RCSMode.Rotate:
                gameController.GetComponent<InputController>().RotateButton.isToggled = true;
                gameController.GetComponent<InputController>().TranslateButton.isToggled = false;
                if (gameController.FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCS))
                {
                    gameController.FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
                }
                break;
            case RCSMode.Translate:
            default:
                gameController.GetComponent<InputController>().RotateButton.isToggled = false;
                gameController.GetComponent<InputController>().TranslateButton.isToggled = true;
                break;
        }
    }

    void ToggleRCSMode()
    {
        switch (currentRCSMode)
        {
            case RCSMode.Rotate:
                currentRCSMode = RCSMode.Translate;
                break;
            case RCSMode.Translate:
            default:
                currentRCSMode = RCSMode.Rotate;
                break;
        }
        UpdateRCSMode();
    }

    void ToggleCamera()
    {
        switch (currentCameraMode)
        {
            default:
            case CameraMode.FPS:
                currentCameraMode = CameraMode.ThirdParty;
                break;
            case CameraMode.ThirdParty:
                currentCameraMode = CameraMode.FPS;
                break;
        }
        UpdateCameraMode();
    }

    void UpdateCameraMode()
    {
        switch (currentCameraMode)
        {
            default:
            case CameraMode.FPS:
                gameController.FPSCamera.enabled = true;
                gameController.OverShoulderCamera.enabled = false;
                break;
            case CameraMode.ThirdParty:
                gameController.FPSCamera.enabled = false;
                gameController.OverShoulderCamera.enabled = true;
                break;
        }
    }

    public void UpdateInputGoThrust()
    {
        mainEngineOn = true;
    }

    public void UpdateInputStopThrust()
    {
        mainEngineOn = false;
    }

    private void UpdateEngine()
    {
        bool stillRunning;
        if (mainEngineOn)
        {
            if (currentFuelKg > EngineFuelKgPerSec * Time.deltaTime)
            {
                ApplyImpulse(transform.forward, EngineThrustPerSec * Time.deltaTime);
                ApplyFuel(-EngineFuelKgPerSec * Time.deltaTime);
                gameController.FPSCamera.GetComponent<FPSCameraController>().StartContinuousShake();
                stillRunning = true;
            }
            else
            {
                stillRunning = false;
            }
        }
        else
        {
            stillRunning = false;
        }
        if (stillRunning)
        {
            if (!gameController.FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE))
            {
                gameController.FPSCamera.GetComponent<FPSAudioController>().Play(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
            }
            gameController.GetComponent<InputController>().GoThrustButton.isToggled = true;
            gameController.GetComponent<InputController>().StopThrustButton.isToggled = false;
        }
        else
        {
            gameController.FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
            gameController.FPSCamera.GetComponent<FPSCameraController>().StopShake();
            gameController.GetComponent<InputController>().GoThrustButton.isToggled = false;
            gameController.GetComponent<InputController>().StopThrustButton.isToggled = true;
        }
    }

    private void ApplyFuel(float deltaFuel)
    {
        currentFuelKg += deltaFuel;
        if (currentFuelKg <= 0)
        {
            currentFuelKg = 0;
        }
        UpdateFuel();
    }

    private void UpdateFuel()
    {
        if (currentFuelKg <= 0)
        {
            // warn?
        }
        currentTotalMassKg = EmptyMassKg + currentFuelKg;
        RCSThrustPerSec = RCSThrustNewtons / currentTotalMassKg;
        EngineThrustPerSec = EngineThrustNewtons / currentTotalMassKg;
        RCSAngularDegPerSec = Mathf.Rad2Deg * Mathf.Sqrt(RCSThrustPerSec / RCSRadiusM);
        gameController.GetComponent<InputController>().FuelRemainingText.text = string.Format("{0:0}", currentFuelKg);
        gameController.GetComponent<InputController>().FuelSlider.value = NormalizedFuel(currentFuelKg);
    }

    private float NormalizedFuel(float fuelRawKg)
    {
        return fuelRawKg/ FuelMassKg;
    }

    private void ApplyImpulse(Vector3 normalizedDirection, float thrustPer)
    {
        Vector3 thrust = normalizedDirection * thrustPer * Time.deltaTime;
        //thrust = transform.rotation * thrust * Time.deltaTime;
        GravityEngine.instance.ApplyImpulse(nbody, thrust);
    }

    private void UpdateCameraInput()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleCamera();
        }
    }

    private void UpdateRCSInput()
    {
        if (Input.GetKeyDown(KeyCode.KeypadDivide))
        {
            ToggleRCSMode();
        }
        switch (currentRCSMode)
        {
            case RCSMode.Rotate:
                UpdateInputRotation();
                break;
            case RCSMode.Translate:
            default:
                UpdateInputTranslation();
                break;
        }
        UpdateAutoRotate();
        PlayRotateSounds();
    }

    private void UpdateEngineInput()
    {
        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            UpdateInputGoThrust();
        }
        if (Input.GetKeyUp(KeyCode.KeypadEnter))
        {
            UpdateInputStopThrust();
        }
        UpdateEngine();
    }

    private bool ShouldBounce(GameObject otherBody, out float relVel)
    {
        Vector3 relVelVec =
            GravityEngine.instance.GetVelocity(otherBody.transform.parent.gameObject)
            -
            GravityEngine.instance.GetVelocity(transform.parent.gameObject);
        relVel = relVelVec.magnitude;
        bool bouncing = relVel < minRelVtoExplode;
        return bouncing;
    }

    void OnTriggerEnter(Collider collider)
    {
        GameObject otherBody = collider.attachedRigidbody.gameObject;
        float relVel;
        if (ShouldBounce(otherBody, out relVel))
        {
            gameController.FPSCamera.GetComponent<FPSCameraController>().PlayShake();
            if (relVel >= minRelVtoDamage)
            {
                health--;
            }
        }
        else
        {
            health = 0; // boom
            GameOverCollision(otherBody.name);
        }
    }

    private void GameOverCollision(string otherName)
    {
        PlayExplosion();
        string msg = string.Format("Smashed into {0}", otherName);
        gameController.GameOver(msg);
    }

    private void PlayExplosion()
    {
        ShipExplosion.GetComponent<ParticleSystem>().Play();
        ShipExplosion.GetComponent<AudioSource>().Play();
        //        Instantiate(ShipExplosion, transform.parent.transform.position, transform.parent.transform.rotation);
    }


    private void UpdateHUD()
    {
        gameController.UpdateHUD(transform.parent);
    }

    private bool ApplySpinVector(Vector3 inputSpinVector)
    {
        Vector3 spinVector = new Vector3(
            Mathf.Clamp(inputSpinVector.x, -RCSAngularDegPerSec * Time.deltaTime, RCSAngularDegPerSec * Time.deltaTime),
            Mathf.Clamp(inputSpinVector.y, -RCSAngularDegPerSec * Time.deltaTime, RCSAngularDegPerSec * Time.deltaTime),
            Mathf.Clamp(inputSpinVector.z, -RCSAngularDegPerSec * Time.deltaTime, RCSAngularDegPerSec * Time.deltaTime)
            );
        currentSpin.x = Mathf.Clamp(currentSpin.x + spinVector.x, -MaxRotationDegPerSec, MaxRotationDegPerSec);
        currentSpin.y = Mathf.Clamp(currentSpin.y + spinVector.y, -MaxRotationDegPerSec, MaxRotationDegPerSec);
        currentSpin.z = Mathf.Clamp(currentSpin.z + spinVector.z, -MaxRotationDegPerSec, MaxRotationDegPerSec);
        return spinVector != Vector3.zero;
    }

    private bool ApplySpinVector(float x, float y, float z)
    {
        Vector3 spinVector = new Vector3(x, y, z);
        return ApplySpinVector(spinVector);
    }

    void UpdateInputRotation()
    {
        rotInput = false;
        if (Input.GetKey(KeyCode.Keypad2))
        {
            rotInput = ApplySpinVector(-RCSAngularDegPerSec * Time.deltaTime, 0, 0);
        }
        if (Input.GetKey(KeyCode.Keypad8))
        {
            rotInput = ApplySpinVector(RCSAngularDegPerSec * Time.deltaTime, 0, 0);
        }
        if (Input.GetKey(KeyCode.Keypad1))
        {
            rotInput = ApplySpinVector(0, -RCSAngularDegPerSec * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.Keypad3))
        {
            rotInput = ApplySpinVector(0, RCSAngularDegPerSec * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.Keypad6))
        {
            rotInput = ApplySpinVector(0, 0, -RCSAngularDegPerSec * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.Keypad4))
        {
            rotInput = ApplySpinVector(0, 0, RCSAngularDegPerSec * Time.deltaTime);
        }
        if (rotInput)
        {
            killingRot = false;
            autoRotating = false;
            gameController.GetComponent<InputController>().TargetToggleButton.isToggled = false;
            gameController.GetComponent<InputController>().POSToggleButton.isToggled = false;
            gameController.GetComponent<InputController>().NEGToggleButton.isToggled = false;
        }
        if (Input.GetKeyDown(KeyCode.Keypad5)) // kill rot
        {
            killingRot = true;
            autoRotating = false;
            gameController.GetComponent<InputController>().TargetToggleButton.isToggled = false;
            gameController.GetComponent<InputController>().POSToggleButton.isToggled = false;
            gameController.GetComponent<InputController>().NEGToggleButton.isToggled = false;
        }
    }

    void UpdateAutoRotate()
    {
        if (Input.GetKeyDown(KeyCode.Keypad7)) // autorot
        {
            killingRot = false;
            autoRotating = true;
            gameController.GetComponent<InputController>().TargetToggleButton.isToggled = true;
            gameController.GetComponent<InputController>().POSToggleButton.isToggled = false;
            gameController.GetComponent<InputController>().NEGToggleButton.isToggled = false;
            autoRotatingTarget = gameController.GetReferenceBody();
        }
        if (Input.GetKeyDown(KeyCode.KeypadPlus)) // autorot pos
        {
            killingRot = false;
            autoRotating = true;
            gameController.GetComponent<InputController>().TargetToggleButton.isToggled = false;
            gameController.GetComponent<InputController>().POSToggleButton.isToggled = true;
            gameController.GetComponent<InputController>().NEGToggleButton.isToggled = false;
            autoRotatingTarget = gameController.GetComponent<InputController>().RelativeVelocityDirectionIndicator;
        }
        if (Input.GetKeyDown(KeyCode.KeypadMinus)) // autorot neg
        {
            killingRot = false;
            autoRotating = true;
            gameController.GetComponent<InputController>().TargetToggleButton.isToggled = false;
            gameController.GetComponent<InputController>().POSToggleButton.isToggled = false;
            gameController.GetComponent<InputController>().NEGToggleButton.isToggled = true;
            autoRotatingTarget = gameController.GetComponent<InputController>().RelativeVelocityAntiDirectionIndicator;
        }

    }

    void PlayRotateSounds()
    {
        if (rotInput || killingRot || autoRotating)
        {
            if (!gameController.FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG))
            {
                gameController.FPSCamera.GetComponent<FPSAudioController>().Play(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG);
            }
            rotInput = false;
        }
        else
        {
            gameController.FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG);
        }
    }

    void ApplyCurrentRotation()
    {
        if (killingRot)
        {
            Vector3 neededOffset = Vector3.zero - currentSpin;
            bool isZero = ApplySpinVector(neededOffset);
            if (currentSpin == Vector3.zero)
            {
                killingRot = false;
            }
        }
        else if (autoRotating)
        {
            if (autoRotatingTarget == gameController.GetComponent<InputController>().RelativeVelocityDirectionIndicator)
            {
                gameController.GetComponent<InputController>().TargetToggleButton.isToggled = false;
                gameController.GetComponent<InputController>().POSToggleButton.isToggled = true;
                gameController.GetComponent<InputController>().NEGToggleButton.isToggled = false;
            }
            else if (autoRotatingTarget == gameController.GetComponent<InputController>().RelativeVelocityDirectionIndicator)
            {
                gameController.GetComponent<InputController>().TargetToggleButton.isToggled = false;
                gameController.GetComponent<InputController>().POSToggleButton.isToggled = false;
                gameController.GetComponent<InputController>().NEGToggleButton.isToggled = true;
            }
            else
            {
                gameController.GetComponent<InputController>().TargetToggleButton.isToggled = true;
                gameController.GetComponent<InputController>().POSToggleButton.isToggled = false;
                gameController.GetComponent<InputController>().NEGToggleButton.isToggled = false;
            }
            Vector3 a = transform.forward;
            Vector3 b = (autoRotatingTarget.transform.position - transform.parent.transform.position).normalized;
            Quaternion q = Quaternion.FromToRotation(a, b);
            //q = Quaternion.Euler(-transform.rotation.eulerAngles.x, -transform.rotation.eulerAngles.y, -transform.rotation.eulerAngles.z) * q;
            q = Quaternion.Euler(0, 0, -transform.rotation.eulerAngles.z) * q;

            float deltaXToApply;
            bool thetaSatX;
            CalcRotationDelta(q.eulerAngles.x, currentSpin.x, out deltaXToApply, out thetaSatX);
            float deltaYToApply;
            bool thetaSatY;
            CalcRotationDelta(q.eulerAngles.y, currentSpin.y, out deltaYToApply, out thetaSatY);
            float deltaZToApply;
            bool thetaSatZ;
            CalcRotationDelta(q.eulerAngles.z, currentSpin.z, out deltaZToApply, out thetaSatZ);
            ApplySpinVector(deltaXToApply, deltaYToApply, deltaZToApply);
            if (thetaSatX && thetaSatY && thetaSatZ)
            {
                autoRotating = false;
                killingRot = true;
                autoRotatingTarget = gameController.GetReferenceBody();
                gameController.GetComponent<InputController>().TargetToggleButton.isToggled = false;
                gameController.GetComponent<InputController>().POSToggleButton.isToggled = false;
                gameController.GetComponent<InputController>().NEGToggleButton.isToggled = false;
            }
        }
        transform.Rotate(
            currentSpin.x * Time.deltaTime,
            currentSpin.y * Time.deltaTime,
            currentSpin.z * Time.deltaTime,
            Space.Self);
    }

    private void CalcRotationDelta(float deltaAngleDeg, float currentSpinDegPerSec, out float deltaToApply, out bool thetaSat)
    {
        float theta = deltaAngleDeg;
        float absTheta = Mathf.Abs(theta);
        float signTheta = Mathf.Sign(theta);
        if (theta >= 180)
        {
            signTheta = -1;
            absTheta = 360 - absTheta;
        }
        float absCurrentSpin = Mathf.Abs(currentSpinDegPerSec);
        float signCurrentSpin = Mathf.Sign(currentSpinDegPerSec);
        if (absTheta < minDeltaTheta && absCurrentSpin < minSpinDeltaDegPerSec) // we are close enough
        {
            thetaSat = true;
            deltaToApply = 0;
        }
        else // we need to angular impulse to change orientation, maybe
        {
            thetaSat = false;
            float secLeftAtCurrentSpinX = absCurrentSpin == 0 ? 10000000 : absTheta / absCurrentSpin;
            float minTimeToSlowToZeroSec = RotationTimeDampeningFactor * Mathf.Sqrt(2 * absTheta / RCSAngularDegPerSec);
            if (minTimeToSlowToZeroSec < secLeftAtCurrentSpinX) // we can stop in time
            {
                deltaToApply = signTheta * RCSAngularDegPerSec * Time.deltaTime;
            }
            else
            {
                deltaToApply = -1 * (signCurrentSpin * RCSAngularDegPerSec * Time.deltaTime);
            }
        }
    }

    void UpdateInputTranslation()
    {
        Vector3 v = Vector3.zero;
        if (Input.GetKey(KeyCode.Keypad6))
        {
            v = transform.forward;
        }
        if (Input.GetKey(KeyCode.Keypad9))
        {
            v = -transform.forward;
        }
        if (Input.GetKey(KeyCode.Keypad2))
        {
            v = transform.up;
        }
        if (Input.GetKey(KeyCode.Keypad8))
        {
            v = -transform.up;
        }
        if (Input.GetKey(KeyCode.Keypad1))
        {
            v = -transform.right;
        }
        if (Input.GetKey(KeyCode.Keypad3))
        {
            v = transform.right;
        }
        if (v != Vector3.zero)
        {
            ApplyImpulse(v, RCSThrustPerSec * Time.deltaTime);
            ApplyFuel(-RCSFuelKgPerSec * Time.deltaTime);
            if (!gameController.FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCS))
            {
                gameController.FPSCamera.GetComponent<FPSAudioController>().Play(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
            }
        }
        else
        {
            gameController.FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
        }
    }

}
