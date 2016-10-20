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
    //   private bool isInitStatic = false;
    //   private bool isInitReady = false;

    //! Thrust scale
    private RocketShip ship;
    private enum RCSMode { Rotate, Translate };
    private RCSMode currentRCSMode;
    private enum CameraMode { FPS, ThirdParty };
    private CameraMode currentCameraMode;
    private int health;
    private bool rotInput = false;
    private bool inEngineTap = false;
    private float doubleTapEngineTimer = 0;

    void Start()
    {
        ship = GetComponent<RocketShip>();
        //    }

        //    private void InitStatic()
        //    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.parent.transform.position = Vector3.zero;
        transform.parent.transform.rotation = Quaternion.identity;
        currentRCSMode = RCSMode.Rotate;
        currentCameraMode = CameraMode.FPS;
        health = healthMax;
        rotInput = false;
        //    }

        //    private void InitReady()
        //    {
        //        if (!isInitStatic)
        //        {
        //            InitStatic();
        //            isInitStatic = true;
        //        }
        UpdateRCSMode();
        UpdateCameraMode();
        UpdateFuelUI();
        UpdateEngineUI();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameController != null)
        {
            switch (gameController.GetGameState())
            {
                case GameController.GameState.SPLASH:
                    //if (!isInitStatic)
                    //{
                    //    InitStatic();
                    //    isInitStatic = true;
                    //}
                    break;
                case GameController.GameState.RUNNING:
                    //if (!isInitReady)
                    //{
                    //    InitReady();
                    //    isInitReady = true;
                    //}
                    UpdateCameraInput();
                    UpdateRCSInput();
                    UpdateEngineInput();
                    UpdateEngineUI();
                    UpdateSpinUI();
                    UpdateDumpFuel();
                    UpdateHUD();
                    break;
                case GameController.GameState.OVER:
                    //if (isInitStatic || isInitReady)
                    //{
                    PrepareDestroy();
                    //}
                    break;
            }
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
        //isInitStatic = false;
        //isInitReady = false;
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
        if (gameController != null && gameController.GetComponent<InputController>().DumpFuelButton.isToggled)
        {
            ship.DumpFuel();
            UpdateFuelUI();
        }
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
    }

    private void UpdateFuelUI()
    {
        if (gameController != null)
        {
            gameController.GetComponent<InputController>().FuelRemainingText.text = string.Format("{0:0}", ship.CurrentFuelKg());
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
            default:
                UpdateInputTranslation();
                break;
        }
        UpdateAutoRotate();
        PlayRotateSounds();
    }

    private bool scheduleMeco = false;
    private float mecoTimer = 0;

    private void UpdateEngineInput()
    {
        if (scheduleMeco)
        {
            if (mecoTimer > 0)
            {
                mecoTimer -= Time.deltaTime;
            }
            else
            {
                ship.MainEngineCutoff();
                mecoTimer = 0;
                scheduleMeco = false;
            }
        }
        if (!inEngineTap)
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                inEngineTap = true;
                doubleTapEngineTimer = 0.2f;
            }
        }
        else // user tapped before
        {
            if (doubleTapEngineTimer <= 0) // double tap time has passed, do default select
            {
                ship.MainEngineGo();
                mecoTimer = 0.5f;
                scheduleMeco = true;
                inEngineTap = false;
                doubleTapEngineTimer = 0;
            }
            else // still waiting to see if user double tapped
            {
                if (Input.GetKeyDown(KeyCode.KeypadEnter)) // user doubletapped
                {
                    if (ship.IsMainEngineGo())
                    {
                        ship.MainEngineCutoff();
                    }
                    else
                    {
                        ship.MainEngineGo();
                    }
                    inEngineTap = false;
                    doubleTapEngineTimer = 0;
                }
                else // continue countdown
                {
                    doubleTapEngineTimer -= Time.deltaTime;
                }
            }
        }
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
                GameOverCollision(otherBody.name);
            }
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
        if (gameController != null)
        {
            gameController.UpdateHUD(transform.parent);
        }
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
            if (rotInput)
            {
                ship.NoRot();
                ToggleButtons(false, false, false, false);
            }
            if (Input.GetKeyDown(KeyCode.Keypad5)) // kill rot
            {
                ship.KillRot();
                ToggleButtons(true, false, false, false);
            }
        }
    }

    public void RotateTowardsTarget()
    {
        if (gameController != null)
        {
            ship.AutoRot(gameController.GetReferenceBody());
            ToggleButtons(false, true, false, false);
        }
    }

    void UpdateAutoRotate()
    {
        if (gameController != null)
        {
            if (Input.GetKeyDown(KeyCode.KeypadPlus)) // autorot pos
            {
                ship.AutoRot(gameController.GetComponent<InputController>().RelativeVelocityDirectionIndicator);
                ToggleButtons(false, false, true, false);
            }
            if (Input.GetKeyDown(KeyCode.KeypadMinus)) // autorot neg
            {
                ship.AutoRot(gameController.GetComponent<InputController>().RelativeVelocityAntiDirectionIndicator);
                ToggleButtons(false, false, false, true);
            }

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
            if (rotInput || ship.IsRot())
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
            if (ship.IsKillRot())
            {
                // do stuff
            }
            else if (ship.IsAutoRot())
            {
                if (ship.AutoRotTarget() == gameController.GetComponent<InputController>().RelativeVelocityDirectionIndicator)
                {
                    gameController.GetComponent<InputController>().TargetToggleButton.isToggled = false;
                    gameController.GetComponent<InputController>().POSToggleButton.isToggled = true;
                    gameController.GetComponent<InputController>().NEGToggleButton.isToggled = false;
                }
                else if (ship.AutoRotTarget() == gameController.GetComponent<InputController>().RelativeVelocityDirectionIndicator)
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
