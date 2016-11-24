using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Greyman;

public class PlayerShip : MonoBehaviour
{

    public GameController gameController;
    public GameObject ShipExplosion;
    public int healthMax = 3;
    public float minRelVtoDamage = 1;
    public float DoubleTapInterval = 0.2f;

    //! Thrust scale
    private RocketShip ship;
    private Autopilot autopilot;
    private InputController inputController;
    private FPSAudioController audioController;
    private FPSCameraController cameraController;
    private enum RCSMode { Rotate, Translate };
    private RCSMode currentRCSMode;
    private enum CameraMode { FPS, ThirdParty };
    private CameraMode currentCameraMode;
    private int health;
    private bool rotInput = false;
    private float doubleTapEngineTimer;
    private float doubleTapRotatePlusTimer;
    private float doubleTapRotateMinusTimer;
    private float doubleTapTargetSelTimer;

    public void StartShip()
    {
        ship = GetComponent<RocketShip>();
        autopilot = GetComponent<Autopilot>();
        inputController = gameController.GetComponent<InputController>();
        audioController = gameController.FPSCamera.GetComponent<FPSAudioController>();
        cameraController = gameController.FPSCamera.GetComponent<FPSCameraController>();
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.parent.transform.position = Vector3.zero;
        transform.parent.transform.rotation = Quaternion.identity;
        currentRCSMode = RCSMode.Rotate;
        currentCameraMode = CameraMode.FPS;
        doubleTapEngineTimer = -1;
        doubleTapRotatePlusTimer = -1;
        doubleTapRotateMinusTimer = -1;
        doubleTapTargetSelTimer = -1;
        health = healthMax;
        rotInput = false;

    }


    public void UpdateShip()
    {
        UpdateRCSMode();
        UpdateCameraMode();
        UpdateCameraInput();
        UpdateRCSInput();
        UpdateEngineInput();
        UpdateFuelUI();
        UpdateEngineUI();
        UpdateSpinUI();
        UpdateTargetSelection();
    }

    private void HaltAudio()
    {
        if (audioController.IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE))
        {
            audioController.Stop(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
        }
        if (audioController.IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCS))
        {
            audioController.Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
        }
        if (audioController.IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG))
        {
            audioController.Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG);
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

    public void UpdateRCSMode()
    {
        if (gameController != null)
        {
            switch (currentRCSMode)
            {
                case RCSMode.Rotate:
                    inputController.PropertyChanged("RotateButton", true);
                    if (audioController.IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCS))
                    {
                        audioController.Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
                    }
                    break;
                case RCSMode.Translate:
                default:
                    inputController.PropertyChanged("RotateButton", false);
                    break;
            }
        }
    }

    public void ExecuteAutopilotCommand(Autopilot.Command command)
    {
        autopilot.ExecuteCommand(command, gameController.GetHUD().GetReferenceBody());
        inputController.PropertyChanged("CommandExecuted", command);
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
        if (gameController != null)
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
    }

    private void UpdateEngineUI()
    {
        if (gameController != null)
        {
            bool stillRunning = ship.IsMainEngineGo();
            if (stillRunning)
            {
                UpdateFuelUI();
                cameraController.StartContinuousShake();
                if (!audioController.IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE))
                {
                    audioController.Play(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
                }
            }
            else
            {
                audioController.Stop(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
                cameraController.StopShake();
            }
            inputController.PropertyChanged("GoThrustButton", stillRunning);
        }
    }

    private void UpdateFuelUI()
    {
        if (gameController != null)
        {
            inputController.PropertyChanged("FuelRemainingText", string.Format("{0:0}", ship.CurrentFuelKg()));
            inputController.PropertyChanged("FuelSlider", ship.NormalizedFuel());
        }
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
                UpdateInputTranslation();
                break;
        }
        UpdateKillRotV();
        UpdateRotatePlus();
        UpdateRotateMinus();
        PlayRotateSounds();
    }

    private void UpdateTargetSelection()
    {
        UpdateDoubleTap(KeyCode.KeypadMultiply, ref doubleTapTargetSelTimer, gameController.GetHUD().SelectNextTarget, RotateTowardsTarget);
    }

    private void UpdateKillRotV()
    {
        if (Input.GetKeyDown(KeyCode.Keypad5))
        {
            KillRot();
        }
    }

    private delegate void TapFunc();
    private void UpdateDoubleTap(KeyCode keyCode, ref float timer, TapFunc singleTapFunc, TapFunc doubleTapFunc)
    {
        if (Input.GetKeyDown(keyCode))
        {
            if (timer > 0)
            {
                // it's a double tap
                doubleTapFunc();
                timer = -1;
            }
            else
            {
                // start single tap time
                timer = DoubleTapInterval;
            }
        }
        else if (timer > 0)
        {
            // still waiting for double or single time to elapse
            timer -= Time.deltaTime;
        }
        else if (timer > -1 && timer <= 0)
        {
            // time has elapsed for single tap, do it
            singleTapFunc();
            timer = -1;
        }
    }

    private void UpdateEngineInput()
    {
        UpdateDoubleTap(KeyCode.KeypadEnter, ref doubleTapEngineTimer, ToggleEngine, Rendezvous);
    }

    private void BurstEngine()
    {
        float burnTime = DoubleTapInterval;
        ship.MainEngineBurst(burnTime);
    }

    private void ToggleEngine()
    {
        if (ship.IsMainEngineGo())
        {
            ship.MainEngineCutoff();
        }
        else
        {
            ship.MainEngineGo();
        }
    }

    private void Rendezvous()
    {
        autopilot.ExecuteCommand(Autopilot.Command.RENDEZVOUS, gameController.GetHUD().GetReferenceBody());
        inputController.PropertyChanged("CommandExecuted", Autopilot.Command.RENDEZVOUS);
    }

    void OnTriggerEnter(Collider collider)
    {
        if (gameController != null)
        {
            GameObject otherBody = collider.attachedRigidbody.gameObject;
            float relVel;
            if (PhysicsUtils.ShouldBounce(gameObject, otherBody, out relVel))
            {
                cameraController.PlayShake();
                if (relVel >= minRelVtoDamage)
                {
                    health--;
                }
            }
            else
            {
                health = 0; // boom
                GameOverCollision(otherBody.transform.parent.name);
            }
        }
    }

    private void GameOverCollision(string otherName)
    {
        RemoveBody();
        HaltAudio();
        PlayExplosion();
        string msg = string.Format("Smashed into {0}", otherName);
        gameController.TransitionToGameOverFromDeath(msg);
    }

    private void RemoveBody()
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    private void PlayExplosion()
    {
        ShipExplosion.GetComponent<ParticleSystem>().Play();
        ShipExplosion.GetComponent<AudioSource>().Play();
        //        Instantiate(ShipExplosion, transform.parent.transform.position, transform.parent.transform.rotation);
    }

    void UpdateInputRotation()
    {
        if (gameController != null)
        {
            rotInput = false;
            if (Input.GetKey(KeyCode.Keypad2))
            {
                rotInput = ship.ApplyRCSSpin(Quaternion.Euler(-1, 0, 0));
            }
            if (Input.GetKey(KeyCode.Keypad8))
            {
                rotInput = ship.ApplyRCSSpin(Quaternion.Euler(1, 0, 0));
            }
            if (Input.GetKey(KeyCode.Keypad1))
            {
                rotInput = ship.ApplyRCSSpin(Quaternion.Euler(0, -1, 0));
            }
            if (Input.GetKey(KeyCode.Keypad3))
            {
                rotInput = ship.ApplyRCSSpin(Quaternion.Euler(0, 1, 0));
            }
            if (Input.GetKey(KeyCode.Keypad6))
            {
                rotInput = ship.ApplyRCSSpin(Quaternion.Euler(0, 0, -1));
            }
            if (Input.GetKey(KeyCode.Keypad4))
            {
                rotInput = ship.ApplyRCSSpin(Quaternion.Euler(0, 0, 1));
            }
            if (Input.GetKey(KeyCode.Keypad7))
            {
                autopilot.ExecuteCommand(Autopilot.Command.ACTIVE_TRACK, gameController.GetComponent<InputController>().RelativeVelocityNormalMinusDirectionIndicator);
                inputController.PropertyChanged("CommandExecuted", Autopilot.Command.ACTIVE_TRACK);
                ToggleButtons(false, false, false, true);
                rotInput = false;
            }
            if (Input.GetKey(KeyCode.Keypad9))
            {
                autopilot.ExecuteCommand(Autopilot.Command.ACTIVE_TRACK, gameController.GetComponent<InputController>().RelativeVelocityNormalPlusDirectionIndicator);
                inputController.PropertyChanged("CommandExecuted", Autopilot.Command.ACTIVE_TRACK);
                ToggleButtons(false, false, true, false);
                rotInput = false;
            }
            if (rotInput)
            {
                autopilot.ExecuteCommand(Autopilot.Command.OFF, null);
                inputController.PropertyChanged("CommandExecuted", Autopilot.Command.OFF);
                ToggleButtons(false, false, false, false);
            }
        }
    }

    public void RotateTowardsTarget()
    {
        if (gameController != null)
        {
            autopilot.ExecuteCommand(Autopilot.Command.ACTIVE_TRACK, gameController.GetHUD().GetReferenceBody());
            inputController.PropertyChanged("CommandExecuted", Autopilot.Command.ACTIVE_TRACK);
            ToggleButtons(false, true, false, false);
        }
    }

    public void KillRot()
    {
        if (gameController != null)
        {
            autopilot.ExecuteCommand(Autopilot.Command.KILL_ROTATION, null);
            inputController.PropertyChanged("CommandExecuted", Autopilot.Command.KILL_ROTATION);
            ToggleButtons(true, false, false, false);
        }
    }

    public void KillVTarget()
    {
        if (gameController != null)
        {
            autopilot.ExecuteCommand(Autopilot.Command.KILL_REL_V, gameController.GetHUD().GetReferenceBody());
            inputController.PropertyChanged("CommandExecuted", Autopilot.Command.KILL_REL_V);
            ToggleButtons(false, true, false, false);
        }
    }

    private void UpdateRotatePlus()
    {
        UpdateDoubleTap(KeyCode.KeypadPlus, ref doubleTapRotatePlusTimer, RotateToPos, StrafeTarget);
    }

    private void RotateToPos()
    {
        if (gameController != null)
        {
            autopilot.ExecuteCommand(Autopilot.Command.FACE_TARGET, gameController.GetComponent<InputController>().RelativeVelocityDirectionIndicator);
            inputController.PropertyChanged("CommandExecuted", Autopilot.Command.FACE_TARGET);
            ToggleButtons(false, false, true, false);
        }
    }

    private void UpdateRotateMinus()
    {
        UpdateDoubleTap(KeyCode.KeypadMinus, ref doubleTapRotateMinusTimer, RotateToMinus, KillVTarget);
    }

    private void RotateToMinus()
    {
        if (gameController != null)
        {
            autopilot.ExecuteCommand(Autopilot.Command.FACE_TARGET, gameController.GetComponent<InputController>().RelativeVelocityAntiDirectionIndicator);
            inputController.PropertyChanged("CommandExecuted", Autopilot.Command.FACE_TARGET);
            ToggleButtons(false, false, false, true);
        }
    }

    private void APNGToTarget()
    {
        if (gameController != null)
        {
            autopilot.ExecuteCommand(Autopilot.Command.INTERCEPT, gameController.GetHUD().GetReferenceBody());
            inputController.PropertyChanged("CommandExecuted", Autopilot.Command.INTERCEPT);
            ToggleButtons(false, true, false, false);
        }

    }

    private void StrafeTarget()
    {
        if (gameController != null)
        {
            autopilot.ExecuteCommand(Autopilot.Command.STRAFE, gameController.GetHUD().GetReferenceBody());
            inputController.PropertyChanged("CommandExecuted", Autopilot.Command.STRAFE);
            ToggleButtons(false, true, false, false);
        }

    }

    private void ToggleButtons(bool kill, bool target, bool pos, bool neg)
    {
//        gameController.GetComponent<InputController>().KILLToggleButton.isToggled = kill;
  //      gameController.GetComponent<InputController>().TargetToggleButton.isToggled = target;
    //    gameController.GetComponent<InputController>().POSToggleButton.isToggled = pos;
      //  gameController.GetComponent<InputController>().NEGToggleButton.isToggled = neg;
    }

    void PlayRotateSounds()
    {
        if (gameController != null)
        {
            if (rotInput || autopilot.IsRot())
            {
                if (!audioController.IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG))
                {
                    audioController.Play(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG);
                }
                rotInput = false;
            }
            else
            {
                audioController.Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCSCMG);
            }
        }
    }

    private void UpdateSpinUI()
    {
        if (gameController != null)
        {
            if (autopilot.IsRot())
            {
//                gameController.GetComponent<InputController>().TargetToggleButton.isToggled = false;
  //              gameController.GetComponent<InputController>().POSToggleButton.isToggled = false;
    //            gameController.GetComponent<InputController>().NEGToggleButton.isToggled = false;
            }
        }
    }


    void UpdateInputTranslation()
    {
        if (gameController != null)
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
                ship.ApplyRCSImpulse(v);
                if (!audioController.IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCS))
                {
                    audioController.Play(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
                }
            }
            else
            {
                audioController.Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
            }
        }
    }

}
