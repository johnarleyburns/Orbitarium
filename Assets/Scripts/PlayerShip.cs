using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Greyman;
using System.Collections;

public class PlayerShip : MonoBehaviour, IControllableShip
{

    public GameController gameController;
    public GameObject ShipExplosion;
    public int healthMax = 3;
    public float minRelVtoDamage = 1;
    public float LowFuelThreshold = 0.1f;
    public enum RCSMode { Rotate, Translate };
    public RCSMode currentRCSMode;

    //! Thrust scale
    private RocketShip ship;
    private Autopilot autopilot;
    private InputController inputController;
    private FPSAudioController audioController;
    private FPSCameraController cameraController;
    private enum CameraMode { OFF, FPS, ThirdParty };
    private CameraMode currentCameraMode = CameraMode.OFF;
    private int health;
    private bool rotInput = false;

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
        health = healthMax;
        rotInput = false;
        inputController.ControlsEnabled = true;
        inputController.PropertyChanged("RotateButton", true);
        inputController.PropertyChanged("TranslateButton", false);
        GetComponent<ShipWeapons>().AddMissiles();
        UpdateCameraMode(CameraMode.FPS);
    }

    public void UpdateShip()
    {
        //UpdateCameraMode();
        UpdateRCSAudio();
        UpdateFuelUI();
        UpdateEngineUI();
        //UpdateSpinUI();
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

    private void UpdateRCSAudio()
    {
        PlayTranslateSounds();
        PlayRotateSounds();
    }

    public void SetRCSMode(RCSMode rcsMode)
    {
        if (currentRCSMode != rcsMode)
        {
            currentRCSMode = rcsMode;
            inputController.PropertyChanged("TranslateButton", currentRCSMode == RCSMode.Translate);
            inputController.PropertyChanged("RotateButton", currentRCSMode == RCSMode.Rotate);
            inputController.PropertyChanged("RCSModeAudio", currentRCSMode);
        }
    }

    public void RCSBurst(Vector3 translateVec)
    {
        ship.RCSBurst(translateVec, ship.RCSBurnMinSec);
    }

    public void RCSCutoff()
    {
        ship.RCSCutoff();
    }

    private void PlayTranslateSounds()
    {
        if (ship.IsRCSFiring() && !audioController.IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCS))
        {
            audioController.Play(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
        }
        else if (!ship.IsRCSFiring() && audioController.IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_RCS))
        {
            audioController.Stop(FPSAudioController.AudioClipEnum.SPACESHIP_RCS);
        }
    }

    public void ExecuteAutopilotCommand(Autopilot.Command command)
    {
        autopilot.ExecuteCommand(command, gameController.HUD().GetSelectedTarget());
        inputController.PropertyChanged("CommandExecuted", command);
    }

    public Autopilot.Command CurrentAutopilotCommand()
    {
        return autopilot.CurrentCommand();
    }

    public bool IsLowFuel()
    {
        return ship.NormalizedFuel() < LowFuelThreshold;
    }

    public void ToggleCamera()
    {
        CameraMode newMode;
        switch (currentCameraMode)
        {
            default:
            case CameraMode.FPS:
                newMode = CameraMode.ThirdParty;
                break;
            case CameraMode.ThirdParty:
                newMode = CameraMode.FPS;
                break;
        }
        UpdateCameraMode(newMode);
    }

    void UpdateCameraMode(CameraMode newMode)
    {
        if (gameController != null && currentCameraMode != newMode)
        {
            switch (newMode)
            {
                default:
                case CameraMode.FPS:
                    gameController.EnableFPSCamera();
                    gameController.HUD().ShowTargetIndicator();
                    break;
                case CameraMode.ThirdParty:
                    gameController.EnableOverShoulderCamera();
                    gameController.OverShoulderCamera.GetComponent<OverShoulderCameraSpin>().UpdateTarget(NUtils.GetNBodyGameObject(transform.gameObject));
                    gameController.HUD().HideTargetIndicator();
                    break;
            }
            currentCameraMode = newMode;
        }
    }

    private void UpdateEngineUI()
    {
        if (gameController != null)
        {
            bool mainOn = ship.IsMainEngineGo();
            bool auxOn = ship.IsAuxEngineGo();
            if (mainOn)
            {
                cameraController.StartContinuousMainEngineShake();
                if (!audioController.IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE))
                {
                    audioController.Play(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
                }
            }
            else
            {
                audioController.Stop(FPSAudioController.AudioClipEnum.SPACESHIP_MAIN_ENGINE);
            }
            if (!mainOn && auxOn)
            {
                cameraController.StartContinuousAuxEngineShake();
                if (!audioController.IsPlaying(FPSAudioController.AudioClipEnum.SPACESHIP_AUX))
                {
                    audioController.Play(FPSAudioController.AudioClipEnum.SPACESHIP_AUX);
                }
            }
            else
            {
                audioController.Stop(FPSAudioController.AudioClipEnum.SPACESHIP_AUX);
            }
            if (mainOn || auxOn)
            {
                UpdateFuelUI();
            }
            else
            {
                cameraController.StopShake();
            }
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

    public void ToggleEngine()
    {
        bool go = ship.IsMainEngineGo();
        if (go)
        {
            ship.MainEngineCutoff();
        }
        else
        {
            ship.MainEngineGo();
        }
        inputController.PropertyChanged("MainOnButton", !go);
    }

    public void ToggleAuxEngine()
    {
        bool go = ship.IsAuxEngineGo();
        if (go)
        {
            ship.AuxEngineCutoff();
        }
        else
        {
            ship.AuxEngineGo();
        }
        inputController.PropertyChanged("AuxOnButton", !go);
    }

    public void ToggleRCSFineControl()
    {
        bool fine = ship.IsRCSFineControlOn();
        if (fine)
        {
            ship.RCSFineControlOff();
        }
        else
        {
            ship.RCSFineControlOn();
        }
        inputController.PropertyChanged("RCSFineOnButton", !fine);
    }

    public void KillRot()
    {
        PlayerShip playerShip = gameController.GetPlayerShip();
        playerShip.ExecuteAutopilotCommand(Autopilot.Command.KILL_ROTATION);
    }

    private void Rendezvous()
    {
        autopilot.ExecuteCommand(Autopilot.Command.RENDEZVOUS, gameController.HUD().GetSelectedTarget());
        inputController.PropertyChanged("CommandExecuted", Autopilot.Command.RENDEZVOUS);
    }

    void OnTriggerEnter(Collider collider)
    {
        if (gameController != null)
        {
            GameObject otherBody = collider.attachedRigidbody.gameObject;
            float relVel;
            if (!PhysicsUtils.Fused(otherBody) && PhysicsUtils.ShouldBounce(gameObject, otherBody, out relVel))
            {
                if (relVel >= minRelVtoDamage)
                {
                    cameraController.PlayCollisionShake();
                    health--;
                }
                else if (otherBody.tag == "Dock")
                {
                    //PerformDock(otherBody); // called from collider
                }
                else
                {
                    cameraController.PlayCollisionShake();
                }
            }
            else
            {
                health = 0; // boom
                GameOverCollision(otherBody.name);
            }
        }
    }

    public void Dock(GameObject dockModel, bool withSound = true)
    {
        GameObject shipNbodyBody = transform.parent.gameObject;
        GameObject dockGhost = dockModel.transform.GetChild(0).gameObject;
        GameObject dockingPort = dockModel;
        inputController.ControlsEnabled = false;
        StopAllThrust();
        ship.NullSpin();
        gameController.GetComponent<MFDController>().DisarmForDocking();
        GameObject shipDockingPort = GetComponent<RocketShip>().DockingPort;
        gameController.Dock(shipNbodyBody, shipDockingPort, dockGhost, dockingPort);
        cameraController.PlayCollisionShake();
        if (withSound)
        {
            audioController.Play(FPSAudioController.AudioClipEnum.SPACESHIP_DOCK);
        }
    }

    public void Undock()
    {
        GameObject shipNBodyBody = NUtils.GetNBodyGameObject(gameObject);
        inputController.ControlsEnabled = true;
        gameController.Undock(shipNBodyBody);
        audioController.Play(FPSAudioController.AudioClipEnum.SPACESHIP_DOCK);
    }

    private void StopAllThrust()
    {
        if (ship.IsMainEngineGo())
        {
            ToggleEngine();
        }
        if (ship.IsAuxEngineGo())
        {
            ToggleAuxEngine();
        }
    }

    private void GameOverCollision(string otherName)
    {
        RemoveBody();
        HaltAudio();
        PlayExplosion();
        gameController.TransitionToGameOverFromDeath(otherName);
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

    public void ApplyRCSSpin(Quaternion q)
    {
        rotInput = true;
        ship.ApplyRCSSpin(q);
    }

}
