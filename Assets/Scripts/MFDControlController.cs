using UnityEngine;
using UnityEngine.UI;

public class MFDControlController : IPropertyChangeObserver
{
    private GameObject panel;
    private GameController gameController;
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

    private Slider FuelSlider;
    private Text FuelRemainingText;

    private Vector3 translateVec = Vector3.zero;
    private bool translatePointerDown = false;
    private bool translatePointerUp = false;

    public void Connect(GameObject controlPanel, InputController inputController, GameController game)
    {
        panel = controlPanel;
        gameController = game;
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

        inputController.AddObserver("FuelSlider", this);
        inputController.AddObserver("FuelRemainingText", this);

        MainOnButton.onClick.AddListener(delegate { gameController.GetPlayerShip().ToggleEngine(); });
        AuxOnButton.onClick.AddListener(delegate { gameController.GetPlayerShip().ToggleAuxEngine(); });
        RCSFineOnButton.onClick.AddListener(delegate { gameController.GetPlayerShip().ToggleRCSFineControl(); });
        TranslateButton.onClick.AddListener(delegate { gameController.GetPlayerShip().ToggleRCSMode(); });
        RotateButton.onClick.AddListener(delegate { gameController.GetPlayerShip().ToggleRCSMode(); });
        Circle1Up.GetComponent<RCSButton>().inputController = inputController;
        Circle1Down.GetComponent<RCSButton>().inputController = inputController;
        Circle1Left.GetComponent<RCSButton>().inputController = inputController;
        Circle1Right.GetComponent<RCSButton>().inputController = inputController;
        Circle1Aft.GetComponent<RCSButton>().inputController = inputController;
        Circle1Fore.GetComponent<RCSButton>().inputController = inputController;
    }

    public void Speak(string text)
    {
        gameController.GetComponent<MFDController>().Speak(text);
    }

    public void PropertyChanged(string name, object value)
    {
        switch (name)
        {
            case "MainOnButton":
                bool? thrust = value as bool?;
                MainOnButton.isToggled = thrust != null ? thrust.Value : false;
                MainOnButton.transform.GetChild(0).GetComponent<Text>().text = MainOnButton.isToggled ? "ON" : "OFF";
                string mainOnText = MainOnButton.isToggled ? DialogText.MainEngineOn : DialogText.MainEngineOff;
                Speak(mainOnText);
                break;
            case "AuxOnButton":
                bool? auxThrust = value as bool?;
                AuxOnButton.isToggled = auxThrust != null ? auxThrust.Value : false;
                AuxOnButton.transform.GetChild(0).GetComponent<Text>().text = AuxOnButton.isToggled ? "ON" : "OFF";
                string auxOnText = AuxOnButton.isToggled ? DialogText.AuxEngineOn : DialogText.AuxEngineOff;
                Speak(auxOnText);
                break;
            case "RCSFineOnButton":
                bool? rcsFine = value as bool?;
                RCSFineOnButton.isToggled = rcsFine != null ? rcsFine.Value : false;
                RCSFineOnButton.transform.GetChild(0).GetComponent<Text>().text = RCSFineOnButton.isToggled ? "ON" : "OFF";
                string rcsFineText = RCSFineOnButton.isToggled ? DialogText.RCSFineControlOn : DialogText.RCSFineControlOff;
                Speak(rcsFineText);
                break;
            case "TranslateButton":
                bool? rot2 = value as bool?;
                TranslateButton.isToggled = rot2 != null ? rot2.Value : false;
                if (TranslateButton.isToggled) { Speak(DialogText.Translation); }
                break;
            case "RotateButton":
                bool? rot = value as bool?;
                RotateButton.isToggled = rot != null ? rot.Value : false;
                if (RotateButton.isToggled) { Speak(DialogText.Rotation); }
                break;
            case "TranslateButtonNoAudio":
                bool? rot2a = value as bool?;
                TranslateButton.isToggled = rot2a != null ? rot2a.Value : false;
                break;
            case "RotateButtonNoAudio":
                bool? rota = value as bool?;
                RotateButton.isToggled = rota != null ? rota.Value : false;
                break;

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
        PlayerShip playerShip = gameController.GetPlayerShip();
        RocketShip ship = playerShip.RocketShip();

        if (translatePointerDown)
        {
            ship.RCSBurst(translateVec, ship.RCSBurnMinSec);
        }
        if (translatePointerUp)
        {
            ship.RCSCutoff();
            translatePointerUp = false;
        }
    }
}


