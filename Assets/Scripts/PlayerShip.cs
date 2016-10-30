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
    private enum RCSMode { Rotate, Translate };
    private RCSMode currentRCSMode;
    private enum CameraMode { FPS, ThirdParty };
    private CameraMode currentCameraMode;
    private int health;
    private bool rotInput = false;
    private float doubleTapEngineTimer;
    private float doubleTapKillRotVTimer;
    private float doubleTapRotatePlusTimer;
    private float doubleTapRotateMinusTimer;
    private float doubleTapTargetSelTimer;

    public void StartShip()
    {
        ship = GetComponent<RocketShip>();
        autopilot = GetComponent<Autopilot>();
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.parent.transform.position = Vector3.zero;
        transform.parent.transform.rotation = Quaternion.identity;
        currentRCSMode = RCSMode.Rotate;
        currentCameraMode = CameraMode.FPS;
        doubleTapEngineTimer = -1;
        doubleTapKillRotVTimer = -1;
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

    public void UpdateRCSMode()
    {
        if (gameController != null)
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
                gameController.FPSCamera.GetComponent<FPSCameraController>().StartContinuousShake();
                if (!gameController.FPSCamera.GetComponent<FPSAudioController>().IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE))
                {
                    gameController.FPSCamera.GetComponent<FPSAudioController>().Play(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
                }
                gameController.GetComponent<InputController>().GoThrustButton.isToggled = true;
            }
            else
            {
                gameController.FPSCamera.GetComponent<FPSAudioController>().Stop(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
                gameController.FPSCamera.GetComponent<FPSCameraController>().StopShake();
                gameController.GetComponent<InputController>().GoThrustButton.isToggled = false;
            }
        }
    }

    private void UpdateFuelUI()
    {
        if (gameController != null)
        {
            //gameController.GetComponent<InputController>().FuelRemainingText.text = string.Format("{0:0}", ship.CurrentFuelKg());
            gameController.GetComponent<InputController>().FuelSlider.value = ship.NormalizedFuel();
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
        autopilot.Rendezvous(gameController.GetHUD().GetReferenceBody());
    }

    void OnTriggerEnter(Collider collider)
    {
        if (gameController != null)
        {
            GameObject otherBody = collider.attachedRigidbody.gameObject;
            float relVel;
            if (PhysicsUtils.ShouldBounce(gameObject, otherBody, out relVel))
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
                autopilot.AutoRot(gameController.GetComponent<InputController>().RelativeVelocityNormalMinusDirectionIndicator);
                ToggleButtons(false, false, false, true);
                rotInput = false;
            }
            if (Input.GetKey(KeyCode.Keypad9))
            {
                autopilot.AutoRot(gameController.GetComponent<InputController>().RelativeVelocityNormalPlusDirectionIndicator);
                ToggleButtons(false, false, true, false);
                rotInput = false;
            }
            if (rotInput)
            {
                autopilot.AutopilotOff();
                ToggleButtons(false, false, false, false);
            }
        }
    }

    public void RotateTowardsTarget()
    {
        if (gameController != null)
        {
            autopilot.AutoRot(gameController.GetHUD().GetReferenceBody());
            ToggleButtons(false, true, false, false);
        }
    }

    public void KillRot()
    {
        if (gameController != null)
        {
            autopilot.KillRot();
            ToggleButtons(true, false, false, false);
        }
    }

    public void KillVTarget()
    {
        if (gameController != null)
        {
            autopilot.KillRelV(gameController.GetHUD().GetReferenceBody());
            ToggleButtons(false, true, false, false);
        }
    }

    private void UpdateRotatePlus()
    {
        UpdateDoubleTap(KeyCode.KeypadPlus, ref doubleTapRotatePlusTimer, RotateToPos, APNGToTarget);
    }

    private void RotateToPos()
    {
        if (gameController != null)
        {
            autopilot.AutoRot(gameController.GetComponent<InputController>().RelativeVelocityDirectionIndicator);
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
            autopilot.AutoRot(gameController.GetComponent<InputController>().RelativeVelocityAntiDirectionIndicator);
            ToggleButtons(false, false, false, true);
        }
    }

    private void APNGToTarget()
    {
        if (gameController != null)
        {
            autopilot.APNGToTarget(gameController.GetHUD().GetReferenceBody());
            ToggleButtons(false, true, false, false);
        }

    }

    private void ToggleButtons(bool kill, bool target, bool pos, bool neg)
    {
        gameController.GetComponent<InputController>().KILLToggleButton.isToggled = kill;
        gameController.GetComponent<InputController>().TargetToggleButton.isToggled = target;
        gameController.GetComponent<InputController>().POSToggleButton.isToggled = pos;
        gameController.GetComponent<InputController>().NEGToggleButton.isToggled = neg;
    }

    void PlayRotateSounds()
    {
        if (gameController != null)
        {
            if (rotInput || autopilot.IsRot())
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
    }

    private void UpdateSpinUI()
    {
        if (gameController != null)
        {
            if (autopilot.IsKillRot())
            {
                // do stuff
            }
            else if (autopilot.IsAutoRot())
            {
                if (autopilot.CurrentTarget() == gameController.GetComponent<InputController>().RelativeVelocityDirectionIndicator)
                {
                    gameController.GetComponent<InputController>().TargetToggleButton.isToggled = false;
                    gameController.GetComponent<InputController>().POSToggleButton.isToggled = true;
                    gameController.GetComponent<InputController>().NEGToggleButton.isToggled = false;
                }
                else if (autopilot.CurrentTarget() == gameController.GetComponent<InputController>().RelativeVelocityAntiDirectionIndicator)
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
            }
            else
            {
                gameController.GetComponent<InputController>().TargetToggleButton.isToggled = false;
                gameController.GetComponent<InputController>().POSToggleButton.isToggled = false;
                gameController.GetComponent<InputController>().NEGToggleButton.isToggled = false;
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

}
