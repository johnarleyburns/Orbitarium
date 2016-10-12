using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Spaceship : MonoBehaviour {

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
    public ToggleButton RotateButton;
    public ToggleButton TranslateButton;
    public ToggleButton StopThrustButton;
    public ToggleButton GoThrustButton;
    public Slider FuelSlider;
    public Text FuelRemainingText;
    public ToggleButton DumpFuelButton;
    public GameObject ReferenceBody;
    public GameObject Didymos;
    public GameObject Didymoon;
    public GameObject HUD;
    public GameObject RelativeVelocityDirectionIndicator;
    public GameObject RelativeVelocityAntiDirectionIndicator;
    public Camera FPSCamera;
    public Camera ThirdPartyCamera;

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

    //private Vector3 coneScale; // nitial scale of thrust cone


    // Use this for initialization
    void Start() {
        if (transform.parent == null) {
            Debug.LogError("Spaceship script must be applied to a model that is a child of an NBody object.");
            return;
        }
        nbody = transform.parent.GetComponent<NBody>();
        if (nbody == null) {
            Debug.LogError("Parent must have an NBody script attached.");
        }
		//running = false;
		//ravityEngine.instance.SetEvolve(running);
        health = healthMax;
        mainEngineOn = false;
        currentTotalMassKg = EmptyMassKg + FuelMassKg;
        currentFuelKg = FuelMassKg;
        //coneScale = thrustCone.transform.localScale;
        GravityEngine.instance.Setup();
        currentRCSMode = RCSMode.Rotate;
        UpdateRCSMode();
        currentCameraMode = CameraMode.FPS;
        UpdateCameraMode();
        UpdateFuel();
        UpdateEngine();
    }

    // Update is called once per frame
    void Update()
    {
        if (nbody == null)
        {
            return; // misconfigured
        }
        UpdateCameraInput();
        UpdateRCSInput();
        UpdateEngineInput();
        ApplyCurrentRotation();
        UpdateDumpFuel();
        UpdateHUD();
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
        DumpFuelButton.isToggled = !DumpFuelButton.isToggled;
    }

    public void UpdateDumpFuel()
    {
        if (DumpFuelButton.isToggled)
        {
            ApplyFuel(-DumpFuelRateKgPerSec * Time.deltaTime);
        }
    }

    void UpdateRCSMode()
    {
        switch (currentRCSMode)
        {
            case RCSMode.Rotate:
                RotateButton.isToggled = true;
                TranslateButton.isToggled = false;
                if (FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCS))
                {
                    FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
                }
                break;
            case RCSMode.Translate:
            default:
                RotateButton.isToggled = false;
                TranslateButton.isToggled = true;
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
                FPSCamera.enabled = true;
                ThirdPartyCamera.enabled = false;
                break;
            case CameraMode.ThirdParty:
                FPSCamera.enabled = false;
                ThirdPartyCamera.enabled = true;
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
                FPSCamera.GetComponent<FPSCameraController>().StartContinuousShake();
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
            if (!FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE))
            {
                FPSCamera.GetComponent<FPSAudioController>().Play(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
            }
            GoThrustButton.isToggled = true;
            StopThrustButton.isToggled = false;
        }
        else
        {
            FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
            FPSCamera.GetComponent<FPSCameraController>().StopShake();
            GoThrustButton.isToggled = false;
            StopThrustButton.isToggled = true;
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
        FuelRemainingText.text = string.Format("{0:0}", currentFuelKg);
        FuelSlider.value = NormalizedFuel(currentFuelKg);
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
            FPSCamera.GetComponent<FPSCameraController>().PlayShake();
            if (relVel >= minRelVtoDamage)
            {
                health--;
            }
        }
        else
        {
            health = 0; // boom
        }
    }

    private static int HUD_INDICATOR_DIDYMOS = 0;
    private static int HUD_INDICATOR_RELV_PRO = 1;
    private static int HUD_INDICATOR_RELV_RETR = 2;
    private static int HUD_INDICATOR_DIDYMOON = 3;

    private void CalcRelV(GameObject target, out float dist, out float relv, out Vector3 relVelUnit)
    {
        dist = (target.transform.position - transform.parent.transform.position).magnitude;
        Vector3 myVel = GravityEngine.instance.GetVelocity(transform.parent.gameObject);
        Vector3 targetVel = GravityEngine.instance.GetVelocity(target);
        Vector3 relVel = myVel - targetVel;
        Vector3 targetPos = target.transform.position;
        Vector3 myPos = transform.parent.transform.position;
        Vector3 relLoc = targetPos - myPos;
        float relVelDot = Vector3.Dot(relVel, relLoc);
        float relVelScalar = relVel.magnitude;
        relv = Mathf.Sign(relVelDot) * relVelScalar;
        relVelUnit = relVel.normalized;
    }

    private Vector3 RelVIndicatorScaled(Vector3 relVelUnit)
    {
        float RelativeVelocityIndicatorScale = 1000;
        return RelativeVelocityIndicatorScale * relVelUnit;
    }

    private void UpdateHUD()
    {
        float referenceBodyDist;
        float referenceBodyRelV;
        Vector3 refBodyRelVelUnit;
        CalcRelV(ReferenceBody, out referenceBodyDist, out referenceBodyRelV, out refBodyRelVelUnit);
        Greyman.OffScreenIndicator offScreenIndicator = HUD.GetComponent<Greyman.OffScreenIndicator>();
        if (offScreenIndicator.indicators[HUD_INDICATOR_DIDYMOS].hasOnScreenText)
        {
            string targetName = Didymos.name;
            string targetString = string.Format("{0}\n{1:0,0} m\n{2:0,0.0} m/s", targetName, referenceBodyDist, referenceBodyRelV);
            offScreenIndicator.UpdateIndicatorText(HUD_INDICATOR_DIDYMOS, targetString);
        }
        if (offScreenIndicator.indicators[HUD_INDICATOR_DIDYMOON].hasOnScreenText)
        {
            float moonBodyDist;
            float moonBodyRelV;
            Vector3 moonBodyRelVelUnit;
            CalcRelV(Didymoon, out moonBodyDist, out moonBodyRelV, out moonBodyRelVelUnit);
            string targetName = Didymoon.name;
            string targetString = string.Format("{0}\n{1:0,0} m\n{2:0,0.0} m/s", targetName, moonBodyDist, moonBodyRelV);
            offScreenIndicator.UpdateIndicatorText(HUD_INDICATOR_DIDYMOON, targetString);
        }

        Vector3 relVIndicatorScaled = RelVIndicatorScaled(refBodyRelVelUnit);
        Vector3 myPos = transform.parent.transform.position;
        if (offScreenIndicator.indicators[HUD_INDICATOR_RELV_PRO].hasOnScreenText)
        {
            RelativeVelocityDirectionIndicator.transform.position = myPos + relVIndicatorScaled;
            string targetString = string.Format("PRO {0:0,0.0} m/s", referenceBodyRelV);
            offScreenIndicator.UpdateIndicatorText(HUD_INDICATOR_RELV_PRO, targetString);
        }
        if (offScreenIndicator.indicators[HUD_INDICATOR_RELV_RETR].hasOnScreenText)
        {
            RelativeVelocityAntiDirectionIndicator.transform.position = myPos + -relVIndicatorScaled;
            string targetString = string.Format("RETR {0:0,0.0} m/s", -referenceBodyRelV);
            offScreenIndicator.UpdateIndicatorText(HUD_INDICATOR_RELV_RETR, targetString);
        }
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
            if (!FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG))
            {
                FPSCamera.GetComponent<FPSAudioController>().Play(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG);
            }
        }
        else
        {
            FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG);
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
            if (!FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCS))
            {
                FPSCamera.GetComponent<FPSAudioController>().Play(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
            }
        }
        else
        {
            FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
        }
    }

}
