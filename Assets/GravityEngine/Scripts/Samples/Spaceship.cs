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
    public int healthMax = 3;
    public GameObject ShipExplosion;

    private NBody nbody; 
    private enum RCSMode { Rotate, Translate };
    private RCSMode currentRCSMode;
    private Vector3 currentSpin;
    private bool killingRot;
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

    //private Vector3 coneScale; // nitial scale of thrust cone

    private Vector3 initPos;
    private Quaternion initRot;
    private Vector3 initParentPos;
    private Quaternion initParentRot;

    // Use this for initialization
    void Start() {
        initPos = transform.position;
        initRot = transform.rotation;
        initParentPos = transform.parent.transform.position;
        initParentRot = transform.parent.transform.rotation;
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
        /*
        transform.position = initPos;
        transform.rotation = initRot;
        transform.parent.transform.position = initParentPos;
        transform.parent.transform.rotation = initParentRot;
        */
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
                    StopAll();
                    isInitStatic = false;
                    isInitReady = false;
                }
                break;
        }
    }

    private void StopAll()
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
    }

    private void UpdateEngineInput()
    {
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            UpdateInputGoThrust();
        }
        if (Input.GetKeyDown(KeyCode.KeypadMinus))
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
            GameOverAsteroidHit();
        }
    }

    private void GameOverAsteroidHit()
    {
        PlayExplosion();
        gameController.GameOver("You hit an asteroid!");
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

    void UpdateInputRotation()
    {
        bool rotInput = false;
        if (Input.GetKey(KeyCode.Keypad2))
        {
            rotInput = true;
            killingRot = false;
            currentSpin.x -= RCSAngularDegPerSec * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Keypad8))
        {
            rotInput = true;
            killingRot = false;
            currentSpin.x += RCSAngularDegPerSec * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Keypad1))
        {
            rotInput = true;
            killingRot = false;
            currentSpin.y -= RCSAngularDegPerSec * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Keypad3))
        {
            rotInput = true;
            killingRot = false;
            currentSpin.y += RCSAngularDegPerSec * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Keypad6))
        {
            rotInput = true;
            killingRot = false;
            currentSpin.z -= RCSAngularDegPerSec * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Keypad4))
        {
            rotInput = true;
            killingRot = false;
            currentSpin.z += RCSAngularDegPerSec * Time.deltaTime;
        }
        if (Input.GetKeyDown(KeyCode.Keypad5)) // kill rot
        {
            killingRot = !killingRot;
        }
        if (rotInput || killingRot)
        {
            if (!gameController.FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG))
            {
                gameController.FPSCamera.GetComponent<FPSAudioController>().Play(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG);
            }
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
            Vector3 allowedOffset = new Vector3(
                Mathf.Clamp(neededOffset.x, -RCSAngularDegPerSec * Time.deltaTime, RCSAngularDegPerSec * Time.deltaTime),
                Mathf.Clamp(neededOffset.y, -RCSAngularDegPerSec * Time.deltaTime, RCSAngularDegPerSec * Time.deltaTime),
                Mathf.Clamp(neededOffset.z, -RCSAngularDegPerSec * Time.deltaTime, RCSAngularDegPerSec * Time.deltaTime)
            );
            currentSpin += allowedOffset;
            if (currentSpin == Vector3.zero)
            {
                killingRot = false;
            }
        }
        transform.Rotate(
            currentSpin.x * Time.deltaTime,
            currentSpin.y * Time.deltaTime,
            currentSpin.z * Time.deltaTime,
            Space.Self);
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
