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
    private bool isPrimaryPanel = false;

    public void Connect(GameObject autoPanel, InputController input, GameController game, bool panelIsPrimaryPanel)
    {
        inputController = input;
        gameController = game;
        panel = autoPanel;
        isPrimaryPanel = panelIsPrimaryPanel;

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

    public void Update()
    {
        if (isPrimaryPanel)
        {
            UpdateKeyInput();
        }
    }

    private void UpdateKeyInput()
    {
        UpdateTargetSelection();
    }

    private void UpdateTargetSelection()
    {
        //      UpdateDoubleTap(KeyCode.KeypadMultiply, ref doubleTapTargetSelTimer, gameController.HUD().SelectNextTargetPreferClosestEnemy, RotateTowardsTarget);
        if (Input.GetKeyDown(KeyCode.KeypadMultiply))
        {
            gameController.HUD().SelectNextTargetPreferClosestEnemy();
        }
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
                if (command != Autopilot.Command.OFF && command != Autopilot.Command.KILL_ROTATION && isPrimaryPanel)
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

    /*
  //public float DoubleTapInterval = 0.2f;
//private float doubleTapEngineTimer;
//private float doubleTapRotatePlusTimer;
//private float doubleTapRotateMinusTimer;
//private float doubleTapTargetSelTimer;
//doubleTapRotatePlusTimer = -1;
//doubleTapRotateMinusTimer = -1;
//doubleTapTargetSelTimer = -1;

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

//  private void BurstEngine()
//    {
//      float burnTime = DoubleTapInterval;
//        ship.MainEngineBurst(burnTime);
//    }

public void RotateTowardsTarget()
{
    if (gameController != null)
    {
        autopilot.ExecuteCommand(Autopilot.Command.ACTIVE_TRACK, gameController.HUD().GetSelectedTarget());
        inputController.PropertyChanged("CommandExecuted", Autopilot.Command.ACTIVE_TRACK);
    }
}

public void KillRot()
{
    if (gameController != null)
    {
        autopilot.ExecuteCommand(Autopilot.Command.KILL_ROTATION, null);
        inputController.PropertyChanged("CommandExecuted", Autopilot.Command.KILL_ROTATION);
    }
}

public void KillVTarget()
{
    if (gameController != null)
    {
        autopilot.ExecuteCommand(Autopilot.Command.KILL_REL_V, gameController.HUD().GetSelectedTarget());
        inputController.PropertyChanged("CommandExecuted", Autopilot.Command.KILL_REL_V);
    }
}

private void UpdateRotatePlus()
{
    UpdateDoubleTap(KeyCode.KeypadPlus, ref doubleTapRotatePlusTimer, RotateToPos, StrafeTarget);
    if (Input.GetKeyDown(KeyCode.KeypadPlus))
    {
        RotateToPos();
    }
}

private void RotateToPos()
{
    if (gameController != null)
    {
        autopilot.ExecuteCommand(Autopilot.Command.FACE_TARGET, gameController.GetComponent<InputController>().RelativeVelocityDirectionIndicator);
        inputController.PropertyChanged("CommandExecuted", Autopilot.Command.FACE_TARGET);
    }
}

private void UpdateRotateMinus()
{
    //        UpdateDoubleTap(KeyCode.KeypadMinus, ref doubleTapRotateMinusTimer, RotateToMinus, KillVTarget);
    if (Input.GetKeyDown(KeyCode.KeypadMinus))
    {
        RotateToMinus();
    }
}

private void RotateToMinus()
{
    if (gameController != null)
    {
        autopilot.ExecuteCommand(Autopilot.Command.FACE_TARGET, gameController.GetComponent<InputController>().RelativeVelocityAntiDirectionIndicator);
        inputController.PropertyChanged("CommandExecuted", Autopilot.Command.FACE_TARGET);
    }
}

private void APNGToTarget()
{
    if (gameController != null)
    {
        autopilot.ExecuteCommand(Autopilot.Command.INTERCEPT, gameController.HUD().GetSelectedTarget());
        inputController.PropertyChanged("CommandExecuted", Autopilot.Command.INTERCEPT);
    }

}

private void StrafeTarget()
{
    if (gameController != null)
    {
        autopilot.ExecuteCommand(Autopilot.Command.STRAFE, gameController.HUD().GetSelectedTarget());
        inputController.PropertyChanged("CommandExecuted", Autopilot.Command.STRAFE);
    }

}
*/

}