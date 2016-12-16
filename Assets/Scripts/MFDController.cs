using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Crosstales.RTVoice;

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
    public AudioSource AS;

    public static Color COLOR_GOOD = new Color(14f, 236f, 89f, 218f);
    public static Color COLOR_WARN = new Color(233f, 236f, 19f, 218f);
    public static Color COLOR_BAD = new Color(194f, 45f, 39f, 218f);

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
    private Crosstales.RTVoice.Model.Voice speakerVoice;

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
        MFDControlPanelController.Connect(MFDControlPanel, inputController, gameController);
        MFDAutopilotPanelController = new MFDAutopilotController();
        MFDAutopilotPanelController.Connect(MFDAutopilotPanel, inputController, gameController);
        MFDWeaponsPanelController = new MFDWeaponsController();
        MFDWeaponsPanelController.Connect(MFDWeaponsPanel, inputController);
        MFDDockingPanelController = new MFDDockingController();
        MFDDockingPanelController.Connect(MFDDockingPanel, inputController, gameController);
        MFDControlPanelController2 = new MFDControlController();
        MFDControlPanelController2.Connect(MFDControlPanel2, inputController, gameController);
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

        speakerVoice = Speaker.Voices.Count > 0 ? Speaker.Voices[Speaker.Voices.Count - 1] : null;
    }

    public void Speak(string text)
    {
        if (speakerVoice != null)
        {
            Speaker.Speak(text, AS, speakerVoice, false, 1, 0.4f, null, 3f);
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

    private bool IsShowingMFD1(MFDPanelType t)
    {
        int mfdCode = Convert.ToInt32(t);
        return MFDPanel.activeInHierarchy && MFDDropdown.value == mfdCode;
    }

    private bool IsShowingMFD2(MFDPanelType t)
    {
        int mfdCode = Convert.ToInt32(t);
        return MFDPanel2.activeInHierarchy && MFDDropdown2.value == mfdCode;
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
        if (IsShowingMFD1(MFDPanelType.DOCKING))
        {
            MFDDockingPanelController.Update();
        }
        if (IsShowingMFD2(MFDPanelType.DOCKING))
        {
            MFDDockingPanelController2.Update();
        }
        MFDControlPanelController.Update();
        MFDControlPanelController2.Update();
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
