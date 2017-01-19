using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    private enum RotToggle
    {
        FACE,
        KILL,
        NML_POS,
        NML_NEG,
        POS,
        NEG,
        NONE
    }
    private Dictionary<RotToggle, ToggleButton> toggleMap = new Dictionary<RotToggle, ToggleButton>();

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

        toggleMap = new Dictionary<RotToggle, ToggleButton>()
        {
            { RotToggle.FACE, panel.transform.Search("FaceRotButton").GetComponent<ToggleButton>() },
            { RotToggle.KILL, panel.transform.Search("KillRotToggleButton").GetComponent<ToggleButton>() },
            { RotToggle.NML_POS, panel.transform.Search("RotNmlPosToggleButton").GetComponent<ToggleButton>() },
            { RotToggle.NML_NEG, panel.transform.Search("RotNmlNegToggleButton").GetComponent<ToggleButton>() },
            { RotToggle.POS, panel.transform.Search("RotPosToggleButton").GetComponent<ToggleButton>() },
            { RotToggle.NEG, panel.transform.Search("RotNegToggleButton").GetComponent<ToggleButton>() }
        };

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
        inputController.AddObserver("RCSModeAudio", this);
        inputController.AddObserver("CommandExecuted", this);

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

        MainOnButton.onClick.AddListener(delegate { if (inputController.ControlsEnabled) { gameController.GetPlayerShip().ToggleEngine(); } });
        AuxOnButton.onClick.AddListener(delegate { if (inputController.ControlsEnabled) { gameController.GetPlayerShip().ToggleAuxEngine(); } });
        RCSFineOnButton.onClick.AddListener(delegate { if (inputController.ControlsEnabled) { gameController.GetPlayerShip().ToggleRCSFineControl(); } });
        TranslateButton.onClick.AddListener(delegate { if (inputController.ControlsEnabled) { ToggleTranslate(); } });
        RotateButton.onClick.AddListener(delegate { if (inputController.ControlsEnabled) { ToggleRotate(); } });
        foreach (KeyValuePair<RotToggle, ToggleButton> tb in toggleMap)
        {
            RotToggle selected = tb.Key;
            tb.Value.onClick.AddListener(delegate (bool isToggled)
            {
                if (inputController.ControlsEnabled)
                {
                    RotToggle selectRot = selected;
                    ToggleButton b = toggleMap[selectRot];
                    if (b.isToggled)
                    {
                        ToggleToRot(selectRot);
                    }
                    else
                    {
                        ToggleToRot(RotToggle.NONE);
                    }
                }
            });
        }

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
        Circle2Kill.GetComponent<Button>().onClick.AddListener(delegate
        {
            if (inputController.ControlsEnabled)
            {
                gameController.GetPlayerShip().KillRot();
            }
        });
    }

    private void ToggleRCSModeFromKey()
    {
        switch (gameController.GetPlayerShip().currentRCSMode)
        {
            case PlayerShip.RCSMode.Rotate:
                gameController.GetPlayerShip().SetRCSMode(PlayerShip.RCSMode.Translate);
                break;
            case PlayerShip.RCSMode.Translate:
            default:
                gameController.GetPlayerShip().SetRCSMode(PlayerShip.RCSMode.Rotate);
                break;
        }
    }

    private void ToggleTranslate()
    {
        gameController.GetPlayerShip().SetRCSMode(PlayerShip.RCSMode.Translate);
    }

    private void ToggleRotate()
    {
        gameController.GetPlayerShip().SetRCSMode(PlayerShip.RCSMode.Rotate);
    }

    private void ToggleToRot(RotToggle t)
    {
        SetToggleButtons(t);
        switch (t)
        {
            case RotToggle.FACE:
                gameController.GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.FACE_TARGET);
                break;
            case RotToggle.KILL:
                gameController.GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.KILL_ROTATION);
                MarkKillRot();
                break;
            case RotToggle.NML_POS:
                gameController.GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.FACE_NML_POS);
                break;
            case RotToggle.NML_NEG:
                gameController.GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.FACE_NML_NEG);
                break;
            case RotToggle.POS:
                gameController.GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.FACE_POS);
                break;
            case RotToggle.NEG:
                gameController.GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.FACE_NEG);
                break;
            case RotToggle.NONE:
                gameController.GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.OFF);
                break;
        }
    }

    private RotToggle CommandToToggle(Autopilot.Command c)
    {
        RotToggle t;
        switch (c)
        {
            case Autopilot.Command.FACE_TARGET:
                t = RotToggle.FACE;
                break;
            case Autopilot.Command.KILL_ROTATION:
                t = RotToggle.KILL;
                break;
            case Autopilot.Command.FACE_NML_POS:
                t = RotToggle.NML_POS;
                break;
            case Autopilot.Command.FACE_NML_NEG:
                t = RotToggle.NML_NEG;
                break;
            case Autopilot.Command.FACE_POS:
                t = RotToggle.POS;
                break;
            case Autopilot.Command.FACE_NEG:
                t = RotToggle.NEG;
                break;
            default:
                t = RotToggle.NONE;
                break;
        }
        return t;
    }

    private void SetToggleButtons(RotToggle t)
    {
        foreach (KeyValuePair<RotToggle, ToggleButton> tb in toggleMap)
        {
            if (tb.Key == t)
            {
                tb.Value.isToggled = true;
            }
            else
            {
                tb.Value.isToggled = false;
            }
        }
    }

    private void Speak(string text)
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
        else if (name.StartsWith("Translate") || name.StartsWith("Rotate") || name.StartsWith("RCSModeAudio"))
        {
            HandleRCSModePropertyChanged(name, value);
        }
        else
        {
            HandleOtherPropertyChanged(name, value);
        }
    }

    private void HandleOtherPropertyChanged(string name, object value)
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
            case "CommandExecuted":
                Autopilot.Command? commandP = value as Autopilot.Command?;
                Autopilot.Command command = commandP == null ? Autopilot.Command.OFF : commandP.Value;
                RotToggle t = CommandToToggle(command);
                SetToggleButtons(t);
                if (command == Autopilot.Command.KILL_ROTATION)
                {
                    MarkKillRot();
                }
                else
                {
                    UnmarkKillRot();
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
                break;
            case "RotateButton":
                bool? rot = value as bool?;
                RotateButton.isToggled = rot != null ? rot.Value : false;
                break;
            case "RCSModeAudio":
                if (isPrimaryPanel)
                {
                    PlayerShip.RCSMode? mode = value as PlayerShip.RCSMode?;
                    if (mode != null && mode.Value == PlayerShip.RCSMode.Rotate)
                    {
                        Speak(DialogText.Rotation);
                    }
                    else if (mode != null && mode.Value == PlayerShip.RCSMode.Translate)
                    {
                        Speak(DialogText.Translation);
                    }
                }
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
        toggleMap[RotToggle.KILL].isToggled = true;
    }

    private void UnmarkKillRot()
    {
        Circle2Kill.GetChild(0).GetComponent<Image>().color = BUTTON_GREY;
        toggleMap[RotToggle.KILL].isToggled = false;
    }

    private bool warnedLow = false;

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
                if (!warnedLow && gameController.GetPlayerShip().IsLowFuel())
                {
                    Speak(DialogText.LowFuel);
                    warnedLow = true;
                }
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
        UpdateToggle("ToggleCamera", gameController.GetPlayerShip().ToggleCamera);
        if (inputController.ControlsEnabled)
        {
            UpdateToggle("RCSTranslateRotateToggle", ToggleRCSModeFromKey);
            UpdateToggle("MainEngineToggle", gameController.GetPlayerShip().ToggleEngine);
            UpdateToggle("AuxEngineToggle", gameController.GetPlayerShip().ToggleAuxEngine);
            UpdateToggle("KillRotation", ToggleKillRot);
            UpdateToggle("RotateToPos", ToggleRotPos);
            UpdateToggle("RotateToNeg", ToggleRotNeg);
            UpdateKeyInputTranslateRotate();
        }
    }

    private void UpdateKeyInputTranslateRotate()
    {
        switch (gameController.GetPlayerShip().currentRCSMode)
        {
            case PlayerShip.RCSMode.Rotate:
                UpdateKeyInputRotate();
                break;
            case PlayerShip.RCSMode.Translate:
                UpdateKeyInputTranslate();
                break;
        }
    }

    private Dictionary<string, bool> wasPressed = new Dictionary<string, bool>();

    delegate void ToggleFunc();

    private void UpdateToggle(string inputAxis, ToggleFunc toggle)
    {
        bool wasP;
        if (!wasPressed.TryGetValue(inputAxis, out wasP))
        {
            wasPressed[inputAxis] = false;
            wasP = false;
        }
        if (Input.GetAxisRaw(inputAxis) > 0 && !wasP)
        {
            wasPressed[inputAxis] = true;
            toggle();
        }
        else if (Input.GetAxisRaw(inputAxis) == 0 && wasP)
        {
            wasPressed[inputAxis] = false;
        }
    }

    private void UpdateAxis(string inputAxis, ToggleFunc toggleNegOn, ToggleFunc toggleNegOff, ToggleFunc togglePosOn, ToggleFunc togglePosOff)
    {
        string inputAxisNeg = inputAxis + "Neg";
        string inputAxisPos = inputAxis + "Pos";
        bool wasNeg;
        if (!wasPressed.TryGetValue(inputAxisNeg, out wasNeg))
        {
            wasPressed[inputAxisNeg] = false;
            wasNeg = false;
        }
        bool wasPos;
        if (!wasPressed.TryGetValue(inputAxisPos, out wasPos))
        {
            wasPressed[inputAxisPos] = false;
            wasPos = false;
        }
        if (Input.GetAxisRaw(inputAxis) < 0 && !wasNeg)
        {
            wasPressed[inputAxisNeg] = true;
            toggleNegOn();
        }
        else if (Input.GetAxisRaw(inputAxis) == 0 && wasNeg)
        {
            wasPressed[inputAxisNeg] = false;
            toggleNegOff();
        }
        if (Input.GetAxisRaw(inputAxis) > 0 && !wasPos)
        {
            wasPressed[inputAxisPos] = true;
            togglePosOn();
        }
        else if (Input.GetAxisRaw(inputAxis) == 0 && wasPos)
        {
            wasPressed[inputAxisPos] = false;
            togglePosOff();
        }
    }

    private void UpdateHoldAxis(string inputAxis, ToggleFunc toggleNegOn, ToggleFunc toggleNegOff, ToggleFunc togglePosOn, ToggleFunc togglePosOff)
    {
        string inputAxisNeg = inputAxis + "Neg";
        string inputAxisPos = inputAxis + "Pos";
        bool wasNeg;
        if (!wasPressed.TryGetValue(inputAxisNeg, out wasNeg))
        {
            wasPressed[inputAxisNeg] = false;
            wasNeg = false;
        }
        bool wasPos;
        if (!wasPressed.TryGetValue(inputAxisPos, out wasPos))
        {
            wasPressed[inputAxisPos] = false;
            wasPos = false;
        }
        if (Input.GetAxisRaw(inputAxis) < 0)
        {
            wasPressed[inputAxisNeg] = true;
            toggleNegOn();
        }
        else if (Input.GetAxisRaw(inputAxis) == 0 && wasNeg)
        {
            wasPressed[inputAxisNeg] = false;
            toggleNegOff();
        }
        if (Input.GetAxisRaw(inputAxis) > 0)
        {
            wasPressed[inputAxisPos] = true;
            togglePosOn();
        }
        else if (Input.GetAxisRaw(inputAxis) == 0 && wasPos)
        {
            wasPressed[inputAxisPos] = false;
            togglePosOff();
        }
    }

    private void ToggleKillRot()
    {
        if (gameController.GetPlayerShip().CurrentAutopilotCommand() != Autopilot.Command.KILL_ROTATION)
        {
            gameController.GetPlayerShip().KillRot();
        }
    }

    private void ToggleRotPos()
    {
        if (gameController.GetPlayerShip().CurrentAutopilotCommand() != Autopilot.Command.FACE_POS)
        {
            gameController.GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.FACE_POS);
        }
    }

    private void ToggleRotNeg()
    {
        if (gameController.GetPlayerShip().CurrentAutopilotCommand() != Autopilot.Command.FACE_NEG)
        {
            gameController.GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.FACE_NEG);
        }
    }

    private void UpdateKeyInputTranslate()
    {
        UpdateAxis("RCSTranslateZ",
            delegate { inputController.PropertyChanged("Circle1Aft_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle1Aft_OnPointerUp", null); },
            delegate { inputController.PropertyChanged("Circle1Fore_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle1Fore_OnPointerUp", null); }
        );
        UpdateAxis("RCSTranslateY",
            delegate { inputController.PropertyChanged("Circle1Down_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle1Down_OnPointerUp", null); },
            delegate { inputController.PropertyChanged("Circle1Up_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle1Up_OnPointerUp", null); }
        );
        UpdateAxis("RCSTranslateX",
            delegate { inputController.PropertyChanged("Circle1Left_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle1Left_OnPointerUp", null); },
            delegate { inputController.PropertyChanged("Circle1Right_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle1Right_OnPointerUp", null); }
        );
    }

    private void UpdateKeyInputRotate()
    {
        UpdateHoldAxis("RCSRoll",
            delegate { inputController.PropertyChanged("Circle2CounterClock_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle2CounterClock_OnPointerUp", null); },
            delegate { inputController.PropertyChanged("Circle2Clock_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle2Clock_OnPointerUp", null); }
        );
        UpdateHoldAxis("RCSPitch",
            delegate { inputController.PropertyChanged("Circle2Down_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle2Down_OnPointerUp", null); },
            delegate { inputController.PropertyChanged("Circle2Up_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle2Up_OnPointerUp", null); }
        );
        UpdateHoldAxis("RCSYaw",
            delegate { inputController.PropertyChanged("Circle2Left_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle2Left_OnPointerUp", null); },
            delegate { inputController.PropertyChanged("Circle2Right_OnPointerDown", null); },
            delegate { inputController.PropertyChanged("Circle2Right_OnPointerUp", null); }
        );
    }

    private void UpdateTranslate()
    {
        PlayerShip playerShip = gameController.GetPlayerShip();
        if (translatePointerDown)
        {
            if (playerShip.CurrentAutopilotCommand() != Autopilot.Command.OFF)
            {
                playerShip.ExecuteAutopilotCommand(Autopilot.Command.OFF);
            }
            playerShip.RCSBurst(translateVec);
            playerShip.SetRCSMode(PlayerShip.RCSMode.Translate);
        }
        if (translatePointerUp)
        {
            playerShip.RCSCutoff();
            translatePointerUp = false;
        }
    }

    private void UpdateRotate()
    {
        PlayerShip playerShip = gameController.GetPlayerShip();
        if (rotatePointerDown)
        {
            if (playerShip.CurrentAutopilotCommand() != Autopilot.Command.OFF)
            {
                playerShip.ExecuteAutopilotCommand(Autopilot.Command.OFF);
            }
            playerShip.SetRCSMode(PlayerShip.RCSMode.Rotate);
            float s = playerShip.GetComponent<RocketShip>().CurrentRCSAngularDegPerSec();
            Vector3 v = s * rotateVec.normalized;
            Vector3 rel = playerShip.transform.TransformDirection(v);
            Quaternion q = Quaternion.Euler(rel);
            Quaternion deltaQ = q;
            Quaternion p = playerShip.GetComponent<RocketShip>().QToSpinDelta(deltaQ);
            playerShip.UserRCSSpinInput(p);
        }
        if (playerShip.CurrentAutopilotCommand() != Autopilot.Command.KILL_ROTATION && IsKillRotMarked())
        {
            UnmarkKillRot();
        }

    }

}


