using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{
    public ToggleButton RotateButton;
    public ToggleButton TranslateButton;
    public ToggleButton StopThrustButton;
    public ToggleButton GoThrustButton;
    public Slider FuelSlider;
    public Text FuelRemainingText;
    public ToggleButton DumpFuelButton;
    public ToggleButton POSToggleButton;
    public ToggleButton NEGToggleButton;
    public ToggleButton NMLPOSToggleButton;
    public ToggleButton NMLNEGToggleButton;
    public ToggleButton KILLToggleButton;
    public Text TargetToggleText;
    public ToggleButton TargetToggleButton;
    public GameObject HUDLogic;
    public GameObject RelativeVelocityDirectionIndicator;
    public GameObject RelativeVelocityAntiDirectionIndicator;
    public GameObject RelativeVelocityNormalPlusDirectionIndicator;
    public GameObject RelativeVelocityNormalMinusDirectionIndicator;
    public GameObject TargetDirectionIndicator;
}
