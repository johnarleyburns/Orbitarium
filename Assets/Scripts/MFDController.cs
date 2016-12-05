using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MFDController : MonoBehaviour
{
    public Transform FPSCanvas;
    public GameObject MFDPanel;
    public Dropdown MFDDropdown;
    public GameObject MFDInnerPanel;
    public GameObject MFDPanel2;
    public Dropdown MFDDropdown2;
    public GameObject MFDInnerPanel2;
    public GameObject MFDControlPanelPrefab;
    public GameObject MFDAutopilotPanelPrefab;
    public GameObject MFDWeaponPanelPrefab;
    public GameObject MFDDockingPanelPrefab;

    private GameObject MFDControlPanel;
    private GameObject MFDAutopilotPanel;
    private GameObject MFDWeaponsPanel;
    private GameObject MFDDockingPanel;
    private GameObject MFDControlPanel2;
    private GameObject MFDAutopilotPanel2;
    private GameObject MFDWeaponsPanel2;
    private GameObject MFDDockingPanel2;
    private MFDControlController MFDControlPanelController;
    private MFDAutopilotController MFDAutopilotPanelController;
    private MFDWeaponsController MFDWeaponsPanelController;
    private MFDDockingController MFDDockingPanelController;
    private MFDControlController MFDControlPanelController2;
    private MFDAutopilotController MFDAutopilotPanelController2;
    private MFDWeaponsController MFDWeaponsPanelController2;
    private MFDDockingController MFDDockingPanelController2;

    protected GameController gameController;
    protected InputController inputController;

    // Use this for initialization
    void Awake()
    {
        gameController = GetComponent<GameController>();
        inputController = GetComponent<InputController>();

        MFDControlPanel = (GameObject)Instantiate(MFDControlPanelPrefab, MFDInnerPanel.transform);
        MFDAutopilotPanel = (GameObject)Instantiate(MFDAutopilotPanelPrefab, MFDInnerPanel.transform);
        MFDWeaponsPanel = (GameObject)Instantiate(MFDWeaponPanelPrefab, MFDInnerPanel.transform);
        MFDDockingPanel = (GameObject)Instantiate(MFDDockingPanelPrefab, MFDInnerPanel.transform);
        MFDControlPanel2 = (GameObject)Instantiate(MFDControlPanelPrefab, MFDInnerPanel2.transform);
        MFDAutopilotPanel2 = (GameObject)Instantiate(MFDAutopilotPanelPrefab, MFDInnerPanel2.transform);
        MFDWeaponsPanel2 = (GameObject)Instantiate(MFDWeaponPanelPrefab, MFDInnerPanel2.transform);
        MFDDockingPanel2 = (GameObject)Instantiate(MFDDockingPanelPrefab, MFDInnerPanel2.transform);

        int dropdownHeight = 30;
        Vector3 pos = MFDControlPanel.transform.localPosition;
        MFDControlPanel.transform.localPosition = new Vector3(pos.x, pos.y + dropdownHeight, pos.z);
        MFDAutopilotPanel.transform.localPosition = new Vector3(pos.x, pos.y + dropdownHeight, pos.z);
        MFDWeaponsPanel.transform.localPosition = new Vector3(pos.x, pos.y + dropdownHeight, pos.z);
        MFDDockingPanel.transform.localPosition = new Vector3(pos.x, pos.y + dropdownHeight, pos.z);
        Vector3 pos2 = MFDControlPanel2.transform.localPosition;
        MFDControlPanel2.transform.localPosition = new Vector3(pos.x, pos2.y + dropdownHeight, pos2.z);
        MFDAutopilotPanel2.transform.localPosition = new Vector3(pos.x, pos2.y + dropdownHeight, pos2.z);
        MFDWeaponsPanel2.transform.localPosition = new Vector3(pos.x, pos2.y + dropdownHeight, pos2.z);
        MFDDockingPanel2.transform.localPosition = new Vector3(pos.x, pos2.y + dropdownHeight, pos2.z);

        MFDControlPanelController = new MFDControlController();
        MFDControlPanelController.Connect(MFDControlPanel, inputController);
        MFDAutopilotPanelController = new MFDAutopilotController();
        MFDAutopilotPanelController.Connect(MFDAutopilotPanel, inputController, gameController);
        MFDWeaponsPanelController = new MFDWeaponsController();
        MFDWeaponsPanelController.Connect(MFDWeaponsPanel, inputController);
        MFDDockingPanelController = new MFDDockingController();
        MFDDockingPanelController.Connect(MFDDockingPanel, inputController, gameController);
        MFDControlPanelController2 = new MFDControlController();
        MFDControlPanelController2.Connect(MFDControlPanel2, inputController);
        MFDAutopilotPanelController2 = new MFDAutopilotController();
        MFDAutopilotPanelController2.Connect(MFDAutopilotPanel2, inputController, gameController);
        MFDWeaponsPanelController2 = new MFDWeaponsController();
        MFDWeaponsPanelController2.Connect(MFDWeaponsPanel2, inputController);
        MFDDockingPanelController2 = new MFDDockingController();
        MFDDockingPanelController2.Connect(MFDDockingPanel2, inputController, gameController);

        MFDDropdown.onValueChanged.AddListener(delegate { MFDDropdownOnValueChanged(); });
        MFDDropdown.value = Convert.ToInt32(MFDPanelType.CONTROL);
        SetMFDPanels(MFDPanelType.CONTROL);

        MFDDropdown2.onValueChanged.AddListener(delegate { MFDDropdownOnValueChanged2(); });
        MFDDropdown2.value = Convert.ToInt32(MFDPanelType.AUTOPILOT);
        SetMFDPanels2(MFDPanelType.AUTOPILOT);
    }

    public class MFDControlController : IPropertyChangeObserver
    {
        private GameObject panel;
        private ToggleButton RotateButton;
        private ToggleButton TranslateButton;
        private ToggleButton GoThrustButton;
        private Slider FuelSlider;
        private Text FuelRemainingText;

        public void Connect(GameObject controlPanel, InputController inputController)
        {
            panel = controlPanel;
            RotateButton = panel.transform.Search("RotateButton").GetComponent<ToggleButton>();
            TranslateButton = panel.transform.Search("TranslateButton").GetComponent<ToggleButton>();
            GoThrustButton = panel.transform.Search("GoThrustButton").GetComponent<ToggleButton>();
            FuelSlider = panel.transform.Search("FuelSlider").GetComponent<Slider>();
            FuelRemainingText = panel.transform.Search("FuelRemainingText").GetComponent<Text>();
            inputController.AddObserver("RotateButton", this);
            inputController.AddObserver("TranslateButton", this);
            inputController.AddObserver("GoThrustButton", this);
            inputController.AddObserver("FuelSlider", this);
            inputController.AddObserver("FuelRemainingText", this);
        }

        public void PropertyChanged(string name, object value)
        {
            switch (name)
            {
                case "RotateButton":
                    bool? rot = value as bool?;
                    RotateButton.isToggled = rot != null ? rot.Value : true;
                    TranslateButton.isToggled = rot != null ? !rot.Value : false;
                    break;
                case "TranslateButton":
                    bool? rot2 = value as bool?;
                    RotateButton.isToggled = rot2 != null ? rot2.Value : false;
                    TranslateButton.isToggled = rot2 != null ? !rot2.Value : true;
                    break;
                case "GoThrustButton":
                    bool? thrust = value as bool?;
                    GoThrustButton.isToggled = thrust != null ? thrust.Value : false;
                    break;
                case "FuelSlider":
                    float? fuel = value as float?;
                    FuelSlider.value = fuel != null ? fuel.Value : 0;
                    break;
                case "FuelRemainingText":
                    FuelRemainingText.text = value as string;
                    break;
            }
        }

    }

    public class MFDAutopilotController : IPropertyChangeObserver
    {
        private InputController inputController;
        private GameController gameController;
        private GameObject panel;
        private Dropdown TargetSelectorDropdown;
        private Dropdown TargetTypeDropdown;
        private Text DistanceText;
        private Text RelvText;
        private Text TimeToTargetText;
        private Dropdown CommandDropdown;
        private Button CommandButton;

        public void Connect(GameObject autoPanel, InputController input, GameController game)
        {
            inputController = input;
            gameController = game;
            panel = autoPanel;

            TargetSelectorDropdown = panel.transform.Search("TargetSelectorDropdown").GetComponent<Dropdown>();
            TargetTypeDropdown = panel.transform.Search("TargetTypeDropdown").GetComponent<Dropdown>();
            DistanceText = panel.transform.Search("DistanceText").GetComponent<Text>();
            RelvText = panel.transform.Search("RelvText").GetComponent<Text>();
            TimeToTargetText = panel.transform.Search("TimeToTargetText").GetComponent<Text>();
            CommandDropdown = panel.transform.Search("CommandDropdown").GetComponent<Dropdown>();
            CommandButton = panel.transform.Search("CommandButton").GetComponent<Button>();

            inputController.AddObserver("TargetList", this);
            inputController.AddObserver("SelectTarget", this);
            inputController.AddObserver("SelectedTargetType", this);
            inputController.AddObserver("DistanceText", this);
            inputController.AddObserver("RelvText", this);
            inputController.AddObserver("TimeToTargetText", this);
            inputController.AddObserver("CommandExecuted", this);

            TargetSelectorDropdown.onValueChanged.AddListener(delegate { TargetSelectorDropdownOnValueChanged(); });
            TargetSelectorDropdown.value = 0;
            CommandButton.onClick.AddListener(delegate { CommandButtonClicked(); });
        }

        public void PropertyChanged(string name, object value)
        {
            switch (name)
            {
                case "TargetList":
                    List<GameObject> targets = value as List<GameObject>;
                    if (targets != null)
                    {
                        List<string> names = new List<string>();
                        names.Add("No Target");
                        foreach (GameObject g in targets)
                        {
                            names.Add(g.name);
                        }
                        TargetSelectorDropdown.ClearOptions();
                        TargetSelectorDropdown.AddOptions(names);
                        //this swith from 1 to 0 is only to refresh the visual DdMenu
                        //TargetSelectorDropdown.value = 0;
                        //TargetSelectorDropdown.value = 1;
                    }
                    break;
                case "SelectTarget":
                    int? tgt = value as int?;
                    int tgtVal = tgt == null ? 0 : tgt.Value + 1;
                    if (TargetSelectorDropdown.value != tgtVal)
                    {
                        TargetSelectorDropdown.value = tgtVal;
                    }
                    break;
                case "SelectedTargetType":
                    int? tType = value as int?;
                    int typeVal = tType == null ? 0 : tType.Value;
                    if (TargetTypeDropdown.value != typeVal)
                    {
                        TargetTypeDropdown.value = typeVal;
                    }
                    break;
                case "DistanceText":
                    DistanceText.text = value as string;
                    break;
                case "RelvText":
                    RelvText.text = value as string;
                    break;
                case "TimeToTargetText":
                    TimeToTargetText.text = value as string;
                    break;
                case "CommandExecuted":
                    Autopilot.Command? commandP = value as Autopilot.Command?;
                    Autopilot.Command command = commandP == null ? Autopilot.Command.OFF : commandP.Value;
                    int commandIdx = Autopilot.Commands.IndexOf(command);
                    if (CommandDropdown.value != commandIdx)
                    {
                        CommandDropdown.value = commandIdx;
                    }
                    break;
            }
        }

        private void TargetSelectorDropdownOnValueChanged()
        {
            int val = TargetSelectorDropdown.value - 1;
            inputController.PropertyChanged("SelectTargetFromDropdown", val);
        }

        private void CommandButtonClicked()
        {
            int index = CommandDropdown.value;
            Autopilot.Command command = Autopilot.CommandFromInt(index);
            gameController.GetPlayerShip().ExecuteAutopilotCommand(command);
        }
    }

    public class MFDWeaponsController : IPropertyChangeObserver
    {
        private GameObject panel;
//        private Text targetText;

        public void Connect(GameObject weaponsPanel, InputController inputController)
        {
            panel = weaponsPanel;
//            targetText = panel.transform.Search("TargetText").GetComponent<Text>();
//            inputController.AddObserver("TargetText", this);
        }

        public void PropertyChanged(string name, object value)
        {
            switch (name)
            {
                case "TargetText":
//                    targetText.text = value as string;
                    break;
            }
        }

    }

    public class MFDDockingController : IPropertyChangeObserver
    {
        private InputController inputController;
        private GameController gameController;
        private GameObject panel;
        private Text ClosingDistText;
        private Text ClosingVText;
        private RectTransform DockingX;

        public void Connect(GameObject autoPanel, InputController input, GameController game)
        {
            inputController = input;
            gameController = game;
            panel = autoPanel;
            ClosingDistText = panel.transform.Search("ClosingDistText").GetComponent<Text>();
            ClosingVText = panel.transform.Search("ClosingVText").GetComponent<Text>();
            DockingX = panel.transform.Search("DockingX").GetComponent<RectTransform>();            
            inputController.AddObserver("ClosingDistText", this);
            inputController.AddObserver("ClosingVText", this);
            inputController.AddObserver("DockingX", this);
            ClosingDistText.text = "INF";
            ClosingVText.text = "INF";
            DockingX.anchoredPosition = Vector2.zero;
        }

        public void PropertyChanged(string name, object value)
        {
            switch (name)
            {
                case "ClosingDistText":
                    ClosingDistText.text = value as string;
                    break;
                case "ClosingVText":
                    ClosingVText.text = value as string;
                    break;
                case "DockingX":
                    Vector2? vec = value as Vector2?;
                    Vector2 planarVec = vec == null ? Vector2.zero : vec.Value;
                    UpdateDockingX(planarVec);
                    break;
            }
        }  

        private void UpdateDockingX(Vector2 planeVec)
        {
            Vector2 planeVecN = planeVec.normalized;
            float r = MapWorldToMFDDockR(planeVec.magnitude);
            float x = r * planeVecN.x;
            float y = r * planeVecN.y;
            DockingX.anchoredPosition = new Vector2(x, y);
        }

        private float innerRadius = 25f;
        private float outerRadius = 100f;
        // 0 - 0 - nan
        // 1 - 25 - 0.1
        // 10 - 50 - 1
        // 100 - 75 - 2
        // 1000 - 100 - 3
        private float MapWorldToMFDDockR(float x)
        {
            float y;
            if (x <= 1)
            {
                y = x * innerRadius;
            }
            else if (x <= 1000)
            {
                y = innerRadius + Mathf.Log10(x) * innerRadius;
            }
            else
            {
                y = outerRadius;
            }
            return y;
        }

    }

    private void MFDDropdownOnValueChanged()
    {
        int val = MFDDropdown.value;
        MFDPanelType panel = MFDPanelTypeFromInt(val);
        SetMFDPanels(panel);
    }

    private void SetMFDPanels(MFDPanelType panel)
    {
        MFDControlPanel.SetActive(panel == MFDPanelType.CONTROL);
        MFDAutopilotPanel.SetActive(panel == MFDPanelType.AUTOPILOT);
        MFDWeaponsPanel.SetActive(panel == MFDPanelType.WEAPONS);
        MFDDockingPanel.SetActive(panel == MFDPanelType.DOCKING);
    }

    private void MFDDropdownOnValueChanged2()
    {
        int val = MFDDropdown2.value;
        MFDPanelType panel = MFDPanelTypeFromInt(val);
        SetMFDPanels2(panel);
    }

    private void SetMFDPanels2(MFDPanelType panel)
    {
        MFDControlPanel2.SetActive(panel == MFDPanelType.CONTROL);
        MFDAutopilotPanel2.SetActive(panel == MFDPanelType.AUTOPILOT);
        MFDWeaponsPanel2.SetActive(panel == MFDPanelType.WEAPONS);
        MFDDockingPanel2.SetActive(panel == MFDPanelType.DOCKING);
    }

    public enum MFDPanelType
    {
        CONTROL,
        AUTOPILOT,
        WEAPONS,
        DOCKING
    }

    public bool IsShowingMFD(MFDPanelType t)
    {
        int mfdCode = Convert.ToInt32(t);
        return (MFDPanel.activeInHierarchy && MFDDropdown.value == mfdCode)
        || (MFDPanel2.activeInHierarchy && MFDDropdown2.value == mfdCode);
    }

    private MFDPanelType MFDPanelTypeFromInt(int val)
    {
        MFDPanelType panel;
        if (Enum.IsDefined(typeof(MFDPanelType), val))
        {
            panel = (MFDPanelType)val;
        }
        else
        {
            panel = MFDPanelType.CONTROL;
        }
        return panel;
    }

    // Update is called once per frame
    void Update()
    {
        switch (gameController.GetGameState())
        {
            case GameController.GameState.RUNNING:                
                UpdateMFD();
                break;
            default:
                HideMFD();
                break;
        }
    }

    public void UpdateMFD()
    {
        if (!MFDPanel.activeInHierarchy)
        {
            MFDPanel.SetActive(true);
        }
        if (!MFDPanel2.activeInHierarchy)
        {
            MFDPanel2.SetActive(true);
        }
    }

    public void HideMFD()
    {
        if (MFDPanel.activeInHierarchy)
        {
            MFDPanel.SetActive(false);
        }
        if (MFDPanel2.activeInHierarchy)
        {
            MFDPanel2.SetActive(false);
        }
    }

}
