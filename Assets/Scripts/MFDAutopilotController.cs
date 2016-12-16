using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    public void Speak(string text)
    {
        gameController.GetComponent<MFDController>().Speak(text);
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
                if (command != Autopilot.Command.OFF)
                {
                    string verb;
                    if (command == Autopilot.Command.STRAFE)
                    {
                        verb = DialogText.Strafe;
                    }
                    else
                    {
                        verb = CommandDropdown.options[CommandDropdown.value].text;
                    }
                    string obj = TargetSelectorDropdown.options[TargetSelectorDropdown.value].text;
                    string commandText;
                    if (command == Autopilot.Command.KILL_ROTATION)
                    {
                        commandText = verb;
                    }
                    else
                    {
                        commandText = DialogText.CombineImperative(verb, obj);
                    }
                    Speak(commandText);
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