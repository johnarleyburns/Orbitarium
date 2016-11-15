using System;
using UnityEngine;
using UnityEngine.UI;

public class MFDController : MonoBehaviour
{
    public Transform FPSCanvas;
    public GameObject MFDPanel;
    public Dropdown MFDDropdown;
    public GameObject MFDControlPanel;
    public GameObject MFDAutopilotPanel;
    public GameObject MFDWeaponPanel;

    private GameController gameController;
    private InputController inputController;

    // Use this for initialization
    void Awake()
    {
        gameController = GetComponent<GameController>();
        inputController = GetComponent<InputController>();

        Instantiate(MFDPanel, FPSCanvas.transform);
        MFDDropdown.onValueChanged.AddListener(delegate { MFDDropdownOnValueChanged(); });
        MFDDropdown.value = Convert.ToInt32(MFDPanelType.CONTROL);
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
        MFDWeaponPanel.SetActive(panel == MFDPanelType.WEAPONS);
    }

    private enum MFDPanelType
    {
        CONTROL,
        AUTOPILOT,
        WEAPONS
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
        if (gameController != null && gameController.GetPlayer() != null)
        {
        }
    }

    public void HideMFD()
    {
        if (MFDPanel.activeInHierarchy)
        {
            MFDPanel.SetActive(false);
        }
    }

}
