using UnityEngine;
using UnityEngine.UI;

public class MFDControlController : IPropertyChangeObserver
{
    private GameObject panel;
    private GameController gameController;
    private InputController inputController;
    private ToggleButton MainOnButton;
    private ToggleButton AuxOnButton;
    private ToggleButton RCSFineOnButton;
    private ToggleButton TranslateButton;
    private ToggleButton RotateButton;

    private Transform Circle1Up;
    private Transform Circle1Down;
    private Transform Circle1Left;
    private Transform Circle1Right;
    private Transform Circle1Aft;
    private Transform Circle1Fore;

    private Transform Circle2Up;
    private Transform Circle2Down;
    private Transform Circle2Left;
    private Transform Circle2Right;
    private Transform Circle2CounterClock;
    private Transform Circle2Clock;

    private Transform Circle2Kill;

    private Slider FuelSlider;
    private Text FuelRemainingText;

    private Vector3 translateVec = Vector3.zero;
    private bool translatePointerDown = false;
    private bool translatePointerUp = false;

    private Vector3 rotateVec = Vector3.zero;
    private bool rotatePointerDown = false;
    private bool isPrimaryPanel = false;

    private enum RCSMode { Rotate, Translate };
    private RCSMode currentRCSMode;

    public void Connect(GameObject controlPanel, InputController input, GameController game, bool panelIsPrimaryPanel)
    {
        panel = controlPanel;
        gameController = game;
        inputController = input;
        isPrimaryPanel = panelIsPrimaryPanel;

        MainOnButton = panel.transform.Search("MainOnButton").GetComponent<ToggleButton>();
        AuxOnButton = panel.transform.Search("AuxOnButton").GetComponent<ToggleButton>();
        RCSFineOnButton = panel.transform.Search("RCSFineOnButton").GetComponent<ToggleButton>();
        TranslateButton = panel.transform.Search("TranslateButton").GetComponent<ToggleButton>();
        RotateButton = panel.transform.Search("RotateButton").GetComponent<ToggleButton>();

        Circle1Up = panel.transform.Search("Circle1Up");
        Circle1Down = panel.transform.Search("Circle1Down");
        Circle1Left = panel.transform.Search("Circle1Left");
        Circle1Right = panel.transform.Search("Circle1Right");
        Circle1Aft = panel.transform.Search("Circle1Aft");
        Circle1Fore = panel.transform.Search("Circle1Fore");

        Circle2Up = panel.transform.Search("Circle2Up");
        Circle2Down = panel.transform.Search("Circle2Down");
        Circle2Left = panel.transform.Search("Circle2Left");
        Circle2Right = panel.transform.Search("Circle2Right");
        Circle2CounterClock = panel.transform.Search("Circle2CounterClock");
        Circle2Clock = panel.transform.Search("Circle2Clock");

        Circle2Kill = panel.transform.Search("Circle2Kill");

        FuelSlider = panel.transform.Search("FuelSlider").GetComponent<Slider>();
        FuelRemainingText = panel.transform.Search("FuelRemainingText").GetComponent<Text>();

        inputController.AddObserver("MainOnButton", this);
        inputController.AddObserver("AuxOnButton", this);
        inputController.AddObserver("RCSFineOnButton", this);
        inputController.AddObserver("TranslateButton", this);
        inputController.AddObserver("RotateButton", this);
        inputController.AddObserver("TranslateButtonNoAudio", this);
        inputController.AddObserver("RotateButtonNoAudio", this);

        inputController.AddObserver("Circle1Up_OnPointerDown", this);
        inputController.AddObserver("Circle1Up_OnPointerUp", this);
        inputController.AddObserver("Circle1Down_OnPointerDown", this);
        inputController.AddObserver("Circle1Down_OnPointerUp", this);
        inputController.AddObserver("Circle1Left_OnPointerDown", this);
        inputController.AddObserver("Circle1Left_OnPointerUp", this);
        inputController.AddObserver("Circle1Right_OnPointerDown", this);
        inputController.AddObserver("Circle1Right_OnPointerUp", this);
        inputController.AddObserver("Circle1Aft_OnPointerDown", this);
        inputController.AddObserver("Circle1Aft_OnPointerUp", this);
        inputController.AddObserver("Circle1Fore_OnPointerDown", this);
        inputController.AddObserver("Circle1Fore_OnPointerUp", this);

        inputController.AddObserver("Circle2Up_OnPointerDown", this);
        inputController.AddObserver("Circle2Up_OnPointerUp", this);
        inputController.AddObserver("Circle2Down_OnPointerDown", this);
        inputController.AddObserver("Circle2Down_OnPointerUp", this);
        inputController.AddObserver("Circle2Left_OnPointerDown", this);
        inputController.AddObserver("Circle2Left_OnPointerUp", this);
        inputController.AddObserver("Circle2Right_OnPointerDown", this);
        inputController.AddObserver("Circle2Right_OnPointerUp", this);
        inputController.AddObserver("Circle2CounterClock_OnPointerDown", this);
        inputController.AddObserver("Circle2CounterClock_OnPointerUp", this);
        inputController.AddObserver("Circle2Clock_OnPointerDown", this);
        inputController.AddObserver("Circle2Clock_OnPointerUp", this);

        inputController.AddObserver("Circle2Kill_OnClick", this);

        inputController.AddObserver("FuelSlider", this);
        inputController.AddObserver("FuelRemainingText", this);

        MainOnButton.onClick.AddListener(delegate { gameController.GetPlayerShip().ToggleEngine(); });
        AuxOnButton.onClick.AddListener(delegate { gameController.GetPlayerShip().ToggleAuxEngine(); });
        RCSFineOnButton.onClick.AddListener(delegate { gameController.GetPlayerShip().ToggleRCSFineControl(); });
        TranslateButton.onClick.AddListener(delegate { ToggleRCSMode(); });
        RotateButton.onClick.AddListener(delegate { ToggleRCSMode(); });
        Circle1Up.GetComponent<RCSButton>().inputController = inputController;
        Circle1Down.GetComponent<RCSButton>().inputController = inputController;
        Circle1Left.GetComponent<RCSButton>().inputController = inputController;
        Circle1Right.GetComponent<RCSButton>().inputController = inputController;
        Circle1Aft.GetComponent<RCSButton>().inputController = inputController;
        Circle1Fore.GetComponent<RCSButton>().inputController = inputController;
        Circle2Up.GetComponent<RCSButton>().inputController = inputController;
        Circle2Down.GetComponent<RCSButton>().inputController = inputController;
        Circle2Left.GetComponent<RCSButton>().inputController = inputController;
        Circle2Right.GetComponent<RCSButton>().inputController = inputController;
        Circle2CounterClock.GetComponent<RCSButton>().inputController = inputController;
        Circle2Clock.GetComponent<RCSButton>().inputController = inputController;
        Circle2Kill.GetComponent<Button>().onClick.AddListener(delegate {
            gameController.GetPlayerShip().KillRot();
            inputController.PropertyChanged("Circle2Kill_OnClick", null);
        });

        currentRCSMode = RCSMode.Rotate;
    }

    public void ToggleRCSMode()
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
        inputController.PropertyChanged("TranslateButton", currentRCSMode == RCSMode.Translate);
        inputController.PropertyChanged("RotateButton", currentRCSMode == RCSMode.Rotate);
    }

    public void Speak(string text)
    {
        gameController.GetComponent<MFDController>().Speak(text);
    }

    public void PropertyChanged(string name, object value)
    {
        if (name.StartsWith("Circle1"))
        {
            HandleTranslateCirclePropertyChanged(name, value);
        }
        if (name.StartsWith("Circle2"))
        {
            HandleRotateCirclePropertyChanged(name, value);
        }
        else if (name.StartsWith("Fuel"))
        {
            HandleFuelPropertyChanged(name, value);
        }
        else if (name.StartsWith("Translate") || name.StartsWith("Rotate"))
        {
            HandleRCSModePropertyChanged(name, value);
        }
        else
        {
            HandleEnginePropertyChanged(name, value);
        }
    }

    private void HandleEnginePropertyChanged(string name, object value)
    {
        switch (name)
        {
            case "MainOnButton":
                bool? thrust = value as bool?;
                MainOnButton.isToggled = thrust != null ? thrust.Value : false;
                MainOnButton.transform.GetChild(0).GetComponent<Text>().text = MainOnButton.isToggled ? "ON" : "OFF";
                string mainOnText = MainOnButton.isToggled ? DialogText.MainEngineOn : DialogText.MainEngineOff;
                if (isPrimaryPanel)
                {
                    Speak(mainOnText);
                }
                break;
            case "AuxOnButton":
                bool? auxThrust = value as bool?;
                AuxOnButton.isToggled = auxThrust != null ? auxThrust.Value : false;
                AuxOnButton.transform.GetChild(0).GetComponent<Text>().text = AuxOnButton.isToggled ? "ON" : "OFF";
                string auxOnText = AuxOnButton.isToggled ? DialogText.AuxEngineOn : DialogText.AuxEngineOff;
                if (isPrimaryPanel)
                {
                    Speak(auxOnText);
                }
                break;
            case "RCSFineOnButton":
                bool? rcsFine = value as bool?;
                RCSFineOnButton.isToggled = rcsFine != null ? rcsFine.Value : false;
                RCSFineOnButton.transform.GetChild(0).GetComponent<Text>().text = RCSFineOnButton.isToggled ? "ON" : "OFF";
                string rcsFineText = RCSFineOnButton.isToggled ? DialogText.RCSFineControlOn : DialogText.RCSFineControlOff;
                if (isPrimaryPanel)
                {
                    Speak(rcsFineText);
                }
                break;
        }
    }

    private void HandleRCSModePropertyChanged(string name, object value)
    {
        switch (name)
        {
            case "TranslateButton":
                bool? rot2 = value as bool?;
                TranslateButton.isToggled = rot2 != null ? rot2.Value : false;
                if (TranslateButton.isToggled && isPrimaryPanel) { Speak(DialogText.Translation); }
                break;
            case "RotateButton":
                bool? rot = value as bool?;
                RotateButton.isToggled = rot != null ? rot.Value : false;
                if (RotateButton.isToggled && isPrimaryPanel) { Speak(DialogText.Rotation); }
                break;
            case "TranslateButtonNoAudio":
                bool? rot2a = value as bool?;
                TranslateButton.isToggled = rot2a != null ? rot2a.Value : false;
                break;
            case "RotateButtonNoAudio":
                bool? rota = value as bool?;
                RotateButton.isToggled = rota != null ? rota.Value : false;
                break;
        }
    }

    private void HandleTranslateCirclePropertyChanged(string name, object value)
    {
        switch (name)
        {
            case "Circle1Up_OnPointerDown":
                Circle1Up.GetComponent<ToggleButton>().isToggled = true;
                translateVec = gameController.GetPlayerShip().transform.up;
                translatePointerUp = false;
                translatePointerDown = true;
                break;
            case "Circle1Up_OnPointerUp":
                Circle1Up.GetComponent<ToggleButton>().isToggled = false;
                translatePointerDown = false;
                translatePointerUp = true;
                break;
            case "Circle1Down_OnPointerDown":
                Circle1Down.GetComponent<ToggleButton>().isToggled = true;
                translateVec = -gameController.GetPlayerShip().transform.up;
                translatePointerUp = false;
                translatePointerDown = true;
                break;
            case "Circle1Down_OnPointerUp":
                Circle1Down.GetComponent<ToggleButton>().isToggled = false;
                translatePointerDown = false;
                translatePointerUp = true;
                break;
            case "Circle1Left_OnPointerDown":
                Circle1Left.GetComponent<ToggleButton>().isToggled = true;
                translateVec = -gameController.GetPlayerShip().transform.right;
                translatePointerUp = false;
                translatePointerDown = true;
                break;
            case "Circle1Left_OnPointerUp":
                Circle1Left.GetComponent<ToggleButton>().isToggled = false;
                translatePointerDown = false;
                translatePointerUp = true;
                break;
            case "Circle1Right_OnPointerDown":
                Circle1Right.GetComponent<ToggleButton>().isToggled = true;
                translateVec = gameController.GetPlayerShip().transform.right;
                translatePointerUp = false;
                translatePointerDown = true;
                break;
            case "Circle1Right_OnPointerUp":
                Circle1Right.GetComponent<ToggleButton>().isToggled = false;
                translatePointerDown = false;
                translatePointerUp = true;
                break;
            case "Circle1Aft_OnPointerDown":
                Circle1Aft.GetComponent<ToggleButton>().isToggled = true;
                Circle1Aft.GetChild(0).GetComponent<Image>().color = HUD_GREEN;
                translateVec = -gameController.GetPlayerShip().transform.forward;
                translatePointerUp = false;
                translatePointerDown = true;
                break;
            case "Circle1Aft_OnPointerUp":
                Circle1Aft.GetComponent<ToggleButton>().isToggled = false;
                Circle1Aft.GetChild(0).GetComponent<Image>().color = BUTTON_GREY;
                translatePointerDown = false;
                translatePointerUp = true;
                break;
            case "Circle1Fore_OnPointerDown":
                Circle1Fore.GetComponent<ToggleButton>().isToggled = true;
                Circle1Fore.GetChild(0).GetComponent<Image>().color = HUD_GREEN;
                translateVec = gameController.GetPlayerShip().transform.forward;
                translatePointerUp = false;
                translatePointerDown = true;
                break;
            case "Circle1Fore_OnPointerUp":
                Circle1Fore.GetComponent<ToggleButton>().isToggled = false;
                Circle1Fore.GetChild(0).GetComponent<Image>().color = BUTTON_GREY;
                translatePointerDown = false;
                translatePointerUp = true;
                break;
        }
    }

    private void HandleRotateCirclePropertyChanged(string name, object value)
    {
        switch (name)
        {
            case "Circle2Up_OnPointerDown":
                Circle2Up.GetComponent<ToggleButton>().isToggled = true;
                rotateVec = new Vector3(-1, 0, 0);
                rotatePointerDown = true;
                break;
            case "Circle2Up_OnPointerUp":
                Circle2Up.GetComponent<ToggleButton>().isToggled = false;
                rotatePointerDown = false;
                break;
            case "Circle2Down_OnPointerDown":
                Circle2Down.GetComponent<ToggleButton>().isToggled = true;
                rotateVec = new Vector3(1, 0, 0);
                rotatePointerDown = true;
                break;
            case "Circle2Down_OnPointerUp":
                Circle2Down.GetComponent<ToggleButton>().isToggled = false;
                rotatePointerDown = false;
                break;
            case "Circle2Left_OnPointerDown":
                Circle2Left.GetComponent<ToggleButton>().isToggled = true;
                rotateVec = new Vector3(0, -1, 0);
                rotatePointerDown = true;
                break;
            case "Circle2Left_OnPointerUp":
                Circle2Left.GetComponent<ToggleButton>().isToggled = false;
                rotatePointerDown = false;
                break;
            case "Circle2Right_OnPointerDown":
                Circle2Right.GetComponent<ToggleButton>().isToggled = true;
                rotateVec = new Vector3(0, 1, 0);
                rotatePointerDown = true;
                break;
            case "Circle2Right_OnPointerUp":
                Circle2Right.GetComponent<ToggleButton>().isToggled = false;
                rotatePointerDown = false;
                break;
            case "Circle2CounterClock_OnPointerDown":
                Circle2CounterClock.GetComponent<ToggleButton>().isToggled = true;
                Circle2CounterClock.GetChild(0).GetComponent<Image>().color = HUD_GREEN;
                rotateVec = new Vector3(0, 0, 1);
                rotatePointerDown = true;
                break;
            case "Circle2CounterClock_OnPointerUp":
                Circle2CounterClock.GetComponent<ToggleButton>().isToggled = false;
                Circle2CounterClock.GetChild(0).GetComponent<Image>().color = BUTTON_GREY;
                rotatePointerDown = false;
                break;
            case "Circle2Clock_OnPointerDown":
                Circle2Clock.GetComponent<ToggleButton>().isToggled = true;
                Circle2Clock.GetChild(0).GetComponent<Image>().color = HUD_GREEN;
                rotateVec = new Vector3(0, 0, -1);
                rotatePointerDown = true;
                break;
            case "Circle2Clock_OnPointerUp":
                Circle2Clock.GetComponent<ToggleButton>().isToggled = false;
                Circle2Clock.GetChild(0).GetComponent<Image>().color = BUTTON_GREY;
                rotatePointerDown = false;
                break;
            case "Circle2Kill_OnClick":
                if (!IsKillRotMarked())
                {
                    MarkKillRot();
                }
                rotatePointerDown = false;
                break;
        }
    }
 
    private bool IsKillRotMarked()
    {
        return Circle2Kill.GetChild(0).GetComponent<Image>().color != BUTTON_GREY;
    }

    private void MarkKillRot()
    {
        Circle2Kill.GetChild(0).GetComponent<Image>().color = HUD_GREEN;
        if (currentRCSMode != RCSMode.Rotate)
        {
            ToggleRCSMode();
        }
    }

    private void UnmarkKillRot()
    {
        Circle2Kill.GetChild(0).GetComponent<Image>().color = BUTTON_GREY;
    }

    private void HandleFuelPropertyChanged(string name, object value)
    {
        switch (name)
        {
            case "FuelSlider":
                float? fuel = value as float?;
                FuelSlider.value = fuel != null ? fuel.Value : 0;
                break;
            case "FuelRemainingText":
                FuelRemainingText.text = value as string;
                if (gameController.GetPlayerShip().IsLowFuel()) { Speak(DialogText.LowFuel); }
                break;
        }
    }

    private static Color BUTTON_GREY = new Color32(0x71, 0x71, 0x71, 0xFF);
    private static Color HUD_GREEN = new Color32(0x03, 0xB8, 0x15, 0xDA);

    public void Update()
    {
        if (inputController != null)
        {
            if (isPrimaryPanel)
            {
                UpdateKeyInput();
            }
            UpdateTranslate();
            UpdateRotate();
        }
    }

    private void UpdateKeyInput()
    {
        UpdateEngineInput();
        UpdateCameraInput();
        UpdateKeyInputToggleMode();
        switch (currentRCSMode)
        {
            case RCSMode.Rotate:
                UpdateKeyInputRotate();
                break;
            case RCSMode.Translate:
                UpdateKeyInputTranslate();
                break;
        }
        UpdateKeyInputKillRot();
    }

    private void UpdateEngineInput()
    {
        //      UpdateDoubleTap(KeyCode.KeypadEnter, ref doubleTapEngineTimer, ToggleEngine, Rendezvous);
        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            gameController.GetPlayerShip().ToggleEngine();
        }
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            gameController.GetPlayerShip().ToggleAuxEngine();
        }
    }

    private void UpdateCameraInput()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            gameController.GetPlayerShip().ToggleCamera();
        }
    }


    private void UpdateKeyInputToggleMode()
    {
        if (Input.GetKeyDown(KeyCode.KeypadDivide))
        {
            ToggleRCSMode();
        }
    }

    private void UpdateKeyInputKillRot()
    {
        if (Input.GetKeyDown(KeyCode.Keypad5))
        {
            if (gameController.GetPlayerShip().CurrentAutopilotCommand() != Autopilot.Command.KILL_ROTATION)
            {
                gameController.GetPlayerShip().KillRot();
                inputController.PropertyChanged("Circle2Kill_OnClick", null);
            }
        }
    }

    private void UpdateKeyInputTranslate()
    {
        if (Input.GetKeyDown(KeyCode.Keypad6))
        {
            inputController.PropertyChanged("Circle1Fore_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad6))
        {
            inputController.PropertyChanged("Circle1Fore_OnPointerUp", null);
        }

        if (Input.GetKeyDown(KeyCode.Keypad9))
        {
            inputController.PropertyChanged("Circle1Aft_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad9))
        {
            inputController.PropertyChanged("Circle1Aft_OnPointerUp", null);
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            inputController.PropertyChanged("Circle1Up_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad2))
        {
            inputController.PropertyChanged("Circle1Up_OnPointerUp", null);
        }

        if (Input.GetKeyDown(KeyCode.Keypad8))
        {
            inputController.PropertyChanged("Circle1Down_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad8))
        {
            inputController.PropertyChanged("Circle1Down_OnPointerUp", null);
        }

        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            inputController.PropertyChanged("Circle1Left_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad1))
        {
            inputController.PropertyChanged("Circle1Left_OnPointerUp", null);
        }

        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            inputController.PropertyChanged("Circle1Right_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad3))
        {
            inputController.PropertyChanged("Circle1Right_OnPointerUp", null);
        }
    }

    private void UpdateKeyInputRotate()
    {
        if (Input.GetKeyDown(KeyCode.Keypad6))
        {
            inputController.PropertyChanged("Circle2Clock_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad6))
        {
            inputController.PropertyChanged("Circle2Clock_OnPointerUp", null);
        }

        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            inputController.PropertyChanged("Circle2CounterClock_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad4))
        {
            inputController.PropertyChanged("Circle2CounterClock_OnPointerUp", null);
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            inputController.PropertyChanged("Circle2Up_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad2))
        {
            inputController.PropertyChanged("Circle2Up_OnPointerUp", null);
        }

        if (Input.GetKeyDown(KeyCode.Keypad8))
        {
            inputController.PropertyChanged("Circle2Down_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad8))
        {
            inputController.PropertyChanged("Circle2Down_OnPointerUp", null);
        }

        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            inputController.PropertyChanged("Circle2Left_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad1))
        {
            inputController.PropertyChanged("Circle2Left_OnPointerUp", null);
        }

        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            inputController.PropertyChanged("Circle2Right_OnPointerDown", null);
        }
        else if (Input.GetKeyUp(KeyCode.Keypad3))
        {
            inputController.PropertyChanged("Circle2Right_OnPointerUp", null);
        }
    }

    private void UpdateTranslate()
    {
        PlayerShip playerShip = gameController.GetPlayerShip();
        RocketShip ship = playerShip.RocketShip();
        if (translatePointerDown)
        {
            playerShip.ExecuteAutopilotCommand(Autopilot.Command.OFF);
            ship.RCSBurst(translateVec, ship.RCSBurnMinSec);
            if (currentRCSMode != RCSMode.Translate)
            {
                ToggleRCSMode();
            }
        }
        if (translatePointerUp)
        {
            ship.RCSCutoff();
            translatePointerUp = false;
        }
    }

    private void UpdateRotate()
    {
        PlayerShip playerShip = gameController.GetPlayerShip();
        RocketShip ship = playerShip.RocketShip();
        if (rotatePointerDown)
        {
            Quaternion q = Quaternion.Euler(rotateVec.x, rotateVec.y, rotateVec.z);
            playerShip.ExecuteAutopilotCommand(Autopilot.Command.OFF);
            playerShip.ApplyRCSSpin(q);
            if (currentRCSMode != RCSMode.Rotate)
            {
                ToggleRCSMode();
            }
        }
        if (playerShip.CurrentAutopilotCommand() != Autopilot.Command.KILL_ROTATION && IsKillRotMarked())
        {
            UnmarkKillRot();
        }

    }

}


