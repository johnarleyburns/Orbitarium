using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{

    public ToggleButton RotateButton;
    public ToggleButton TranslateButton;
    public ToggleButton GoThrustButton;
    public Slider FuelSlider;
    public Text FuelRemainingText;
    public ToggleButton POSToggleButton;
    public ToggleButton NEGToggleButton;
    public ToggleButton NMLPOSToggleButton;
    public ToggleButton NMLNEGToggleButton;
    public ToggleButton KILLToggleButton;
    public Text TargetText;
    public Text DistanceText;
    public Text RelvText;
    public ToggleButton TargetToggleButton;
    public GameObject HUDLogic;
    public GameObject RelativeVelocityDirectionIndicator;
    public GameObject RelativeVelocityAntiDirectionIndicator;
    public GameObject RelativeVelocityNormalPlusDirectionIndicator;
    public GameObject RelativeVelocityNormalMinusDirectionIndicator;
    public GameObject TargetDirectionIndicator;
}
